"""BlockBuddy weapon movesets (Sword / Bow / Spear) -> poses_weapons.json for build3.py anims.
usage: python anims_weapons.py <out.json> [<meta.json>]

Same pose format as pipeline/poses2.js (viewer space, 1 unit = 1 skin px, faces +Z, right hand at -X):
  flat = body[3] head[3] rArm[4] lArm[4] rLeg[4] lLeg[4] y z ry x rx       (limb[3] = elbow / knee bend)
Arms are authored with IK: a target for the GRIP point (inside the fist) and for the weapon axes, both in
BODY space (origin = hip pivot, rotates with body[]). The weapon is fixed in the fist, so its orientation
is the lower-arm rotation: W = R(arm) * Rx(-bend). See weapons_def.py for the weapon space."""
import json, math, sys
import numpy as np
from scipy.optimize import least_squares

FPS = 30
TAU = 2 * math.pi
S, C = math.sin, math.cos

# ---------------------------------------------------------------- rotations (three.js Euler XYZ)
def Rx(a): c, s = C(a), S(a); return np.array([[1, 0, 0], [0, c, -s], [0, s, c]])
def Ry(a): c, s = C(a), S(a); return np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]])
def Rz(a): c, s = C(a), S(a); return np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]])
def eul(e): return Rx(e[0]) @ Ry(e[1]) @ Rz(e[2])
def unit(v): v = np.asarray(v, float); return v / np.linalg.norm(v)

SHOULDER = {"R": np.array([-6.0, 10.0, 0.0]), "L": np.array([6.0, 10.0, 0.0])}   # body space
UPPER, LOWER = 4.0, 5.0            # shoulder -> elbow, elbow -> grip (grip = 1 px above the end of the fist)
BEND_MAX = 2.75

def arm_fk(side, q):
    """q = [x, y, z, bend] -> grip position, weapon rotation (body space)."""
    R = eul(q[:3]); W = R @ Rx(-q[3])
    return SHOULDER[side] + R @ np.array([0, -UPPER, 0]) + W @ np.array([0, -LOWER, 0]), W

def arm_ik(side, p, dz=None, dy=None, prev=None, wz=7.0, wy=3.0):
    p = np.asarray(p, float)
    def res(q):
        g, W = arm_fk(side, q)
        r = list(g - p)
        if dz is not None: r += list((W[:, 2] - dz) * wz)
        if dy is not None: r += list((W[:, 1] - dy) * wy)
        if prev is not None: r += list((q - prev) * 0.25)
        return r
    lo, hi = [-2 * TAU, -TAU, -TAU, 0], [2 * TAU, TAU, TAU, BEND_MAX]
    seeds = [prev] if prev is not None else []
    if prev is None or True:
        seeds += [np.array(s, float) for s in ([0, 0, 0, 0.5], [-1.5, 0, 0, 1.0], [-1.5, 0, (-1 if side == "R" else 1) * 1.2, 0.3],
                                               [-2.8, 0, 0, 1.2], [-0.5, 0.5, 0, 1.5], [-0.5, -0.5, 0, 1.5], [0.4, 0, 0, 0.3])]
    best = None
    for s in seeds:
        s = np.clip(s, np.array(lo) + 1e-6, np.array(hi) - 1e-6)
        r = least_squares(res, s, bounds=(lo, hi))
        if best is None or r.cost < best.cost - 1e-9: best = r
        if prev is not None and r.cost < 1e-3: break
    return best.x

# ---------------------------------------------------------------- legs / ground contact
HIP = {"R": np.array([-2.0, 12.0, 0.0]), "L": np.array([2.0, 12.0, 0.0])}         # player space
def foot_low(side, leg):
    R = eul(leg[:3]); L = R @ Rx(leg[3])
    knee = HIP[side] + R @ np.array([0, -6.0, 0])
    return min((knee + L @ np.array([sx, -6.0, sz]))[1] for sx in (-2, 2) for sz in (-2, 2))

# ---------------------------------------------------------------- keyframe helpers
def ease(k, mode):
    k = min(1.0, max(0.0, k))
    if mode == "lin": return k
    if mode == "out": return 1 - (1 - k) ** 3          # fast start (strike)
    if mode == "in": return k ** 3
    return k * k * (3 - 2 * k)                          # smooth

def track(keys, t):
    """keys: [(time, value, ease_into_this_key='smooth'), ...] -> value at t (lists / arrays lerped)."""
    if t <= keys[0][0]: return np.asarray(keys[0][1], float)
    for a, b in zip(keys, keys[1:]):
        if t <= b[0]:
            k = ease((t - a[0]) / max(1e-9, b[0] - a[0]), b[2] if len(b) > 2 else "smooth")
            va, vb = np.asarray(a[1], float), np.asarray(b[1], float)
            return va + (vb - va) * k
    return np.asarray(keys[-1][1], float)

