"""BlockBuddy for Unity.
  model : python build3.py model <skin.png> classic <out.fbx> <material_name> [blend]
  anims : python build3.py anims <poses.json> classic <out_dir>
Hierarchy in every file:  BlockBuddy (empty) / Rig (armature) [/ Body (mesh)]
so Unity binds clip paths "Rig/Root/Hips/..." the same way in the model and in the clip files.
Viewer space: Y up, faces +Z, character's right = -X, 1 unit = 1 skin pixel.
Blender space: Z up, faces -Y.  viewer (x, y, z) -> blender (x, -z, y)."""
import bpy, bmesh, sys, json, os
from mathutils import Matrix, Vector

MODE = sys.argv[1]
PX = 1 / 16
C = Matrix(((1, 0, 0), (0, 0, -1), (0, 1, 0)))
def vpos(x, y, z): return Vector((x, -z, y)) * PX
def vrot(m3): return C @ m3 @ C.inverted()
def euler_xyz(x, y, z): return Matrix.Rotation(x, 3, 'X') @ Matrix.Rotation(y, 3, 'Y') @ Matrix.Rotation(z, 3, 'Z')

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
SLIM = sys.argv[3] == "slim"
aw = 3 if SLIM else 4
xa = 4 + aw / 2

# ---------------- armature (same in model and animation files) ----------------
BONES = {
    "Root":       ((0, 0, 0),    (0, 0, 4),    None),
    "Hips":       ((0, 12, 0),   (0, 14, 0),   "Root"),
    "Spine":      ((0, 12, 0),   (0, 24, 0),   "Hips"),
    "Head":       ((0, 24, 0),   (0, 32, 0),   "Spine"),
    "UpperArm.R": ((-xa, 22, 0), (-xa, 18, 0), "Spine"),
    "Elbow.R":    ((-xa, 18, 0), (-xa, 17, 0), "UpperArm.R"),
    "LowerArm.R": ((-xa, 18, 0), (-xa, 12, 0), "Elbow.R"),
    "Hand.R":     ((-xa, 12, 0), (-xa, 10, 0), "LowerArm.R"),
    "UpperArm.L": ((xa, 22, 0),  (xa, 18, 0),  "Spine"),
    "Elbow.L":    ((xa, 18, 0),  (xa, 17, 0),  "UpperArm.L"),
    "LowerArm.L": ((xa, 18, 0),  (xa, 12, 0),  "Elbow.L"),
    "Hand.L":     ((xa, 12, 0),  (xa, 10, 0),  "LowerArm.L"),
    "UpperLeg.R": ((-2, 12, 0),  (-2, 6, 0),   "Hips"),
    "Knee.R":     ((-2, 6, 0),   (-2, 5, 0),   "UpperLeg.R"),
    "LowerLeg.R": ((-2, 6, 0),   (-2, 0, 0),   "Knee.R"),
    "Foot.R":     ((-2, 0, 0),   (-2, 0, 3),   "LowerLeg.R"),
    "UpperLeg.L": ((2, 12, 0),   (2, 6, 0),    "Hips"),
    "Knee.L":     ((2, 6, 0),    (2, 5, 0),    "UpperLeg.L"),
    "LowerLeg.L": ((2, 6, 0),    (2, 0, 0),    "Knee.L"),
    "Foot.L":     ((2, 0, 0),    (2, 0, 3),    "LowerLeg.L"),
}
top = bpy.data.objects.new("BlockBuddy", None)
scene.collection.objects.link(top)
# The FBX exporter turns Blender Z-up into Y-up by rotating the root node. Unity drops / overrides the
# root rotation (e.g. Instantiate with Quaternion.identity), so the character ended up lying down.
# Pre-rotate the root by the inverse so it exports with an identity transform, and put the
# up-axis rotation on its children instead (their world transform stays identity in Blender).
from bpy_extras.io_utils import axis_conversion
CONV = axis_conversion(from_forward='Y', from_up='Z', to_forward='-Z', to_up='Y').to_4x4()
top.matrix_world = CONV.inverted()
def attach(child):
    child.parent = top
    child.matrix_parent_inverse = Matrix()
    child.matrix_basis = CONV.copy()
