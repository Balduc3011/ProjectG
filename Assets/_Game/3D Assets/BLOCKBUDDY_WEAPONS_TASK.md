# BlockBuddy Weapons: nhiệm vụ cho Claude Code (Unity MCP)

Gói animation vũ khí `BlockBuddyWeapons` (Kiếm / Cung / Thương) đã được dựng sẵn bằng Blender headless và nằm trong project. Nhiệm vụ: dựng **hệ thống animation theo vũ khí** (data-driven, không Animator Controller) và **scene demo**. Scene có 3 BlockBuddy, mỗi người cầm một loại vũ khí và cứ 30 giây đổi sang vũ khí khác, để kiểm tra việc đổi bộ animation có mượt hay không.

Ngôn ngữ trả lời: tiếng Việt. Tên file, tên object, code: giữ nguyên tiếng Anh.

---

## 1. Đầu vào (đã có sẵn trong project)

```
Assets/_Game/3D Assets/
  BlockBuddy/                      nhân vật (ĐÃ CÓ, KHÔNG SỬA): Models/BlockBuddy.fbx, Materials/BlockBuddy_Skin.mat, Textures/Skin_*.png
  BlockBuddyWeapons/               gói mới
    Animations/Sword/BlockBuddy@Sword_*.fbx   5 clip
    Animations/Bow/BlockBuddy@Bow_*.fbx       4 clip
    Animations/Spear/BlockBuddy@Spear_*.fbx   5 clip
    Models/Weapon_Sword.fbx, Weapon_Bow.fbx, Weapon_Spear.fbx, Weapon_Arrow.fbx
    Materials/BlockBuddyWeapons_Mat.mat       shader BlockBuddy/Skin, texture atlas
    Textures/BlockBuddyWeapons_Atlas.png      64x32 pixel art
    Data/BlockBuddyWeapons_Clips.json         độ dài clip, loop, thời điểm sự kiện (đọc file này, đừng gõ tay số liệu)
    Editor/BlockBuddyWeaponsImport.cs         AssetPostprocessor (tự chạy khi import)
    README.txt
  New Scene.unity                  scene trống (Main Camera + Directional Light, URP) → dùng làm scene demo
```

`BlockBuddyWeaponsImport.cs` nhận diện theo đoạn đường dẫn `/BlockBuddyWeapons/`. Script này chỉ áp dụng ở **lần import đầu**. Nếu Unity đã import gói trước khi script compile xong, chuột phải thư mục `BlockBuddyWeapons` → **Reimport**.

---

## 2. Nội dung gói

### 2.1 Clip

Mọi clip có **cùng hierarchy** với `BlockBuddy.fbx` (`BlockBuddy/Rig/Root/Hips/...`, rig Generic 20 xương), nên phát trực tiếp lên nhân vật hiện có. Mỗi FBX chứa đúng 1 clip, tên clip = phần sau `@`. Các clip một lần **bắt đầu và kết thúc ở tư thế Idle của chính vũ khí đó**, nên crossfade về Idle không bị giật.

