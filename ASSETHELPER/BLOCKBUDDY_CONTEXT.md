# BlockBuddy — ngữ cảnh để tiếp tục ở chat khác

Đính kèm file này + `BlockBuddy_Source.zip` vào đầu chat mới. Nếu làm tiếp phần Unity thì kèm `BlockBuddy_Unity.zip`, `BlockKit_Unity.zip` và `BLOCKKIT_UNITY_TASK.md`.

## Người dùng
Ducnb, iKame Games. Làm game/web. Trả lời ngắn gọn, tiếng Việt, tên file/code giữ tiếng Anh.

## BlockBuddy là gì
Nhân vật khối hộp kiểu Minecraft (không dùng chữ "Minecraft" trong tên asset), dùng skin PNG 64×64 chuẩn, có rig 2 khớp (vai/hông + khuỷu/gối gập mượt) và một hệ pose/animation bằng code. Có 3 dạng đầu ra, cùng một nguồn:
1. **Skin Viewer** (trang web three.js, artifact "Minecraft Skin Viewer"): import skin (chọn file / kéo thả / dán), classic/slim tự nhận, lớp phủ ngoài bật tắt, 13 anim gốc (9 cơ bản + 4 boxing), xoay/zoom camera.
2. **Gói Unity `BlockBuddy`**: `Models/BlockBuddy.fbx` (rig Generic 20 xương: Root/Hips/Spine/Head, UpperArm→Elbow→LowerArm→Hand, UpperLeg→Knee→LowerLeg→Foot; tay 4 px) + `Animations/Base` (9 clip) + `Animations/Gym` (12 clip), mỗi clip 1 FBX, cùng hierarchy `BlockBuddy/Rig/Root/...` với model. Shader `BlockBuddy/Skin` (cutout, 2 mặt, Built-in + URP). Người dùng đặt tại `Assets/_Game/3D Assets/BlockBuddy`.
3. **Mã nguồn** (`BlockBuddy_Source.zip`): `character/skin-viewer.src.html` là định nghĩa chuẩn của hình khối, rig và mọi hàm pose; `pipeline/poses2.js` → poses.json; `pipeline/build3.py` (Blender headless) → FBX model và clip; `check2.py` kiểm tra bằng render.

## Quy ước kỹ thuật
- Pose `{ body[3], head[3], rArm[4], lArm[4], rLeg[4], lLeg[4], x, y, z, rx, ry }`, đơn vị pixel skin (1 px = 1/16 m, nhân vật 2 m). limb[3] = góc gập khuỷu/gối. Anim = hàm theo thời gian hoặc keyframe (`keyed`); chuyển tiếp = `cross` (crossfade) hoặc `concat`. Xuất 30 fps; cờ loop theo clip.
- Nhân vật quay mặt +Z, tay phải −X (three.js). three.js (x,y,z) → Blender (x,−z,y) → FBX forward −Z, up Y. Root empty xuất rotation 0, child Body/Rig mang −90° X. Unity giả định import lật X (unity = (−x, y, z), yaw = −yaw).
- Clip chuyển tiếp chỉ làm khi tư thế khác hẳn (nằm xuống ghế, cúi nhặt tạ); Idle ↔ boxing/chạy dùng crossfade.

## Lỗi Unity đã gặp và cách sửa (giữ nguyên khi làm tiếp)
- Lớp phủ ngoài hiện đặc → material phải cutout (shader `BlockBuddy/Skin`).
- Anim không chạy → model và mọi file clip phải có cùng hierarchy gốc (`BlockBuddy/Rig/...`).
- Model nằm ngang → root rotation 0 bằng cách xoay sẵn root empty, không dựa vào Bake Axis Conversion.

## BlockKit (bộ lắp cảnh đi cùng BlockBuddy) và ví dụ BlockGym
**BlockKit** là gói Unity dùng chung cho mọi cảnh khối hộp: `Models/` (model FBX riêng lẻ, model nhiều phần giữ nguyên bộ với object con có pivot riêng), `Layouts/*.json` (mỗi cảnh một file: placement + spawn, toạ độ `unity` và `fbx`), `Editor/BlockKitBuilder.cs` (menu `BlockKit > Build Layout Prefabs` → `Prefabs/<root>.prefab`), script runtime `BlockKitScene` / `BlockKitActor` (vai + vòng lặp, PlayableGraph) / `BlockKitBag`, material palette/gỗ/gạch, shader `BlockKit/Flat`.
**BlockGym** chỉ là cảnh ví dụ đầu tiên: phòng gym boxing isometric (trang web artifact "Block Gym") với 8 BlockBuddy tập (2 đấu, 2 đấm bao, 1 đẩy tạ, 1 tạ đơn, 2 chạy máy) → `Layouts/BlockGym_Layout.json` (30 placement + 8 spawn) và 25 model hiện có trong Models/. Nguồn ở `examples/BlockKit_BlockGym/` trong zip; cách làm (defModel / place / actor → export JSON → build_gym.py) dùng cho cảnh mới.

## Chưa xác minh
Toàn bộ phần Unity (C#, prefab, .meta tối giản, dấu lật trục) chưa chạy thử trong Unity; hình học và clip đã kiểm tra bằng import ngược vào Blender và render (khớp bản web). Skin OldMan là tay slim nên không có trong gói Unity.