arm_data = bpy.data.armatures.new("Rig")
rig = bpy.data.objects.new("Rig", arm_data)
scene.collection.objects.link(rig)
attach(rig)
bpy.context.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode="EDIT")
for name, (h, t, parent) in BONES.items():
    eb = arm_data.edit_bones.new(name)
    eb.head, eb.tail, eb.roll = vpos(*h), vpos(*t), 0
    if parent:
        eb.parent = arm_data.edit_bones[parent]
        eb.use_connect = False
bpy.ops.object.mode_set(mode="OBJECT")

FBX_COMMON = dict(
    use_selection=True, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
    bake_space_transform=False, add_leaf_bones=False, use_armature_deform_only=False,
    primary_bone_axis="Y", secondary_bone_axis="X",
)

# =====================================================================================
if MODE == "model":
    SKIN, OUT, NAME = sys.argv[2], sys.argv[4], sys.argv[5]
    BLEND = sys.argv[6] if len(sys.argv) > 6 else None
    img = bpy.data.images.load(os.path.abspath(SKIN))
    TW, TH = img.size
    S = TW / 64                       # HD skins
    px = list(img.pixels)
    def opaque(u, v):                 # u, v in 64-grid pixels, v from the top
        x, y = int(u * S + S / 2), int(v * S + S / 2)
        return px[((TH - 1 - y) * TW + x) * 4 + 3] >= 0.5

    # One material for both layers; the outer layer needs alpha cutout (BlockBuddy/Skin shader in Unity).
    mat = bpy.data.materials.new(NAME)
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    tex = nt.nodes.new("ShaderNodeTexImage"); tex.image = img; tex.interpolation = "Closest"
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 1.0
    nt.links.new(tex.outputs["Alpha"], bsdf.inputs["Alpha"])

    ZONE = 2.0     # px on each side of the elbow / knee that blend into the joint

    def faces_of_box(w, h, d, tu, tv, inflate):
        x, y, z = w / 2 + inflate, h / 2 + inflate, d / 2 + inflate
        return [  # corners BL, BR, TR, TL seen from outside; texture rect u0, v0, u1, v1; side?
            ([(-x,-y, z),( x,-y, z),( x, y, z),(-x, y, z)], (tu+d, tv+d, tu+d+w, tv+d+h), True),
            ([( x,-y,-z),(-x,-y,-z),(-x, y,-z),( x, y,-z)], (tu+2*d+w, tv+d, tu+2*d+2*w, tv+d+h), True),
            ([(-x,-y,-z),(-x,-y, z),(-x, y, z),(-x, y,-z)], (tu, tv+d, tu+d, tv+d+h), True),
            ([( x,-y, z),( x,-y,-z),( x, y,-z),( x, y, z)], (tu+d+w, tv+d, tu+2*d+w, tv+d+h), True),
            ([(-x, y, z),( x, y, z),( x, y,-z),(-x, y,-z)], (tu+d, tv, tu+d+w, tv+d), False),
            ([(-x,-y,-z),( x,-y,-z),( x,-y, z),(-x,-y, z)], (tu+d+w, tv, tu+d+2*w, tv+d), False),
        ]

    def bilerp(cs, s, t):   # s: 0 left -> 1 right, t: 0 bottom -> 1 top
        BL, BR, TR, TL = (Vector(c) for c in cs)
        return (BL.lerp(BR, s)).lerp(TL.lerp(TR, s), t)

    PARTS = [  # center (viewer), size, base uv, overlay uv, inflate, bones
        ((0, 18, 0),   (8, 12, 4),  (16, 16), (16, 32), 0.25, ("Spine",)),
        ((0, 28, 0),   (8, 8, 8),   (0, 0),   (32, 0),  0.5,  ("Head",)),
        ((-xa, 18, 0), (aw, 12, 4), (40, 16), (40, 32), 0.25, ("UpperArm.R", "Elbow.R", "LowerArm.R")),
        ((xa, 18, 0),  (aw, 12, 4), (32, 48), (48, 48), 0.25, ("UpperArm.L", "Elbow.L", "LowerArm.L")),
        ((-2, 6, 0),   (4, 12, 4),  (0, 16),  (0, 32),  0.25, ("UpperLeg.R", "Knee.R", "LowerLeg.R")),
        ((2, 6, 0),    (4, 12, 4),  (16, 48), (0, 48),  0.25, ("UpperLeg.L", "Knee.L", "LowerLeg.L")),
    ]

    me = bpy.data.meshes.new("Body")
    ob = bpy.data.objects.new("Body", me)
    scene.collection.objects.link(ob)
    groups = {b: ob.vertex_groups.new(name=b) for b in BONES}
    bm = bmesh.new()
    uvl = bm.loops.layers.uv.new("UVMap")
    dl = bm.verts.layers.deform.verify()

    def weights(bones, y):
        """Limb skinning (same as the web viewer): the joint ring takes half the bend, then the weight
        slides to the upper / lower bone over 2 px; beyond that the limb is rigid.
        Inner and outer layer use the same rule so they bend together."""
        if len(bones) == 1:
            return {bones[0]: 1.0}
        up, joint, low = bones
        w = min(1.0, abs(y) / ZONE)
        if abs(y) < 1e-4:
            return {joint: 1.0}
        return {up if y > 0 else low: w, joint: 1.0 - w} if w < 1.0 else {up if y > 0 else low: 1.0}

    def add_quad(center, bones, corners_local, uvs, double):
        verts = []
        for (x, y, z) in corners_local:
            v = bm.verts.new(vpos(center[0] + x, center[1] + y, center[2] + z))
            for b, w in weights(bones, y).items():
                if w > 0:
                    v[dl][groups[b].index] = w
            verts.append(v)
        for vs, us in ([(verts, uvs)] + ([(verts[::-1], uvs[::-1])] if double else [])):
            if double and vs is not verts:
                vs = [bm.verts.new(v.co) for v in vs]
                for nv, ov in zip(vs, verts[::-1]):
                    for k, w in ov[dl].items():
                        nv[dl][k] = w
            f = bm.faces.new(vs)
            f.smooth = False
            for loop, (u, v) in zip(f.loops, us):
                loop[uvl].uv = (u / 64, 1 - v / 64)

    for center, (w, h, d), buv, ouv, infl, bones in PARTS:
        limb = len(bones) == 3
        # inner and outer layer: full faces like the viewer; limb side faces cut into 1 px rows
        for uvo, infl_ in ((buv, 0), (ouv, infl)):
            for cs, (u0, v0, u1, v1), side in faces_of_box(w, h, d, *uvo, infl_):
                rows = int(v1 - v0) if (limb and side) else 1
                for r in range(rows):
                    ta, tb = 1 - r / rows, 1 - (r + 1) / rows
                    va, vb = v0 + (v1 - v0) * r / rows, v0 + (v1 - v0) * (r + 1) / rows
                    quad = [bilerp(cs, 0, tb), bilerp(cs, 1, tb), bilerp(cs, 1, ta), bilerp(cs, 0, ta)]
                    add_quad(center, bones, [tuple(q) for q in quad], [(u0, vb), (u1, vb), (u1, va), (u0, va)], False)
    bm.to_mesh(me)
    bm.free()
    me.materials.append(mat)
    attach(ob)
    mod = ob.modifiers.new("Armature", "ARMATURE"); mod.object = rig

    if BLEND:
        bpy.ops.wm.save_as_mainfile(filepath=BLEND)
    bpy.ops.object.select_all(action="DESELECT")
    top.select_set(True); rig.select_set(True); ob.select_set(True)
    bpy.ops.export_scene.fbx(filepath=OUT, object_types={"EMPTY", "ARMATURE", "MESH"}, mesh_smooth_type="FACE",
                             bake_anim=False, path_mode="RELATIVE", embed_textures=False, **FBX_COMMON)
    print("MODEL", OUT, len(me.vertices), "verts")