def dtrack(keys, t): return unit(track(keys, t))   # direction keys (nlerp)

# ---------------------------------------------------------------- pose container
def pose(**kw):
    p = dict(body=[0, 0, 0], head=[0, 0, 0], rArm=None, lArm=None, rLeg=[0, 0, 0, 0.04], lLeg=[0, 0, 0, 0.04],
             lift=0.0, z=0.0, ry=0.0, x=0.0, rx=0.0, ground=True,
             R=None, L=None)      # R / L: ("ik", grip, dz, dy) | ("euler", [x,y,z,bend]) | ("shaft", dist)  (L only)
    p.update(kw); return p

STANCE_R, STANCE_L = [0.22, 0, -0.07, 0.32], [-0.22, 0, 0.07, 0.36]
RELAX_R, RELAX_L = [0.0, 0, -0.07, 0.15], [0.0, 0, 0.07, 0.15]
def lunge_r(k): return [0.22 + 0.35 * k, 0, -0.07, 0.32 + 0.1 * k]          # back leg
def lunge_l(k): return [-0.22 - 0.55 * k, 0, 0.07, 0.36 + 0.55 * k]          # front leg bends

def run_legs(t, p):
    ph = t * 9; a = S(ph)
    p["rLeg"] = [a * 0.95 - 0.15, 0, 0, 0.25 + max(0, -C(ph)) * 1.7]
    p["lLeg"] = [-a * 0.95 - 0.15, 0, 0, 0.25 + max(0, C(ph)) * 1.7]
    p["lift"] = abs(C(ph)) * 1.4 - 0.4 + 0.4; p["ground"] = True
    return a

# =====================================================================================================
# SWORD (right hand)
SW_READY_G = [-5.0, 4.0, 6.0]; SW_READY_D = [0.12, 0.78, 0.6]
def sword_idle(t):
    br = S(t * TAU / 3.0)
    return pose(body=[0.08, 0.28, 0], head=[-0.05, -0.2 + S(t * TAU / 6) * 0.1, 0], rLeg=STANCE_R, lLeg=STANCE_L,
                R=("ik", [SW_READY_G[0], SW_READY_G[1] + br * 0.35, SW_READY_G[2]], SW_READY_D, None),
                L=("euler", [-0.35 - br * 0.03, 0, 0.18, 0.75]))

def sword_run(t):
    p = pose(body=[0.25, 0, 0], head=[-0.22, 0, 0]); a = run_legs(t, p)
    p["body"][1] = a * 0.1
    p["R"] = ("ik", [-5.5, 4.5 + 0.5 * a, 4.5 - 0.6 * a], [0.1, 0.5, 0.86], None)   # blade forward, slightly up
    p["L"] = ("euler", [a * 0.95, 0, 0.1, 1.45])
    return p

def sword_attack1(t):    # diagonal slash, high right -> low left
    T = [0.0, 0.24, 0.34, 0.42, 0.56, 0.9]
    g = track([(T[0], SW_READY_G), (T[1], [-6, 13, -1]), (T[2], [-4, 10, 4], "in"), (T[3], [-2, 4, 5], "out"), (T[4], [-2, 3, 3]), (T[5], SW_READY_G)], t)
    d = dtrack([(T[0], SW_READY_D), (T[1], [-0.35, 0.4, -0.85]), (T[2], [-0.1, 0.95, 0.3], "in"), (T[3], [0.55, -0.45, 0.7], "out"),
                (T[4], [0.8, -0.5, 0.3]), (T[5], SW_READY_D)], t)
    body = track([(T[0], [0.08, 0.28, 0]), (T[1], [-0.1, -0.45, 0.05]), (T[3], [0.3, 0.55, -0.05], "out"), (T[4], [0.32, 0.65, -0.05]), (T[5], [0.08, 0.28, 0])], t)
    k = track([(0, 0), (T[2], 0.2), (T[3], 1, "out"), (T[4], 1), (T[5], 0)], t)
    return pose(body=list(body), head=[-0.05 - 0.15 * k, -0.25 - 0.3 * k, 0], rLeg=lunge_r(k), lLeg=lunge_l(k), z=1.8 * k,
                R=("ik", g, d, None), L=("euler", [-0.35 + 0.45 * k, 0, 0.18 + 0.25 * k, 0.75]))

