// Run only against the isolated database created by AuditRegression.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const fixtures = JSON.parse(fs.readFileSync('/tmp/audit-browser-fixtures.json'));
const baseURL = process.env.AUDIT_BASE_URL || 'http://127.0.0.1:5015';
(async () => {
    const browser = await chromium.launch({headless: true, executablePath: process.env.CHROMIUM_EXECUTABLE, args: ['--no-sandbox']});
    const errors = [];
    try {
        for (const role of ['Admin', 'Doctor', 'Patient']) {
            const context = await browser.newContext({baseURL, viewport: {width: 820, height: 1180}});
            const login = await context.request.post('/api/v1/auth/signin', {data: {email: fixtures[role.toLowerCase()], password: 'AuditTest123!'}});
            assert.equal(login.status(), 200);
            const page = await context.newPage();
            page.on('pageerror', error => errors.push(`${role}: ${error.message}`));
            const profilePath = `/${role}/${role === 'Doctor' ? 'Personal' : 'Profile'}`;
            await page.goto(profilePath);
            await page.evaluate(() => window.cleanupShell = true);
            const statusBefore = await page.evaluate(() => JSON.parse(localStorage.user).verificationStatus);
            let saves = 0, changes = 0;
            page.on('request', req => {
                if (req.method() === 'PUT' && req.url().endsWith('/api/v1/user/profile')) saves++;
                if (req.method() === 'PUT' && req.url().endsWith('/api/v1/user/password')) changes++;
            });
            const name = `Kiểm Thử ${role === 'Admin' ? 'Quản Trị' : role === 'Doctor' ? 'Bác Sĩ' : 'Bệnh Nhân'}`;
            await page.locator('#fullName').fill(name);
            await page.locator('#phoneNumber').fill('0912345678');
            await page.locator('#submitBtn').click();
            await page.waitForFunction(() => document.querySelector('#messageBox').textContent.includes('thành công'));
            assert.equal(saves, 1);
            assert.equal(await page.evaluate(() => JSON.parse(localStorage.user).fullName), name);
            assert.equal(await page.evaluate(() => JSON.parse(localStorage.user).verificationStatus), statusBefore);
            async function navigate(url) {
                await page.evaluate(url => {
                    const link = document.createElement('a'); link.href = url; link.id = 'cleanupNav'; link.textContent = 'Navigate';
                    document.querySelector('#workspaceContent').prepend(link);
                }, url);
                await page.locator('#cleanupNav').click();
                await page.waitForURL('**' + url);
                await page.waitForFunction(() => !document.querySelector('#workspaceContent').hasAttribute('aria-busy'));
                assert.equal(await page.evaluate(() => window.cleanupShell), true);
            }
            await navigate(`/${role}/ChangePassword`);
            await page.locator('#oldPassword').fill('AuditTest123!');
            await page.locator('#newPassword').fill('CleanupTest456!');
            await page.locator('#confirmPassword').fill('Different789!');
            await page.locator('#submitBtn').click();
            assert.equal(changes, 0);
            await page.locator('#confirmPassword').fill('CleanupTest456!');
            await page.locator('#submitBtn').click();
            await page.waitForFunction(() => document.querySelector('#messageBox').textContent.includes('thành công'));
            assert.equal(changes, 1);
            // Restore the fixture password through the same public API.
            const restore = await context.request.put('/api/v1/user/password', {data: {oldPassword: 'CleanupTest456!', newPassword: 'AuditTest123!'}});
            assert.equal(restore.status(), 200);
            await navigate(profilePath);
            assert.equal(await page.locator('#fullName').inputValue(), name);
            await page.reload();
            assert.equal(await page.locator('#fullName').inputValue(), name);
            if (role === 'Doctor') assert.equal(await page.evaluate(() => JSON.parse(localStorage.user).verificationStatus), statusBefore);
            await page.evaluate(() => { window.Toastify = undefined; showApiToast('Fallback cleanup', 'error'); });
            await page.locator('#apiToast').waitFor();
            if (role === 'Admin') {
                const all = await (await context.request.get('/api/v1/admin/specialties')).json();
                assert.ok(all.some(item => item.id === fixtures.specialtyId && item.canDelete === false));
                const match = all.find(item => item.id === fixtures.specialtyId);
                const search = await (await context.request.get('/api/v1/admin/specialties?search=' + encodeURIComponent(match.name))).json();
                assert.ok(search.some(item => item.id === match.id));
                const facilities = await (await context.request.get('/api/v1/admin/facilities')).json();
                const publicFacilities = await (await context.request.get('/api/v1/portal/facilities')).json();
                assert.deepEqual([...facilities].sort((a,b) => a.id-b.id), [...publicFacilities].sort((a,b) => a.id-b.id));
                assert.deepEqual(await (await context.request.get('/api/v1/admin/facilities?province=__missing__')).json(), []);
                assert.equal((await context.request.get('/Portal/Booking?scheduleId=1')).status(), 404);
            }
            await context.close();
            console.log(`PASS: ${role} shared profile/password submits once, AJAX lifecycle, fresh reload and toast fallback.`);
        }
        assert.deepEqual(errors, []);
        console.log('PASS: shared catalog projections/filtering and removed dead booking route.');
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exit(1); });
