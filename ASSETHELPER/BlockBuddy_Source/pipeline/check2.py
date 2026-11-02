import bpy, sys, json, math, os
model, animdir, tag, out = sys.argv[1:5]; shots = json.loads(sys.argv[5])
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=model)
rig = [o for o in bpy.data.objects if o.type == 'ARMATURE'][0]
mesh = [o for o in bpy.data.objects if o.type == 'MESH'][0]
print('IMG', [(i.name, i.size[:], i.filepath) for i in bpy.data.images], 'ACTS', len(bpy.data.actions))
sc = bpy.context.scene
sc.render.engine='CYCLES'; sc.cycles.device='CPU'; sc.cycles.samples=16; sc.cycles.use_denoising=False
sc.render.film_transparent=True; sc.render.resolution_x, sc.render.resolution_y = 300, 360
w=bpy.data.worlds.new('w'); sc.world=w; w.use_nodes=True; w.node_tree.nodes['Background'].inputs[0].default_value=(1,1,1,1)
for m in bpy.data.materials:
    if m.node_tree:
        for n in m.node_tree.nodes:
            if n.type=='TEX_IMAGE': n.interpolation='Closest'
        bs=[n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'][0]; bs.inputs['Roughness'].default_value=1
        bs.inputs['Specular IOR Level'].default_value=0
sun=bpy.data.lights.new('s','SUN'); sun.energy=2.0; so=bpy.data.objects.new('s',sun); sc.collection.objects.link(so); so.rotation_euler=(math.radians(40),0,math.radians(-30))
cd=bpy.data.cameras.new('c'); cd.type='ORTHO'; cd.ortho_scale=2.6; cam=bpy.data.objects.new('c',cd); sc.collection.objects.link(cam); sc.camera=cam
for i,(clip, frame, ang) in enumerate(shots):
    for pb in rig.pose.bones: pb.matrix_basis = __import__('mathutils').Matrix()
    if clip:
        before_o = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=os.path.join(animdir, f'BlockBuddy{tag}@{clip}.fbx'))
        arig = [o for o in set(bpy.data.objects) - before_o if o.type == 'ARMATURE'][0]
        sc.frame_set(frame + 1)
        for pb in rig.pose.bones:   # hierarchy order: copy armature-space pose (rest differs on rig-only import)
            pb.matrix = arig.pose.bones[pb.name].matrix.copy()
            bpy.context.view_layer.update()
        for o in set(bpy.data.objects) - before_o: bpy.data.objects.remove(o)
        bpy.context.view_layer.update()
    a=math.radians(ang); cam.location=(math.sin(a)*6,-math.cos(a)*6,1.05); cam.rotation_euler=(math.radians(90),0,a)
    sc.render.filepath=f'{out}{i:02d}.png'; bpy.ops.render.render(write_still=True)