def sword_attack2(t):    # horizontal sweep, left -> right (backhand)
    T = [0.0, 0.26, 0.36, 0.44, 0.58, 0.95]
    g = track([(T[0], SW_READY_G), (T[1], [-1, 6, 3]), (T[2], [-4, 5, 7], "in"), (T[3], [-12, 5, 4], "out"), (T[4], [-12, 5, 2]), (T[5], SW_READY_G)], t)
    d = dtrack([(T[0], SW_READY_D), (T[1], [0.75, 0.2, -0.6]), (T[2], [0.05, 0.08, 1.0], "in"), (T[3], [-0.85, 0.02, 0.5], "out"),
                (T[4], [-0.95, 0.05, 0.25]), (T[5], SW_READY_D)], t)
    body = track([(T[0], [0.08, 0.28, 0]), (T[1], [0.05, 0.85, 0]), (T[2], [0.15, 0.1, 0], "in"), (T[3], [0.18, -0.6, 0], "out"), (T[4], [0.15, -0.7, 0]), (T[5], [0.08, 0.28, 0])], t)
    k = track([(0, 0), (T[1], 0.3), (T[3], 1, "out"), (T[4], 1), (T[5], 0)], t)
    return pose(body=list(body), head=[-0.05, -0.2 - body[1] * 0.6, 0], rLeg=lunge_r(0.5 * k), lLeg=lunge_l(0.6 * k), z=1.2 * k,
                R=("ik", g, d, None), L=("euler", [-0.35, 0, 0.18 + 0.6 * k, 0.75 - 0.4 * k]))

def sword_skill(t):      # 2-hit combo: rising slash low-left -> high-right, then chop high-right -> low-left
    T = [0.0, 0.22, 0.36, 0.46, 0.56, 0.68, 0.76, 0.92, 1.3]
    g = track([(T[0], SW_READY_G), (T[1], [-1.5, 4.5, 4.5]), (T[2], [-3, 8, 6], "out"), (T[3], [-7, 13, 2]), (T[4], [-6, 14, 0]),
               (T[5], [-4, 10, 4], "in"), (T[6], [-2, 4, 5], "out"), (T[7], [-2, 3, 3]), (T[8], SW_READY_G)], t)
    d = dtrack([(T[0], SW_READY_D), (T[1], [0.75, -0.45, 0.45]), (T[2], [-0.1, 0.6, 0.8], "out"), (T[3], [-0.6, 0.75, -0.25]),
                (T[4], [-0.4, 0.5, -0.75]), (T[5], [-0.1, 0.95, 0.3], "in"), (T[6], [0.55, -0.45, 0.7], "out"), (T[7], [0.8, -0.5, 0.3]),
                (T[8], SW_READY_D)], t)
    yaw = track([(T[0], 0.28), (T[1], 0.7), (T[2], 0.0, "out"), (T[3], -0.45), (T[4], -0.5), (T[6], 0.55, "out"), (T[7], 0.65), (T[8], 0.28)], t)
    pitch = track([(T[0], 0.08), (T[1], 0.25), (T[2], -0.05, "out"), (T[4], -0.1), (T[6], 0.3, "out"), (T[7], 0.32), (T[8], 0.08)], t)
    k = track([(T[0], 0), (T[1], 0.45), (T[2], 0.6, "out"), (T[4], 0.5), (T[6], 1.0, "out"), (T[7], 1.0), (T[8], 0)], t)
    zz = track([(T[0], 0), (T[1], 0.3), (T[2], 1.2, "out"), (T[4], 1.2), (T[6], 2.4, "out"), (T[7], 2.4), (T[8], 0)], t)
    return pose(body=[float(pitch), float(yaw), 0], head=[-0.05 - 0.6 * float(pitch), -0.2 - float(yaw) * 0.6, 0],
                rLeg=lunge_r(float(k)), lLeg=lunge_l(float(k)), z=float(zz),
                R=("ik", g, d, None), L=("euler", [-0.35 + 0.4 * float(k), 0, 0.18 + 0.3 * float(k), 0.75]))

# =====================================================================================================
# BOW (bow in the left hand, the right hand draws; the arrow is placed by script between the hands)
AIM_YAW = -1.4           # body + root yaw while shooting (left shoulder towards the target)
BW_IDLE_G = [5.0, 3.5, 4.0]; BW_IDLE_D = [0.1, 0.85, 0.5]; BW_IDLE_Y = [-0.9, 0.0, 0.35]
def bow_idle(t):
    br = S(t * TAU / 3.0)
    return pose(body=[0.03, 0, 0], head=[S(t * TAU / 6) * 0.03, S(t * TAU / 6) * 0.25, 0], rLeg=RELAX_R, lLeg=RELAX_L,
                L=("ik", [BW_IDLE_G[0], BW_IDLE_G[1] + br * 0.3, BW_IDLE_G[2]], BW_IDLE_D, BW_IDLE_Y),
                R=("euler", [S(t * TAU / 3) * 0.05, 0, -0.1 - br * 0.03, 0.18]))

