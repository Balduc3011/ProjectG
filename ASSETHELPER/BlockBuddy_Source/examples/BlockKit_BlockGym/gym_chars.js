  // ======================= BlockBuddy characters =======================
  // Same rig and pose format as the Skin Viewer / BlockBuddy Unity export:
  // limbs are [x, y, z, bend] (bend folds the elbow forward / knee backward), plus a root offset
  // x / y / z (in skin pixels) and root rotation rx / ry. Every clip below is a function of time.
  const SKINS = {
    oldman: { src: "data:image/png;base64,__OLDMAN__", slim: true },
    green:  { src: "data:image/png;base64,__GREEN__", slim: false },
    cyan:   { src: "data:image/png;base64,__CYAN__", slim: false },
  };
  const S = Math.sin, C = Math.cos, PI = Math.PI, max0 = v => Math.max(0, v), lerp = (a, b, k) => a + (b - a) * k;
  const smooth = k => k <= 0 ? 0 : k >= 1 ? 1 : k * k * (3 - 2 * k);
  const LIMBS = ["rArm", "lArm", "rLeg", "lLeg"], ALL = ["body", "head", ...LIMBS], ROOT = ["x", "y", "z", "rx", "ry"];
  const zero = () => ({ body: [0,0,0], head: [0,0,0], rArm: [0,0,0,0], lArm: [0,0,0,0], rLeg: [0,0,0,0], lLeg: [0,0,0,0], x: 0, y: 0, z: 0, rx: 0, ry: 0, hit: 0 });
  const pose = o => Object.assign(zero(), o);
  function mix(a, b, k) {
    const o = zero();
    for (const key of ALL) for (let i = 0; i < o[key].length; i++) o[key][i] = lerp(a[key][i], b[key][i], k);
    for (const key of ROOT) o[key] = lerp(a[key], b[key], k);
    o.hit = k < 0.5 ? a.hit : b.hit;
    return o;
  }
  // keyframed clip: [[time, pose], ...] with smooth interpolation, clamped at both ends
  const keyed = keys => t => {
    if (t <= keys[0][0]) return keys[0][1];
    for (let i = 0; i < keys.length - 1; i++)
      if (t < keys[i + 1][0]) return mix(keys[i][1], keys[i + 1][1], smooth((t - keys[i][0]) / (keys[i + 1][0] - keys[i][0])));
    return keys[keys.length - 1][1];
  };
  // crossfade clip between two running clips
  const cross = (a, b, dur) => t => mix(a(t), b(t), smooth(t / dur));
  const hold = (p, dur) => () => p;

  // ---- skinned block geometry (64x64 skin layout) ----
  function skinBox(w, h, d, tu, tv, inflate, split) {
    const x = w / 2 + inflate, y = h / 2 + inflate, z = d / 2 + inflate;
    const faces = [
      [[-x,-y, z],[ x,-y, z],[ x, y, z],[-x, y, z], [tu+d, tv+d, tu+d+w, tv+d+h], 1],
      [[ x,-y,-z],[-x,-y,-z],[-x, y,-z],[ x, y,-z], [tu+2*d+w, tv+d, tu+2*d+2*w, tv+d+h], 1],
      [[-x,-y,-z],[-x,-y, z],[-x, y, z],[-x, y,-z], [tu, tv+d, tu+d, tv+d+h], 1],
      [[ x,-y, z],[ x,-y,-z],[ x, y,-z],[ x, y, z], [tu+d+w, tv+d, tu+2*d+w, tv+d+h], 1],
      [[-x, y, z],[ x, y, z],[ x, y,-z],[-x, y,-z], [tu+d, tv, tu+d+w, tv+d], 0],
      [[-x,-y,-z],[ x,-y,-z],[ x,-y, z],[-x,-y, z], [tu+d+w, tv, tu+d+2*w, tv+d], 0],
    ];
    const pos = [], uv = [], idx = [];
    const quad = (cs, uvs) => {
      const b = pos.length / 3;
      for (let k = 0; k < 4; k++) { pos.push(...cs[k]); uv.push(uvs[k][0] / 64, 1 - uvs[k][1] / 64); }
      idx.push(b, b + 1, b + 2, b, b + 2, b + 3);
    };
    for (const f of faces) {
      const [u0, v0, u1, v1] = f[4];
      if (split && f[5]) {
        const l3 = (A, B, t) => A.map((v, i) => v + (B[i] - v) * t), rows = Math.round(v1 - v0);
        for (let r = 0; r < rows; r++) {
          const ta = 1 - r / rows, tb = 1 - (r + 1) / rows, va = v0 + (v1 - v0) * r / rows, vb = v0 + (v1 - v0) * (r + 1) / rows;
          quad([l3(f[0], f[3], tb), l3(f[1], f[2], tb), l3(f[1], f[2], ta), l3(f[0], f[3], ta)], [[u0, vb], [u1, vb], [u1, va], [u0, va]]);
        }
      } else quad(f.slice(0, 4), [[u0, v1], [u1, v1], [u1, v0], [u0, v0]]);
    }
    const g = new THREE.BufferGeometry();
    g.setAttribute("position", new THREE.Float32BufferAttribute(pos, 3));
    g.setAttribute("uv", new THREE.Float32BufferAttribute(uv, 2));
    g.setIndex(idx); g.computeVertexNormals();
    g.userData.rest = Float32Array.from(pos);
    return g;
  }
  function bendLimb(grp, bend) {
    if (Math.abs(grp.userData.bend - bend) < 1e-5) return;
    grp.userData.bend = bend;
    const a = grp.userData.bendDir * bend, c = C(a), s = S(a), c2 = C(a / 2), s2 = S(a / 2);
    for (const m of grp.userData.meshes) {
      const rest = m.geometry.userData.rest, attr = m.geometry.attributes.position, p = attr.array;
      for (let i = 0; i < rest.length; i += 3) {
        const y = rest[i + 1], z = rest[i + 2];
        const hy = y * c2 - z * s2, hz = y * s2 + z * c2;
        let ey = y, ez = z;
        if (y < 0) { ey = y * c - z * s; ez = y * s + z * c; }
        const w = Math.min(1, Math.abs(y) / 2);
        p[i + 1] = hy + (ey - hy) * w; p[i + 2] = hz + (ez - hz) * w;
      }
      attr.needsUpdate = true; m.geometry.computeVertexNormals();
    }
  }
  const skinTex = {};
  function texFor(key) {
    if (skinTex[key]) return skinTex[key];
    const tex = new THREE.Texture();
    tex.magFilter = tex.minFilter = THREE.NearestFilter; tex.generateMipmaps = false; tex.encoding = THREE.sRGBEncoding;
    const img = new Image(); img.onload = () => { tex.image = img; tex.needsUpdate = true; }; img.src = SKINS[key].src;
    return skinTex[key] = tex;
  }
  function makeCharacter(skinKey) {
    const aw = SKINS[skinKey].slim ? 3 : 4, tex = texFor(skinKey);
    const matIn = new THREE.MeshLambertMaterial({ map: tex }), matOut = new THREE.MeshLambertMaterial({ map: tex, alphaTest: 0.5, side: THREE.DoubleSide });
    const root = new THREE.Group(); root.scale.setScalar(1 / 16);
    const player = new THREE.Group(); player.rotation.order = "YXZ"; root.add(player);
    const parts = {};
    function part(parent, name, pivot, w, h, d, offset, base, over, inflate, bendDir) {
      const grp = new THREE.Group(); grp.position.set(...pivot);
      const inner = new THREE.Mesh(skinBox(w, h, d, base[0], base[1], 0, !!bendDir), matIn);
      const outer = new THREE.Mesh(skinBox(w, h, d, over[0], over[1], inflate, !!bendDir), matOut);
      for (const m of [inner, outer]) { m.position.set(...offset); m.castShadow = true; grp.add(m); }
      grp.userData = { meshes: [inner, outer], bendDir: bendDir || 0, bend: 0 };
      parent.add(grp); parts[name] = grp; return grp;
    }
    const body = part(player, "body", [0, 12, 0], 8, 12, 4, [0, 6, 0], [16, 16], [16, 32], 0.25);
    part(body, "head", [0, 12, 0], 8, 8, 8, [0, 4, 0], [0, 0], [32, 0], 0.5);
    part(body, "rArm", [-(4 + aw / 2), 10, 0], aw, 12, 4, [0, -4, 0], [40, 16], [40, 32], 0.25, -1);
    part(body, "lArm", [4 + aw / 2, 10, 0], aw, 12, 4, [0, -4, 0], [32, 48], [48, 48], 0.25, -1);
    part(player, "rLeg", [-2, 12, 0], 4, 12, 4, [0, -6, 0], [0, 16], [0, 32], 0.25, 1);
    part(player, "lLeg", [2, 12, 0], 4, 12, 4, [0, -6, 0], [16, 48], [0, 48], 0.25, 1);
    const tmp = new THREE.Vector3();
    return {
      root, player, parts,
      apply(p) {
        for (const k of ALL) parts[k].rotation.set(p[k][0], p[k][1], p[k][2]);
        for (const k of LIMBS) bendLimb(parts[k], max0(p[k][3]));
        player.position.set(p.x, p.y, p.z); player.rotation.set(p.rx, p.ry, 0);
      },
      hand(side, out) {   // world position of the fist (elbow sits 4 px below the shoulder pivot)
        const b = parts[side].userData.bend;
        return parts[side].localToWorld(out.set(0, -4 - 5 * C(b), 5 * S(b)));
      },
    };
  }

  // ---- clips shared with the viewer ----
  const idle = t => { const p = zero(), br = C(t * 1.8);
    p.rArm = [S(t * 1.35) * 0.05, 0, -(br * 0.05 + 0.06), 0.14 + br * 0.03]; p.lArm = [-S(t * 1.35) * 0.05, 0, br * 0.05 + 0.06, 0.14 + br * 0.03];
    p.rLeg[3] = 0.04; p.lLeg[3] = 0.04; p.head[1] = S(t * 0.5) * 0.25; p.head[0] = S(t * 0.37) * 0.06; return p; };
  const run = t => { const p = zero(), ph = t * 9, a = S(ph);
    p.rLeg = [a * 0.95 - 0.15, 0, 0, 0.25 + max0(-C(ph)) * 1.7]; p.lLeg = [-a * 0.95 - 0.15, 0, 0, 0.25 + max0(C(ph)) * 1.7];
    p.rArm = [-a * 0.95, 0, -0.1, 1.45]; p.lArm = [a * 0.95, 0, 0.1, 1.45];
    p.body = [0.22, a * 0.12, 0]; p.head = [-0.22, -a * 0.1, 0]; p.y = Math.abs(C(ph)) * 1.4 - 0.4; return p; };
  function guard(t) {
    const p = zero(), bob = S(t * 4.2), sway = S(t * 2.1);
    p.y = -1.1 + bob * 0.35; p.body = [0.1, -0.32 + sway * 0.05, sway * 0.03]; p.head = [0.14, 0.28 - sway * 0.04, 0];
    p.rArm = [-0.76 + bob * 0.04, 0.44, -0.58, 2.04]; p.lArm = [-0.82 - bob * 0.04, -0.51, 0.54, 1.9];
    p.lLeg = [-0.38, 0, -0.05, 0.35 + bob * 0.05]; p.rLeg = [0.12, 0, 0.06, 0.4 + bob * 0.05]; return p;
  }
  function punchK(q) { if (q <= 0 || q >= 1) return 0; if (q < 0.28) return 1 - Math.pow(1 - q / 0.28, 3); if (q < 0.42) return 1; const k = (q - 0.42) / 0.58; return 1 - k * k * (3 - 2 * k); }
  const toward = (arr, target, k) => { for (let i = 0; i < target.length; i++) arr[i] = lerp(arr[i], target[i], k); };
  const jabL = (p, k) => { if (!k) return; toward(p.lArm, [-1.55, 0.05, -0.08, 0.08], k); p.body[1] -= 0.3 * k; p.head[1] += 0.24 * k; p.rArm[0] -= 0.08 * k; p.z += 0.9 * k; p.lLeg[3] += 0.15 * k; };
  const crossR = (p, k) => { if (!k) return; toward(p.rArm, [-1.55, -0.05, 0.1, 0.08], k); p.body[1] += 0.75 * k; p.head[1] -= 0.6 * k; p.body[0] += 0.08 * k; p.rLeg[0] -= 0.1 * k; p.rLeg[1] += 0.3 * k; p.lLeg[3] += 0.2 * k; p.z += 1.2 * k; p.y -= 0.3 * k; };
  const hookL = (p, k) => { if (!k) return; toward(p.lArm, [0.01, -0.71, 1.53, 1.71], k); p.body[1] += 0.6 * k; p.head[1] -= 0.45 * k; p.lLeg[1] += 0.35 * k; p.lLeg[3] += 0.2 * k; p.y -= 0.4 * k; };
  const uppercutR = (p, q) => { if (q <= 0 || q >= 1) return; const dip = q < 0.32 ? S(q / 0.32 * PI) : 0, k = punchK((q - 0.2) / 0.8);
    toward(p.rArm, [-0.06, 0.13, -0.37, 1.78], dip); p.body[0] += 0.2 * dip; p.body[1] += 0.25 * dip; p.y -= 1.2 * dip; p.rLeg[3] += 0.4 * dip; p.lLeg[3] += 0.4 * dip;
    toward(p.rArm, [-0.93, 0.2, -0.21, 1.84], k); p.body[1] += 0.45 * k; p.body[0] -= 0.12 * k; p.head[1] -= 0.35 * k; p.head[0] -= 0.1 * k; p.y += 1.0 * k; p.z += 0.6 * k; p.rLeg[3] -= 0.2 * k; p.lLeg[3] -= 0.2 * k; };
  const hash = n => ((Math.imul(n | 0, 2654435761) >>> 0) / 4294967296);
  // guard with a random punch every 1.3 s; p.hit > 0 on the frames where the fist lands
  const boxing = seed => t => {
    const p = guard(t), cyc = 1.3, i = Math.floor(t / cyc), r = Math.floor(hash(i * 7919 + seed * 131) * 5), q = (t - i * cyc - 0.15) / 0.5;
    if (r === 1) jabL(p, punchK(q)); else if (r === 2) crossR(p, punchK(q)); else if (r === 3) hookL(p, punchK(q)); else if (r === 4) uppercutR(p, q * 0.7);
    p.hit = r > 0 && q > 0.26 && q < 0.4 ? (r === 2 || r === 4 ? 1.6 : 1) : 0;
    return p;
  };

  // ---- bench press clips (character root at the bench, +Z = away from the rack) ----
  const benchArm = (s, k) => [-PI / 2 - 0.5, 0, s * 0.15, 0.1];                      // reaching back to the rack
  const B = {
    side:  pose({ x: 15, z: 4, ry: -PI / 2, rArm: [0, 0, -0.06, 0.15], lArm: [0, 0, 0.06, 0.15], rLeg: [0,0,0,0.05], lLeg: [0,0,0,0.05] }),
    end:   pose({ x: 0, z: 13, ry: 0, rArm: [0, 0, -0.06, 0.15], lArm: [0, 0, 0.06, 0.15], rLeg: [0,0,0,0.05], lLeg: [0,0,0,0.05] }),
    sit:   pose({ x: 0, y: -4, z: 13, body: [0.15, 0, 0], head: [-0.1, 0, 0], rArm: [-0.75, 0, -0.1, 0.9], lArm: [-0.75, 0, 0.1, 0.9], rLeg: [-1.23, 0, -0.12, 1.23], lLeg: [-1.23, 0, 0.12, 1.23] }),
    lie:   pose({ x: 0, y: 10, z: 16, rx: -PI / 2, rArm: [-0.6, 0, -0.5, 1.4], lArm: [-0.6, 0, 0.5, 1.4], rLeg: [1.05, 0, -0.5, 1.2], lLeg: [1.05, 0, 0.5, 1.2] }),
  };
  B.reach = pose({ ...B.lie, rArm: benchArm(-1), lArm: benchArm(1) });
  B.up    = pose({ ...B.lie, rArm: [-PI / 2, 0, -0.12, 0.1], lArm: [-PI / 2, 0, 0.12, 0.1] });
  B.down  = pose({ ...B.lie, rArm: [-0.2, 0, -1.25, 1.55], lArm: [-0.2, 0, 1.25, 1.55] });   // elbows out, forearms vertical
  const benchEnter = keyed([[0, B.side], [0.8, B.end], [1.6, B.sit], [2.6, B.lie], [3.2, B.reach]]);
  const benchUnrack = keyed([[0, B.reach], [0.6, B.up]]);
  const benchPress = t => mix(B.up, B.down, (1 - C(t / 1.6 * 2 * PI)) / 2);
  const benchRerack = keyed([[0, B.up], [0.6, B.reach]]);
  const benchExit = keyed([[0, B.reach], [0.5, B.lie], [1.5, B.sit], [2.3, B.end], [3.1, B.side]]);

  // ---- dumbbell curl clips ----
  const DB = {
    squat: pose({ y: -2.8, z: 2, body: [1.1, 0, 0], head: [-0.75, 0, 0], rArm: [-1.1, 0, -0.12, 0.05], lArm: [-1.1, 0, 0.12, 0.05], rLeg: [-0.9, 0, -0.1, 1.3], lLeg: [-0.9, 0, 0.1, 1.3] }),   // bent over, reaching the floor
    stand: pose({ rArm: [0.05, 0, -0.1, 0.25], lArm: [0.05, 0, 0.1, 0.25], rLeg: [0,0,-0.03,0.05], lLeg: [0,0,0.03,0.05] }),
  };
  const dbEnter = keyed([[0, idle(0)], [0.9, DB.squat]]);
  const dbUp = keyed([[0, DB.squat], [0.9, DB.stand]]);
  const dbCurl = t => { const p = pose(DB.stand), ph = t / 1.4 * 2 * PI, kr = (1 - C(ph)) / 2, kl = (1 + C(ph)) / 2;
    p.rArm[3] = 0.25 + kr * 2.1; p.lArm[3] = 0.25 + kl * 2.1; p.rArm[0] = -0.1 * kr; p.lArm[0] = -0.1 * kl;
    p.body[2] = (kr - kl) * 0.03; p.head[1] = S(t * 0.7) * 0.1; return p; };
  const dbDown = keyed([[0, DB.stand], [0.9, DB.squat]]);
  const dbExit = keyed([[0, DB.squat], [0.9, idle(0)]]);

  // ---- actors: a looping list of steps, blended at each step change ----
  const actors = [];
  function actor(skin, steps, parent, x, y, z, ry, phase = 0) {
    const ch = makeCharacter(skin);
    ch.root.position.set(x, y, z); ch.root.rotation.y = ry; parent.add(ch.root);
    const a = { ch, steps, i: 0, t: phase, prev: null, prevT: 0, blend: 1, pose: idle(0), skinKey: skin };
    // skip ahead so the characters are not all in sync
    while (a.t >= steps[a.i].dur) { a.t -= steps[a.i].dur; a.i = (a.i + 1) % steps.length; }
    actors.push(a); return a;
  }
  function updateActor(a, dt) {
    a.t += dt;
    let step = a.steps[a.i];
    if (a.t >= step.dur) { a.prev = step; a.prevT = step.dur; a.t -= step.dur; a.i = (a.i + 1) % a.steps.length; step = a.steps[a.i]; a.blend = 0; }
    let p = step.anim(a.t);
    if (a.blend < 1 && a.prev) { a.blend = Math.min(1, a.blend + dt / 0.35); p = mix(a.prev.anim(a.prevT + a.t), p, smooth(a.blend)); }
    a.pose = p; a.ch.apply(p);
    return step;
  }
  const seq = (...s) => s;
  const st = (anim, dur, extra) => Object.assign({ anim, dur }, extra);

  // two boxers in the ring
  const boxSteps = seed => seq(st(idle, 2.5), st(cross(idle, guard, 0.6), 0.6), st(boxing(seed), 9), st(cross(guard, idle, 0.6), 0.6));
  actor("green", boxSteps(1), scene, 0, 0.87, 1.15, PI, 0.8);
  actor("cyan", boxSteps(2), scene, 0, 0.87, -1.15, 0, 4.1);
  // two on the heavy bags (bags 0 and 2 of the rack)
  const bagHitters = [actor("oldman", boxSteps(3), scene, 4.2, 0, -7.25, PI, 6.5), actor("green", boxSteps(4), scene, 8.8, 0, -7.25, PI, 2.2)];
  bagHitters[0].bag = bags[0]; bagHitters[1].bag = bags[2];
  // bench press
  const lifter = actor("cyan", seq(st(idle, 2.0), st(benchEnter, 3.2), st(benchUnrack, 0.6, { bar: true }), st(benchPress, 3.2, { bar: true }),
    st(benchRerack, 0.6, { bar: true }), st(benchExit, 3.1), st(idle, 1.5)), scene, -8.5, 0, 4.5, 0, 0);
  // dumbbell curls
  const curler = actor("oldman", seq(st(idle, 2.2), st(dbEnter, 0.9), st(dbUp, 0.9, { db: true }), st(dbCurl, 7, { db: true }),
    st(dbDown, 0.9, { db: true }), st(dbExit, 0.9), st(idle, 1.0)), scene, -9.6, 0, -0.6, 0, 3);
  // two runners on the treadmills
  const runSteps = seq(st(idle, 2.0), st(cross(idle, run, 1.2), 1.2), st(run, 8), st(cross(run, idle, 1.2), 1.2));
  actor("green", runSteps, treadmills[0], 0, 0.36, 0.25, PI, 1.0);
  actor("cyan", runSteps, treadmills[1], 0, 0.36, 0.25, PI, 7.5);

  // ---- props that follow the hands ----
  const hR = new THREE.Vector3(), hL = new THREE.Vector3(), mid = new THREE.Vector3();
  const barRack = benchBar.position.clone();
  // resting spots on the floor = where the hands are at the bottom of the squat
  curler.ch.apply(DB.squat); curler.ch.root.updateMatrixWorld(true);
  const dbRest = [curler.ch.hand("rArm", hR).clone(), curler.ch.hand("lArm", hL).clone()];
  dbRest.forEach(v => v.y = 0.16);
  // model "Dumbbells" = the two loose dumbbells, placed at the curler's spot (its parts rest where the hands reach)
  const cr = curler.ch.root.position;
  defModel("Dumbbells", g => dbRest.forEach((v, i) => part(g, "Dumbbell_" + (i + 1), v.x - cr.x, v.y - cr.y, v.z - cr.z, d => { box(0.3, 0.07, 0.07, P.steel, 0, -0.035, 0, d); box(0.2, 0.2, 0.2, P.blue, -0.2, -0.1, 0, d); box(0.2, 0.2, 0.2, P.blue, 0.2, -0.1, 0, d); })));
  const dbParts = parts(place("Dumbbells", "Dumbbells", cr.x, cr.y, cr.z, 0));
  const dbs = [dbParts.Dumbbell_1, dbParts.Dumbbell_2];
  function updateProps() {
    // barbell: in the rack, or between the lifter's fists
    if (lifter.steps[lifter.i].bar) {
      lifter.ch.root.updateMatrixWorld(true);
      lifter.ch.hand("rArm", hR); lifter.ch.hand("lArm", hL);
      mid.addVectors(hR, hL).multiplyScalar(0.5); benchBar.parent.worldToLocal(mid);
      benchBar.position.lerp(mid, 0.5);
    } else benchBar.position.lerp(barRack, 0.2);
    // dumbbells: on the floor, or in the curler's hands
    const holding = curler.steps[curler.i].db;
    if (holding) { curler.ch.root.updateMatrixWorld(true); curler.ch.hand("rArm", hR); curler.ch.hand("lArm", hL); }
    dbs.forEach((d, i) => { const target = holding ? (i ? hL : hR).clone() : dbRest[i].clone(); d.parent.worldToLocal(target); d.position.lerp(target, holding ? 0.6 : 0.25); d.rotation.y = curler.ch.root.rotation.y; });
  }
  // heavy bags: spring + damping, punched by the hitters
  bags.forEach(b => (b.userData.v = 0));
  function updateBags(dt, t) {
    for (const h of bagHitters) {
      const i = Math.floor(h.t / 1.3);
      if (h.pose.hit && h.lastHit !== i) { h.lastHit = i; h.bag.userData.v += 0.9 * h.pose.hit; }
    }
    bags.forEach((b, i) => {
      b.userData.v -= (b.rotation.x * 9 + b.userData.v * 1.6) * dt;
      b.rotation.x += b.userData.v * dt;
      b.rotation.z = C(t * 0.9 + i) * 0.015;
    });
  }
  function updateCharacters(dt, t) {
    for (const a of actors) updateActor(a, dt);
    updateProps(); updateBags(dt, t);
  }
