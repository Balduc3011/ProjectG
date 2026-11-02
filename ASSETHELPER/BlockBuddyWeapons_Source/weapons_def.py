"""Weapon sprites -> voxel boxes in WEAPON space (viewer / three.js convention, unit = 1 skin px).
Weapon space: origin = grip (the point inside the fist), +Z = towards the tip (sword / spear / arrow)
or the upper limb (bow), X = thickness. At rest the socket on Hand.R / Hand.L has the same orientation
as the character (faces +Z), so with the arm hanging down the weapon points forward.
Pure python (PIL), shared by weapons_build.py (Blender) and the preview."""
import math
from PIL import Image

H = math.sqrt(0.5)
WEAPONS = {
    # sprite, grid, voxel size (skin px), grip (image px, y down, pixel corners), axis to +Z, axis to +Y, atlas offset
    "Sword": dict(png="sword.png", n=16, s=0.8, grip=(3.5, 12.5), z=(H, -H), y=(-H, -H), atlas=(0, 0)),
    "Bow":   dict(png="bow.png",   n=16, s=0.85, grip=(6.0, 6.0), z=(H, -H), y=(H, H), atlas=(16, 0)),
    "Arrow": dict(png="arrow.png", n=16, s=0.75, grip=(1.5, 14.5), z=(H, -H), y=(-H, -H), atlas=(0, 16)),
    "Spear": dict(png="spear.png", n=32, s=0.7, grip=(24.5, 23.5), z=(-H, -H), y=(H, -H), atlas=(32, 0)),
}
ATLAS = (64, 32)


def make_arrow(path):
    """16x16 arrow sprite in the same style as the other three (tip top-right, fletching bottom-left)."""
    im = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    shaft = [(137, 103, 39, 255), (104, 78, 30, 255)]
    for k in range(3, 12):
        im.putpixel((k, 15 - k), shaft[k % 2])
    tip = {(12, 3): (107, 107, 107), (13, 2): (216, 216, 216), (14, 1): (255, 255, 255), (12, 2): (68, 68, 68),
           (13, 3): (68, 68, 68), (13, 1): (24, 24, 24), (14, 2): (24, 24, 24), (11, 2): (24, 24, 24), (13, 4): (24, 24, 24)}
    for (x, y), c in tip.items():
        im.putpixel((x, y), c + (255,))
    fl = {(2, 13): (104, 78, 30), (1, 14): (73, 54, 21), (0, 15): (40, 30, 11),            # shaft end + nock
          (1, 12): (255, 255, 255), (2, 11): (216, 216, 216), (0, 13): (190, 190, 190),     # feathers, one side
          (3, 14): (255, 255, 255), (4, 13): (216, 216, 216), (2, 15): (190, 190, 190)}     # other side
    for (x, y), c in fl.items():
        im.putpixel((x, y), c + (255,))
    im.save(path)


