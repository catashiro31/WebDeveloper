(() => {
    const center = document.getElementById('notificationCenter');
    if (!center) return;
    const bell = document.getElementById('notificationBell');
    const panel = document.getElementById('notificationPanel');
    const badge = document.getElementById('notificationBadge');
    const list = document.getElementById('notificationList');
    const status = document.getElementById('notificationStatus');
    const more = document.getElementById('notificationMore');
    const retry = document.getElementById('notificationRetry');
    const all = document.getElementById('notificationReadAll');
    const header = document.querySelector('#mainHeader .hdr-container, header');
    if (header) {
        const actions = header.classList.contains('role-header') ? header.lastElementChild : header;
        (actions || header).appendChild(center);
    }
    document.body.appendChild(panel); // Avoid clipping by animated/sticky headers.
    let lastId = null, loading = false, stopped = false;
    function close() { panel.hidden = true; bell.setAttribute('aria-expanded', 'false'); }
    async function request(url, options) {
        const response = await fetch(url, { credentials:'same-origin', cache:'no-store', signal:AbortSignal.timeout(15000), ...options });
        if (response.status === 401 || response.status === 403) { stopped = true; close(); center.hidden = true; }
        if (!response.ok) throw new Error('Không thể tải hoặc cập nhật thông báo. Vui lòng thử lại.');
        return response;
    }
    async function load(append = false, countOnly = false) {
        if (loading || stopped) return;
        loading = true;
        try {
            const response = await request('/api/v1/notifications' + (append && lastId ? '?before=' + lastId : ''));
            const data = await response.json();
            badge.textContent = data.unreadCount > 99 ? '99+' : data.unreadCount;
            badge.hidden = !data.unreadCount;
            bell.setAttribute('aria-label', `Thông báo, ${data.unreadCount} chưa đọc`);
            if (countOnly && (panel.hidden || list.children.length > 0)) return;
            if (!append) list.replaceChildren();
            for (const item of data.items) {
                const button = document.createElement('button');
                button.type = 'button'; button.className = 'notification-item' + (item.readAt ? '' : ' unread');
                const title = document.createElement('strong'); title.textContent = item.title;
                const message = document.createElement('span'); message.textContent = item.message;
                const time = document.createElement('small'); time.textContent = new Date(item.createdAt).toLocaleString('vi-VN');
                button.append(title, message, time);
                button.onclick = async () => {
                    button.disabled = true;
                    try {
                        await request(`/api/v1/notifications/${item.id}/read`, { method:'PATCH' });
                        button.classList.remove('unread');
                        const url = new URL(item.url, location.origin);
                        if (url.origin === location.origin && item.url.startsWith('/') && !item.url.startsWith('//')) location.assign(url.href);
                        else await load();
                    } catch (error) { status.textContent = error.message; }
                    finally { button.disabled = false; }
                };
                list.append(button); lastId = item.id;
            }
            status.textContent = list.children.length ? '' : 'Bạn chưa có thông báo.';
            more.hidden = !data.hasMore; retry.hidden = true;
        } catch (error) { if (!countOnly) { status.textContent = error.message; retry.hidden = false; } }
        finally { loading = false; }
    }
    bell.onclick = () => { panel.hidden = !panel.hidden; bell.setAttribute('aria-expanded', String(!panel.hidden)); if (!panel.hidden) load(); };
    document.getElementById('notificationClose').onclick = () => { close(); bell.focus(); };
    document.addEventListener('keydown', e => { if (e.key === 'Escape') close(); });
    document.addEventListener('click', e => { if (!center.contains(e.target) && !panel.contains(e.target)) close(); });
    more.onclick = () => load(true); retry.onclick = () => load();
    all.onclick = async () => {
        all.disabled = true;
        try { await request('/api/v1/notifications/read-all', { method:'PATCH' }); await load(); }
        catch (error) { status.textContent = error.message; }
        finally { all.disabled = false; }
    };
    load(false, true);
    setInterval(() => { if (!document.hidden) load(false, true); }, 30000);
})();
