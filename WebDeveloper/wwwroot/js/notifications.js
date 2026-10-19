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
    let lastId = null, loading = false, stopped = false, actionPending = false;
    let pendingLoad = Promise.resolve();
    function close() { panel.hidden = true; bell.setAttribute('aria-expanded', 'false'); }
    async function request(url, options) {
        const response = await fetch(url, { credentials:'same-origin', cache:'no-store', signal:AbortSignal.timeout(15000), ...options });
        if (response.status === 401 || response.status === 403) { stopped = true; close(); center.hidden = true; }
        if (!response.ok) throw new Error('Không thể tải hoặc cập nhật thông báo. Vui lòng thử lại.');
        return response;
    }
    // Tải trang đầu/cursor và cập nhật badge; polling tránh chạy khi tab bị ẩn.
    function load(append = false, countOnly = false) {
        pendingLoad = pendingLoad.catch(() => {}).then(() => performLoad(append, countOnly));
        return pendingLoad;
    }
    async function performLoad(append = false, countOnly = false) {
        if (loading || stopped) return;
        loading = true;
        try {
            const response = await request('/api/v1/notifications' + (append && lastId ? '?before=' + lastId : ''));
            const data = await response.json();
            badge.textContent = data.unreadCount > 99 ? '99+' : data.unreadCount;
            badge.hidden = !data.unreadCount;
            bell.setAttribute('aria-label', `Thông báo, ${data.unreadCount} chưa đọc`);
            if (countOnly && (panel.hidden || list.children.length > 0)) return;
            if (!append) { list.replaceChildren(); lastId = null; }
            for (const item of data.items) {
                const button = document.createElement('button');
                button.type = 'button'; button.className = 'notification-item' + (item.readAt ? '' : ' unread');
                const title = document.createElement('strong'); title.textContent = item.title;
                const message = document.createElement('span'); message.textContent = item.message;
                const time = document.createElement('small'); time.textContent = new Date(item.createdAt).toLocaleString('vi-VN');
                button.append(title, message, time);
                button.dataset.notificationId = item.id;
                button.title = 'Bấm một lần để đánh dấu đã đọc; bấm đúp để mở. Alt+Enter để mở bằng bàn phím.';
                let marking = null, lastTap = 0, navigating = false;
                function markRead() {
                    if (item.readAt) return Promise.resolve();
                    if (marking) return marking;
                    button.setAttribute('aria-busy', 'true');
                    marking = request(`/api/v1/notifications/${item.id}/read`, { method: 'PATCH' })
                        .then(() => {
                            item.readAt = new Date().toISOString();
                            button.classList.remove('unread');
                            return load(false, true);
                        }).finally(() => { marking = null; button.removeAttribute('aria-busy'); });
                    return marking;
                }
                async function openNotification() {
                    if (navigating) return;
                    navigating = true;
                    try {
                        await markRead();
                        if (typeof item.url !== 'string') { navigating = false; return; }
                        const url = new URL(item.url, location.origin);
                        if (url.origin === location.origin && item.url.startsWith('/') && !item.url.startsWith('//')) location.assign(url.href);
                        else navigating = false;
                    } catch (error) { navigating = false; status.textContent = vietnameseApiMessage(error.message); }
                }
                button.onclick = () => markRead().catch(error => { status.textContent = vietnameseApiMessage(error.message); });
                button.ondblclick = event => { event.preventDefault(); openNotification(); };
                button.onpointerup = event => {
                    if (event.pointerType !== 'touch') return;
                    const now = Date.now();
                    if (lastTap && now - lastTap < 350) { lastTap = 0; openNotification(); }
                    else lastTap = now;
                };
                button.onkeydown = event => {
                    if (event.key === 'Enter' && event.altKey) { event.preventDefault(); openNotification(); }
                };
                list.append(button); lastId = item.id;
            }
            status.textContent = list.children.length ? '' : 'Bạn chưa có thông báo.';
            more.hidden = !data.hasMore; retry.hidden = true;
        } catch (error) { if (!countOnly) { status.textContent = vietnameseApiMessage(error.message); retry.hidden = false; } }
        finally { loading = false; }
    }
    bell.onclick = () => { panel.hidden = !panel.hidden; bell.setAttribute('aria-expanded', String(!panel.hidden)); if (!panel.hidden) load(); };
    const removeRead = document.getElementById('notificationDeleteRead');
    const confirmModal = document.getElementById('notificationConfirmModal');
    if (confirmModal) document.body.appendChild(confirmModal);
    const modalIcon = document.getElementById('notifModalIcon');
    const modalTitle = document.getElementById('notifModalTitle');
    const modalDesc = document.getElementById('notifModalDesc');
    const modalCancel = document.getElementById('notifModalCancel');
    const modalConfirm = document.getElementById('notifModalConfirm');
    let pendingDeleteRead = false;

    function openConfirmModal(deleteRead) {
        pendingDeleteRead = deleteRead;
        if (!confirmModal) {
            updateNotifications(deleteRead);
            return;
        }
        if (deleteRead) {
            if (modalIcon) { modalIcon.className = 'notification-modal-icon icon-delete'; modalIcon.textContent = '🗑️'; }
            if (modalTitle) modalTitle.textContent = 'Xóa thông báo đã đọc';
            if (modalDesc) modalDesc.textContent = 'Chỉ xóa các thông báo đã đọc. Các thông báo chưa đọc sẽ được giữ lại.';
            if (modalConfirm) { modalConfirm.className = 'btn-notif-confirm btn-delete'; modalConfirm.textContent = 'Xóa thông báo'; }
        } else {
            if (modalIcon) { modalIcon.className = 'notification-modal-icon icon-read'; modalIcon.textContent = '✓'; }
            if (modalTitle) modalTitle.textContent = 'Đánh dấu tất cả đã đọc';
            if (modalDesc) modalDesc.textContent = 'Bạn có chắc chắn muốn đánh dấu tất cả thông báo là đã đọc?';
            if (modalConfirm) { modalConfirm.className = 'btn-notif-confirm btn-read'; modalConfirm.textContent = 'Đánh dấu đã đọc'; }
        }
        confirmModal.hidden = false;
    }

    function closeConfirmModal() {
        if (confirmModal) confirmModal.hidden = true;
    }

    if (modalCancel) modalCancel.onclick = closeConfirmModal;
    if (confirmModal) {
        confirmModal.onclick = (e) => {
            if (e.target === confirmModal) closeConfirmModal();
        };
    }
    if (modalConfirm) {
        modalConfirm.onclick = () => {
            closeConfirmModal();
            updateNotifications(pendingDeleteRead);
        };
    }

    document.getElementById('notificationClose').onclick = () => { close(); bell.focus(); };
    document.addEventListener('keydown', e => { 
        if (e.key === 'Escape') {
            if (confirmModal && !confirmModal.hidden) closeConfirmModal();
            else close();
        } 
    });
    document.addEventListener('click', e => { 
        if (!center.contains(e.target) && !panel.contains(e.target) && (!confirmModal || !confirmModal.contains(e.target))) close(); 
    });
    more.onclick = () => load(true); retry.onclick = () => load();
    async function updateNotifications(deleteRead) {
        if (actionPending) return;
        actionPending = true; all.disabled = removeRead.disabled = true;
        try {
            await pendingLoad;
            const response = await request('/api/v1/notifications/' + (deleteRead ? 'read' : 'read-all'), { method:deleteRead ? 'DELETE' : 'PATCH' });
            const deleted = deleteRead ? (await response.json()).deleted : 0;
            await load();
            if (deleteRead) status.textContent = `Đã xóa ${deleted} thông báo đã đọc.` + (list.children.length ? '' : ' Không còn thông báo.');
        } catch (error) { status.textContent = vietnameseApiMessage(error.message); }
        finally { actionPending = false; all.disabled = removeRead.disabled = false; }
    }
    all.onclick = () => openConfirmModal(false);
    removeRead.onclick = () => openConfirmModal(true);
    load(false, true);
    setInterval(() => { if (!document.hidden && !actionPending) load(false, true); }, 30000);
})();
