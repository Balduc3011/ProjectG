# BlockBuddyWeapons — mã nguồn

Sinh ra gói Unity `Assets/_Game/3D Assets/BlockBuddyWeapons` (19 clip + 6 model vũ khí; găng Gauntlet_R/L dựng từ hộp trong `weapons_def.py` GAUNTLET_R). Sửa nguồn rồi chạy lại, không sửa FBX tay.
Cần `BlockBuddy_Source/pipeline/build3.py` (copy thư mục `pipeline/` và `skins/` cạnh các file này) — rig/clip dùng đúng pipeline của BlockBuddy.

- `weapons_def.py` — 3 sprite (sword/bow 16×16, spear 32×32, arrow tự vẽ) → voxel trong không gian socket: pivot = điểm cầm, +Z = mũi / cánh cung trên, X = độ dày. Bảng `WEAPONS`: kích thước voxel (px skin), điểm cầm, trục.
- `weapons_build.py` — Blender headless → `Models/Weapon_*.fbx` (root empty rotation 0 → `Weapon_X_Mesh`, material `BlockBuddyWeapons_Mat`).
  `python weapons_build.py sprites <out>/Models sprites/BlockBuddyWeapons_Atlas.png`
- `anims_weapons.py` — định nghĩa 14 clip. Tay được điều khiển bằng IK (scipy): mục tiêu là vị trí điểm cầm + hướng vũ khí trong không gian thân; chân/thân/đầu theo keyframe; tự đặt chân chạm đất. Xuất `poses_weapons.json` (format giống poses2.js).
  `python anims_weapons.py poses_weapons.json` rồi `cd pipeline && python build3.py anims ../poses_weapons.json classic <out>/Animations`
- `preview.py` — render contact sheet từng clip kèm vũ khí (Workbench) → `previews/`. `verify_fbx.py` — import ngược BlockBuddy.fbx + clip FBX + vũ khí để kiểm tra.

Quy ước (giữ nguyên): socket = con của Hand.R/L, tạo ở bind pose, vị trí = Hand + 1 px lên trên, hướng = hướng nhân vật. Mũi tên không gắn tay mà đặt từ tay phải tới tay cung (+0.12 m lên trên).
Môi trường: Python 3.13, `pip install bpy==5.2.3 pillow scipy`.
