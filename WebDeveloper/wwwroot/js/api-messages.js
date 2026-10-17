// Notifications must not prevent a successful save from refreshing the UI when
// an external notification script is unavailable.
function showApiToast(message, type = 'success') {
    const background = type === 'error' ? '#ef4444' : type === 'warning' ? '#f59e0b' : '#10b981';
    if (typeof Toastify === 'function') {
        try {
            Toastify({
                text: message,
                duration: 2500,
                gravity: 'top',
                position: 'center',
                style: {
                    background,
                    borderRadius: '12px',
                    boxShadow: '0 8px 24px rgba(0, 0, 0, 0.18)',
                    fontWeight: '600',
                    fontSize: '14px',
                    padding: '12px 24px'
                }
            }).showToast();
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
    setTimeout(() => notification.remove(), 2500);
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

// ==========================================
// Chuẩn hóa bộ quy tắc Validate toàn hệ thống
// ==========================================
window.Validation = {
    // 1. Họ và tên: 2-60 ký tự, chỉ chữ cái tiếng Việt và khoảng trắng, không số
    validateFullName(name) {
        if (!name || !name.trim()) {
            return { valid: false, message: 'Họ và tên không được để trống.' };
        }
        const trimmed = name.trim();
        if (trimmed.length < 2 || trimmed.length > 60) {
            return { valid: false, message: 'Họ và tên phải có độ dài từ 2 đến 60 ký tự.' };
        }
        if (/\d/.test(trimmed)) {
            return { valid: false, message: 'Họ và tên không được chứa chữ số.' };
        }
        // Bao gồm toàn bộ chữ cái tiếng Việt có dấu hoa/thường và khoảng trắng
        const regex = /^[a-zA-ZàáảãạâầấẩẫậăằắẳẵặèéẻẽẹêềếểễệđìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵÀÁẢÃẠÂẦẤẨẪẬĂẰẮẲẴẶÈÉẺẼẸÊỀẾỂỄỆĐÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴ\s]+$/;
        if (!regex.test(trimmed)) {
            return { valid: false, message: 'Họ và tên chỉ được chứa chữ cái tiếng Việt và khoảng trắng, không chứa ký tự đặc biệt.' };
        }
        return { valid: true, message: '' };
    },

    // 2. Ngày sinh: bắt buộc, không ở tương lai, tuổi <= 100
    validateDateOfBirth(dobStr) {
        if (!dobStr || !dobStr.trim()) {
            return { valid: false, message: 'Ngày sinh không được để trống.' };
        }
        const dob = new Date(dobStr);
        if (isNaN(dob.getTime())) {
            return { valid: false, message: 'Ngày sinh không đúng định dạng.' };
        }
        const today = new Date();
        today.setHours(23, 59, 59, 999);
        if (dob > today) {
            return { valid: false, message: 'Ngày sinh không được ở tương lai.' };
        }
        const age = today.getFullYear() - dob.getFullYear();
        if (age > 100) {
            return { valid: false, message: 'Độ tuổi không được vượt quá 100 tuổi.' };
        }
        return { valid: true, message: '' };
    },

    // 3. Giới tính: bắt buộc chọn (Nam/Nữ/Khác), không để rỗng/mặc định
    validateGender(gender) {
        if (!gender || !gender.toString().trim() || gender === 'NONE' || gender === '') {
            return { valid: false, message: 'Vui lòng chọn giới tính (Nam, Nữ hoặc Khác).' };
        }
        return { valid: true, message: '' };
    },

    // 4. Số điện thoại: đúng đầu số di động VN (03, 05, 07, 08, 09), 10 số, không lặp
    validatePhoneNumber(phone) {
        if (!phone || !phone.trim()) {
            return { valid: false, message: 'Số điện thoại không được để trống.' };
        }
        const trimmed = phone.trim().replace(/[\s\.\-]+/g, '');
        const regex = /^(03|05|07|08|09)\d{8}$/;
        if (!regex.test(trimmed)) {
            return { valid: false, message: 'Số điện thoại phải gồm 10 chữ số và thuộc đầu số di động Việt Nam (03, 05, 07, 08, 09).' };
        }
        // Kiểm tra chuỗi lặp như 0000000000 hoặc 0999999999
        if (/^(\d)\1{9}$/.test(trimmed) || /^0[35789](\d)\1{7}$/.test(trimmed)) {
            return { valid: false, message: 'Số điện thoại không hợp lệ (không được là chuỗi số lặp).' };
        }
        return { valid: true, message: '' };
    },

    // 5. Địa chỉ: chưa nhập hiển thị "Chưa cập nhật", không bao giờ hiển thị "null"
    formatAddress(addr) {
        if (!addr || typeof addr !== 'string' || !addr.trim() || addr.trim().toLowerCase() === 'null') {
            return 'Chưa cập nhật';
        }
        return addr.trim();
    },

    // 6. Lý do khám: 10-500 ký tự
    validateReason(reason) {
        if (!reason || !reason.trim()) {
            return { valid: false, message: 'Lý do khám không được để trống.' };
        }
        const len = reason.trim().length;
        if (len < 10) {
            return { valid: false, message: `Lý do khám phải từ 10 đến 500 ký tự (hiện có ${len} ký tự).` };
        }
        if (len > 500) {
            return { valid: false, message: `Lý do khám không được vượt quá 500 ký tự (hiện có ${len} ký tự).` };
        }
        return { valid: true, message: '' };
    },

    // 7. Mật khẩu: tối thiểu 6 ký tự, có chữ hoa, chữ thường, số
    validatePassword(password) {
        if (!password) {
            return { valid: false, message: 'Mật khẩu không được để trống.' };
        }
        if (password.length < 6) {
            return { valid: false, message: 'Mật khẩu phải có tối thiểu 6 ký tự.' };
        }
        if (!/[A-Z]/.test(password)) {
            return { valid: false, message: 'Mật khẩu phải chứa ít nhất 1 chữ cái in hoa (A-Z).' };
        }
        if (!/[a-z]/.test(password)) {
            return { valid: false, message: 'Mật khẩu phải chứa ít nhất 1 chữ cái viết thường (a-z).' };
        }
        if (!/\d/.test(password)) {
            return { valid: false, message: 'Mật khẩu phải chứa ít nhất 1 chữ số (0-9).' };
        }
        return { valid: true, message: '' };
    }
};