def downsample(src, n, out):
    im = Image.open(src).convert("RGBA")
    k = im.width // n
    small = Image.new("RGBA", (n, n))
    for y in range(n):
        for x in range(n):
            small.putpixel((x, y), im.getpixel((x * k + k // 2, y * k + k // 2)))
    small.save(out)


def build_atlas(d, out):
    at = Image.new("RGBA", ATLAS, (0, 0, 0, 0))
    for name, w in WEAPONS.items():
        at.paste(Image.open(f"{d}/{w['png']}").convert("RGBA"), w["atlas"])
    for k, (u, v) in PAL_UV.items():                 # gauntlet colours
        at.putpixel((int(u), int(v)), PALETTE[k] + (255,))
    at.save(out)


def voxels(name, d):
    """-> list of quads: (4 corners in weapon space, uv (atlas px, y down) of the pixel)."""
    if name in BOXMODELS:
        return box_quads(name)
    w = WEAPONS[name]
    im = Image.open(f"{d}/{w['png']}").convert("RGBA")
    n, s, (gx, gy) = w["n"], w["s"], w["grip"]
    solid = {(x, y) for y in range(n) for x in range(n) if im.getpixel((x, y))[3] >= 128}
    za, ya = w["z"], w["y"]

    def P(u, v, x):   # image point (y down) -> weapon space
        du, dv = u - gx, v - gy
        return (x * s, s * (du * ya[0] + dv * ya[1]), s * (du * za[0] + dv * za[1]))
    quads = []
    for (x, y) in solid:
        uv = (w["atlas"][0] + x + 0.5, w["atlas"][1] + y + 0.5)
        c = [(x, y), (x + 1, y), (x + 1, y + 1), (x, y + 1)]      # image corners
        # front / back (thickness along X)
        quads.append(([P(u, v, 0.5) for u, v in c], uv))
        quads.append(([P(u, v, -0.5) for u, v in c[::-1]], uv))
        # sides, only where the neighbour is empty
        for (nx, ny), (a, b) in (((0, -1), (c[0], c[1])), ((1, 0), (c[1], c[2])), ((0, 1), (c[2], c[3])), ((-1, 0), (c[3], c[0]))):
            if (x + nx, y + ny) in solid:
                continue
            quads.append(([P(*a, -0.5), P(*b, -0.5), P(*b, 0.5), P(*a, 0.5)], uv))
    return quads


# ---------------------------------------------------------------- gauntlets (box models, original design)
# Socket space, right hand. Arm box = 4x4 px centred on the socket; end of the fist at y = -3, elbow at y = +5.
# Colours live as single pixels in the free atlas area (x 16..31, y 16).
PALETTE = {"steelL": (169, 175, 183), "steelM": (125, 131, 139), "steelD": (75, 80, 88), "copper": (184, 101, 42), "copperL": (216, 138, 70)}
PAL_UV = {k: (16 + i + 0.5, 16 + 0.5) for i, k in enumerate(PALETTE)}
GAUNTLET_R = [  # (centre xyz, size xyz, colour)
    ((0, -1.4, 0), (5.0, 4.8, 5.0), "steelM"),                          # fist shell
    ((0, -1.4, 2.65), (3.6, 3.4, 0.5), "steelL"), ((0, -1.4, -2.65), (3.6, 3.4, 0.5), "steelD"),   # front / back plates
    *[((x, -4.1, 0), (0.9, 0.6, 3.6), "copper") for x in (-1.65, -0.55, 0.55, 1.65)],                 # knuckle bars
    ((0, 2.6, 0), (4.7, 3.8, 4.7), "steelD"),                           # forearm cuff
    ((0, 1.2, 0), (5.3, 0.8, 5.3), "copperL"), ((0, 4.1, 0), (5.0, 0.6, 5.0), "steelL"),           # bands
    ((-2.85, 2.4, 0), (0.7, 3.2, 2.2), "copper"),                       # outer fin (character's right side)
]
BOXMODELS = {"Gauntlet_R": GAUNTLET_R, "Gauntlet_L": [((-c[0], c[1], c[2]), s, col) for c, s, col in GAUNTLET_R]}


def add_palette(atlas_path):
    at = Image.open(atlas_path).convert("RGBA")
    for k, (u, v) in PAL_UV.items():
        at.putpixel((int(u), int(v)), PALETTE[k] + (255,))
    at.save(atlas_path)


def box_quads(name):
    quads = []
    for (cx, cy, cz), (w, h, d), col in BOXMODELS[name]:
        x, y, z = w / 2, h / 2, d / 2
        faces = [[(-x,-y, z),( x,-y, z),( x, y, z),(-x, y, z)], [( x,-y,-z),(-x,-y,-z),(-x, y,-z),( x, y,-z)],
                 [(-x,-y,-z),(-x,-y, z),(-x, y, z),(-x, y,-z)], [( x,-y, z),( x,-y,-z),( x, y,-z),( x, y, z)],
                 [(-x, y, z),( x, y, z),( x, y,-z),(-x, y,-z)], [(-x,-y,-z),( x,-y,-z),( x,-y, z),(-x,-y, z)]]
        for f in faces:
            quads.append(([(cx + a, cy + b, cz + c) for a, b, c in f], PAL_UV[col]))
    return quads
