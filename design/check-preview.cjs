const {chromium}=require(process.env.WINDECK_PLAYWRIGHT || 'playwright');
const path=require('path');
const design=__dirname;
const fs=require('fs');
(async()=>{
 const browser=await chromium.launch({executablePath:process.env.WINDECK_BROWSER || (require('fs').existsSync('/usr/bin/chromium')?'/usr/bin/chromium':undefined),headless:true,args:['--no-sandbox']});
 const page=await browser.newPage({viewport:{width:1440,height:1000},deviceScaleFactor:1});
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto(process.env.WINDECK_PREVIEW_URL || 'http://127.0.0.1:8765/preview.html');
 await page.waitForTimeout(80);
 await page.screenshot({path:design+'/windeck-repouso.png'});
 await page.evaluate(()=>state('peek'));await page.waitForTimeout(750);
 await page.mouse.move(1000,400);
 await page.screenshot({path:design+'/windeck-aproximacao.png'});
 await page.evaluate(()=>state('expanded'));await page.waitForTimeout(750);
 for(const id of ['home','apps','windows','tray','media','calendar','controls','settings']){
   await page.evaluate(id=>window.page(id),id);await page.waitForTimeout(100);
   if(!await page.locator('#body').innerText())throw Error('Empty layout: '+id);
   await page.locator('#island').screenshot({path:`${design}/layout-${id}.png`});
 }
 await page.evaluate(()=>{theme('dock');window.page('home');});await page.waitForTimeout(250);
 await page.mouse.move(1400,990);await page.screenshot({path:design+'/windeck-dock.png'});
 await page.evaluate(()=>{theme('fluent');window.page('home');});await page.waitForTimeout(100);
 await page.mouse.move(1400,990);await page.screenshot({path:design+'/windeck-fluent.png'});
 await page.keyboard.press('Escape');await page.waitForTimeout(750);
 if(await page.locator('#island').getAttribute('data-state')!=='rest')throw Error('Escape did not collapse');
 await page.mouse.move(720,35);await page.waitForTimeout(220);
 if(await page.locator('#island').getAttribute('data-state')!=='peek')throw Error('Hover did not peek');
 await page.locator('.peek').click();await page.waitForTimeout(750);
 if(await page.locator('#island').getAttribute('data-state')!=='expanded')throw Error('Click did not expand');
 await page.evaluate(()=>window.page('media'));
 await page.locator('#body .play').click();
 if(await page.locator('#body .play').innerText()!=='►')throw Error('Play/pause preview failed');
 for(const width of [1024,768,390]){
   await page.setViewportSize({width,height:900});await page.evaluate(()=>{state('expanded');window.page('home');});await page.waitForTimeout(750);
   const overflow=await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth+1);
   if(overflow)throw Error('Horizontal overflow at '+width);
 }
 if(errors.length)throw Error(errors.join('\n'));
 fs.writeFileSync(design+'/verification.json',JSON.stringify({status:'passed',layouts:8,themes:2,viewports:[1440,1024,768,390],checks:['hover','click','escape','media toggle','no page errors','no horizontal overflow'],note:'Design preview only; native Windows UI not executed'},null,2));
 console.log('PASS: eight layouts, two themes, hover, click, Escape, media toggle and four viewport sizes');
 await browser.close();
})().catch(e=>{console.error(e);process.exit(1)});
