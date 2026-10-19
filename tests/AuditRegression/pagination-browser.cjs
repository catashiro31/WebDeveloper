const {chromium}=require('playwright'),assert=require('node:assert/strict'),fs=require('node:fs');
const f=JSON.parse(fs.readFileSync('/tmp/audit-browser-fixtures.json'));
const baseURL=process.env.AUDIT_BASE_URL||'http://127.0.0.1:5013';
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:process.env.CHROMIUM_EXECUTABLE,args:['--no-sandbox']});const errors=[];
 async function session(role){const c=await browser.newContext({baseURL,viewport:{width:1440,height:1000},reducedMotion:'reduce'});await c.route('https://**/*',r=>r.abort());if(role)assert.equal((await c.request.post('/api/v1/auth/signin',{data:{email:f[role],password:'AuditTest123!'}})).status(),200);const p=await c.newPage();p.on('pageerror',e=>errors.push(e.message));return{c,p};}
 const {c,p}=await session('admin');
 const rows=Array.from({length:17},(_,i)=>({doctorId:i+1,fullName:'Doctor '+(i+1),verificationStatus:'APPROVED',id:i+1,patientName:'Patient '+(i+1),doctorName:'Doctor',rating:4,comment:'Review '+(i+1),isVisible:i%2===0,specialtyName:'Specialty '+(i+1),facilityId:i+1,facilityName:'Facility '+(i+1),isVerified:true,canDelete:true}));
 const paged=route=>{const q=new URL(route.request().url()).searchParams;const page=+(q.get('page')||0),size=+(q.get('size')||5);return route.fulfill({json:{content:rows.slice(page*size,(page+1)*size),totalPages:Math.ceil(rows.length/size),totalElements:rows.length}})};
 await c.route('**/api/v1/admin/doctor-all?*',paged);await c.route('**/api/v1/admin/reviews?*',paged);
 await c.route('**/api/v1/admin/specialties?*',r=>r.fulfill({json:rows}));await c.route('**/api/v1/admin/facilities?*',r=>r.fulfill({json:rows}));
 await p.goto('/Admin/Users');await p.evaluate(()=>window.testShell=true);
 for(const [name,container,pageSize] of [['Doctors','#doctorsContainer',3],['Reviews','#tableBody',5],['Specialties','#tableBody',4],['Facilities','#facilitiesContainer',3]]){
  await p.locator(`#workspaceSidebar a[href="/Admin/${name}"]`).click();await p.waitForURL('**/Admin/'+name);
  const nav=p.locator('.admin-pagination');await nav.getByRole('button',{name:'Trang sau',exact:true}).waitFor();
  assert.equal(await nav.locator('[aria-current="page"]').textContent(),'1');assert.equal(await nav.getByRole('button',{name:'Trang trước',exact:true}).isDisabled(),true);
  const first=await p.locator(container).innerText();await nav.getByRole('button',{name:'Trang sau',exact:true}).click();
  await p.waitForFunction(()=>document.querySelector('.admin-pagination [aria-current="page"]')?.textContent==='2');assert.notEqual(await p.locator(container).innerText(),first);
  const lastPage=Math.ceil(rows.length/pageSize);
  while(!(await nav.getByRole('button',{name:'Trang sau',exact:true}).isDisabled())){
   const before=+(await nav.locator('[aria-current="page"]').textContent());await nav.getByRole('button',{name:'Trang sau',exact:true}).click();await p.waitForFunction(before=>+(document.querySelector('.admin-pagination [aria-current="page"]')?.textContent)>before,before);
  }
  assert.equal(+(await nav.locator('[aria-current="page"]').textContent()),lastPage);
  while(+(await nav.locator('[aria-current="page"]').textContent())>2){const before=+(await nav.locator('[aria-current="page"]').textContent());await nav.getByRole('button',{name:'Trang trước',exact:true}).click();await p.waitForFunction(before=>+(document.querySelector('.admin-pagination [aria-current="page"]')?.textContent)<before,before);}
  await nav.getByRole('button',{name:'Trang trước',exact:true}).click();await p.waitForFunction(()=>document.querySelector('.admin-pagination [aria-current="page"]')?.textContent==='1');
  assert.equal(await p.locator(container).innerText(),first);assert.equal(await p.evaluate(()=>window.testShell),true);
  for(const width of [390,820,1440]){await p.setViewportSize({width,height:1000});assert.ok(await p.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+2));}
  await p.setViewportSize({width:1440,height:1000});
  if(name==='Reviews'){const style=await p.locator('.badge-status').first().evaluate(e=>({whiteSpace:getComputedStyle(e).whiteSpace,border:getComputedStyle(e).borderTopWidth,bg:getComputedStyle(e).backgroundColor}));assert.equal(style.whiteSpace,'nowrap');assert.equal(style.border,'0px');assert.equal(style.bg,'rgba(0, 0, 0, 0)');}
  console.log('PASS pagination '+name+' via AJAX');
 }
 await c.close();
 for(const role of ['admin','doctor','patient']){
  const {c,p}=await session(role);await p.goto('/Portal/Doctors');await p.locator('#doctorsGrid .doc-card').first().waitFor();
  assert.equal(await p.locator('#pageSizeSelect').count(),0);
  assert.equal(await p.locator('#mobileResultCount').innerText(),await p.locator('#doctorsCountSummary').innerText());
  for(const width of [768,820,1024]){await p.setViewportSize({width,height:1100});const bounds=await p.locator('#doctorsGrid .doc-card').evaluateAll(es=>es.slice(0,2).map(e=>{const r=e.getBoundingClientRect();return{x:r.x,y:r.y,w:r.width}}));assert.equal(bounds.length,2);assert.ok(Math.abs(bounds[0].y-bounds[1].y)<2&&bounds[1].x>bounds[0].x,JSON.stringify({width,bounds}));
   assert.equal(await p.locator('#sidebar').isVisible(),false);
   await p.locator('#mobileFilterBtn').click();assert.equal(await p.locator('#sidebar').isVisible(),true);await p.locator('#sidebar button[aria-label="Đóng bộ lọc"]').click();
   await p.locator('#mobileToggleBtn').click();await p.locator('#userDdBtn').click();assert.equal(await p.locator('.hdr-dd-header').isVisible(),false);assert.equal(await p.locator('.hdr-user-info').isVisible(),true);await p.locator('#drawerCloseBtn').click();}
  await p.setViewportSize({width:820,height:1100});await p.screenshot({path:'/tmp/portal-doctors-ipad.png',fullPage:true,animations:'disabled'});
  assert.equal(await p.locator('.hdr-nav a[href="/Portal/Doctors"]').getAttribute('aria-current'),'page');
  await p.setViewportSize({width:390,height:844});await p.locator('#mobileToggleBtn').click();await p.screenshot({path:'/tmp/public-menu-'+role+'.png',animations:'disabled'});await p.locator('#drawerCloseBtn').click();
  await c.close();console.log('PASS iPad grid, menu identity and active navigation '+role);
 }
 const {c:home,p:hp}=await session();await hp.goto('/');assert.equal(await hp.getByRole('heading',{name:'Đội ngũ chuyên gia'}).evaluate(e=>getComputedStyle(e.parentElement.parentElement).opacity),'1');const track=hp.locator('#featuredDoctors');await track.locator('a').first().waitFor();assert.equal(await track.locator('a').count(),6);
 const sorted=await(await home.request.get('/api/v1/portal/doctors?sortBy=rating&page=0&size=6')).json();assert.ok(sorted.content.every(d=>d.totalReviews>0));const ratings=sorted.content.map(d=>d.ratingAverage);assert.deepEqual(ratings,[...ratings].sort((a,b)=>b-a));
 for(const [width,visible] of [[1440,3],[820,2],[390,1]]){await hp.setViewportSize({width,height:1000});await track.evaluate(e=>e.scrollLeft=0);await hp.waitForTimeout(100);const actual=await track.evaluate(e=>{const r=e.getBoundingClientRect();return[...e.querySelectorAll('a')].filter(a=>{const b=a.getBoundingClientRect();return b.left>=r.left-2&&b.right<=r.right+2}).length});assert.equal(actual,visible);
  await hp.locator('#featuredNext').click();await hp.waitForFunction(()=>document.querySelector('#featuredDoctors').scrollLeft>0);await hp.waitForFunction(()=>!document.getElementById('featuredPrevious').disabled);await hp.locator('#featuredPrevious').click();await hp.waitForFunction(()=>document.querySelector('#featuredDoctors').scrollLeft<=2);}
 await hp.setViewportSize({width:1440,height:1000});await track.scrollIntoViewIfNeeded();await hp.screenshot({path:'/tmp/featured-desktop.png',animations:'disabled'});await home.close();console.log('PASS ranked featured doctors, cap 6, visible 3/2/1, both arrows.');
 await browser.close();assert.deepEqual(errors,[]);console.log('PASS all requested UI changes.');
})().catch(e=>{console.error(e);process.exit(1)});