# =====================================================================================
elif MODE == "anims":
    POSES, OUTDIR = sys.argv[2], sys.argv[4]
    os.makedirs(OUTDIR, exist_ok=True)
    tag = "Slim" if SLIM else "Classic"
    clips = json.load(open(POSES))
    rig.animation_data_create()
    pb_all = rig.pose.bones
    for pb in pb_all:
        pb.rotation_mode = "QUATERNION"
    REST = {b.name: b.matrix_local.to_3x3() for b in arm_data.bones}
    LIMBS = [("UpperArm.R", "Elbow.R", "LowerArm.R", 6, -1), ("UpperArm.L", "Elbow.L", "LowerArm.L", 10, -1),
             ("UpperLeg.R", "Knee.R", "LowerLeg.R", 14, 1), ("UpperLeg.L", "Knee.L", "LowerLeg.L", 18, 1)]
    import io_scene_fbx.export_fbx_bin as _efb
    _orig = _efb.get_blenderID_name
    _efb.get_blenderID_name = lambda ids: ids[1].name if isinstance(ids, tuple) and len(ids) == 2 and isinstance(ids[1], bpy.types.Action) else _orig(ids)
    scene.render.fps = 30
    for clip in clips:
        for a in list(bpy.data.actions):       # one take per file
            bpy.data.actions.remove(a)
        act = bpy.data.actions.new(clip["name"])
        rig.animation_data.action = act
        prev = {}
        for fi, v in enumerate(clip["frames"]):
            # root offset / rotation of the viewer's player group goes on the Root bone (pivot at the feet)
            rx = v[26] if len(v) > 26 else 0.0
            rots = {"Root": Matrix.Rotation(v[24], 3, 'Y') @ Matrix.Rotation(rx, 3, 'X'), "Spine": euler_xyz(*v[0:3]), "Head": euler_xyz(*v[3:6])}
            for up, joint, low, o, sgn in LIMBS:
                rots[up] = euler_xyz(*v[o:o + 3])
                half = Matrix.Rotation(sgn * max(0.0, v[o + 3]) / 2, 3, 'X')
                rots[joint] = half
                rots[low] = half
            for bone, R in rots.items():
                O = REST[bone]
                q = (O.inverted() @ vrot(R) @ O).to_quaternion()
                if bone in prev and prev[bone].dot(q) < 0:
                    q.negate()
                prev[bone] = q
                pb_all[bone].rotation_quaternion = q
                pb_all[bone].keyframe_insert("rotation_quaternion", frame=fi)
            pb_all["Root"].location = REST["Root"].inverted() @ vpos(v[25] if len(v) > 25 else 0.0, v[22], v[23])
            pb_all["Root"].keyframe_insert("location", frame=fi)
        act.frame_range = (0, len(clip["frames"]) - 1)
        act.use_frame_range = True
        scene.frame_start, scene.frame_end = 0, len(clip["frames"]) - 1
        bpy.ops.object.select_all(action="DESELECT")
        top.select_set(True); rig.select_set(True)
        sub = os.path.join(OUTDIR, clip.get("folder", "")); os.makedirs(sub, exist_ok=True)
        out = os.path.join(sub, f"BlockBuddy@{clip['name']}.fbx")
        bpy.ops.export_scene.fbx(filepath=out, object_types={"EMPTY", "ARMATURE"},
                                 bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False,
                                 bake_anim_use_all_actions=True, bake_anim_force_startend_keying=True,
                                 bake_anim_step=1.0, bake_anim_simplify_factor=0.0, **FBX_COMMON)
        for pb in pb_all:                      # back to rest for the next clip
            pb.rotation_quaternion = (1, 0, 0, 0); pb.location = (0, 0, 0)
    print("ANIMS", OUTDIR, len(clips))
