// Each role page owns its requests, timers, exported handlers and detached modals.
// Disposing a page prevents delayed responses from updating the next page's DOM.
(() => {
    let active;
    const pending = () => new Promise(() => {});
    function createPage() {
        const controller = new AbortController();
        const timers = new Set(), intervals = new Set(), globals = new Map(), owned = new Set();
        let disposed = false;
        const expose = (name, value) => {
            if (!globals.has(name)) globals.set(name, Object.getOwnPropertyDescriptor(window, name));
            window[name] = value;
        };
        const page = {
            globals: new Proxy(window, { set(_, name, value) { expose(name, value); return true; } }),
            expose(values) { Object.entries(values).forEach(([name, value]) => expose(name, value)); },
            own(node) { if (node) owned.add(node); return node; },
            setTimeout(fn, delay, ...args) {
                const id = window.setTimeout(() => { timers.delete(id); if (!disposed) fn(...args); }, delay);
                timers.add(id); return id;
            },
            setInterval(fn, delay, ...args) {
                const id = window.setInterval(() => { if (!disposed) fn(...args); }, delay);
                intervals.add(id); return id;
            },
            clearTimeout(id) { window.clearTimeout(id); timers.delete(id); },
            clearInterval(id) { window.clearInterval(id); intervals.delete(id); },
            async fetch(url, options = {}) {
                if (disposed) return pending();
                const signal = options.signal ? AbortSignal.any([options.signal, controller.signal]) : controller.signal;
                try {
                    const response = await window.fetch(url, { ...options, signal });
                    if (disposed) return pending();
                    // A page can be removed between receiving headers and reading the response body.
                    for (const method of ['json', 'text', 'blob']) {
                        const read = response[method].bind(response);
                        response[method] = async () => {
                            try { const result = await read(); return disposed ? pending() : result; }
                            catch (error) { if (disposed) return pending(); throw error; }
                        };
                    }
                    return response;
                } catch (error) { if (disposed) return pending(); throw error; }
            },
            async fetchApiList(url) {
                const response = await page.fetch(url, { cache: 'no-store' });
                if (!response.ok) throw new Error(await readApiMessage(response));
                const data = await response.json();
                if (!Array.isArray(data)) throw new Error('Danh sách không hợp lệ.');
                return data;
            },
            dispose() {
                disposed = true; controller.abort();
                timers.forEach(id => window.clearTimeout(id));
                intervals.forEach(id => window.clearInterval(id));
                owned.forEach(node => node.remove());
                globals.forEach((descriptor, name) => {
                    if (descriptor) Object.defineProperty(window, name, descriptor); else delete window[name];
                });
                document.body.style.overflow = '';
            }
        };
        return page;
    }
    window.DocBookingPage = {
        mount(initialize) { active?.dispose(); active = createPage(); initialize(active); },
        dispose() { active?.dispose(); active = null; }
    };

    document.addEventListener('DOMContentLoaded', () => {
        const content = document.getElementById('workspaceContent');
        if (!content) return;
        const role = document.body.dataset.workspace;
        const sidebar = document.getElementById('workspaceSidebar');
        const menu = document.getElementById('workspaceMenu');
        const shade = document.getElementById('workspaceShade');
        const loading = document.getElementById('workspaceLoading');
        const errorBox = document.getElementById('workspaceError');
        let navigation, version = 0;
        function closeMenu() {
            document.body.classList.remove('workspace-menu-open');
            menu.setAttribute('aria-expanded', 'false');
        }
        menu.addEventListener('click', () => {
            const open = document.body.classList.toggle('workspace-menu-open');
            menu.setAttribute('aria-expanded', String(open));
        });
        shade.addEventListener('click', closeMenu);
        document.addEventListener('keydown', event => {
            if (event.key === 'Escape' && document.body.classList.contains('workspace-menu-open')) { closeMenu(); menu.focus(); }
        });
        function updateActive() {
            sidebar.querySelectorAll('a[href]').forEach(link => {
                const selected = new URL(link.href).pathname.toLowerCase() === location.pathname.toLowerCase();
                link.classList.toggle('active', selected);
                if (selected) link.setAttribute('aria-current', 'page'); else link.removeAttribute('aria-current');
            });
        }
        async function navigate(url, pop = false) {
            navigation?.abort(); navigation = new AbortController();
            const current = ++version;
            loading.hidden = false; errorBox.hidden = true;
            content.setAttribute('aria-busy', 'true'); closeMenu();
            try {
                const response = await fetch(url, {
                    headers: { 'X-Workspace-Navigation': '1' }, credentials: 'same-origin', cache: 'no-store',
                    signal: AbortSignal.any([navigation.signal, AbortSignal.timeout(20000)])
                });
                if (current !== version) return;
                if (response.redirected || response.status === 401 || response.status === 403) {
                    location.assign(response.redirected ? response.url : url); return;
                }
                if (!response.ok) throw new Error('Không tải được trang.');
                const html = new DOMParser().parseFromString(await response.text(), 'text/html');
                if (current !== version) return;
                const fragment = html.querySelector('template[data-workspace-fragment]');
                if (!fragment || fragment.dataset.workspaceFragment !== role) { location.assign(url); return; }
                DocBookingPage.dispose();
                content.replaceChildren(fragment.content.cloneNode(true));
                document.querySelectorAll('[data-workspace-page-style]').forEach(node => node.remove());
                html.querySelector('template[data-workspace-styles]')?.content.childNodes.forEach(node => {
                    if (node.nodeType !== Node.ELEMENT_NODE) return;
                    const copy = node.cloneNode(true); copy.setAttribute('data-workspace-page-style', ''); document.head.append(copy);
                });
                if (!pop) history.pushState({}, '', url);
                document.title = fragment.dataset.title;
                updateActive();
                for (const original of html.querySelector('template[data-workspace-scripts]')?.content.querySelectorAll('script') || []) {
                    const script = document.createElement('script');
                    // Role views use trusted, scoped inline initializers. Never execute scripts from API data.
                    script.textContent = original.textContent;
                    content.append(script); script.remove();
                }
                document.dispatchEvent(new CustomEvent('workspace:loaded', { detail: { root: content } }));
                content.focus({ preventScroll: true }); window.scrollTo(0, 0);
            } catch (error) {
                if (current !== version || navigation.signal.aborted) return;
                errorBox.replaceChildren();
                const message = document.createElement('span'); message.textContent = 'Không thể tải trang. Kiểm tra kết nối và thử lại. ';
                const retry = document.createElement('button'); retry.type = 'button'; retry.textContent = 'Thử lại';
                retry.onclick = () => navigate(url, pop);
                const full = document.createElement('a'); full.href = url; full.textContent = 'Mở trang đầy đủ'; full.dataset.noAjax = '';
                errorBox.append(message, retry, full); errorBox.hidden = false;
            } finally {
                if (current === version) { loading.hidden = true; content.removeAttribute('aria-busy'); }
            }
        }
        document.addEventListener('click', event => {
            const link = event.target.closest('a[href]');
            if (!link || event.defaultPrevented || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey ||
                link.target || link.hasAttribute('download') || link.hasAttribute('data-no-ajax')) return;
            const url = new URL(link.href);
            if (url.origin !== location.origin || !url.pathname.toLowerCase().startsWith('/' + role.toLowerCase() + '/') || url.hash) return;
            event.preventDefault();
            if (url.href === location.href) { closeMenu(); return; }
            navigate(url.href);
        });
        window.addEventListener('popstate', () => navigate(location.href, true));
        updateActive();
    });
})();