def bow_run(t):
    p = pose(body=[0.22, 0, 0], head=[-0.22, 0, 0]); a = run_legs(t, p)
    p["body"][1] = a * 0.1
    p["L"] = ("ik", [6.0, 3.0 - 0.4 * a, 3.0 + 1.0 * a], [0.1, 0.55, 0.83], [-0.9, 0.1, 0.1])
    p["R"] = ("euler", [-a * 0.95, 0, -0.1, 1.45])
    return p

def aim_dir(body_yaw):    # world +Z expressed in body space (root yaw + body yaw = AIM_YAW)
    return unit([S(-AIM_YAW), 0.0, C(-AIM_YAW)])

def bow_shot_pose(t, keysL, keysR, root_k, extra_body=None):
    a = aim_dir(0)
    k = float(track(root_k, t))                      # 0 = relaxed idle, 1 = aiming stance
    bowp = track(keysL, t); hand = track(keysR, t)
    p = pose(body=[0.03, 0.3 * AIM_YAW * k, 0], head=[0, -AIM_YAW * k * 0.9, 0], ry=0.7 * AIM_YAW * k,
             rLeg=[0.1 * k, 0, -0.07 - 0.25 * k, 0.15 + 0.1 * k], lLeg=[-0.1 * k, 0, 0.07 + 0.25 * k, 0.15 + 0.1 * k])
    dz = unit(np.asarray(BW_IDLE_D) * (1 - k) + np.array([0, 1, 0]) * k)
    dy = unit(np.asarray(BW_IDLE_Y) * (1 - k) + (-a) * k)
    p["L"] = ("ik", bowp, dz, dy)
    p["R"] = ("ik", hand, None, None)
    return p

def bow_points():
    a = aim_dir(0)
    anchor = np.array([1.5, 11.5, 3.0])                # full draw: right hand in front of the chin
    bow_full = anchor + a * 11.0                        # bow hand on the same line, towards the target
    nock = anchor + a * 1.8 + np.array([0, -0.4, 0.4])  # right hand pulls the string from here (arrow nocked)
    release = anchor + np.array([-4.5, 1.0, -2.5])      # hand flies back and out after release
    return bow_full, anchor, nock, release

BW_REST_R = [-6.5, 1.5, 1.0]
def bow_shoot(t):
    bf, an, nk, rl = bow_points()
    keysL = [(0, BW_IDLE_G), (0.32, list(bf - aim_dir(0) * 1.5)), (0.8, list(bf)), (1.5, list(bf)), (1.8, BW_IDLE_G)]
    keysR = [(0, BW_REST_R), (0.32, list(nk)), (0.85, list(an), "smooth"), (1.0, list(an + np.array([0, 0, -0.2]))),
             (1.06, list(rl), "out"), (1.5, list(rl)), (1.8, BW_REST_R)]
    return bow_shot_pose(t, keysL, keysR, [(0, 0), (0.32, 1), (1.5, 1), (1.8, 0)])

def bow_skill(t):        # rapid triple shot
    bf, an, nk, rl = bow_points()
    keysL = [(0, BW_IDLE_G), (0.28, list(bf)), (1.75, list(bf)), (2.05, BW_IDLE_G)]
    keysR = [(0, BW_REST_R), (0.28, list(nk))]
    for i, t0 in enumerate((0.28, 0.73, 1.18)):
        keysR += [(t0 + 0.27, list(an)), (t0 + 0.31, list(rl), "out"), (t0 + 0.45, list(nk))]
    keysR[-1] = (1.75, list(rl)); keysR.append((2.05, BW_REST_R))
    p = bow_shot_pose(t, keysL, keysR, [(0, 0), (0.28, 1), (1.75, 1), (2.05, 0)])
    return p

# =====================================================================================================
# SPEAR (right hand holds it, the left hand is placed on the shaft by IK)
SP_IDLE_G = [-7.0, 4.0, 3.0]; SP_IDLE_D = [0.0, 1.0, 0.1]
def spear_idle(t):
    br = S(t * TAU / 3.0)
    return pose(body=[0.03, 0.05, 0], head=[S(t * TAU / 6) * 0.03, S(t * TAU / 6) * 0.2, 0], rLeg=[0, 0, -0.1, 0.12], lLeg=[0, 0, 0.1, 0.12],
                R=("ik", [SP_IDLE_G[0], SP_IDLE_G[1] + br * 0.25, SP_IDLE_G[2]], SP_IDLE_D, None),
                L=("euler", [S(t * TAU / 3) * 0.04, 0, 0.1 + br * 0.03, 0.18]))