| Vũ khí (tay) | Clip | Dài (s) | Lặp | Sự kiện (s, tính từ đầu clip) |
|---|---|---|---|---|
| Sword (phải) | `Sword_Idle` | 3.0 | có | thế thủ, kiếm dựng trước hông phải |
| | `Sword_Run` | 0.7 | có | chạy, kiếm chĩa về phía trước (hơi chếch lên) |
| | `Sword_Attack1` | 0.9 | không | chém chéo từ trên-phải xuống dưới-trái, hit 0.40 |
| | `Sword_Attack2` | 0.93 | không | chém ngang trái → phải, hit 0.41 |
| | `Sword_Skill` | 1.3 | không | combo 2 chém: chém hất từ dưới-trái lên trên-phải (hit 0.36), rồi chém bổ từ trên-phải xuống dưới-trái (hit 0.76) |
| Bow (trái) | `Bow_Idle` | 3.0 | có | cung cầm thấp bên trái |
| | `Bow_Run` | 0.7 | có | |
| | `Bow_Shoot` | 1.8 | không | xoay người ngang, giương, kéo, bắn 1 mũi. Mũi tên: nock 0.32 → release 1.06 |
| | `Bow_Skill` | 2.03 | không | bắn liên 3 mũi. Mũi tên: [0.28→0.59], [0.72→1.03], [1.17→1.48] |
| Spear (phải, tay trái đặt lên cán) | `Spear_Idle` | 3.0 | có | thương dựng đứng bên phải |
| | `Spear_Run` | 0.7 | có | thương chĩa chéo lên trước |
| | `Spear_Attack1` | 1.0 | không | đâm thẳng có lao người, hit 0.50 |
| | `Spear_Attack2` | 1.07 | không | quét ngang phải → trái, hit 0.53 |
| | `Spear_Skill` | 1.33 | không | đâm nhanh 3 phát về trước, phát cuối lao mạnh nhất, hit 0.33 / 0.58 / 0.85 |

Số liệu trên chỉ để tham khảo. Code phải đọc từ `Data/BlockBuddyWeapons_Clips.json` (các trường `clips[].length/loop/hits/arrows`, `weapons.<Type>.hand/idle/run/attacks/skill`).

- **Xương `Root`** mang dịch chuyển thân người (lao tới, nhảy) và xoay toàn thân (Bow_Shoot xoay ngang). Đây là chuyển động trong clip, **không dùng root motion**: `animator.applyRootMotion = false`, transform nhân vật đứng yên.
- **Loop** do importer đặt: `*_Idle` và `*_Run` lặp, các clip còn lại không lặp.

### 2.2 Cầm vũ khí: socket

Model vũ khí đã được dựng trong **không gian socket**: pivot là điểm cầm trong nắm tay, +Z là hướng mũi kiếm/mũi thương, hoặc cánh trên của cung. Socket tạo **một lần, ở tư thế bind**, tức là ngay sau khi Instantiate, trước khi PlayableGraph chạy frame nào:

```csharp
Transform hand = FindDeep(character, side == Hand.Right ? "Hand.R" : "Hand.L");
var socket = new GameObject(side == Hand.Right ? "Socket_R" : "Socket_L").transform;
socket.SetParent(hand, false);
socket.position = hand.position + character.up * 0.0625f;   // 1 px skin, vào giữa nắm tay
socket.rotation = character.rotation;                       // cùng hướng nhân vật lúc bind
// vũ khí: Instantiate(Weapon_X.fbx, socket) với localPosition = 0, localRotation = identity, localScale = 1
```

Nhân vật tạo cả hai socket. Kiếm và thương vào `Socket_R`, cung vào `Socket_L`.

**Mũi tên** không gắn vào tay. Trong cửa sổ `nock → release` của clip cung, mỗi `LateUpdate` đặt `Weapon_Arrow` như sau:

```csharp
Vector3 up = character.up * 0.12f;                       // nhấc lên trên nắm tay, không chui vào cánh tay
Vector3 nock = socketR.position + up, rest = socketL.position + up;
arrow.position = nock; arrow.rotation = Quaternion.LookRotation(rest - nock, character.up);
```

Đến thời điểm `release`: ẩn mũi tên trên tay, sinh một mũi tên bay tại `rest`, hướng `(rest - nock)`, tốc độ 18 m/s, có chút trọng lực, tự huỷ sau 2 s.

---

## 3. Kiến trúc hệ thống

Ý tưởng: **một bộ phát clip chung, đổi vũ khí là đổi bộ dữ liệu clip**. Đây là cùng ý tưởng với Animator Override Controller nhưng làm bằng Playables, giống `BlockKitActor` đã có. Không tạo Animator Controller.

Tạo các file trong `Assets/_Game/3D Assets/BlockBuddyWeapons/Scripts/` (runtime) và `.../Editor/` (editor):

