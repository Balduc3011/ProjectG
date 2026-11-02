"""Weapon FBX for BlockBuddy (same axis pipeline as build3.py / build_gym.py).
usage: python weapons_build.py <sprite_dir> <out_models_dir> <atlas.png>
Each file:  Weapon_<Name> (empty, rotation 0, pivot = grip)  ->  Weapon_<Name>_Mesh
Material name in the FBX: BlockBuddyWeapons_Mat (remapped in Unity by BlockBuddyWeaponsImport)."""
import bpy, bmesh, sys, os
from mathutils import Matrix, Vector
from bpy_extras.io_utils import axis_conversion
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from weapons_def import WEAPONS, ATLAS, voxels, BOXMODELS

PX = 1 / 16
CONV = axis_conversion(from_forward='Y', from_up='Z', to_forward='-Z', to_up='Y').to_4x4()
def vpos(x, y, z): return Vector((x, -z, y)) * PX           # viewer -> blender

def weapon_mesh(name, d, mat):
    me = bpy.data.meshes.new(f"Weapon_{name}_Mesh")
    bm = bmesh.new(); uvl = bm.loops.layers.uv.new("UVMap")
    for corners, (u, v) in voxels(name, d):
        f = bm.faces.new([bm.verts.new(vpos(*c)) for c in corners]); f.smooth = False
        for l in f.loops: l[uvl].uv = (u / ATLAS[0], 1 - v / ATLAS[1])
    if name not in BOXMODELS: bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bm.to_mesh(me); bm.free()
    me.materials.append(mat)
    return me

def make_material(atlas):
    m = bpy.data.materials.new("BlockBuddyWeapons_Mat"); m.use_nodes = True
    nt = m.node_tree; bsdf = nt.nodes["Principled BSDF"]
    tex = nt.nodes.new("ShaderNodeTexImage"); tex.image = bpy.data.images.load(os.path.abspath(atlas)); tex.interpolation = "Closest"
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"]); bsdf.inputs["Roughness"].default_value = 1.0
    return m

def make_weapon(name, d, mat, scene):
    """Root empty pre-rotated by CONV^-1 (exports with identity), mesh child carries CONV (like BlockBuddy)."""
    top = bpy.data.objects.new(f"Weapon_{name}", None); scene.collection.objects.link(top)
    top.matrix_world = CONV.inverted()
    ob = bpy.data.objects.new(f"Weapon_{name}_Mesh", weapon_mesh(name, d, mat)); scene.collection.objects.link(ob)
    ob.parent = top; ob.matrix_parent_inverse = Matrix(); ob.matrix_basis = CONV.copy()
    return top, ob

if __name__ == "__main__":
    D, OUT, ATL = sys.argv[1:4]
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    mat = make_material(ATL)
    os.makedirs(OUT, exist_ok=True)
    for name in list(WEAPONS) + list(BOXMODELS):
        top, ob = make_weapon(name, D, mat, scene)
        bpy.ops.object.select_all(action="DESELECT"); top.select_set(True); ob.select_set(True)
        bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, f"Weapon_{name}.fbx"), use_selection=True,
                                 object_types={"EMPTY", "MESH"}, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z",
                                 axis_up="Y", bake_space_transform=False, mesh_smooth_type="FACE", path_mode="RELATIVE",
                                 embed_textures=False, bake_anim=False)
        print("WEAPON", name, len(ob.data.vertices), "verts", len(ob.data.polygons), "faces")
        bpy.data.objects.remove(ob); bpy.data.objects.remove(top)
