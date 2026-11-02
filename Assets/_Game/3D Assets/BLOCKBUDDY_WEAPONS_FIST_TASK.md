# BlockBuddy Weapons: thêm vũ khí Fist (găng đấm) cho Claude Code (Unity MCP)

Đây là phần **mở rộng** của `BLOCKBUDDY_WEAPONS_TASK.md` (hệ thống Sword / Bow / Spear + scene demo đổi vũ khí mỗi 30 s).

- **Điều kiện:** làm xong file đó trước. Nếu chưa xong, làm file đó trước, rồi mới làm file này.
- **Nhiệm vụ:** thêm vũ khí thứ 4 là **Fist**, gồm 2 găng đấm, mỗi tay một chiếc. Đưa Fist vào hệ thống và vào scene demo.

Ngôn ngữ trả lời: tiếng Việt. Tên file, tên object, code: giữ nguyên tiếng Anh.

---

## 1. File mới / đã thay (đã có trong project)

```
Assets/_Game/3D Assets/BlockBuddyWeapons/
  Animations/Fist/BlockBuddy@Fist_Idle.fbx    MỚI
  Animations/Fist/BlockBuddy@Fist_Run.fbx     MỚI
  Animations/Fist/BlockBuddy@Fist_Jab.fbx     MỚI
  Animations/Fist/BlockBuddy@Fist_Cross.fbx   MỚI
  Animations/Fist/BlockBuddy@Fist_Skill.fbx   MỚI
  Models/Weapon_Gauntlet_R.fbx                MỚI  găng tay phải
  Models/Weapon_Gauntlet_L.fbx                MỚI  găng tay trái (bản đối xứng)
  Textures/BlockBuddyWeapons_Atlas.png        THAY  thêm 5 pixel màu cho găng ở hàng y = 16, x = 16..20; phần cũ giữ nguyên
  Data/BlockBuddyWeapons_Clips.json           THAY  thêm weapons.Fist, 5 clip Fist, thêm trường hitHands và weapons.<X>.models
  README.txt                                  THAY
```

`BlockBuddyWeaponsImport.cs` không đổi và đã xử lý được các file mới:

- `Fist_Idle` / `Fist_Run` lặp, 3 clip còn lại không lặp.
- Găng dùng chung material `BlockBuddyWeapons_Mat`. Texture atlas giữ GUID cũ nên không cần gán lại.

---

## 2. Clip Fist

Thế đứng orthodox: tay trái là tay trước (jab, hook), tay phải là tay sau (cross).

| Clip | Dài (s) | Lặp | Mô tả | Hit (s) / tay |
|---|---|---|---|---|
| `Fist_Idle` | 3.0 | có | thế thủ boxer: tay phải nâng trước cằm, tay trái hạ thấp trước bụng, nhún nhẹ | |
| `Fist_Run` | 0.7 | có | chạy, hai găng giữ trước ngực | |
| `Fist_Jab` | 0.7 | không | đấm thẳng tay trái (attack thường 1) | 0.21 / L |
| `Fist_Cross` | 0.8 | không | đấm thẳng tay phải, xoay hông (attack thường 2) | 0.24 / R |
| `Fist_Skill` | 1.8 | không | combo jab → cross → móc trái (left hook: khuỷu nâng ngang vai, cẳng tay nằm ngang, nắm đấm vòng từ ngoài vào trước mặt) | 0.22 / L, 0.58 / R, 1.15 / L |

- **Đọc số liệu từ JSON:** lấy từ `Data/BlockBuddyWeapons_Clips.json`, gồm `clips[].length/loop/hits/hitHands` và `weapons.Fist`. Không gõ tay.
- **Ý nghĩa `hitHands[i]`:** cho biết hit thứ i do găng nào đánh trúng (`R` = Socket_R, `L` = Socket_L).
- **Crossfade:** mọi clip đấm bắt đầu và kết thúc ở thế thủ của `Fist_Idle`, nên crossfade qua lại không bị giật.

---

## 3. Sửa hệ thống

1. **`WeaponType`:** thêm `Fist`.
2. **`WeaponData`:** thay field `model` (một model) bằng hai field `GameObject modelRight` và `GameObject modelLeft`, được phép để trống.
   - Thêm `Hand.Both` nếu enum `Hand` đang có.
   - Cập nhật Sword / Bow / Spear: kiếm và thương dùng `modelRight`, cung dùng `modelLeft`.
   - `WeaponClip` thêm `char[] hitHands` (hoặc `Hand[]`).