def spear_run(t):
    p = pose(body=[0.25, 0, 0], head=[-0.22, 0, 0]); a = run_legs(t, p)
    p["body"][1] = a * 0.1
    p["R"] = ("ik", [-6.0, 3.0 + 0.4 * a, 3.0 - 0.8 * a], [0.08, 0.45, 0.89], None)
    p["L"] = ("euler", [a * 0.95, 0, 0.1, 1.45])
    return p

SP_YAW = -0.5   # stance: left shoulder forward
def sp_fwd(): return unit([S(-SP_YAW), 0.04, C(-SP_YAW)])
def spear_attack1(t):    # two hand thrust with a lunge
    a = sp_fwd(); T = [0.0, 0.28, 0.4, 0.5, 0.62, 1.0]
    g = track([(T[0], SP_IDLE_G), (T[1], [-2.5, 5.0, -2.5]), (T[2], [-2.5, 5.0, -3.2]), (T[3], [-1.0, 5.5, 3.5], "out"), (T[4], [-1.0, 5.5, 3.2]), (T[5], SP_IDLE_G)], t)
    d = dtrack([(T[0], SP_IDLE_D), (T[1], list(a + [0, 0.08, 0])), (T[3], list(a), "out"), (T[4], list(a)), (T[5], SP_IDLE_D)], t)
    st = track([(0, 0), (T[1], 1), (T[4], 1), (T[5], 0)], t)
    k = track([(0, 0), (T[2], 0), (T[3], 1, "out"), (T[4], 1), (T[5], 0)], t)
    shaft = track([(0, 7.0), (T[2], 7.0), (T[3], 4.0, "out"), (T[5], 7.0)], t)
    return pose(body=[0.05 + 0.25 * k, SP_YAW * st, 0], head=[-0.2 * k, -SP_YAW * st * 0.8, 0], z=4.0 * k,
                rLeg=lunge_r(0.4 * st + 0.6 * k), lLeg=lunge_l(0.3 * st + 0.7 * k),
                R=("ik", g, d, None), L=("shaft", float(shaft), float(st)))

def spear_attack2(t):    # two hand horizontal sweep, right -> left
    T = [0.0, 0.3, 0.42, 0.52, 0.66, 1.05]
    yaw = track([(T[0], 0.05), (T[1], -0.9), (T[2], -0.4, "in"), (T[3], 0.9, "out"), (T[4], 1.0), (T[5], 0.05)], t)
    g = track([(T[0], SP_IDLE_G), (T[1], [-4.0, 6.0, 0.0]), (T[3], [-2.0, 6.0, 3.0], "out"), (T[4], [-2.0, 6.0, 3.0]), (T[5], SP_IDLE_G)], t)
    d = dtrack([(T[0], SP_IDLE_D), (T[1], [-0.45, 0.15, 0.88]), (T[3], [0.5, -0.05, 0.86], "out"), (T[4], [0.55, -0.05, 0.83]), (T[5], SP_IDLE_D)], t)
    st = track([(0, 0), (T[1], 1), (T[4], 1), (T[5], 0)], t)
    k = track([(0, 0), (T[2], 0), (T[3], 1, "out"), (T[4], 1), (T[5], 0)], t)
    return pose(body=[0.1 + 0.1 * k, float(yaw), 0], head=[-0.1, -float(yaw) * 0.7, 0], z=1.0 * k,
                rLeg=lunge_r(0.4 * st), lLeg=lunge_l(0.5 * st + 0.3 * k), R=("ik", g, d, None), L=("shaft", 5.0, float(st)))

