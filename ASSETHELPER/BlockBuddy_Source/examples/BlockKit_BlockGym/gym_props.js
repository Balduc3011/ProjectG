  // ---------- helpers ----------
  const mats = {};
  const mat = (color) => mats[color] || (mats[color] = new THREE.MeshLambertMaterial({ color }));
  function box(w, h, d, color, x, y, z, parent = scene, opts = {}) {
    const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), opts.material || mat(color));
    m.position.set(x, y + h / 2, z);   // y is the bottom
    m.castShadow = opts.shadow !== false; m.receiveShadow = true;
    m.userData.box = { w, h, d, color, tex: opts.tex || null, tile: opts.tile || 1 };
    parent.add(m);
    return m;
  }
  const group = (x = 0, y = 0, z = 0, ry = 0, parent = scene) => { const g = new THREE.Group(); g.position.set(x, y, z); g.rotation.y = ry; parent.add(g); return g; };
  // Every piece of furniture is a reusable MODEL (one FBX). A model can have named PARTS with their own
  // pivot (bags on a rack, barbell on a bench). A PLACEMENT puts an instance of a model into the room.
  const MODELS = {}, props = [], defModel = (name, build) => (MODELS[name] = build);
  function place(model, name, x, y, z, ry, parent = scene) {
    const g = group(x, y, z, ry, parent); g.name = name; g.userData.model = model;
    MODELS[model](g); props.push(g); return g;
  }
  function part(parent, name, x, y, z, build) { const g = group(x, y, z, 0, parent); g.name = name; g.userData.part = true; build(g); return g; }
  const parts = g => { const o = {}; g.traverse(c => { if (c.userData.part) o[c.name] = c; }); return o; };

  // ---------- room ----------
  const W = 24, D = 24, H = 7;
  woodTex.repeat.set(W / 4, D / 4);
  defModel("Floor", g => box(W, 0.5, D, P.woodA, 0, -0.5, 0, g, { material: new THREE.MeshLambertMaterial({ map: woodTex }), tex: "wood", tile: 4 }));
  place("Floor", "Floor", 0, 0, 0, 0);

  const brickWall = brickTex.clone(); brickWall.needsUpdate = true; brickWall.repeat.set(W / 2, H / 2);
  defModel("Wall", g => {
    box(W, H, 0.5, P.brickA, 0, 0, 0, g, { material: new THREE.MeshLambertMaterial({ map: brickWall }), tex: "brick", tile: 2 });
    box(W, 0.35, 0.56, P.trim, 0, 0, 0, g);
  });
  const walls = [];
  function wall(name, nx, nz, x, z, ry) { const g = place("Wall", name, x, 0, z, ry); g.userData.normal = new THREE.Vector3(nx, 0, nz); walls.push(g); return g; }
  wall("Wall_North", 0, -1, 0, -D / 2 - 0.25, 0);
  wall("Wall_West", -1, 0, -W / 2 - 0.25, 0, Math.PI / 2);
  wall("Wall_South", 0, 1, 0, D / 2 + 0.25, 0);
  wall("Wall_East", 1, 0, W / 2 + 0.25, 0, Math.PI / 2);

  // wall decoration (north + west only; they are the ones usually in view)
  const posterModel = (c1, c2) => g => {
    box(1.6, 2.0, 0.08, c1, 0, -1.0, 0, g); box(1.1, 0.9, 0.1, c2, 0, -0.25, 0, g);
    box(0.9, 0.18, 0.1, P.white, 0, -0.65, 0, g); box(0.6, 0.18, 0.1, P.white, 0, -0.9, 0, g);
  };
  defModel("Poster_Red", posterModel(P.red, P.black)); defModel("Poster_Blue", posterModel(P.blue, P.white)); defModel("Poster_Black", posterModel(P.black, P.yellow));
  defModel("Clock", g => { box(1.1, 1.1, 0.08, P.white, 0, -0.55, 0, g); box(0.08, 0.4, 0.1, P.black, 0, 0, 0, g); box(0.3, 0.08, 0.1, P.black, 0.13, 0, 0, g); });
  defModel("Window", g => { box(3.2, 2.2, 0.1, 0xbfe3f0, 0, 0, 0, g); box(3.4, 0.14, 0.14, P.white, 0, -0.07, 0, g); box(3.4, 0.14, 0.14, P.white, 0, 2.13, 0, g); box(0.14, 2.2, 0.14, P.white, -1.63, 0, 0, g); box(0.14, 2.2, 0.14, P.white, 1.63, 0, 0, g); box(0.1, 2.2, 0.12, P.white, 0, 0, 0, g); });
  const deco = [
    place("Poster_Red", "Poster_Red", -4, 4.2, -D / 2 + 0.05, 0),
    place("Poster_Blue", "Poster_Blue", -1.6, 4.2, -D / 2 + 0.05, 0),
    place("Poster_Black", "Poster_Black", -W / 2 + 0.05, 4.2, 6, Math.PI / 2),
    place("Clock", "Clock", 2.5, 5.75, -D / 2 + 0.05, 0),
    place("Window", "Window", -W / 2 + 0.05, 3.4, -3, Math.PI / 2),
  ];
  deco[0].userData.wall = walls[0]; deco[1].userData.wall = walls[0]; deco[3].userData.wall = walls[0]; deco[2].userData.wall = walls[1]; deco[4].userData.wall = walls[1];

  // ---------- boxing ring ----------
  defModel("BoxingRing", g => {
    box(9, 0.75, 9, P.leather, 0, 0, 0, g);                 // apron
    box(8.6, 0.12, 8.6, P.cream, 0, 0.75, 0, g);             // canvas
    box(0.9, 0.9, 1.0, P.dark, 0, 0, 5.0, g); box(0.9, 0.45, 1.0, P.dark, 0, 0, 6.0, g);   // steps on +Z
    const posts = [[-4, -4, P.red], [4, 4, P.red], [-4, 4, P.blue], [4, -4, P.blue]];
    for (const [x, z, c] of posts) { box(0.3, 2.3, 0.3, P.steel, x, 0.75, z, g); box(0.5, 1.3, 0.5, c, x, 1.35, z, g); }
    const ropeC = [P.red, P.white, P.blue];
    [1.35, 1.85, 2.35].forEach((y, i) => {
      box(8, 0.1, 0.1, ropeC[i], 0, y, -4, g); box(8, 0.1, 0.1, ropeC[i], 0, y, 4, g);
      box(0.1, 0.1, 8, ropeC[i], -4, y, 0, g); box(0.1, 0.1, 8, ropeC[i], 4, y, 0, g);
    });
    for (const [x, z] of [[0, -4], [0, 4], [-4, 0], [4, 0]]) box(0.12, 1.25, 0.12, P.white, x, 1.3, z, g);
  });
  place("BoxingRing", "BoxingRing", 0, 0, 0, 0);

  // ---------- heavy bag rack: frame + 3 bags, each bag is a part pivoting at its hook ----------
  defModel("HeavyBagRack", g => {
    box(0.3, 4.0, 0.3, P.steelDark, -3.5, 0, 0, g); box(0.3, 4.0, 0.3, P.steelDark, 3.5, 0, 0, g);
    box(7.3, 0.3, 0.3, P.steelDark, 0, 4.0, 0, g);
    box(0.3, 0.3, 1.6, P.steelDark, -3.5, 0, 0, g); box(0.3, 0.3, 1.6, P.steelDark, 3.5, 0, 0, g);
    [[-2.3, P.red], [0, P.black], [2.3, P.red]].forEach(([x, c], i) => part(g, "HeavyBag_" + (i + 1), x, 4.0, 0, pivot => {
      box(0.1, 0.7, 0.1, P.steel, 0, -0.7, 0, pivot);
      box(1.0, 2.4, 1.0, c, 0, -3.1, 0, pivot);
      box(1.04, 0.3, 1.04, P.dark, 0, -1.0, 0, pivot); box(1.04, 0.3, 1.04, P.dark, 0, -3.1, 0, pivot);
      box(1.06, 0.12, 1.06, P.white, 0, -2.0, 0, pivot);
    }));
  });
  const bagRack = place("HeavyBagRack", "HeavyBagRack", 6.5, 0, -8.5, 0), bagParts = parts(bagRack);
  const bags = [bagParts.HeavyBag_1, bagParts.HeavyBag_2, bagParts.HeavyBag_3];

  // ---------- speed bag: wall platform + swinging bag part ----------
  defModel("SpeedBag", g => {
    box(0.3, 0.3, 1.4, P.steelDark, 0, 3.6, 0.7, g); box(1.6, 0.14, 1.6, P.trim, 0, 3.35, 1.4, g);
    part(g, "SpeedBag_Ball", 0, 3.35, 1.4, b => { box(0.08, 0.3, 0.08, P.steel, 0, -0.3, 0, b); box(0.45, 0.6, 0.45, P.red, 0, -0.9, 0, b); box(0.25, 0.2, 0.25, P.red, 0, -0.3, 0, b); });
  });
  const speedBag = parts(place("SpeedBag", "SpeedBag", -8.5, 0, -D / 2 + 0.3, 0)).SpeedBag_Ball;

  // ---------- treadmill (used twice) ----------
  defModel("Treadmill", g => {
    box(1.1, 0.3, 2.6, P.dark, 0, 0, 0, g);
    box(0.8, 0.06, 2.3, P.black, 0, 0.3, 0.1, g);
    box(0.12, 1.4, 0.12, P.steel, -0.5, 0.3, -1.1, g); box(0.12, 1.4, 0.12, P.steel, 0.5, 0.3, -1.1, g);
    box(1.1, 0.5, 0.25, P.dark, 0, 1.7, -1.1, g);
    box(0.7, 0.3, 0.1, P.teal, 0, 1.8, -1.0, g);
    box(0.1, 0.1, 1.0, P.steel, -0.6, 1.5, -0.6, g); box(0.1, 0.1, 1.0, P.steel, 0.6, 1.5, -0.6, g);
  });
  const treadmills = [place("Treadmill", "Treadmill_1", 9.5, 0, -1.2, Math.PI / 2), place("Treadmill", "Treadmill_2", 9.5, 0, 1.2, Math.PI / 2)];

  defModel("ExerciseBike", g => {
    box(0.2, 0.15, 1.4, P.dark, 0, 0, 0, g); box(1.0, 0.15, 0.2, P.dark, 0, 0, -0.55, g); box(1.0, 0.15, 0.2, P.dark, 0, 0, 0.55, g);
    box(0.2, 1.0, 0.2, P.steel, 0, 0.15, 0.35, g); box(0.5, 0.15, 0.35, P.black, 0, 1.15, 0.35, g);
    box(0.2, 1.3, 0.2, P.steel, 0, 0.15, -0.45, g); box(0.8, 0.12, 0.12, P.black, 0, 1.45, -0.45, g);
    box(0.6, 0.7, 0.3, P.red, 0, 0.15, -0.05, g);
  });
  place("ExerciseBike", "ExerciseBike", 9.5, 0, 4.4, Math.PI / 2);

  // ---------- free-weight area ----------
  defModel("RubberMat", g => box(7, 0.06, 8, P.rubber, 0, 0, 0, g, { shadow: false }));
  place("RubberMat", "RubberMat", -8.3, 0, 3, 0);
  // bench + barbell part (pivot at the centre of the bar, resting in the rack)
  defModel("BenchPress", g => {
    box(0.5, 0.3, 0.2, P.steel, 0, 0, 0.75, g); box(0.5, 0.3, 0.2, P.steel, 0, 0, -0.6, g);
    box(0.6, 0.2, 1.9, P.red, 0, 0.3, 0.05, g);
    box(0.15, 1.3, 0.15, P.steelDark, -0.9, 0, -0.8, g); box(0.15, 1.3, 0.15, P.steelDark, 0.9, 0, -0.8, g);
    box(0.15, 0.08, 0.3, P.steelDark, -0.9, 1.14, -0.68, g); box(0.15, 0.08, 0.3, P.steelDark, 0.9, 1.14, -0.68, g);
    part(g, "Barbell", 0, 1.24, -0.72, b => {
      box(2.4, 0.08, 0.08, P.steel, 0, -0.04, 0, b);
      for (const s of [-1, 1]) { box(0.12, 0.6, 0.6, P.black, s * 1.0, -0.3, 0, b); box(0.1, 0.45, 0.45, P.dark, s * 1.13, -0.225, 0, b); }
    });
  });
  const benchBar = parts(place("BenchPress", "BenchPress", -8.5, 0, 4.5, 0)).Barbell;
  defModel("DumbbellRack", g => {
    box(3.2, 0.5, 0.9, P.steelDark, 0, 0, 0, g); box(3.2, 0.15, 0.6, P.steelDark, 0, 0.9, -0.3, g);
    box(0.15, 0.5, 0.6, P.steelDark, -1.5, 0.4, -0.3, g); box(0.15, 0.5, 0.6, P.steelDark, 1.5, 0.4, -0.3, g);
    const colors = [P.yellow, P.green, P.blue, P.red, P.black];
    for (let tier = 0; tier < 2; tier++) for (let i = 0; i < 5; i++) {
      const x = -1.2 + i * 0.6, y = tier ? 1.05 : 0.5, z = tier ? -0.3 : 0.1, s = 0.16 + i * 0.03 + tier * 0.04;
      const d = group(x, y, z, 0, g);
      box(0.3, 0.07, 0.07, P.steel, 0, s / 2 - 0.035, 0, d);
      box(s, s, s, colors[i], -0.2, 0, 0, d); box(s, s, s, colors[i], 0.2, 0, 0, d);
    }
  });
  place("DumbbellRack", "DumbbellRack", -11.0, 0, 0.8, Math.PI / 2);
  defModel("SquatRack", g => {
    for (const [x, z] of [[-1, -0.6], [1, -0.6], [-1, 0.6], [1, 0.6]]) box(0.15, 2.6, 0.15, P.steelDark, x, 0, z, g);
    box(2.15, 0.15, 0.15, P.steelDark, 0, 2.6, -0.6, g); box(2.15, 0.15, 0.15, P.steelDark, 0, 2.6, 0.6, g);
    box(0.15, 0.15, 1.35, P.steelDark, -1, 2.6, 0, g); box(0.15, 0.15, 1.35, P.steelDark, 1, 2.6, 0, g);
    box(0.15, 0.1, 0.3, P.steel, -1, 1.5, -0.4, g); box(0.15, 0.1, 0.3, P.steel, 1, 1.5, -0.4, g);
    box(2.6, 0.08, 0.08, P.steel, 0, 1.6, -0.4, g);
    for (const s of [-1, 1]) box(0.14, 0.7, 0.7, P.black, s * 1.1, 1.29, -0.4, g);
  });
  place("SquatRack", "SquatRack", -8.5, 0, 9.5, 0);
  defModel("PlateTree", g => {
    box(1.0, 0.12, 1.0, P.steelDark, 0, 0, 0, g); box(0.15, 1.6, 0.15, P.steelDark, 0, 0, 0, g);
    [[0.45, P.black, 0.8], [0.36, P.dark, 0.45], [0.28, P.red, 0.6]].forEach(([r, c, y], a) => {
      const px = [0.35, -0.35, 0][a], pz = [0, 0, 0.35][a];
      box(0.4, 0.08, 0.08, P.steel, px, y + 0.2, pz, g);
      for (let k = 0; k < 3; k++) { const m = box(0.1, r * 2, r * 2, c, px + (a === 2 ? 0 : (a ? -1 : 1) * (0.05 + k * 0.11)), y + 0.24 - r, pz + (a === 2 ? 0.05 + k * 0.11 : 0), g); if (a === 2) m.rotation.y = Math.PI / 2; }
    });
  });
  place("PlateTree", "PlateTree", -5.0, 0, 9.0, 0);
  defModel("Kettlebells", g => [[-0.5, P.black], [0, P.dark], [0.5, P.red]].forEach(([x, c], i) => {
    const s = 0.34 + i * 0.03;
    part(g, "Kettlebell_" + (i + 1), x, 0, 0, k => { box(s, s, s, c, 0, 0, 0, k); box(0.07, 0.25, 0.07, c, -0.12, s, 0, k); box(0.07, 0.25, 0.07, c, 0.12, s, 0, k); box(0.31, 0.07, 0.07, c, 0, s + 0.22, 0, k); });
  }));
  place("Kettlebells", "Kettlebells", -5.3, 0, 1.0, 0);
  defModel("MedicineBalls", g => { box(0.5, 0.5, 0.5, P.blue, 0, 0, 0, g); box(0.42, 0.42, 0.42, P.yellow, 0, 0.5, 0, g); box(0.46, 0.46, 0.46, P.green, 0.6, 0, 0.3, g); });
  place("MedicineBalls", "MedicineBalls", -4.2, 0, 1.0, 0);

  defModel("Lockers", g => {
    for (let i = 0; i < 4; i++) {
      const x = -1.5 + i;
      box(0.96, 2.2, 0.6, P.lockerBlue, x, 0, 0, g);
      box(0.6, 0.05, 0.04, P.dark, x, 1.8, 0.31, g); box(0.6, 0.05, 0.04, P.dark, x, 1.65, 0.31, g); box(0.6, 0.05, 0.04, P.dark, x, 1.5, 0.31, g);
      box(0.08, 0.3, 0.06, P.steel, x + 0.33, 1.0, 0.31, g);
    }
    box(4.2, 0.1, 0.7, P.dark, 0, 2.2, 0, g);
  });
  place("Lockers", "Lockers", -2.5, 0, -D / 2 + 0.6, 0);

  defModel("WaterCooler", g => { box(0.6, 1.1, 0.6, P.white, 0, 0, 0, g); box(0.42, 0.65, 0.42, 0x9fd3ea, 0, 1.1, 0, g); box(0.2, 0.15, 0.1, P.blue, 0, 0.75, 0.32, g); });
  place("WaterCooler", "WaterCooler", 10.5, 0, 9.5, 0);
  defModel("Bench", b => {
    box(0.15, 0.45, 0.4, P.dark, -0.8, 0, 0, b); box(0.15, 0.45, 0.4, P.dark, 0.8, 0, 0, b); box(2.0, 0.15, 0.5, P.woodC, 0, 0.45, 0, b);
    box(0.4, 0.3, 0.25, P.red, -0.5, 0.6, 0, b); box(0.3, 0.4, 0.2, P.white, 0.4, 0.6, 0.02, b);
  });
  place("Bench", "Bench", 7.9, 0, 9.5, 0);
  defModel("StretchMat", g => box(2.2, 0.06, 1.2, P.blue, 0, 0, 0, g));
  place("StretchMat", "StretchMat_1", 4.5, 0, 9.5, 0); place("StretchMat", "StretchMat_2", 7.0, 0, 9.5, 0);
  defModel("SparePlates", g => { box(0.35, 0.35, 0.35, P.black, 0, 0, 0, g); box(0.35, 0.35, 0.35, P.black, 0, 0.35, 0, g); });
  place("SparePlates", "SparePlates", 5.5, 0.06, 7.8, 0);
