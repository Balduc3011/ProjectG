import bpy, sys, math, os, json
from mathutils import Matrix, Vector
models_dir, layout_path, model_fbx, animdir, out = sys.argv[1:6]
layout = json.load(open(layout_path))
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
def import_fbx(path):
    before = set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=path)
    new = set(bpy.data.objects) - before
    return [o for o in new if o.parent is None][0], new
# fbx-space (Y up) placement -> blender: (x, y, z) -> (x, -z, y); yaw about Y -> about Z
for pl in layout['placements']:
    root, _ = import_fbx(os.path.join(models_dir, pl['model'] + '.fbx'))
    x, y, z = pl['fbx']['position']; root.location = (x, -z, y); root.rotation_euler.z = math.radians(pl['fbx']['rotationY'])
    root.name = pl['name']
    if pl['name'] in ('Wall_South', 'Wall_East'):
        for o in [root] + list(root.children_recursive): o.hide_render = True
clipFor = {'RingBoxer': 'Box_Guard', 'BagBoxer': 'Box_JabLeft', 'BenchPress': 'Bench_Press', 'DumbbellCurl': 'Curl_Loop', 'Treadmill': 'Run'}
for sp in layout['spawns']:
    root, new = import_fbx(model_fbx)
    x, y, z = sp['fbx']['position']; root.location = (x, -z, y); root.rotation_euler.z = math.radians(sp['fbx']['rotationY'])
    rig = [o for o in new if o.type == 'ARMATURE'][0]
    clip = clipFor[sp['role']]
    b2 = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=os.path.join(animdir, f'BlockBuddy@{clip}.fbx'))
    arig = [o for o in set(bpy.data.objects) - b2 if o.type == 'ARMATURE'][0]
    sc.frame_set(12)
    for pb in rig.pose.bones:
        pb.matrix = arig.pose.bones[pb.name].matrix.copy(); bpy.context.view_layer.update()
    for o in set(bpy.data.objects) - b2: bpy.data.objects.remove(o)
sc.render.engine = 'CYCLES'; sc.cycles.device = 'CPU'; sc.cycles.samples = 16; sc.cycles.use_denoising = False
sc.render.resolution_x, sc.render.resolution_y = 1100, 720
w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True; w.node_tree.nodes['Background'].inputs[0].default_value = (0.85, 0.9, 0.95, 1)
for m in bpy.data.materials:
    if m.node_tree:
        for n in m.node_tree.nodes:
            if n.type == 'TEX_IMAGE': n.interpolation = 'Closest'
        bs = [n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'][0]; bs.inputs['Roughness'].default_value = 1; bs.inputs['Specular IOR Level'].default_value = 0
sun = bpy.data.lights.new('s', 'SUN'); sun.energy = 3; so = bpy.data.objects.new('s', sun); sc.collection.objects.link(so); so.rotation_euler = (math.radians(35), 0, math.radians(-30))
cd = bpy.data.cameras.new('c'); cd.type = 'ORTHO'; cd.ortho_scale = 34; cam = bpy.data.objects.new('c', cd); sc.collection.objects.link(cam); sc.camera = cam
a = math.radians(45); d = 60
cam.location = (math.sin(a) * d, -math.cos(a) * d, d * 0.8165); cam.rotation_euler = (math.radians(54.7), 0, a)
sc.render.filepath = out; bpy.ops.render.render(write_still=True)