| File | Vai trò |
|---|---|
| `WeaponData.cs` | `ScriptableObject`: `WeaponType type` (Sword/Bow/Spear), `Hand hand` (Right/Left), `GameObject model`, `WeaponClip idle, run`, `WeaponClip[] attacks`, `WeaponClip skill`. `WeaponClip` = `{ AnimationClip clip; float[] hits; Vector2[] arrows (x=nock, y=release); }`. |
| `BlockBuddyAnimPlayer.cs` | Bọc PlayableGraph: `AnimationMixerPlayable` 2 input, crossfade theo smoothstep (giống `BlockKitActor.Play`). API: `Play(AnimationClip clip, float fade)`, `float Time` (thời gian của clip đang phát), `float Length`, `bool IsDone` (clip không lặp đã hết). Clip lặp thì để `SetTime` tự chạy, clip không lặp thì khi hết giữ frame cuối. `SetApplyFootIK(false)`. Huỷ graph trong `OnDestroy`. |
| `WeaponHolder.cs` | Tạo `Socket_R/L` ở bind pose (mục 2.2). `Equip(WeaponData)`: huỷ model cũ, Instantiate model mới vào đúng socket, hiệu ứng scale pop 0 → 1 trong 0.2 s. Quản lý mũi tên tay và mũi tên bay. Phát `HitSpark` tại đầu vũ khí (`socket.position + socket.forward * 0.9` với kiếm, `1.3` với thương): 5 khối nhỏ bắn ra, tự huỷ sau 0.4 s. |
| `BlockBuddyFighter.cs` | "Não" demo: chạy routine theo vũ khí hiện tại, đổi vũ khí mỗi 30 s, gọi sự kiện hit/arrow theo `WeaponClip`. |
| `FighterLabel.cs` | `TextMesh` nổi trên đầu, luôn quay về camera. Dòng 1: tên vũ khí. Dòng 2: tên clip đang phát. Dòng 3: đếm ngược tới lần đổi vũ khí (`swap in 12s`). |
| `Editor/BlockBuddyWeaponsSetup.cs` | Menu `BlockBuddyWeapons > Create Weapon Data`: đọc `Data/BlockBuddyWeapons_Clips.json`, tạo/ghi đè `Data/WeaponData_Sword.asset`, `_Bow`, `_Spear`. Clip lấy bằng `AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"))`. Menu `BlockBuddyWeapons > Build Demo Scene`: dựng scene ở mục 4. |

### 3.1 Routine của `BlockBuddyFighter`

Mỗi vũ khí có một vòng lặp, lấy clip từ `WeaponData`:

```
Idle 2.0s → Run 2.5s (chạy 1 vòng tròn bán kính 1.2 m quanh điểm gốc, mặt theo tiếp tuyến, kết thúc đúng điểm gốc)
→ attacks[0] → attacks[1] (Bow: Bow_Shoot 2 lần) → Idle 1.0s → skill → Idle 1.5s → lặp lại
```

- **Thời gian bước:** bước clip một lần kéo dài đúng `clip.length` của clip đó.
- **Fade:** Idle ↔ Run 0.25 s; vào attack/skill 0.1 s; từ attack/skill về Idle 0.2 s, bắt đầu fade khi còn 0.2 s cuối clip. Giữa hai attack liền nhau dùng fade 0.1 s.
- **Sau Run:** quay mặt về hướng gốc (slerp 0.3 s trong lúc fade sang Idle).
- **Sự kiện:** mỗi frame so `Time` trước/sau với `hits[]` và `arrows[]`. Không dùng AnimationEvent trong clip vì clip được sinh lại từ nguồn.

### 3.2 Đổi vũ khí (điểm chính của demo)