def spear_skill(t):      # 3 quick thrusts forward (hits 0.33 / 0.58 / 0.85)
    a = sp_fwd(); back, out = [-2.5, 5.0, -2.5], [-1.0, 5.5, 3.5]
    gk = [(0.0, SP_IDLE_G), (0.25, back), (0.33, out, "out"), (0.38, out), (0.5, back), (0.58, out, "out"), (0.63, out), (0.75, back),
          (0.85, [-0.8, 5.6, 4.0], "out"), (0.97, [-0.8, 5.6, 3.8]), (1.35, SP_IDLE_G)]
    g = track(gk, t)
    d = dtrack([(0.0, SP_IDLE_D), (0.25, list(a + [0, 0.08, 0])), (0.33, list(a), "out"), (0.97, list(a)), (1.35, SP_IDLE_D)], t)
    st = track([(0, 0), (0.25, 1), (0.97, 1), (1.35, 0)], t)
    k = track([(0, 0), (0.25, 0), (0.33, 0.7, "out"), (0.38, 0.7), (0.5, 0.35), (0.58, 0.8, "out"), (0.63, 0.8), (0.75, 0.4),
               (0.85, 1.0, "out"), (0.97, 1.0), (1.35, 0)], t)
    shaft = track([(0, 7.0), (0.25, 7.0), (0.33, 4.0, "out"), (0.38, 4.0), (0.5, 7.0), (0.58, 4.0, "out"), (0.63, 4.0), (0.75, 7.0),
                   (0.85, 4.0, "out"), (0.97, 4.0), (1.35, 7.0)], t)
    zz = track([(0, 0), (0.25, 0), (0.33, 1.8, "out"), (0.5, 1.4), (0.58, 2.6, "out"), (0.75, 2.2), (0.85, 3.8, "out"), (0.97, 3.8), (1.35, 0)], t)
    k = float(k)
    return pose(body=[0.05 + 0.25 * k, SP_YAW * float(st), 0], head=[-0.2 * k, -SP_YAW * float(st) * 0.8, 0], z=float(zz),
                rLeg=lunge_r(0.4 * float(st) + 0.6 * k), lLeg=lunge_l(0.3 * float(st) + 0.7 * k),
                R=("ik", g, d, None), L=("shaft", float(shaft), float(st)))


# =====================================================================================================
# FIST (gauntlets on both hands). Orthodox stance: left hand lead, ported from the BlockBuddy viewer's
# boxing poses (guard / jabL / crossR / hookL / punchK) so the timing matches the gym boxers.
def lerp(a, b, k): return a + (b - a) * k
def punchK(q):
    if q <= 0 or q >= 1: return 0.0
    if q < 0.28: return 1 - (1 - q / 0.28) ** 3
    if q < 0.42: return 1.0
    k = (q - 0.42) / 0.58
    return 1 - k * k * (3 - 2 * k)
def toward(arr, target, k):
    for i in range(len(target)): arr[i] = lerp(arr[i], target[i], k)

FIST_LEAD_LOW = [-0.25, -0.511, -0.141, 1.233]  # lead (left) fist low in front of the belly (IK: grip at body (3, 5.5, 5))
FIST_REAR_HIGH = [-0.604, 0.174, 0.142, 1.458]  # rear (right) fist forward at chin height (IK: grip at body (-4.5, 9, 6.5))
def fist_guard(t):
    """Boxer guard like the reference pose: rear (right) fist up beside the chin, lead (left) fist low in front."""
    bob, sway = S(t * 4.2), S(t * 2.1)
    p = dict(body=[0.1, -0.32 + sway * 0.05, sway * 0.03], head=[0.14, 0.28 - sway * 0.04, 0],
             rArm=[FIST_REAR_HIGH[0] + bob * 0.04, *FIST_REAR_HIGH[1:]], lArm=[FIST_LEAD_LOW[0] - bob * 0.04, *FIST_LEAD_LOW[1:]],
             lLeg=[-0.38, 0, -0.05, 0.35 + bob * 0.05], rLeg=[0.12, 0, 0.06, 0.4 + bob * 0.05], y=-1.1 + bob * 0.35, z=0.0)
    return p
def jabL(p, k):
    if not k: return
    toward(p["lArm"], [-1.55, 0.05, -0.08, 0.08], k); p["body"][1] -= 0.3 * k; p["head"][1] += 0.24 * k
    p["rArm"][0] -= 0.08 * k; p["z"] += 0.9 * k; p["lLeg"][3] += 0.15 * k
def crossR(p, k):
    if not k: return
    toward(p["rArm"], [-1.55, -0.05, 0.1, 0.08], k); p["body"][1] += 0.75 * k; p["head"][1] -= 0.6 * k; p["body"][0] += 0.08 * k
    p["rLeg"][0] -= 0.1 * k; p["rLeg"][1] += 0.3 * k; p["lLeg"][3] += 0.2 * k; p["z"] += 1.2 * k; p["y"] -= 0.3 * k
def hookL(p, k):
    if not k: return
    toward(p["lArm"], [0.01, -0.71, 1.53, 1.71], k); p["body"][1] += 0.6 * k; p["head"][1] -= 0.45 * k
    p["lLeg"][1] += 0.35 * k; p["lLeg"][3] += 0.2 * k; p["y"] -= 0.4 * k
