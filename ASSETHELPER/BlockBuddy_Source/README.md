# BlockBuddy — mã nguồn

BlockBuddy là nhân vật khối hộp (rig 2 khớp, skin 64×64) + hệ pose/animation. Mọi FBX và clip Unity đều SINH từ đây; sửa nguồn rồi chạy lại, không sửa FBX tay.
`examples/BlockKit_BlockGym` là cảnh ví dụ (phòng gym) dựng bằng BlockKit — bộ model/layout/script lắp cảnh khối hộp đi cùng BlockBuddy — không phải phần lõi.

## Lõi (character/ + pipeline/)
- `character/skin-viewer.src.html` — nguồn gốc của nhân vật: hình khối + UV theo layout skin, rig (vai/hông + khuỷu/gối gập mượt), và TẤT CẢ hàm pose/anim gốc (idle, walk, run, sneak, wave, jump, attack, dance, sit, guard + các cú đấm). Đây là "định nghĩa chuẩn", các nơi khác copy từ đây.
- `pipeline/poses2.js` — lấy hàm pose từ viewer (và từ examples/BlockKit_BlockGym/gym_chars.js cho các clip gym) → `poses.json` (30 fps, cờ loop, folder Base/Gym).
  Muốn thêm clip: thêm hàm pose, thêm 1 dòng vào danh sách trong poses2.js.
- `pipeline/build3.py` — Blender headless:
  `python build3.py model <skin.png> classic <out.fbx> <material_name>`  → BlockBuddy.fbx (mesh + rig, không anim)
  `python build3.py anims poses.json classic <Animations dir>`           → mỗi clip 1 FBX, chia Base/Gym
- `pipeline/check2.py` — import ngược model + 1 clip vào Blender, chụp từng tư thế để kiểm tra. `dump.py` / `dump2.py` đọc cấu trúc và transform thô trong FBX.
- `skins/` — 3 skin mẫu (OldMan là tay slim 3 px, hai skin còn lại tay classic 4 px; gói Unity dùng classic).

## Pose (đơn vị pixel skin, 1 px = 1/16 m)
`{ body[3], head[3], rArm[4], lArm[4], rLeg[4], lLeg[4], x, y, z, rx, ry }` — mỗi chi: góc Euler XYZ + [3] = góc gập khuỷu/gối (khuỷu gập ra trước, gối gập ra sau). Root của nhân vật ở chân, quay mặt +Z, tay phải ở −X. Anim = hàm theo thời gian hoặc keyframe (`keyed`), chuyển tiếp = `cross` (crossfade) hoặc `concat`.

## Hệ trục
three.js (x, y, z) → Blender (x, −z, y) → FBX forward −Z, up Y (FBX_SCALE_ALL). Root empty xuất với rotation 0 (được xoay sẵn CONV⁻¹), child Body/Rig mang −90° X. Unity: giả định import lật X → unity = (−x, y, z), yaw = −yaw.

## Môi trường
Python 3.13 + `pip install bpy==5.2.3 pillow`; Node 22 + playwright (Chromium có sẵn); three.js r128 lấy từ `npm pack three@0.128.0` → `vendor/three.min.js` (trang web tải từ cdnjs, khi chạy headless thì route sang file local, xem examples/BlockKit_BlockGym/gymshot.js).

## examples/BlockKit_BlockGym (ví dụ cảnh BlockKit)
`gym.src.html` + `gym_props.js` (model/placement/part đồ đạc) + `gym_chars.js` (nhân vật + anim gym) → `buildgym.py` ghép thành trang; `exportscene.js` → `gym_scene.json`; `make_palette.py` → texture; `build_gym.py` → 25 FBX (material BlockKit_*) + `Layouts/BlockGym_Layout.json`; `render_layout.py` lắp lại phòng từ FBX + JSON để kiểm tra. Gói Unity tương ứng là `BlockKit` (Models, Layouts, Editor/BlockKitBuilder, Scripts). Cách làm này dùng lại được cho bất kỳ cảnh khối hộp nào: định nghĩa model bằng `defModel`, đặt bằng `place`, nhân vật bằng `actor`.
