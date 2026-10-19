// Use only the isolated fixtures produced by AuditRegression.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const f = JSON.parse(fs.readFileSync('/tmp/audit-browser-fixtures.json'));
const baseURL = process.env.AUDIT_BASE_URL || 'http://127.0.0.1:5018';
(async () => {
 const browser = await chromium.launch({executablePath:process.env.CHROMIUM_EXECUTABLE, args:['--no-sandbox']});
 const errors = [];
 try {
  async function session(role) {
   const c = await browser.newContext({baseURL,viewport:{width:1440,height:1000},reducedMotion:'reduce'});
   await c.route('https://**/*',r=>r.abort());
   assert.equal((await c.request.post('/api/v1/auth/signin',{data:{email:f[role],password:'AuditTest123!'}})).status(),200);
   const p=await c.newPage();p.on('pageerror',e=>errors.push(e.message)); return {c,p};
  }
  const d=await session('doctor'), a=await session('admin');
  const pending = await (await a.c.request.get('/api/v1/admin/professional-updates?status=PENDING&doctorId='+f.doctorId)).json();
  for(const row of pending.content) assert.equal((await a.c.request.put('/api/v1/admin/professional-updates/'+row.id,{data:{approve:false,reason:'Chuẩn bị lại dữ liệu kiểm thử'}})).status(),200);
  const initial=await (await d.c.request.get('/api/v1/doctor/professional-updates/current')).json();
  if(initial.currentProfile.degree!=='Thạc sĩ') assert.equal((await a.c.request.post('/api/v1/admin/professional-updates/doctors/'+f.doctorId,{multipart:{degree:'Thạc sĩ',reason:'Khôi phục dữ liệu kiểm thử giao diện'}})).status(),200);
  await d.p.goto('/Doctor/ProfessionalUpdates');
  await d.p.waitForFunction(()=>!document.querySelector('#proFields').disabled);
  await d.p.selectOption('#proDegree','Tiến sĩ');
  await d.p.check('#proExisting');
  await d.p.fill('#proReason','Bổ sung bằng cấp để quản trị viên đối chiếu');
  await d.p.click('#proSubmit');
  await d.p.locator('#proMessage').filter({hasText:'Đã gửi yêu cầu'}).waitFor();
  await d.p.waitForFunction(()=>document.querySelector('#proFields').disabled && document.querySelector('#proEligibility').textContent.includes('chờ duyệt'));
  const current = await (await d.c.request.get('/api/v1/doctor/professional-updates/current')).json();
  assert.equal(current.currentProfile.degree,'Thạc sĩ');
  await a.p.goto('/Admin/ProfessionalUpdates');
  await a.p.locator('details.pro-card').first().waitFor();
  await a.p.locator('details.pro-card summary').first().click();
  await a.p.getByRole('button',{name:'Duyệt yêu cầu',exact:true}).first().click();
  await a.p.locator('#proMessage').filter({hasText:'Đã duyệt yêu cầu'}).waitFor();
  assert.equal((await (await d.c.request.get('/api/v1/doctor/professional-updates/current')).json()).currentProfile.degree,'Tiến sĩ');
  await a.p.selectOption('#proDoctor',String(f.doctorId));
  await a.p.waitForFunction(()=>!document.querySelector('#proFields').disabled);
  const editedYear = (await a.p.inputValue('#proYear')) === '2012' ? '2013' : '2012';
  await a.p.fill('#proYear',editedYear);
  await a.p.fill('#proReason','Admin đối chiếu và bổ sung năm hành nghề');
  await a.p.click('#proSubmit');
  await a.p.locator('#proMessage').filter({hasText:'Đã cập nhật'}).waitFor();
  assert.equal((await (await d.c.request.get('/api/v1/doctor/professional-updates/current')).json()).currentProfile.practiceStartYear,Number(editedYear));
  // Legacy doctors may have neither a degree nor a certificate: year-only admin edits still work.
  const choices = await (await a.c.request.get('/api/v1/admin/professional-updates/doctors')).json();
  const legacy = choices.find(row=>row.id!==f.doctorId && row.name==='doctor2');
  assert(legacy);
  await a.p.selectOption('#proDoctor',String(legacy.id));
  await a.p.waitForFunction(()=>!document.querySelector('#proFields').disabled);
  assert.equal(await a.p.inputValue('#proDegree'),'');
  const legacyYear = (await a.p.inputValue('#proYear')) === '2010' ? '2011' : '2010';
  await a.p.fill('#proYear',legacyYear);
  await a.p.fill('#proReason','Bổ sung hồ sơ cũ sau khi đối chiếu giấy tờ');
  await a.p.click('#proSubmit');
  await a.p.waitForFunction(year=>!document.querySelector('#proFields').disabled && document.querySelector('#proCurrent').textContent.includes('Năm bắt đầu: '+year),legacyYear);
  const savedLegacy = await (await a.c.request.get('/api/v1/admin/professional-updates/doctors/'+legacy.id)).json();
  assert.equal(savedLegacy.currentProfile.degree,null);
  assert.equal(savedLegacy.currentProfile.certificateUrl,null);
  // Rejection requires a reason and is visible to the doctor.
  await d.p.reload();
  await d.p.waitForFunction(()=>!document.querySelector('#proFields').disabled);
  await d.p.selectOption('#proDegree','Giáo sư');
  await d.p.check('#proExisting');
  await d.p.fill('#proReason','Yêu cầu kiểm thử luồng từ chối có lý do');
  await d.p.click('#proSubmit');
  await d.p.waitForFunction(()=>document.querySelector('#proEligibility').textContent.includes('chờ duyệt'));
  await a.p.selectOption('#proDoctor',String(f.doctorId));
  await a.p.selectOption('#proStatus','PENDING');
  await a.p.locator('details.pro-card summary').first().click();
  await a.p.getByRole('button',{name:'Từ chối',exact:true}).click();
  await a.p.locator('#proMessage').filter({hasText:'Vui lòng nhập lý do'}).waitFor();
  await a.p.locator('details.pro-card textarea').fill('Minh chứng chưa đủ để xác nhận học vị');
  await a.p.getByRole('button',{name:'Từ chối',exact:true}).click();
  await a.p.locator('#proMessage').filter({hasText:'Đã từ chối'}).waitFor();
  await d.p.reload();
  await d.p.locator('#proHistory details').first().waitFor();
  await d.p.locator('#proHistory details summary').first().click();
  await d.p.getByText('Minh chứng chưa đủ để xác nhận học vị',{exact:false}).waitFor();
  for(const s of [a,d]) {
   for(const width of [820,390]) {
    await s.p.setViewportSize({width,height:1100});
    assert(await s.p.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),'Horizontal overflow at '+width);
   }
   await s.p.setViewportSize({width:1440,height:1000});
   await s.p.evaluate(()=>window.professionalShell='retained');
   const role=s===a?'Admin':'Doctor', other=s===a?'Users':'Personal';
   await s.p.locator('a[href="/'+role+'/'+other+'"]').first().click();
   await s.p.waitForURL('**/'+role+'/'+other);
   await s.p.locator('a[href="/'+role+'/ProfessionalUpdates"]').first().click();
   await s.p.waitForURL('**/'+role+'/ProfessionalUpdates');
   await s.p.locator('#proHistory').filter({hasNotText:'Đang tải'}).waitFor();
   assert.equal(await s.p.evaluate(()=>window.professionalShell),'retained','AJAX replaced shell');
  }
  await a.p.setViewportSize({width:390,height:1000});
  await a.p.selectOption('#proDoctor',String(f.doctorId));
  await a.p.waitForFunction(()=>!document.querySelector('#proFields').disabled);
  await a.p.screenshot({path:'/tmp/professional-admin-mobile.png',fullPage:true});
  const denied=await d.c.request.get('/api/v1/admin/professional-updates');
  assert.equal(denied.status(),403);
  assert.deepEqual(errors,[]);
  console.log('PASS: doctor submits without altering public profile; admin approves and edits legacy data; role protection, AJAX lifecycle, tablet/mobile layouts.');
 } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
