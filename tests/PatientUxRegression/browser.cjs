// Browser behavior with mocked APIs: no database, email or uploads are touched.
const { chromium } = require('playwright');
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../../WebDeveloper');
const read = file => fs.readFileSync(path.join(root, file), 'utf8');
(async () => {
 const browser = await chromium.launch({executablePath: process.env.CHROMIUM_EXECUTABLE || '/usr/bin/chromium', args:['--no-sandbox']});
 try {
 const page = await browser.newPage({locale:'en-US'});
 const errors = []; page.on('pageerror', error => errors.push(error.message));
 let signup, booking, mode = 'success', selfPresent = true;
 const register = read('Views/Auth/Register.cshtml').replace(/^@\{[\s\S]*?\}/, '').replaceAll('@@media','@media');
 const detail = read('Views/Portal/DoctorDetail.cshtml');
 const bookingMarkup = detail.slice(detail.indexOf('<div class="week-tabs"'), detail.indexOf('<!-- STICKY BOTTOM'));
 const script = detail.slice(detail.lastIndexOf('<script>') + 8, detail.lastIndexOf('</script>'))
   .replace('@Model.Id','1').replace('@WebDeveloper.Helpers.BookingWindow.MaxAdvanceDays','30');
 await page.route('**/*', async route => {
  const url = new URL(route.request().url());
  if (url.pathname === '/register') return route.fulfill({contentType:'text/html; charset=utf-8', body:register});
  if (url.pathname === '/booking') return route.fulfill({contentType:'text/html; charset=utf-8', body:`<script src="/js/api-messages.js"></script>${bookingMarkup}<div id="bookingConfirmModal"><span id="confirmTimeSlot"></span><span id="confirmPatientName"></span><span id="confirmReasonText"></span><button id="finalConfirmBtn" onclick="submitFinalBooking()">Xác nhận</button></div><script>${script}</script>`});
  if (url.pathname === '/api/v1/auth/signup') {
   signup = route.request().postDataJSON();
   if (mode === 'network') return route.abort();
   return route.fulfill({contentType:'application/json', body:JSON.stringify('Đăng ký thành công! Vui lòng kiểm tra email.')});
  }
  if (url.pathname === '/api/v1/patient/relatives') return route.fulfill({json: [
   {patientId: 22, fullName:'Người thân', relationship:'Mẹ'},
   ...(selfPresent ? [{patientId:11, fullName:'Nguyễn Văn An', relationship:'SELF'}] : [])
  ]});
  if (url.pathname === '/api/v1/patient/appointments') { booking = route.request().postDataJSON(); return route.fulfill({json:'Đặt lịch thành công'}); }
  if (url.pathname.includes('/slots')) return route.fulfill({json:[{scheduleId:7,timeSlot:'09:00',status:'AVAILABLE'}]});
  if (url.pathname === '/js/api-messages.js') return route.fulfill({contentType:'application/javascript', body:read('wwwroot/js/api-messages.js')});
  return route.fulfill({body:''});
 });
 for (const role of ['DOCTOR','PATIENT']) {
  await page.goto('http://localhost/register');
  await page.locator('#email').fill('doctor@example.com');
  await page.locator('#fullName').fill('Nguyễn Văn An');
  await page.locator('#phone').fill('0912345678');
  await page.locator('#password').fill('Test123!'); await page.locator('#confirmPassword').fill('Test123!');
  await page.locator(role === 'DOCTOR' ? '#role-doctor' : '#role-patient').click();
  signup = null; await page.locator('#submitBtn').click();
  await page.waitForFunction(() => document.getElementById('apiToast')?.textContent.includes('Đăng ký thành công'));
  assert.equal(signup.email,'doctor@example.com'); assert.equal(signup.role,role);
 }
 mode = 'network';
 await page.goto('http://localhost/register');
 await page.locator('#submitBtn').click();
 assert.match(await page.locator('#email').evaluate(el => el.validationMessage), /Vui lòng/);
 await page.locator('#email').fill('doctor@example.com');
 assert.equal(await page.locator('#email').evaluate(el => el.validationMessage), '');
 await page.locator('#fullName').fill('Nguyễn Văn An'); await page.locator('#phone').fill('0912345678');
 await page.locator('#password').fill('Test123!'); await page.locator('#confirmPassword').fill('Test123!');
 await page.locator('#submitBtn').click();
 await page.waitForFunction(() => document.getElementById('apiToast')?.textContent.includes('Không thể kết nối'));
 assert.equal(await page.locator('#submitBtn').isEnabled(),true);
 const messages = await page.evaluate(async () => {
  const read = (body, status=400) => readApiMessage(new Response(JSON.stringify(body), {status,headers:{'content-type':'application/json'}}));
  return [await read({title:'One or more validation errors occurred.', errors:{email:['Email không đúng định dạng']}}),
    await read({errors:{$:['The JSON value could not be converted to System.String.']}}),
    await read({error:'Bạn cần đăng nhập để truy cập tài nguyên này.'},401),
    await readApiMessage(new Response('',{status:429})),
    await readApiMessage(new Response('<html>Bad Gateway</html>',{status:502,headers:{'content-type':'text/html'}})),
    vietnameseApiMessage('Failed to fetch')];
 });
 assert.equal(messages[0], 'Email không đúng định dạng');
 for (const message of messages) assert.match(message, /[À-ỹĐđ]/);
 assert.ok(messages.every(message => !/converted|validation errors|Bad Gateway|Failed to fetch/.test(message)));
 await page.evaluate(() => {localStorage.setItem('token','mock'); localStorage.setItem('user',JSON.stringify({role:'PATIENT'}));});
 await page.goto('http://localhost/booking');
 await page.waitForFunction(() => document.getElementById('patientSelect')?.value === '11');
 assert.equal(await page.locator('select#patientSelect').count(),0);
 await page.locator('#dayGrid button').nth(1).click();
 await page.locator('#slotGrid button').first().click();
 await page.locator('#reasonInput').fill('Đau đầu kéo dài nhiều ngày');
 await page.locator('#bookBtn').click();
 assert.match(await page.locator('#confirmPatientName').textContent(), /Nguyễn Văn An.*Bản thân/);
 await page.locator('#finalConfirmBtn').click();
 await page.waitForFunction(() => document.getElementById('apiToast')?.textContent.includes('Đặt lịch thành công'));
 assert.equal(booking.patientId,11); assert.equal(booking.scheduleId,7);
 selfPresent = false; booking = null;
 await page.goto('http://localhost/booking');
 await page.waitForFunction(() => document.getElementById('patientName')?.textContent.includes('Không thể tải'));
 await page.locator('#dayGrid button').nth(1).click();
 await page.locator('#slotGrid button').first().click();
 assert.equal(await page.locator('#bookBtn').isDisabled(), true);
 await page.evaluate(() => window.submitFinalBooking());
 assert.equal(booking,null);
 assert.deepEqual(errors, []);
 console.log('PASS: signup for both roles, network recovery, Vietnamese validation/API errors, self booking and missing-self guard.');
 } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exit(1); });
