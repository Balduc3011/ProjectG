"""Block Gym props + scene for Unity.
usage: python build_gym.py <gym_scene.json> <palette.json> <out_dir> [blend]
three.js space (Y up, right-handed) -> Blender (x, -z, y). Same pipeline as BlockBuddy, so the
scene FBX, the prop FBX files and the character land in the same Unity space."""
import bpy, bmesh, sys, json, os
from mathutils import Matrix, Vector
from bpy_extras.io_utils import axis_conversion

SCENE, PALETTE, OUT = sys.argv[1:4]
BLEND = sys.argv[4] if len(sys.argv) > 4 else None
data = json.load(open(SCENE))
pal = json.load(open(PALETTE))           # {"colors": {"13421772": [col, row]}, "size": 16}
PS = pal["size"]

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
C = Matrix(((1, 0, 0), (0, 0, -1), (0, 1, 0)))        # three -> blender basis
CONV = axis_conversion(from_forward='Y', from_up='Z', to_forward='-Z', to_up='Y').to_4x4()

def three_m(m16):
    return Matrix((m16[0:4], m16[4:8], m16[8:12], m16[12:16])).transposed()   # column-major -> Matrix (three space)

# ---------------- materials ----------------
def tex_mat(name, img_path):
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; bsdf = nt.nodes["Principled BSDF"]
    img = bpy.data.images.load(os.path.abspath(img_path))
    tex = nt.nodes.new("ShaderNodeTexImage"); tex.image = img; tex.interpolation = "Closest"
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 1.0
    return m
MATS = { None: tex_mat("BlockKit_Palette", pal["palette_png"]), "wood": tex_mat("BlockKit_Wood", pal["wood_png"]), "brick": tex_mat("BlockKit_Brick", pal["brick_png"]) }

# ---------------- mesh per prop ----------------
FACES = [  # (corner signs for 4 verts BL BR TR TL seen from outside, axis u, axis v) in local box space
    ([(-1,-1, 1),( 1,-1, 1),( 1, 1, 1),(-1, 1, 1)], 0, 1),   # +z
    ([( 1,-1,-1),(-1,-1,-1),(-1, 1,-1),( 1, 1,-1)], 0, 1),   # -z
    ([(-1,-1,-1),(-1,-1, 1),(-1, 1, 1),(-1, 1,-1)], 2, 1),   # -x
    ([( 1,-1, 1),( 1,-1,-1),( 1, 1,-1),( 1, 1, 1)], 2, 1),   # +x
    ([(-1, 1, 1),( 1, 1, 1),( 1, 1,-1),(-1, 1,-1)], 0, 2),   # +y (top)
    ([(-1,-1,-1),( 1,-1,-1),( 1,-1, 1),(-1,-1, 1)], 0, 2),   # -y
]
def build_mesh(name, boxes):
    me = bpy.data.meshes.new(name)
    bm = bmesh.new(); uvl = bm.loops.layers.uv.new("UVMap")
    slots = []
    for b in boxes:
        key = b["tex"]
        if key not in slots: slots.append(key)
        mi = slots.index(key)
        M = three_m(b["m"]); size = (b["w"], b["h"], b["d"])
        if key is None:
            col, row = pal["colors"][str(b["color"])]
            u, v = (col + 0.5) / PS, 1 - (row + 0.5) / PS
        for corners, au, av in FACES:
            vs = []
            for sx, sy, sz in corners:
                local = Vector((sx * size[0] / 2, sy * size[1] / 2, sz * size[2] / 2))
                vs.append(bm.verts.new(C @ (M @ local)))
            f = bm.faces.new(vs); f.smooth = False; f.material_index = mi
            if key is None:
                for l in f.loops: l[uvl].uv = (u, v)
            else:
                fw, fh = size[au] / b["tile"], size[av] / b["tile"]
                for l, (uu, vv) in zip(f.loops, [(0, 0), (fw, 0), (fw, fh), (0, fh)]): l[uvl].uv = (uu, vv)
    bm.to_mesh(me); bm.free()
    for key in slots: me.materials.append(MATS[key])
    return me

FBX_COMMON = dict(apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", bake_space_transform=False,
                  add_leaf_bones=False, mesh_smooth_type="FACE", path_mode="RELATIVE", embed_textures=False, bake_anim=False)

def make_root(name):
    """Root empty pre-rotated so that it exports with an identity transform (same trick as BlockBuddy)."""
    top = bpy.data.objects.new(name, None); scene.collection.objects.link(top)
    top.matrix_world = CONV.inverted()
    return top
def attach(child, parent, loc=(0, 0, 0)):
    child.parent = parent
    child.matrix_parent_inverse = Matrix()
    child.matrix_basis = CONV @ Matrix.Translation(loc)

def export(objs, path):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs: o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"EMPTY", "MESH"}, **FBX_COMMON)

# ---------------- one FBX per model: <Model> (root) -> <Model>_Mesh + one object per named part ----------------
os.makedirs(OUT, exist_ok=True)
for name, model in data["models"].items():
    top = make_root(name); objs = [top]
    for part in model["parts"]:
        if not part["boxes"]: continue
        oname = name + "_Mesh" if part["name"] == "" else part["name"]
        ob = bpy.data.objects.new(oname, build_mesh(oname, part["boxes"])); scene.collection.objects.link(ob)
        x, y, z = part["pos"]
        attach(ob, top, Vector((x, -z, y)))          # part pivot (three space) -> blender
        objs.append(ob)
    export(objs, os.path.join(OUT, name + ".fbx"))
    for o in objs: bpy.data.objects.remove(o)

# ---------------- scene layout json (FBX / three.js space + Unity space) ----------------
import math
def unity(pos, ry):   # Unity's FBX import mirrors X: (x, y, z) -> (-x, y, z), yaw -> -yaw
    return {"position": [round(-pos[0], 4), round(pos[1], 4), round(pos[2], 4)], "rotationY": round(-math.degrees(ry), 2)}
def fbx(pos, ry):
    return {"position": [round(v, 4) for v in pos], "rotationY": round(math.degrees(ry), 2)}
layout = {
    "note": "Positions in metres. 'unity' = ready to use in Unity (FBX import mirrors X). 'fbx' = raw coordinates of the FBX files (Y up, right handed). Rotation = yaw around Y in degrees.",
    "root": "BlockGym",
    "placements": [{"model": p["model"], "name": p["name"], "unity": unity(p["pos"], p["ry"]), "fbx": fbx(p["pos"], p["ry"])} for p in data["placements"]],
    "spawns": [{"name": sp["name"], "role": sp["name"][6:].split("@")[0].split("_")[0], "target": (sp["name"].split("@") + [""])[1],
                "unity": unity(sp["pos"], sp["ry"]), "fbx": fbx(sp["pos"], sp["ry"])} for sp in data["spawns"]],
}
os.makedirs(os.path.join(OUT, "..", "Layouts"), exist_ok=True)
json.dump(layout, open(os.path.join(OUT, "..", "Layouts", "BlockGym_Layout.json"), "w"), indent=2, ensure_ascii=False)
if BLEND:
    bpy.ops.wm.save_as_mainfile(filepath=BLEND)
print("DONE", len(data["models"]), "models", len(data["placements"]), "placements", len(data["spawns"]), "spawns")
