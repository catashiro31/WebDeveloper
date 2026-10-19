// Same compact pagination used by Admin/Users, with page-owned callbacks.
function renderAdminPagination(container, current, total, onChange) {
    container.replaceChildren();
    container.classList.add('admin-pagination');
    container.setAttribute('role', 'navigation');
    container.setAttribute('aria-label', 'Phân trang');
    if (total < 1) return;
    const button = (label, page, disabled = false) => {
        const el = document.createElement('button');
        el.type = 'button'; el.textContent = label; el.disabled = disabled;
        if (page === current && /^\d+$/.test(label)) {
            el.classList.add('active'); el.setAttribute('aria-current', 'page');
        }
        el.addEventListener('click', () => onChange(page)); container.append(el);
    };
    button('Trang trước', current - 1, current === 0);
    for (let i = Math.max(0, current - 2); i <= Math.min(total - 1, current + 2); i++) button(String(i + 1), i);
    button('Trang sau', current + 1, current >= total - 1);
}