def fist_pose(v):
    return pose(body=v["body"], head=v["head"], rLeg=v["rLeg"], lLeg=v["lLeg"], lift=v["y"], z=v["z"], ground=False,
                R=("euler", v["rArm"]), L=("euler", v["lArm"]))

def fist_idle(t): return fist_pose(fist_guard(t))
def fist_run(t):
    p = pose(body=[0.25, 0, 0], head=[-0.25, 0, 0]); a = run_legs(t, p)
    p["body"][1] = a * 0.12
    p["R"] = ("euler", [-0.75 - a * 0.25, 0.35, -0.5, 2.0])
    p["L"] = ("euler", [-0.75 + a * 0.25, -0.35, 0.5, 2.0])
    return p
def fist_jab(t):
    v = fist_guard(t); jabL(v, punchK((t - 0.08) / 0.45)); return fist_pose(v)
def fist_cross(t):
    v = fist_guard(t); crossR(v, punchK((t - 0.1) / 0.5)); return fist_pose(v)
HOOK_T0, HOOK_LEN = 0.98, 0.56
HIP = np.array([0.0, 12.0, 0.0])
# Left hook, keyed in PLAYER space (relative to the root, so it reads right whatever the torso twist is):
# fist cocks out to the left at shoulder height, then swings around on a level arc and lands in front of the
# face with the elbow out to the side and the forearm level, pointing inward. dy = socket +Y = fist -> elbow.
HOOK_P = [(0.09, [7.5, 20.5, 4.0]), (0.17, [2.5, 22.0, 10.0], "out"), (0.25, [2.3, 21.8, 9.8])]
HOOK_DY = [(0.09, [0.1, -0.2, -1.0]), (0.17, [0.85, 0.0, -0.5], "out"), (0.25, [0.85, 0.0, -0.5])]
HOOK_DZ = [(0.09, [0.0, 1.0, 0.1]), (0.17, [0.0, 1.0, 0.1], "out"), (0.25, [0.0, 1.0, 0.1])]

def fist_skill(t):       # jab -> cross -> left hook (fist swings around on a level arc)
    v = fist_guard(t)
    jabL(v, punchK((t - 0.10) / 0.42)); crossR(v, punchK((t - 0.45) / 0.48))
    p = fist_pose(v)
    h = t - HOOK_T0
    if 0.0 <= h <= HOOK_LEN:
        turn = float(track([(0, 0), (0.09, 0.3, "smooth"), (0.17, -0.25, "out"), (0.25, -0.25), (HOOK_LEN, 0)], h))
        p["body"][1] += turn; p["head"][1] -= turn * 0.6
        Rb = eul(p["body"])
        g0, W0 = arm_fk("L", np.asarray(v["lArm"], float))          # guard pose of this frame, player space
        P0, DY0, DZ0 = HIP + Rb @ g0, Rb @ W0[:, 1], Rb @ W0[:, 2]
        P = track([(0.0, P0), *HOOK_P, (HOOK_LEN, P0)], h)
        DY = dtrack([(0.0, DY0), *HOOK_DY, (HOOK_LEN, DY0)], h)
        w = float(track([(0, 0), (0.06, 1), (HOOK_LEN - 0.12, 1), (HOOK_LEN, 0)], h))   # ease in / out of the guard arm
        p["L"] = ("ikblend", Rb.T @ (P - HIP), None, Rb.T @ DY, list(v["lArm"]), w)
        k = float(track([(0, 0), (0.17, 1, "out"), (0.25, 1), (HOOK_LEN, 0)], h))
        p["lift"] -= 0.4 * k; p["lLeg"][1] -= 0.35 * k; p["lLeg"][3] += 0.15 * k; p["rLeg"][3] += 0.1 * k
    return p

# =====================================================================================================
CLIPS = [  # name, function, length (s), loop, folder
    ("Sword_Idle", sword_idle, 3.0, True, "Sword"), ("Sword_Run", sword_run, TAU / 9, True, "Sword"),
    ("Sword_Attack1", sword_attack1, 0.9, False, "Sword"), ("Sword_Attack2", sword_attack2, 0.95, False, "Sword"),
    ("Sword_Skill", sword_skill, 1.3, False, "Sword"),
    ("Bow_Idle", bow_idle, 3.0, True, "Bow"), ("Bow_Run", bow_run, TAU / 9, True, "Bow"),
    ("Bow_Shoot", bow_shoot, 1.8, False, "Bow"), ("Bow_Skill", bow_skill, 2.05, False, "Bow"),
    ("Spear_Idle", spear_idle, 3.0, True, "Spear"), ("Spear_Run", spear_run, TAU / 9, True, "Spear"),
    ("Spear_Attack1", spear_attack1, 1.0, False, "Spear"), ("Spear_Attack2", spear_attack2, 1.05, False, "Spear"),
    ("Spear_Skill", spear_skill, 1.35, False, "Spear"),
    ("Fist_Idle", fist_idle, TAU / 2.1, True, "Fist"), ("Fist_Run", fist_run, TAU / 9, True, "Fist"),
    ("Fist_Jab", fist_jab, 0.7, False, "Fist"), ("Fist_Cross", fist_cross, 0.8, False, "Fist"),
    ("Fist_Skill", fist_skill, 1.8, False, "Fist"),
]

