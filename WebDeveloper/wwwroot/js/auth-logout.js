// The server owns the session cookies. Leave the page only after it confirms
// that the refresh token was revoked and the cookies were removed.
async function logoutApp() {
    if (logoutApp.pending) return;
    logoutApp.pending = true;

    try {
        const response = await fetch('/api/v1/auth/signout', {
            method: 'POST',
            credentials: 'same-origin',
            cache: 'no-store'
        });
        if (!response.ok) throw new Error('Sign-out request failed');

        try {
            localStorage.removeItem('token');
            localStorage.removeItem('user');
        } catch (_) {
            // Browser storage is optional; the session is already revoked.
        }
        window.location.replace('/Auth/Login');
    } catch (_) {
        logoutApp.pending = false;
        alert('Không thể đăng xuất lúc này. Vui lòng thử lại.');
    }
}

function logout() {
    return logoutApp();
}
