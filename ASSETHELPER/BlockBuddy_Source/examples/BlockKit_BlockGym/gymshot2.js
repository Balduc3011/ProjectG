const { chromium } = require('playwright');
const [actorIdx, zoom, tx, tz, key] = JSON.parse(process.argv[2]); const shots = JSON.parse(process.argv[3]); const out = process.argv[4];
(async () => {
  const b = await chromium.launch({ args: ['--use-gl=swiftshader','--enable-webgl','--ignore-gpu-blocklist'] });
  const p = await b.newPage({ viewport: { width: 640, height: 520 } });
  await p.route('**/three.min.js', r => r.fulfill({ path: 'vendor/three.min.js', contentType: 'application/javascript' }));
  await p.route('https://fonts.**', r => r.abort());
  p.on('pageerror', e => console.log('ERR', e.message));
  await p.goto('file://' + process.cwd() + '/gym_test.html');
  await p.waitForTimeout(1200);
  await p.keyboard.press(key);
  await p.evaluate(([z,tx,tz]) => { window.__view.zoom = z; window.__view.target.set(tx, 0.8, tz); window.__resize(); window.__paused = true; }, [zoom, tx, tz]);
  await p.waitForTimeout(800);
  let n = 0;
  for (const [i, t] of shots) {
    await p.evaluate(([a,i,t]) => window.__jump(a, i, t), [actorIdx, i, t]);
    await p.waitForTimeout(250);
    await p.screenshot({ path: `${out}${n++}.png` });
  }
  await b.close();
})();
