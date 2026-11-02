const fs = require('fs');
const viewer = fs.readFileSync('viewer.src.html', 'utf8'), gym = fs.readFileSync('gym_chars.js', 'utf8');
const vcode = viewer.slice(viewer.indexOf('  const S = Math.sin'), viewer.indexOf('  const animWrap'));
const gcode = gym.slice(gym.indexOf('  const S = Math.sin'), gym.indexOf('  // ---- skinned block geometry'))
            + gym.slice(gym.indexOf('  // ---- clips shared with the viewer'), gym.indexOf('  // ---- actors'));
const V = new Function('const lerp = (a, b, k) => a + (b - a) * k;\n' + vcode + '\nreturn { ANIMS, guard, hookL, uppercutR, punchK };')();
const G = new Function(gcode + '\nreturn { idle, run, guard, hookL, uppercutR, punchK, cross, benchEnter, benchUnrack, benchPress, benchRerack, benchExit, dbEnter, dbUp, dbCurl, dbDown, dbExit };')();
const FPS = 30, TAU = 2 * Math.PI;
const base = { idle: ['Idle', 7.0, true], walk: ['Walk', TAU / 5.5, true], run: ['Run', TAU / 9, true], sneak: ['Sneak', TAU / 3.5, true],
  wave: ['Wave', TAU, true], jump: ['Jump', 1.1, true], attack: ['Attack', 0.6, true], dance: ['Dance', 4 * Math.PI / 3, true], sit: ['Sit', TAU / 0.6, true],
  guard: ['Box_Guard', TAU / 2.1, true], jabL: ['Box_JabLeft', 1.1, true], crossR: ['Box_CrossRight', 1.2, true], combo: ['Box_Combo', 2.8, true] };
const clips = [];
for (const an of V.ANIMS) { const [name, L, loop] = base[an.id]; clips.push({ name, f: an.f, L, loop }); }
const concat = (a, la, b) => t => t < la ? a(t) : b(t - la);     // play a, then b
const extra = [
  ['Box_HookLeft', t => { const p = G.guard(t); G.hookL(p, G.punchK(((t % 1.1) - 0.15) / 0.5)); return p; }, 1.1, true],
  ['Box_UppercutRight', t => { const p = G.guard(t); G.uppercutR(p, ((t % 1.2) - 0.15) / 0.75); return p; }, 1.2, true],
  // bench press: stand beside the bench -> sit -> lie down -> unrack (bar in hand from 3.2 s)
  ['Bench_LieDown', concat(G.benchEnter, 3.2, G.benchUnrack), 3.8, false],
  ['Bench_Press', G.benchPress, 3.2, true],
  // rerack (bar in hand until 0.6 s) -> sit up -> stand beside the bench
  ['Bench_GetUp', concat(G.benchRerack, 0.6, G.benchExit), 3.7, false],
  // dumbbells: bend down, grab (0.9 s), stand up with them
  ['Curl_PickUp', concat(G.dbEnter, 0.9, G.dbUp), 1.8, false],
  ['Curl_Loop', G.dbCurl, 2.8, true],
  ['Curl_PutDown', concat(G.dbDown, 0.9, G.dbExit), 1.8, false],
];
const GYM = new Set(['Box_Guard', 'Box_JabLeft', 'Box_CrossRight', 'Box_Combo', 'Box_HookLeft', 'Box_UppercutRight', 'Bench_LieDown', 'Bench_Press', 'Bench_GetUp', 'Curl_PickUp', 'Curl_Loop', 'Curl_PutDown']);
for (const [name, f, L, loop] of extra) clips.push({ name, f, L, loop });
const flat = p => [...p.body, ...p.head, ...p.rArm, ...p.lArm, ...p.rLeg, ...p.lLeg, p.y, p.z, p.ry, p.x || 0, p.rx || 0];
const out = [];
for (const c of clips) {
  const n = Math.max(1, Math.round(c.L * FPS)), f0 = flat(c.f(0)), fL = flat(c.f(c.L)), frames = [];
  for (let i = 0; i <= n; i++) {
    const t = i / n * c.L, v = flat(c.f(t));
    frames.push(c.loop ? v.map((x, k) => x + (t / c.L) * (f0[k] - fL[k])) : v);
  }
  out.push({ name: c.name, loop: c.loop, fps: FPS, frames, folder: GYM.has(c.name) ? 'Gym' : 'Base' });
}
fs.writeFileSync(process.argv[2], JSON.stringify(out));
console.log(out.map(o => `${o.name}:${o.frames.length}${o.loop ? '' : '*'}`).join(' '));