3. **`WeaponHolder.Equip(data)`:**
   - Huỷ cả hai model cũ.
   - Instantiate `modelRight` vào `Socket_R` và `modelLeft` vào `Socket_L` (local 0 / identity / scale 1), hiệu ứng pop như cũ.
   - Găng dựng sẵn trong không gian socket nên **không cần offset**: pivot là điểm cầm trong nắm tay, phần cổ găng kéo lên cẳng tay.
4. **HitSpark:**
   - Vị trí theo `hitHands`. Với Fist: `socket.position - socket.up * 0.27`, tức mặt đốt ngón tay của găng.
   - Sword / Spear giữ cách tính cũ ở tay phải.
5. **`BlockBuddyWeapons > Create Weapon Data`:**
   - Tạo thêm `Data/WeaponData_Fist.asset`: idle, run, attacks = [Fist_Jab, Fist_Cross], skill = Fist_Skill.
   - Đọc model theo `weapons.<X>.models.right/left` trong JSON.
   - Chạy lại để cập nhật cả 3 asset cũ theo field mới.
6. **Routine** dùng chung như cũ: Idle → Run → attacks[0] → attacks[1] → Idle → skill → Idle. Với Fist, Run vẫn chạy vòng tròn 1.2 m. Thông số fade giữ nguyên.
7. **Thứ tự đổi vũ khí:** `Sword → Bow → Spear → Fist → Sword ...`, vẫn 30 s một lần, chờ hết đòn đang đánh mới đổi. Đổi từ Fist sang vũ khí 1 tay phải huỷ cả găng bên tay còn lại.

---

## 4. Scene demo (`Assets/_Game/3D Assets/New Scene.unity`)

- **Thêm `Fighter_4`:** cấu hình giống 3 fighter cũ, `startWeapon = Fist`, `startOffset = 2.1`. Nhờ vậy lúc nào cũng có đủ 4 loại vũ khí.
- **Vị trí 4 fighter:** x = -4.5 / -1.5 / 1.5 / 4.5, z = 0.
- **Skin:** luân phiên GreenJacket / CyanShirt.
- **Camera:** position `(9, 6.5, 11)`, nhìn về `(0, 1, 0)`, FOV 35.
- Cập nhật menu `Build Demo Scene` cho khớp, rồi lưu scene.

---

## 5. Kiểm tra (Play mode ≥ 70 s)

1. **Import:**
   - 5 clip Fist mỗi file đúng 1 clip, tên đúng, loop đúng bảng 2.
   - `Weapon_Gauntlet_R/L` có material không xám, không hồng.
   - Atlas vẫn Point, không nén.
2. **`Fighter_4` lúc 0 s:**
   - Đeo găng ở **cả hai** tay.
   - Tay phải nâng trước cằm, tay trái hạ trước bụng.
   - Găng không lệch khỏi cẳng tay, mặt đốt ngón (các thanh màu đồng) nằm ở đầu nắm tay.
3. **Đòn đánh:**
   - Jab duỗi thẳng tay trái về trước.
   - Cross duỗi thẳng tay phải, hông xoay.
   - Skill ra đủ 3 đòn jab → cross → móc trái, có spark ở đúng găng tại mỗi hit.
4. **Đổi vũ khí:**
   - **Giây ~30:** Fighter_3 (Spear) đổi sang Fist, cây thương biến mất, 2 găng pop lên. Fighter_4 (Fist) đổi sang Sword, cả 2 găng biến mất, kiếm xuất hiện ở tay phải.
   - **Giây ~60:** lặp lại theo vòng.
   - Không còn sót găng hay vũ khí cũ trên tay.
5. **Ổn định:** không có frame giật hay T-pose khi chuyển Idle ↔ Run ↔ đấm. Không có log lỗi lặp lại mỗi frame.

**Sửa lỗi** và ghi rõ đã sửa gì. Các lỗi hay gặp:

- **Găng xoay lệch 90° hoặc nằm ngang trên tay:** socket không được tạo ở bind pose (xem mục 6 file gốc).
- **Găng đổi bên** (găng có vây đồng phía trong thay vì phía ngoài): gán nhầm `Weapon_Gauntlet_R` ↔ `_L`.

---

## 6. Báo cáo

- Pass/Fail cho từng mục ở phần 5.
- Danh sách file đã sửa / tạo.
- Ảnh Game view lúc ~5 s và ~35 s.

## 7. Không nên làm

Như file gốc. Ngoài ra:

- Không dùng một model găng cho cả hai tay bằng cách scale X = -1. Đã có sẵn bản L.
- Không sửa keyframe trong Unity. Cần đổi động tác thì báo lại để sinh lại từ `ASSETHELPER/BlockBuddyWeapons_Source`.
