import sys
sys.path.insert(0, 'venv3.13/lib/python3.13/site-packages/bpy/5.2/scripts/addons_core')
from io_scene_fbx import parse_fbx
root, ver = parse_fbx.parse(sys.argv[1])
def find(e, name): return [c for c in e.elems if c.id == name]
objs = find(root, b'Objects')[0]
names = {}
for o in objs.elems:
    uid = o.props[0]; nm = o.props[1].split(b'\x00')[0].decode(errors='replace') if len(o.props) > 1 and isinstance(o.props[1], bytes) else ''
    names[uid] = (o.id.decode(), nm, o.props[2].decode() if len(o.props) > 2 and isinstance(o.props[2], bytes) else '')
from collections import Counter
print('FBX', ver, Counter(v[0]+':'+v[2] for v in names.values()))
conns = find(root, b'Connections')[0]
parent = {}
for c in conns.elems:
    t = c.props[0].decode(); a, b = c.props[1], c.props[2]
    parent.setdefault(a, []).append((b, t, c.props[3].decode() if len(c.props) > 3 else ''))
# model hierarchy
models = {u: v for u, v in names.items() if v[0] == 'Model'}
def par(u):
    for b, t, p in parent.get(u, []):
        if b == 0 or b in models: return b
    return None
for u, v in models.items():
    chain=[]; x=u
    while x: chain.append(models[x][1] if x in models else 'ROOT'); x = par(x)
    print('MODEL', v[2], '/'.join(reversed(chain)))
# which models have curve nodes
cn = {u for u, v in names.items() if v[0] == 'AnimationCurveNode'}
animated = Counter()
for u in cn:
    for b, t, p in parent.get(u, []):
        if b in models: animated[models[b][1] + ':' + p] += 1
print('ANIMATED', sorted(animated))
for st in [v for v in names.values() if v[0]=='AnimationStack']: print('STACK', st)
