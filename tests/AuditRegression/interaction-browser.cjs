const {chromium}=require('playwright');
const assert=require('assert/strict'), fs=require('fs');
const f=JSON.parse(fs.readFileSync('/tmp/audit-browser-fixtures.json','utf8'));
const baseURL=process.env.AUDIT_BASE_URL || 'http://127.0.0.1:5011';
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:process.env.CHROMIUM_EXECUTABLE || undefined,args:['--no-sandbox']});
 const errors=[];
 async function session(role,options={}) {
  const context=await browser.newContext({baseURL,...options}); const page=await context.newPage();
  page.on('pageerror',e=>errors.push(e.message));
  const response=await context.request.post('/api/v1/auth/signin',{data:{email:f[role],password:'AuditTest123!'}});
  assert.equal(response.status(),200);
  return {context,page};
 }
 const {context:admin,page}=await session('admin',{viewport:{width:1440,height:900}});
 await page.goto('/Admin/Dashboard');await page.locator('#statsContent').waitFor({state:'visible'});
 await page.evaluate(()=>window.shellIdentity=crypto.randomUUID());const identity=await page.evaluate(()=>window.shellIdentity);
 await page.route('**/Admin/Users',async route=>{await new Promise(r=>setTimeout(r,300));await route.continue();});
 await page.locator('#workspaceSidebar a[href="/Admin/Users"]').click();
 await page.locator('#workspaceLoading').waitFor({state:'visible'});
 await page.waitForURL('**/Admin/Users');await page.waitForFunction(()=>!document.querySelector('#workspaceContent').hasAttribute('aria-busy'));
 await page.locator('#workspaceSidebar a[href="/Admin/Doctors"]').click();await page.waitForURL('**/Admin/Doctors');
 await page.goBack();await page.waitForURL('**/Admin/Users');await page.waitForFunction(()=>!document.querySelector('#workspaceContent').hasAttribute('aria-busy'));
 await page.goForward();await page.waitForURL('**/Admin/Doctors');await page.waitForFunction(()=>!document.querySelector('#workspaceContent').hasAttribute('aria-busy'));
 assert.equal(await page.evaluate(()=>window.shellIdentity),identity);
 console.log('PASS: loading indicator, History Back/Forward and persistent shell.');
 // Server errors preserve the old screen; retry loads the requested fragment.
 let fail=true;
 await page.route('**/Admin/Facilities',async route=>{if(fail){fail=false;await route.fulfill({status:503,body:'Temporary failure'});}else await route.continue();});
 await page.locator('#workspaceSidebar a[href="/Admin/Facilities"]').click();await page.locator('#workspaceError').waitFor({state:'visible'});
 assert.match(page.url(),/\/Admin\/Doctors$/);
 await page.locator('#workspaceError button').click();await page.waitForURL('**/Admin/Facilities');await page.waitForFunction(()=>!document.querySelector('#workspaceContent').hasAttribute('aria-busy'));
 // A slow list from an abandoned page cannot overwrite the destination.
 await page.route('**/api/v1/admin/users?*',async route=>{await new Promise(r=>setTimeout(r,650));try{await route.continue();}catch{}});
 await page.locator('#workspaceSidebar a[href="/Admin/Users"]').click();await page.waitForURL('**/Admin/Users');
 await page.locator('#workspaceSidebar a[href="/Admin/Specialties"]').click();await page.waitForURL('**/Admin/Specialties');
 await page.waitForTimeout(850);assert.equal(await page.locator('#tableBody .btn-delete').count(),1);
 assert.equal(await page.evaluate(()=>window.shellIdentity),identity);
 console.log('PASS: failed fragment retry and abandoned-page request cancellation.');
 // Page handlers still submit and refresh data after AJAX navigation, including mobile dialogs.
 await page.setViewportSize({width:390,height:844});
 await page.locator('button[onclick="openModal()"]', {hasText:'Thêm chuyên khoa'}).click();
 const temporaryName='UX fixture '+Date.now();
 await page.locator('#specName').fill(temporaryName);
 await page.locator('#specDesc').fill('Temporary isolated UI regression record');
 const saved=page.waitForResponse(r=>r.url().endsWith('/api/v1/admin/specialty')&&r.request().method()==='POST');
 await page.locator('button[onclick="saveSpecialty()" ]').click();assert.equal((await saved).status(),200);
 const row=page.locator('#tableBody tr').filter({hasText:temporaryName});await row.waitFor();
 // Deletion now uses an explicit in-page confirmation dialog.
 const deleted=page.waitForResponse(r=>r.url().includes('/api/v1/admin/specialty/')&&r.request().method()==='DELETE');
 await row.locator('.btn-delete').click();await page.locator('#confirmDeleteBtn').click();assert.equal((await deleted).status(),200);await row.waitFor({state:'detached'});
 console.log('PASS: create/save/delete form handlers after AJAX navigation on mobile.');

 // Tablet/phone drawer remains reachable and closes after selecting a destination.
 for(const width of [820,390]){
  await page.setViewportSize({width,height:900});await page.locator('#workspaceMenu').click();
  assert.equal(await page.locator('#workspaceMenu').getAttribute('aria-expanded'),'true');
  await page.locator('#workspaceSidebar a[href="/Admin/Dashboard"]').click();await page.waitForURL('**/Admin/Dashboard');
  assert.equal(await page.locator('#workspaceMenu').getAttribute('aria-expanded'),'false');
  await page.locator('#workspaceMenu').click();await page.keyboard.press('Escape');
  assert.equal(await page.locator('#workspaceMenu').getAttribute('aria-expanded'),'false');
 }
 console.log('PASS: mobile/tablet menu, navigation and Escape.');
 await admin.clearCookies();
 await page.locator('#workspaceMenu').click();await page.locator('#workspaceSidebar a[href="/Admin/Users"]').click();
 await page.waitForURL('**/Auth/Login**');console.log('PASS: expired session navigates to login.');await admin.close();

 for(const touch of [false,true]){
  const {context,page:p}=await session('patient',{viewport:{width:touch?390:1440,height:touch?844:900},isMobile:touch,hasTouch:touch,timezoneId:'America/Los_Angeles'});
  await p.goto('/Portal/DoctorDetail?id='+f.doctorId);
  assert.equal(await p.locator('#bookingWeeks button').count(),5);
  await p.locator('#week4Btn').click();assert.equal(await p.locator('#dayGrid button').count(),3);
  const lastRequest=p.waitForResponse(r=>r.url().includes('/slots?date='+f.lastDate));
  await p.locator('#dayGrid button').last().click();assert.equal((await lastRequest).status(),200);
  await p.locator('#slotGrid .slot-btn').first().waitFor();
  assert.equal(await p.locator('#week4Btn').getAttribute('aria-pressed'),'true');
  assert.equal(await p.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+2),true);
  console.log(`PASS: 31 inclusive booking dates, last-day slots, Vietnam dates in foreign timezone (${touch?'touch':'desktop'}).`);
  await p.locator('#notificationBell').click();await p.locator('.notification-item').first().waitFor();
  let data=await (await context.request.get('/api/v1/notifications')).json();
  const item=data.items.find(x=>!x.readAt && x.url.startsWith('/Patient/'));assert.ok(item,'Unread fixture required');
  const button=p.locator(`[data-notification-id="${item.id}"]`);const originalURL=p.url();
  let patches=0;p.on('request',r=>{if(r.method()==='PATCH'&&r.url().endsWith(`/${item.id}/read`))patches++;});
  if(touch)await button.tap();else await button.click();
  await p.waitForFunction(id=>!document.querySelector(`[data-notification-id="${id}"]`).classList.contains('unread'),item.id);
  assert.equal(p.url(),originalURL);
  data=await (await context.request.get('/api/v1/notifications')).json();assert.ok(data.items.find(x=>x.id===item.id).readAt);
  assert.equal(patches,1);
  await p.waitForTimeout(400);
  const target=new URL(item.url,baseURL).href;
  if(touch){await button.tap();await button.tap();}else await button.dblclick();
  await p.waitForURL(target);assert.equal(patches,1,'Read item should not send duplicate PATCH');
  console.log(`PASS: single ${touch?'tap':'click'} marks read without navigation; double opens target; idempotent read.`);
  await context.close();
 }
 assert.deepEqual(errors,[]);await browser.close();console.log('PASS: all interaction checks.');
})().catch(error=>{console.error(error);process.exit(1)});
