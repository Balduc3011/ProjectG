"""Round trip check: user's BlockBuddy.fbx + exported clip FBX + weapon FBX, imported back into Blender.
usage: python verify_fbx.py <BlockBuddy.fbx> <out_dir root> <out.png> clip:frame:weapon:side ..."""
import bpy, sys, os, math
from mathutils import Matrix, Vector
from PIL import Image
MODEL, ROOT, OUT = sys.argv[1:4]; SHOTS = [s.split(":") for s in sys.argv[4:]]
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
bpy.ops.import_scene.fbx(filepath=MODEL)
rig = [o for o in bpy.data.objects if o.type == 'ARMATURE'][0]
skin = bpy.data.images.load(os.path.abspath("skins/Skin_GreenJacket.png"))
for m in bpy.data.materials:
    m.use_nodes = True; nt = m.node_tree
    tex = nt.nodes.new("ShaderNodeTexImage"); tex.image = skin; tex.interpolation = "Closest"
    nt.links.new(tex.outputs["Color"], nt.nodes["Principled BSDF"].inputs["Base Color"])
    nt.links.new(tex.outputs["Alpha"], nt.nodes["Principled BSDF"].inputs["Alpha"]); m.surface_render_method = "DITHERED"
PX = 1 / 16
def vpos(x, y, z): return Vector((x, -z, y)) * PX
weapons = {}
for w in ("Sword", "Bow", "Spear", "Gauntlet_R", "Gauntlet_L"):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=f"{ROOT}/Models/Weapon_{w}.fbx")
    new = set(bpy.data.objects) - before
    top = [o for o in new if o.type == 'EMPTY'][0]
    side = "L" if w in ("Bow", "Gauntlet_L") else "R"
    bpy.context.view_layer.update()
    rest = top.matrix_world.copy()
    # socket = identity orientation in character space at rest, at the grip (+1 px from the end of the fist)
    top.parent = rig; top.parent_type = "BONE"; top.parent_bone = "Hand." + side
    bpy.context.view_layer.update()
    top.matrix_world = Matrix.Translation(vpos(-6 if side == "R" else 6, 13, 0)) @ rest
    weapons[w] = new
img = bpy.data.images.load(os.path.abspath(f"{ROOT}/Textures/BlockBuddyWeapons_Atlas.png")) if os.path.exists(f"{ROOT}/Textures/BlockBuddyWeapons_Atlas.png") else None
for m in bpy.data.materials:
    if m.name.startswith("BlockBuddyWeapons") and img:
        nt = m.node_tree; tex = nt.nodes.new("ShaderNodeTexImage"); tex.image = img; tex.interpolation = "Closest"
        nt.links.new(tex.outputs["Color"], nt.nodes["Principled BSDF"].inputs["Base Color"])
sc.render.engine = "BLENDER_WORKBENCH"; sc.display.shading.color_type = "TEXTURE"; sc.display.shading.light = "STUDIO"
sc.render.resolution_x, sc.render.resolution_y = 260, 300
sc.world = bpy.data.worlds.new("w")
cd = bpy.data.cameras.new("c"); cd.type = "ORTHO"; cd.ortho_scale = 3.8
cam = bpy.data.objects.new("c", cd); sc.collection.objects.link(cam); sc.camera = cam
a = math.radians(35); cam.location = (math.sin(a) * 6, -math.cos(a) * 6, 1.9); cam.rotation_euler = (math.radians(80), 0, a)
tiles = []
for i, (clip, frame, weapon) in enumerate(SHOTS):
    for w, objs in weapons.items():
        for o in objs: o.hide_render = not (w == weapon or (weapon == "Fist" and w.startswith("Gauntlet")))
    for pb in rig.pose.bones: pb.matrix_basis = Matrix()
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=f"{ROOT}/Animations/{weapon}/BlockBuddy@{clip}.fbx")
    new = set(bpy.data.objects) - before
    arig = [o for o in new if o.type == 'ARMATURE'][0]
    sc.frame_set(int(frame))
    for pb in rig.pose.bones:
        pb.matrix = arig.pose.bones[pb.name].matrix.copy(); bpy.context.view_layer.update()
    for o in new: bpy.data.objects.remove(o)
    bpy.context.view_layer.update()
    print("HAND.R", clip, frame, (rig.matrix_world @ rig.pose.bones["Hand.R"].head))
    p = f"/tmp/_v{i}.png"; sc.render.filepath = p; bpy.ops.render.render(write_still=True); tiles.append(p)
sheet = Image.new("RGB", (260 * len(tiles), 300), "white")
for k, p in enumerate(tiles): sheet.paste(Image.open(p).convert("RGB"), (k * 260, 0))
sheet.save(OUT); print("SHEET", OUT)