- **Thứ tự:** `Sword → Bow → Spear → Sword ...`. Đồng hồ `swapInterval = 30 s` chạy riêng cho từng nhân vật.
- **Thời điểm đổi:** khi đồng hồ ≥ 30 s, **chờ** tới khi đang ở bước Idle hoặc Run, không cắt ngang attack/skill.
- **Cách đổi:**
  1. `Play(newData.idle.clip, 0.3f)`.
  2. Ở giây 0.12 của fade, gọi `holder.Equip(newData)` (model cũ biến mất, model mới pop lên ở đúng socket: kiếm/thương tay phải, cung tay trái).
  3. Routine bắt đầu lại từ đầu với dữ liệu mới.
  4. Label in `→ <Weapon>` trong 1 s.
  5. `Debug.Log($"{name}: {old} -> {new} at {Time.time:F1}s")`.
- **Thử nhanh:** `[ContextMenu("Swap now")]`, kèm phím `N` để đổi ngay cả 3 người. Dùng `#if ENABLE_INPUT_SYSTEM` với `Keyboard.current.nKey.wasPressedThisFrame`, ngược lại dùng `Input.GetKeyDown(KeyCode.N)`.

---

## 4. Scene demo: `Assets/_Game/3D Assets/New Scene.unity`

Mở scene trống có sẵn, giữ `Main Camera` và `Directional Light`, rồi thêm:

- **Sàn:** `Ground`, Plane scale (2, 1, 2), material màu xám đơn giản.
- **3 nhân vật:** Instantiate `BlockBuddy/Models/BlockBuddy.fbx` tại `(-3, 0, 0)`, `(0, 0, 0)`, `(3, 0, 0)`, rotation 0 (mặt hướng +Z).
  - Tên: `Fighter_1/2/3`.
  - Skin: luân phiên `Skin_GreenJacket` / `Skin_CyanShirt` / `Skin_GreenJacket`. Clone material rồi đổi `mainTexture`, giống `BlockKitScene`.
  - Component: `Animator` (không controller, `applyRootMotion = false`), `BlockBuddyAnimPlayer`, `WeaponHolder`, `BlockBuddyFighter`, và label con.
  - `startWeapon` lần lượt là Sword, Bow, Spear. Nhờ vậy lúc nào cũng có đủ 3 loại, và sau 30 s mỗi người cầm loại khác.
  - `startOffset` 0 / 0.7 / 1.4 s để 3 người không đồng bộ động tác.
- **Camera:** position `(7, 5.5, 9)`, nhìn về `(0, 1, 0)`, FOV 35.
- **Lưu:** lưu scene (giữ tên `New Scene.unity`).

Ưu tiên làm bằng menu `BlockBuddyWeapons > Build Demo Scene` (chạy lại được). Nếu menu lỗi thì dựng tay bằng MCP với đúng các thông số trên.

---

## 5. Những điểm CHƯA được xác minh

1. **C# chưa có, toàn bộ phần Unity là việc mới.** Clip và model đã được kiểm tra bằng cách import ngược vào Blender cùng chính `BlockBuddy.fbx` của project: clip bám đúng rig, vũ khí ở socket nằm đúng tay. Riêng việc Unity import FBX có lật trục X hay không thì chưa chắc. Socket được tính từ vị trí bone thực tế lúc bind, nên không phụ thuộc dấu trục.
2. **`.meta` của texture/material** là bản viết tay (GUID texture `6031bf3a8e334120b78d937d50a754af`, material `53f3d33a3a7949f6a3531ea45486a25c`, shader dùng GUID của `BlockBuddy/Shaders/BlockBuddySkin.shader`). Nếu material hồng hoặc trắng thì gán lại shader `BlockBuddy/Skin` + texture atlas bằng tay.
3. **Thời lượng clip** có thể lệch ±1 frame sau import. Code luôn dùng `clip.length` thực, chỉ lấy hits/arrows từ JSON.

---

## 6. Việc cần làm (theo thứ tự)

