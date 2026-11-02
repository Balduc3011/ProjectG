const { chromium } = require('playwright');
const shots = JSON.parse(process.argv[2]);
(async () => {
  const b = await chromium.launch({ args: ['--use-gl=swiftshader','--enable-webgl','--ignore-gpu-blocklist'] });
  const p = await b.newPage({ viewport: { width: 1280, height: 800 } });
  await p.route('**/three.min.js', r => r.fulfill({ path: 'vendor/three.min.js', contentType: 'application/javascript' }));
  await p.route('https://fonts.**', r => r.abort());
  p.on('pageerror', e => console.log('ERR', e.message));
  p.on('console', m => { if (m.type()==='error') console.log('CONSOLE', m.text()); });
  await p.goto('file://' + process.cwd() + '/gym_test.html');
  await p.waitForTimeout(1500);
  for (const [name, key, wait, zoom, tx, tz] of shots) {
    if (key) await p.keyboard.press(key);
    if (zoom) await p.evaluate(([z,tx,tz]) => { window.__view.zoom = z; window.__view.target.set(tx, 1.0, tz); window.__resize(); }, [zoom, tx, tz]);
    await p.waitForTimeout(wait);
    await p.screenshot({ path: name + '.png' });
  }
  await b.close();
})();
