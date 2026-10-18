// Regressions for frontend failures reproduced on master (isolated fixture DB).
const {chromium} = require('playwright');
const assert = require('node:assert/strict'), fs = require('node:fs');
const fixtures = JSON.parse(fs.readFileSync('/tmp/audit-browser-fixtures.json'));
const baseURL = process.env.AUDIT_BASE_URL || 'http://127.0.0.1:5011';
const quoted = `O'Neil "Phòng khám" & <b>văn bản</b>\nDòng thứ hai`;
(async () => {
 const browser = await chromium.launch({headless:true, executablePath:process.env.CHROMIUM_EXECUTABLE, args:['--no-sandbox']});
 const errors = [];
 async function session(role) {
  const context = await browser.newContext({baseURL, viewport:{width:390,height:844}});
  // Functional UI, including toasts, must work without third-party CDNs.
  await context.route('https://**/*', r => r.abort());
  if (role) assert.equal((await context.request.post('/api/v1/auth/signin',{data:{email:fixtures[role],password:'AuditTest123!'}})).status(),200);
  const page = await context.newPage();page.on('pageerror', e => errors.push(e.message));
  return {context,page};
 }
 async function navigate(page, url) {
  await page.evaluate(url => {const a=document.createElement('a');a.href=url;a.id='testNav';a.textContent='Navigate';document.querySelector('#workspaceContent').prepend(a);},url);
  await page.locator('#testNav').click();await page.waitForURL('**'+url);
  await page.waitForFunction(()=>!document.querySelector('#workspaceContent').hasAttribute('aria-busy'));
 }
 for(const role of ['admin','doctor','patient']) {
  const {context,page}=await session(role);
  const route={admin:'/Admin/Specialties',doctor:'/Doctor/Schedules',patient:'/Patient/Relatives'}[role];
  await page.goto(route);
  for(const width of [360,768,1024]) {
   await page.setViewportSize({width,height:844});
   await page.locator('#workspaceMenu').click();assert.equal(await page.locator('#workspaceMenu').getAttribute('aria-expanded'),'true');
   await page.locator('.workspace-menu-close').click();assert.equal(await page.locator('#workspaceMenu').getAttribute('aria-expanded'),'false');
   await page.locator('#workspaceMenu').click();await page.keyboard.press('Escape');assert.equal(await page.locator('#workspaceMenu').getAttribute('aria-expanded'),'false');
  }
  if(role==='admin') {
   await page.setViewportSize({width:390,height:844});
   assert.equal(await page.locator('#deleteSpecModal').isVisible(),false);
   await page.locator('button[onclick="openModal()"]', {hasText:'Thêm chuyên khoa'}).click();await page.locator('#specName').fill(quoted);
   await page.locator('#specModal button[onclick="closeModal()"]', {hasText:'Hủy'}).click();assert.equal(await page.locator('#specModal').isVisible(),false);
   await page.evaluate(()=>showApiToast('Thông báo cục bộ'));await page.getByText('Thông báo cục bộ',{exact:true}).waitFor();
   await navigate(page,'/Admin/Facilities');assert.equal(await page.evaluate(()=>typeof window.setSpecPage),'undefined');
  }
  await context.close();
 }
 console.log('PASS: all role drawers, hidden mobile dialogs, AJAX handler cleanup and local toast assets.');
 const {context:admin,page:a}=await session('admin');
 await admin.route('**/api/v1/admin/reviews?*',r=>r.fulfill({json:{content:[{id:999,comment:quoted,patientName:'Patient',doctorName:'Doctor',rating:5,isVisible:true}],totalPages:1,totalElements:1}}));
 await a.goto('/Admin/Reviews');await a.locator('.btn-hide').click();
 assert.equal(await a.locator('#toggleReviewCommentText').innerText(),`"${quoted}"`);
 await a.locator('#toggleReviewModal button[onclick="closeToggleReviewModal()"]', {hasText:'Quay lại'}).click();
 await admin.route('**/api/v1/admin/facilities*',r=>r.fulfill({json:[{facilityId:999,facilityName:quoted,address:quoted,description:quoted,isVerified:true}]}));
 await navigate(a,'/Admin/Facilities');await a.getByRole('button',{name:'Gỡ bỏ',exact:true}).click();
 assert.ok((await a.locator('#deleteFacilityModal').innerText()).includes(quoted));
 await a.locator('#deleteFacilityModal button', {hasText:'Quay lại'}).click();
 await a.getByRole('button',{name:'Chỉnh sửa',exact:true}).click();assert.equal(await a.locator('#f_name').inputValue(),quoted.replace(/\n/g,''));
 assert.equal(await a.locator('#f_desc').inputValue(),quoted);
 assert.equal(await a.locator('#facilitiesContainer b').count(),0);
 await a.locator('#facilityModal button', {hasText:'Hủy bỏ'}).click();
 let failDoctors = true;
 await admin.route('**/api/v1/admin/doctor-all?*',r=>failDoctors ? r.fulfill({status:500,json:{message:'Test failure'}}) : r.fulfill({json:{content:[{doctorId:999,fullName:quoted,specialtyName:quoted,verificationStatus:'PENDING'}],totalPages:1,totalElements:1}}));
 await navigate(a,'/Admin/Doctors');await a.getByRole('button',{name:'Thử lại',exact:true}).waitFor();
 assert.equal(await a.locator('#loading').isVisible(),false);failDoctors=false;
 await a.getByRole('button',{name:'Thử lại',exact:true}).click();await a.getByRole('button',{name:'Phê duyệt ngay',exact:true}).click();
 assert.equal(await a.locator('#approveDocName').innerText(),`BS. ${quoted}`);
 await a.screenshot({animations:'disabled',path:'/tmp/frontend-doctor-approval-phone.png'});
 await admin.close();
 console.log('PASS: review and facility actions preserve quotes, newlines and HTML-looking text.');
 const {context:doctor,page:d}=await session('doctor');
 await doctor.route('**/api/v1/doctor/appointments/overdue',r=>r.fulfill({status:500,json:{message:'Test failure'}}));
 await d.goto('/Doctor/Overdue');await d.getByText('Không thể tải danh sách lịch quá hạn',{exact:true}).waitFor();
 assert.equal(await d.getByText('Không có lịch hẹn quá hạn',{exact:true}).count(),0);
 await d.evaluate(()=>{const badge=document.getElementById('overdueBadge');badge.innerText='1';badge.style.display='inline-block';});
 await doctor.unroute('**/api/v1/doctor/appointments/overdue');
 await doctor.route('**/api/v1/doctor/appointments/overdue',r=>r.fulfill({json:[]}));
 await d.evaluate(()=>fetchOverdueCount());await d.locator('#overdueBadge').waitFor({state:'hidden'});
 await doctor.close();
 console.log('PASS: failed overdue API displays an error, not a false empty state.');
 const {context:patient,page:p}=await session('patient');
 await patient.route('**/api/v1/patient/relatives',r=>r.fulfill({json:[{patientId:999,fullName:quoted,relationship:quoted,gender:'MALE',dateOfBirth:'2000-01-01'}]}));
 await p.goto('/Patient/Relatives');await p.locator('button[data-relationship]').click();
 assert.ok((await p.locator('#deleteRelativeModal').innerText()).includes(quoted));
 await p.locator('#deleteRelativeModal button', {hasText:'Quay lại'}).click();
 let first=false;
 await patient.route('**/api/v1/patient/history?*',async r=>{
  const old=new URL(r.request().url()).searchParams.get('startDate')==='2020-01-01';if(old){first=true;await new Promise(resolve=>setTimeout(resolve,600));}
  await r.fulfill({json:{content:[{appointmentId:999,doctorName:old?'OLD RESPONSE':'LATEST RESPONSE',dateWorking:'2026-10-18',hasResult:false}]}});
 });
 await navigate(p,'/Patient/History');await p.getByText('BS. LATEST RESPONSE',{exact:true}).waitFor();
 await p.locator('#startDate').fill('2020-01-01');while(!first)await p.waitForTimeout(10);
 await p.locator('#startDate').fill('2021-01-01');await p.waitForTimeout(800);
 assert.equal(await p.getByText('BS. OLD RESPONSE',{exact:true}).count(),0);await p.getByText('BS. LATEST RESPONSE',{exact:true}).waitFor();
 await patient.close();console.log('PASS: relatives with quotes and out-of-order history filters.');
 const {context:publicContext,page:publicPage}=await session();
 await publicContext.route('**/api/v1/portal/doctors?*',r=>r.fulfill({json:{content:[{doctorId:999,doctorName:quoted,photo:'/missing-test-avatar.png',price:100000}],totalPages:1,totalElements:1}}));
 await publicPage.goto('/Portal/Doctors');await publicPage.locator('#doctorsGrid .initials-avatar:not([hidden])').waitFor();
 assert.equal(await publicPage.locator('#doctorsGrid b').count(),0);
 for(const path of ['/Auth/Login','/Auth/Register','/Auth/ForgotPassword']) {
  await publicPage.goto(path);const form=publicPage.locator('form').first();
  for(const width of [360,768]){await publicPage.setViewportSize({width,height:844});const box=await form.boundingBox();assert.ok(box.width>=250&&box.x>=0&&box.x+box.width<=width,JSON.stringify({path,width,box}));}
  await publicPage.setViewportSize({width:360,height:844});await publicPage.screenshot({animations:'disabled',path:'/tmp/frontend-'+path.split('/').pop()+'.png',fullPage:true});
 }
 await publicContext.close();await browser.close();assert.deepEqual(errors,[]);console.log('PASS: broken avatar fallback and no JavaScript errors.');
})().catch(error=>{console.error(error);process.exit(1)});