1. **Import:** kiểm tra Console sau khi Unity import `BlockBuddyWeapons` (warning/error từ `BlockBuddyWeaponsImport`).
2. **Kiểm tra import:**
   - 3 clip bất kỳ: đúng 1 clip, tên đúng, Loop Time đúng bảng 2.1.
   - `Weapon_*.fbx`: root rotation 0, material = `BlockBuddyWeapons_Mat` (không xám/hồng).
   - Atlas: Point, không nén, Texture2D.
3. **Viết code** (mục 3) và chạy `BlockBuddyWeapons > Create Weapon Data`. Kiểm tra 3 asset: Sword 2 attacks, Bow 1 attack, Spear 2 attacks. Clip nào cũng ≠ None.
4. **Dựng scene** (mục 4).
5. **Play mode ≥ 70 s** (đi qua 2 lần đổi vũ khí):
   - **Lúc 0 s:** Fighter_1 cầm kiếm ở tay phải, mũi chĩa lên-trước. Fighter_2 cầm cung ở tay **trái**, dây cung quay vào người. Fighter_3 cầm thương dựng đứng bên phải, mũi lên trên, đuôi ngang đầu gối.
   - **Kiếm:** Attack1 vung từ trên vai phải chém chéo xuống. Attack2 chém ngang. Skill là combo 2 nhát: hất lên chéo từ dưới-trái, rồi bổ xuống chéo từ trên-phải. Khi chạy, kiếm chĩa về phía trước.
   - **Cung:** khi Shoot, nhân vật xoay ngang (vai trái về hướng +Z), tay trái duỗi thẳng giữ cung đứng, tay phải kéo về trước cằm. Mũi tên nằm giữa hai tay, phía trên nắm tay. Lúc release có mũi tên bay ra phía +Z.
   - **Thương:** Attack1 lao người đâm, tay trái gần cán. Skill đâm nhanh 3 phát liên tiếp về phía trước.
   - **Ở giây ~30:** cả 3 lần lượt đổi vũ khí khi xong đòn đang đánh. Model mới pop ở đúng tay, Idle mới crossfade không giật, label đổi tên vũ khí và tên clip. Console có 3 dòng log đổi vũ khí. Lặp lại ở giây ~60.
   - Chuyển Run ↔ Idle ↔ attack không có frame giật hay T-pose. Không có log lỗi lặp mỗi frame.
6. **Sửa lỗi**, ghi rõ đã sửa gì. Các lỗi hay gặp:
   - **Vũ khí chĩa ngược ra sau, hoặc nằm ngang trong Idle:** socket bị tạo sau khi graph đã chạy (không ở bind pose). Phải tạo socket trước `graph.Play()`.
   - **Kiếm nằm ở tay trái hoặc cung ở tay phải:** sai tên bone. Dùng đúng `Hand.R`/`Hand.L`, không suy ra theo dấu X.
   - **Mũi tên chui vào tay:** tăng `up` lên 0.15.
   - **Clip không chạy mà không báo lỗi:** `Animator` chưa bật, hoặc chưa gọi `graph.Play()`.
   - **Nhân vật nằm ngang hoặc lệch:** không bật `Bake Axis Conversion` cho clip nào trừ khi bật cho cả `BlockBuddy.fbx` (phải giống nhau).
7. **Báo cáo:**
   - Pass/Fail cho bước 2–5.
   - Danh sách file đã tạo.
   - Ảnh Game view lúc ~5 s và ~35 s, để thấy cùng một nhân vật cầm vũ khí khác nhau.

---

## 7. Không nên làm

- Không sửa gì trong `BlockBuddy/` (model, material, `BlockBuddyImport.cs`).
- Không đổi tên thư mục `BlockBuddyWeapons`, tên clip, tên bone `Hand.R/L`, tên model `Weapon_*`.
- Không tạo Animator Controller, không gộp clip vào một file, không sửa keyframe trong Unity. Cần đổi động tác thì báo lại để sinh lại từ nguồn `ASSETHELPER/BlockBuddyWeapons_Source`.
- Không bật `Optimize Game Objects` cho `BlockBuddy.fbx` (cần transform `Hand.R/L`).
- Không dùng root motion.
