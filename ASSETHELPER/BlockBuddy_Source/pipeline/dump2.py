import sys
sys.path.insert(0, 'venv3.13/lib/python3.13/site-packages/bpy/5.2/scripts/addons_core')
from io_scene_fbx import parse_fbx
root, ver = parse_fbx.parse(sys.argv[1])
objs = [c for c in root.elems if c.id == b'Objects'][0]
for o in objs.elems:
    if o.id != b'Model': continue
    nm = o.props[1].split(b'\x00')[0].decode()
    if nm not in ('BlockGym_Scene', 'BoxingRing', 'Spawn_RingBoxer_1', 'Spawn_Treadmill_1', 'Treadmill_1', 'HeavyBag_1', 'Barbell'): continue
    props = [e for e in o.elems if e.id == b'Properties70'][0]
    vals = {p.props[0].decode(): [round(x, 3) for x in p.props[4:]] for p in props.elems if p.props[0] in (b'Lcl Translation', b'Lcl Rotation', b'Lcl Scaling', b'PreRotation')}
    print(nm, vals)
for gs in [c for c in root.elems if c.id == b'GlobalSettings']:
    p70 = [e for e in gs.elems if e.id == b'Properties70'][0]
    print({p.props[0].decode(): p.props[4:] for p in p70.elems if b'Axis' in p.props[0] or b'Scale' in p.props[0]})
