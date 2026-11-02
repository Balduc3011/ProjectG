const { chromium } = require('playwright'); const fs = require('fs');
(async () => {
  const b = await chromium.launch({ args: ['--use-gl=swiftshader','--enable-webgl','--ignore-gpu-blocklist'] });
  const p = await b.newPage({ viewport: { width: 640, height: 520 } });
  await p.route('**/three.min.js', r => r.fulfill({ path: 'vendor/three.min.js', contentType: 'application/javascript' }));
  await p.route('https://fonts.**', r => r.abort());
  p.on('pageerror', e => console.log('ERR', e.message));
  await p.goto('file://' + process.cwd() + '/gym_test.html');
  await p.waitForTimeout(800);
  const data = await p.evaluate(() => {
    window.__paused = true;
    const out = { models: {}, placements: [], spawns: [] };
    const inv = new THREE.Matrix4(), rel = new THREE.Matrix4(), pos = new THREE.Vector3(), q = new THREE.Quaternion(), sc = new THREE.Vector3(), e = new THREE.Euler();
    const boxesUnder = (root, partRoot) => {   // boxes of partRoot (not inside nested parts), relative to partRoot
      const list = [];
      inv.copy(partRoot.matrixWorld).invert();
      partRoot.traverse(m => {
        if (!m.userData.box) return;
        let o = m.parent, inside = false;
        while (o && o !== partRoot) { if (o.userData.part) inside = true; o = o.parent; }
        if (inside) return;
        rel.multiplyMatrices(inv, m.matrixWorld);
        list.push({ ...m.userData.box, m: rel.toArray() });
      });
      return list;
    };
    for (const g of window.__props) {
      g.traverse(o => { if (o.userData.part) { o.rotation.set(0, 0, 0); } });   // bags etc. at rest
      g.updateMatrixWorld(true);
      g.matrixWorld.decompose(pos, q, sc); e.setFromQuaternion(q, 'YXZ');
      const model = g.userData.model;
      out.placements.push({ model, name: g.name, pos: pos.toArray(), ry: e.y });
      if (out.models[model]) continue;
      const parts = [{ name: '', pos: [0, 0, 0], boxes: boxesUnder(g, g) }];
      g.traverse(o => { if (o.userData.part) parts.push({ name: o.name, pos: g.worldToLocal(o.getWorldPosition(new THREE.Vector3())).toArray(), boxes: boxesUnder(g, o) }); });
      out.models[model] = { parts };
    }
    const roles = ['RingBoxer_1', 'RingBoxer_2', 'BagBoxer_1@HeavyBag_1', 'BagBoxer_2@HeavyBag_3', 'BenchPress', 'DumbbellCurl', 'Treadmill_1', 'Treadmill_2'];
    window.__actors.forEach((a, i) => {
      a.ch.root.updateMatrixWorld(true);
      a.ch.root.matrixWorld.decompose(pos, q, sc); e.setFromQuaternion(q, 'YXZ');
      out.spawns.push({ name: 'Spawn_' + roles[i], pos: pos.toArray(), ry: e.y });
    });
    return out;
  });
  fs.writeFileSync('gym_scene.json', JSON.stringify(data));
  console.log(Object.entries(data.models).map(([k, v]) => k + ':' + v.parts.map(p => (p.name || '*') + p.boxes.length).join('+')).join(' '));
  console.log(data.placements.length, 'placements');
  await b.close();
})();
