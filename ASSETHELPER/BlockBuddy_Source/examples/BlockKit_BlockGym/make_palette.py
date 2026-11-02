"""Palette + wood + brick textures for the gym props. usage: python make_palette.py gym_scene.json <BlockGym folder>"""
import json, sys
from PIL import Image
data = json.load(open(sys.argv[1])); G = sys.argv[2]
colors = sorted({b['color'] for m in data['models'].values() for part in m['parts'] for b in part['boxes'] if b['tex'] is None})
PS = 16
pal = Image.new('RGBA', (PS, PS), (255, 0, 255, 255)); cmap = {}
for i, c in enumerate(colors):
    col, row = i % PS, i // PS
    pal.putpixel((col, row), ((c >> 16) & 255, (c >> 8) & 255, c & 255, 255)); cmap[str(c)] = [col, row]
pal.save(f'{G}/Textures/BlockGym_Palette.png')
P = dict(woodA=0xb5824e, woodB=0xa3703f, woodC=0xc4905a, woodLine=0x7a5230, brickA=0xa8584a, brickB=0x985042, brickC=0xb3655a, mortar=0xcfc5b4)
rgb = lambda c: ((c >> 16) & 255, (c >> 8) & 255, c & 255, 255)
def rect(img, x, y, w, h, c):
    for yy in range(y, y + h):
        for xx in range(x, x + w):
            if 0 <= xx < img.width and 0 <= yy < img.height: img.putpixel((xx, yy), rgb(c))
wood = Image.new('RGBA', (64, 64)); shades = [P['woodA'], P['woodB'], P['woodC'], P['woodA']]
for row in range(4):
    off = (row % 2) * 32
    for k in (-1, 0, 1):
        x = off + k * 64
        rect(wood, x, row * 16, 64, 16, shades[(row + k + 4) % 4])
        rect(wood, x, row * 16, 1, 16, P['woodLine']); rect(wood, x, row * 16 + 15, 64, 1, P['woodLine'])
        rect(wood, x + 12, row * 16 + 6, 10, 1, P['woodB']); rect(wood, x + 36, row * 16 + 10, 14, 1, P['woodB'])
wood.save(f'{G}/Textures/BlockGym_Wood.png')
brick = Image.new('RGBA', (32, 32), rgb(P['mortar'])); bs = [P['brickA'], P['brickB'], P['brickC']]
for row in range(4):
    off = (row % 2) * 8
    for k in (-1, 0, 1, 2): rect(brick, off + k * 16 + 1, row * 8 + 1, 14, 6, bs[(row * 3 + k + 3) % 3])
brick.save(f'{G}/Textures/BlockGym_Brick.png')
json.dump({'size': PS, 'colors': cmap, 'palette_png': f'{G}/Textures/BlockGym_Palette.png', 'wood_png': f'{G}/Textures/BlockGym_Wood.png', 'brick_png': f'{G}/Textures/BlockGym_Brick.png'}, open('palette.json', 'w'))
print(len(colors), 'colors')
