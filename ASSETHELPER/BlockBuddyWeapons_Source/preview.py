"""Render contact sheets of the weapon clips straight from poses_weapons.json (same rig math as build3.py anims).
usage: python preview.py <poses.json> <skin.png> <out_prefix> [clip,clip..] [cols]"""
import sys, os, json, math
HERE = os.path.dirname(os.path.abspath(__file__))
POSES, SKIN, OUTP = sys.argv[1:4]
ONLY = sys.argv[4].split(",") if len(sys.argv) > 4 and sys.argv[4] else None
COLS = int(sys.argv[5]) if len(sys.argv) > 5 else 8
sys.argv = ["build3.py", "model", SKIN, "classic", "/tmp/_bb.fbx", "BlockBuddy_Skin"]
exec(open(os.path.join(HERE, "pipeline", "build3.py")).read())          # -> top, rig, ob, arm_data, vpos, vrot, euler_xyz ...
import bpy
from mathutils import Matrix, Vector
sys.path.insert(0, HERE)
from weapons_build import make_weapon, make_material
from PIL import Image

body = ob
scene = bpy.context.scene
wmat = make_material(os.path.join(HERE, "sprites", "BlockBuddyWeapons_Atlas.png"))
W = {}
for name in ("Sword", "Bow", "Spear", "Arrow", "Gauntlet_R", "Gauntlet_L"):
    t, m = make_weapon(name, os.path.join(HERE, "sprites"), wmat, scene)
    W[name] = m
GRIP = {"R": (-6, 13, 0), "L": (6, 13, 0)}
bpy.context.view_layer.update()
def socket(obj, side):
    mw = Matrix.Translation(vpos(*GRIP[side]))
    obj.parent = rig; obj.parent_type = "BONE"; obj.parent_bone = "Hand." + side
    bpy.context.view_layer.update()
    obj.matrix_world = mw
socket(W["Sword"], "R"); socket(W["Spear"], "R"); socket(W["Bow"], "L"); socket(W["Gauntlet_R"], "R"); socket(W["Gauntlet_L"], "L")
# marker empties at the grips (to place the arrow like the Unity script does)
mk = {}
for s in ("R", "L"):
    e = bpy.data.objects.new("grip" + s, None); scene.collection.objects.link(e); socket(e, s); mk[s] = e

# materials: workbench, texture colour
for m in bpy.data.materials:
    m.surface_render_method = "DITHERED"
    if m.node_tree:
        for n in m.node_tree.nodes:
            if n.type == "TEX_IMAGE": n.interpolation = "Closest"
scene.render.engine = "BLENDER_WORKBENCH"
sh = scene.display.shading
sh.light = "STUDIO"; sh.color_type = "TEXTURE"; sh.show_shadows = False
scene.render.resolution_x, scene.render.resolution_y = 260, 300
scene.render.film_transparent = False
w = bpy.data.worlds.new("w"); scene.world = w
# ground grid
gm = bpy.data.meshes.new("ground"); import bmesh
bm = bmesh.new()
for i in range(-6, 7):
    for j in range(-6, 7):
        if (i + j) % 2 == 0:
            bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=0.25, matrix=Matrix.Translation((i * 0.5, j * 0.5, -0.002)))
bm.to_mesh(gm); g = bpy.data.objects.new("ground", gm); scene.collection.objects.link(g)
cd = bpy.data.cameras.new("c"); cd.type = "ORTHO"; cd.ortho_scale = 3.8
cam = bpy.data.objects.new("c", cd); scene.collection.objects.link(cam); scene.camera = cam

# ---- pose application (copy of build3.py anims mode) ----
pb_all = rig.pose.bones
for pb in pb_all: pb.rotation_mode = "QUATERNION"
REST = {b.name: b.matrix_local.to_3x3() for b in arm_data.bones}
LIMBS = [("UpperArm.R", "Elbow.R", "LowerArm.R", 6, -1), ("UpperArm.L", "Elbow.L", "LowerArm.L", 10, -1),
         ("UpperLeg.R", "Knee.R", "LowerLeg.R", 14, 1), ("UpperLeg.L", "Knee.L", "LowerLeg.L", 18, 1)]
def apply(v):
    rots = {"Root": Matrix.Rotation(v[24], 3, 'Y') @ Matrix.Rotation(v[26], 3, 'X'), "Spine": euler_xyz(*v[0:3]), "Head": euler_xyz(*v[3:6])}
    for up, joint, low, o, sgn in LIMBS:
        rots[up] = euler_xyz(*v[o:o + 3]); half = Matrix.Rotation(sgn * max(0.0, v[o + 3]) / 2, 3, 'X'); rots[joint] = half; rots[low] = half
    for bone, R in rots.items():
        O = REST[bone]; pb_all[bone].rotation_quaternion = (O.inverted() @ vrot(R) @ O).to_quaternion()
    pb_all["Root"].location = REST["Root"].inverted() @ vpos(v[25], v[22], v[23])
    bpy.context.view_layer.update()

ARROW = {"Bow_Shoot": [(0.32, 1.06)], "Bow_Skill": [(0.28, 0.59), (0.73, 1.04), (1.18, 1.49)]}
clips = json.load(open(POSES))
sheets = []
for clip in clips:
    if ONLY and clip["name"] not in ONLY: continue
    weapon = clip["name"].split("_")[0]
    for n, o in W.items(): o.hide_render = not (n == weapon or (weapon == "Fist" and n.startswith("Gauntlet")))
    fr = clip["frames"]; L = (len(fr) - 1) / clip["fps"]
    idx = sorted(set(round(i * (len(fr) - 1) / (COLS - 1)) for i in range(COLS)))
    tiles = []
    for view in tuple(int(x) for x in os.environ.get("VIEWS", "35,120").split(",")):
        a = math.radians(view)
        if view == 999:   # top-down
            cam.location = (0, 0, 8); cam.rotation_euler = (0, 0, 0)
        else:
            cam.location = (math.sin(a) * 6, -math.cos(a) * 6, 1.9); cam.rotation_euler = (math.radians(80), 0, a)
        for i in idx:
            t = i / clip["fps"]
            apply(fr[i])
            W["Arrow"].hide_render = True
            for a0, a1 in ARROW.get(clip["name"], []):
                if a0 <= t < a1:
                    up = Vector((0, 0, 0.12)); pr, pl = mk["R"].matrix_world.translation + up, mk["L"].matrix_world.translation + up
                    d = (pl - pr).normalized()
                    W["Arrow"].parent = None
                    W["Arrow"].matrix_world = Matrix.Translation(pr) @ d.to_track_quat('-Y', 'Z').to_matrix().to_4x4()
                    W["Arrow"].hide_render = False
            p = f"/tmp/_t_{view}_{i}.png"; scene.render.filepath = p
            bpy.ops.render.render(write_still=True)
            tiles.append((p, t))
    cols = len(idx)
    sheet = Image.new("RGB", (cols * 260, 2 * 300 + 24), "white")
    from PIL import ImageDraw
    dr = ImageDraw.Draw(sheet); dr.text((6, 4), f"{clip['name']}  L={L:.2f}s  loop={clip['loop']}", fill="black")
    for k, (p, t) in enumerate(tiles):
        im = Image.open(p).convert("RGB"); x, y = (k % cols) * 260, 24 + (k // cols) * 300
        sheet.paste(im, (x, y)); dr.text((x + 4, y + 4), f"{t:.2f}s", fill="black")
    out = f"{OUTP}{clip['name']}.png"; sheet.save(out); sheets.append(out); print("SHEET", out)