def solve_clip(f, L, loop):
    n = max(1, round(L * FPS)); frames = []; prev = {"R": None, "L": None}; info = []
    for i in range(n + 1):
        t = i / n * L; p = f(t)
        q = {}
        for side in ("R", "L"):
            spec = p[side]
            if spec[0] == "euler":
                q[side] = np.asarray(spec[1], float)
            elif spec[0] == "ik":
                q[side] = arm_ik(side, spec[1], None if spec[2] is None else unit(spec[2]), None if len(spec) < 4 or spec[3] is None else unit(spec[3]), prev[side])
            elif spec[0] == "ikblend":   # ("ikblend", target, dz, dy, euler_base, w): IK result blended with an euler pose
                qi = arm_ik(side, spec[1], None if spec[2] is None else unit(spec[2]), None if spec[3] is None else unit(spec[3]), prev[side])
                base = np.asarray(spec[4], float)
                for j in range(3):
                    while qi[j] - base[j] > math.pi: qi[j] -= TAU
                    while qi[j] - base[j] < -math.pi: qi[j] += TAU
                q[side] = base * (1 - spec[5]) + qi * spec[5]
        if p["L"][0] == "shaft":     # left hand on the spear shaft, blended from a relaxed arm by weight
            gR, WR = arm_fk("R", q["R"])
            target = gR + WR[:, 2] * p["L"][1]
            qs = arm_ik("L", target, None, None, prev["L"])
            q["L"] = np.asarray(RELAX_ARM_L) * (1 - p["L"][2]) + qs * p["L"][2] if prev["L"] is not None or True else qs
            info.append(float(np.linalg.norm(arm_fk("L", qs)[0] - target)))
        for side in ("R", "L"):
            # keep Euler continuity with the previous frame
            if prev[side] is not None:
                for j in range(3):
                    while q[side][j] - prev[side][j] > math.pi: q[side][j] -= TAU
                    while q[side][j] - prev[side][j] < -math.pi: q[side][j] += TAU
            prev[side] = q[side].copy()
        y = 0.0
        if p["ground"]:
            y = -min(foot_low("R", p["rLeg"]), foot_low("L", p["lLeg"]))
        y += p["lift"]
        frames.append([*p["body"], *p["head"], *q["R"], *q["L"], *p["rLeg"], *p["lLeg"], y, p["z"], p["ry"], p["x"], p["rx"]])
    frames = np.array(frames, float)
    if loop:   # same seam fix as poses2.js
        k = np.linspace(0, 1, len(frames))[:, None]
        frames = frames + k * (frames[0] - frames[-1])
    return frames, info

RELAX_ARM_L = [0.0, 0.0, 0.1, 0.18]

def residuals(f, L):
    """max grip error of the IK targets (px) - used to check reach."""
    n = max(1, round(L * FPS)); worst = 0
    prev = {"R": None, "L": None}
    for i in range(n + 1):
        p = f(i / n * L)
        for side in ("R", "L"):
            spec = p[side]
            if spec[0] == "ik":
                q = arm_ik(side, spec[1], None if spec[2] is None else unit(spec[2]), None if len(spec) < 4 or spec[3] is None else unit(spec[3]), prev[side])
                prev[side] = q
                worst = max(worst, float(np.linalg.norm(arm_fk(side, q)[0] - np.asarray(spec[1]))))
    return worst

if __name__ == "__main__":
    only = sys.argv[3].split(",") if len(sys.argv) > 3 else None
    out = []
    for name, f, L, loop, folder in CLIPS:
        if only and name not in only: continue
        fr, info = solve_clip(f, L, loop)
        out.append({"name": name, "loop": loop, "fps": FPS, "frames": fr.round(5).tolist(), "folder": folder})
        print(f"{name}: {len(fr)} frames, loop={loop}, worst grip err={residuals(f, L):.2f}px" + (f", shaft err max={max(info):.2f}" if info else ""))
    json.dump(out, open(sys.argv[1], "w"))
