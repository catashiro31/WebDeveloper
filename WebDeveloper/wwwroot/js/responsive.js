(() => {
    function prepare(root = document) {
        for (const table of root.querySelectorAll('main table')) {
            if (table.parentElement.classList.contains('table-scroll')) continue;
            const region = document.createElement('div');
            region.className = 'table-scroll'; region.tabIndex = 0;
            region.setAttribute('role', 'region'); region.setAttribute('aria-label', 'Bảng dữ liệu, cuộn ngang để xem thêm');
            table.before(region); region.append(table);
        }
    }
    document.addEventListener('DOMContentLoaded', () => prepare());
    document.addEventListener('workspace:loaded', () => prepare());
})();
