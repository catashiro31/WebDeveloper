const {chromium}=require('playwright');
const fs=require('fs'); const assert=require('assert/strict'); const failures=[]; const widths=(process.env.AUDIT_WIDTHS || '1440,820,390').split(',').map(Number);const f=JSON.parse(fs.readFileSync('/tmp/audit-browser-fixtures.json'));
(async()=>{
const browser=await chromium.launch({headless:true,executablePath:process.env.CHROMIUM_EXECUTABLE || undefined,args:['--no-sandbox']});
for (const [role,pages] of Object.entries({Admin:['Dashboard','Users','Doctors','Appointments','Reviews','Specialties','Facilities','Appeals','Transfers','Profile','ChangePassword'],Doctor:['Appointments','Schedules','Overdue','Reviews','Profile','Personal','ChangePassword'],Patient:['Appointments','History','Relatives','Profile','ChangePassword']})){
 const context=await browser.newContext({baseURL:process.env.AUDIT_BASE_URL || 'http://127.0.0.1:5011',viewport:{width:1440,height:900}});const page=await context.newPage();
 page.on('pageerror',e=>failures.push('JS '+page.url()+': '+e.message));
 const login=await context.request.post('/api/v1/auth/signin',{data:{email:f[role.toLowerCase()],password:'AuditTest123!'}});if(login.status()!==200)throw Error('login');
 await page.goto('/'+role+'/'+pages[0]);await page.waitForTimeout(500);await page.evaluate(()=>window.__shellMarker=true);
 for(const name of pages.concat(pages.slice(0,2))){
  const url='/'+role+'/'+name;
  if(!page.url().endsWith(url)){
   await page.evaluate(url=>{const a=document.createElement('a');a.id='probeNav';a.href=url;a.textContent='Navigate';document.getElementById('workspaceContent').prepend(a);},url);
   await page.locator('#probeNav').click();await page.waitForURL('**'+url);await page.waitForFunction(()=>!document.querySelector('#workspaceContent').hasAttribute('aria-busy'));await page.waitForTimeout(350);
  }
  if(!await page.evaluate(()=>window.__shellMarker))throw Error('Full reload '+url);
  for(const width of widths){
   await page.setViewportSize({width,height:900});await page.waitForTimeout(50);
   const overflow=await page.evaluate(()=>({width:innerWidth,scroll:document.documentElement.scrollWidth,offenders:[...document.querySelectorAll('main *')].filter(e=>e.getBoundingClientRect().right>innerWidth+5 && getComputedStyle(e).position!=='fixed').slice(0,5).map(e=>({tag:e.tagName,id:e.id,cls:e.className,right:Math.round(e.getBoundingClientRect().right)}))}));
   if(overflow.scroll>width+2)failures.push('OVERFLOW '+url+' '+JSON.stringify(overflow));
  }
  await page.setViewportSize({width:1440,height:900});console.log('VISITED',url);
 }
 await context.close();
}
const context=await browser.newContext({baseURL:process.env.AUDIT_BASE_URL || 'http://127.0.0.1:5011'});const page=await context.newPage();
page.on('pageerror',e=>failures.push('JS '+page.url()+': '+e.message));
for(const url of ['/','/Portal/Doctors','/Portal/Facilities','/Portal/DoctorDetail?id='+f.doctorId,'/Home/About','/Home/Contact','/Auth/Login','/Auth/Register','/Auth/ForgotPassword','/Auth/ResetPassword','/Auth/VerifyAccount','/Auth/ResendVerification','/Auth/Appeal']){
 await page.goto(url);await page.waitForTimeout(250);
 for(const width of widths){await page.setViewportSize({width,height:900});await page.waitForTimeout(40);const sw=await page.evaluate(()=>document.documentElement.scrollWidth);if(sw>width+2)failures.push('OVERFLOW '+url+' '+width+' '+sw);}
 console.log('VISITED',url);
}
await browser.close(); console.log(failures.join('\n')); assert.deepEqual(failures,[]); console.log('PASS: all role/public routes at '+widths.join('/')+'px, repeated AJAX navigation without layout reload or JavaScript errors.');
})().catch(e=>{console.error(e);process.exit(1)});
