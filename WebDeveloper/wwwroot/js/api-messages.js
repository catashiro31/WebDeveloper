// Notifications must not prevent a successful save from refreshing the UI when
// an external notification script is unavailable.
function showApiToast(message, type = 'success') {
    const background = type === 'error' ? '#ef4444' : type === 'warning' ? '#f59e0b' : '#10b981';
    if (typeof Toastify === 'function') {
        try {
            Toastify({ text: message, duration: 5000, style: { background } }).showToast();
            return;
        } catch (_) {}
    }

    document.getElementById('apiToast')?.remove();
    const notification = document.createElement('div');
    notification.id = 'apiToast';
    notification.className = 'api-toast';
    notification.setAttribute('role', type === 'error' ? 'alert' : 'status');
    notification.style.backgroundColor = background;
    notification.textContent = message;
    document.body.appendChild(notification);
    setTimeout(() => notification.remove(), 5000);
}

async function fetchApiList(url) {
    const response = await fetch(url, { cache: 'no-store' });
    if (!response.ok) throw new Error(await readApiMessage(response));
    const data = await response.json();
    if (!Array.isArray(data)) throw new Error('Máy chủ trả về danh sách không hợp lệ.');
    return data;
}

// Read useful API errors without showing a JSON object or proxy HTML in the UI.
async function readApiMessage(response, fallback = 'Yêu cầu không thành công. Vui lòng thử lại.') {
    if (response.status === 413) {
        return 'Tệp tải lên vượt giới hạn máy chủ. Vui lòng chọn tệp nhỏ hơn.';
    }

    let body;
    try {
        body = await response.text();
    } catch (_) {
        return fallback;
    }

    if (!body) return fallback;

    const contentType = response.headers?.get('content-type') || '';
    if (contentType.includes('json') || /^[\s]*[\[{]/.test(body)) {
        try {
            const data = JSON.parse(body);
            if (typeof data === 'string') return data.slice(0, 300) || fallback;
            const validationError = data.errors && Object.values(data.errors)
                .flat().find(value => typeof value === 'string' && value.trim());
            const message = data.detail || data.message || data.Message || validationError || data.title;
            return typeof message === 'string' && message.trim() ? message.slice(0, 300) : fallback;
        } catch (_) {
            return fallback;
        }
    }

    if (contentType.includes('html') || /<[^>]+>/.test(body)) return fallback;
    return body.trim().slice(0, 300) || fallback;
}
