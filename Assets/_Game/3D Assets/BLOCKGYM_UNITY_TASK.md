# BlockGym + BlockBuddy — nhiệm vụ cho Claude Code (Unity MCP)

Hai gói asset đã được dựng sẵn bằng Blender headless và **chưa từng mở trong Unity**. Nhiệm vụ: đưa vào project, dựng prefab phòng gym từ file layout JSON, kiểm tra và sửa những chỗ lệch.

Ngôn ngữ trả lời: tiếng Việt. Tên file, tên object, code: giữ nguyên tiếng Anh.

---

## 1. Đầu vào và vị trí trong project

| File | Nội dung |
|---|---|
| `BlockBuddy_Unity.zip` | Nhân vật khối hộp có rig 2 khớp + 21 animation clip |
| `BlockGym_Unity.zip` | 25 model đồ đạc riêng lẻ, file layout JSON, script dựng prefab và script chạy nhân vật |

Giải nén và đặt đúng đường dẫn:

```
Assets/_Game/3D Assets/BlockBuddy/
Assets/_Game/3D Assets/BlockGym/
```

Script Editor (`AssetPostprocessor`) và các hàm tự tìm asset nhận diện theo đoạn đường dẫn `/BlockBuddy/`, `/BlockGym/`, nên độ sâu thư mục không quan trọng nhưng **không đổi tên hai thư mục này**. Từ đây các đường dẫn viết tắt `BlockBuddy/...`, `BlockGym/...` đều nằm dưới `Assets/_Game/3D Assets/`.

Nếu project đã có bản BlockBuddy/BlockGym cũ: **xoá hẳn** trước khi chép (bản cũ có clip và prefab không tương thích).

---

## 2. Gói BlockBuddy

```
BlockBuddy/
  Models/BlockBuddy.fbx              mesh + rig, KHÔNG có animation
  Animations/Base/BlockBuddy@<Clip>.fbx   9 clip cơ bản
  Animations/Gym/BlockBuddy@<Clip>.fbx    12 clip cho phòng gym
  Materials/BlockBuddy_Skin.mat      shader BlockBuddy/Skin, texture Skin_GreenJacket
  Shaders/BlockBuddySkin.shader      unlit + tô sáng theo mặt khối, alpha cutout, double-sided; chạy Built-in và URP
  Textures/Skin_GreenJacket.png, Skin_CyanShirt.png
  Editor/BlockBuddyImport.cs
  README.txt
```

Mỗi file clip chứa đúng 1 clip, tên clip = phần sau `@`.

| Thư mục | Clip | Lặp |
|---|---|---|
| Base | `Idle`, `Walk`, `Run`, `Sneak`, `Wave`, `Jump`, `Attack`, `Dance`, `Sit` | có |
| Gym | `Box_Guard`, `Box_JabLeft`, `Box_CrossRight`, `Box_HookLeft`, `Box_UppercutRight`, `Box_Combo` | có |
| Gym | `Bench_LieDown` 3.8 s (đứng cạnh ghế → ngồi → nằm → với tay nhấc tạ khỏi giá; **tạ vào tay từ giây 3.2**) | không |
| Gym | `Bench_Press` 3.2 s (2 lần đẩy) | có |
| Gym | `Bench_GetUp` 3.7 s (gác tạ **đến giây 0.6** → ngồi dậy → đứng cạnh ghế) | không |
| Gym | `Curl_PickUp` 1.8 s (cúi xuống, **cầm tạ từ giây 0.9**, đứng lên) | không |
| Gym | `Curl_Loop` 2.8 s (cuốn tay luân phiên) | có |
| Gym | `Curl_PutDown` 1.8 s (cúi xuống, **thả tạ ở giây 0.9**, đứng lên) | không |

Không có clip chuyển tiếp cho Idle ↔ boxing và Idle ↔ chạy: chỉ cần crossfade (0.6 s và 1.0 s). Các clip đấm bắt đầu và kết thúc ở tư thế guard nên crossfade qua lại với `Box_Guard` là liền mạch.

**Hierarchy FBX** (giống nhau ở model và mọi clip):

```
BlockBuddy (empty, rotation 0)
├─ Body   (SkinnedMeshRenderer; child này mang rotation -90° X, là chủ ý)
└─ Rig    (cũng -90° X)
   └─ Root → Hips → Spine → Head
                   ├─ Spine → UpperArm.R → Elbow.R → LowerArm.R → Hand.R   (tương tự .L)
                   └─ Hips  → UpperLeg.R → Knee.R → LowerLeg.R → Foot.R   (tương tự .L)
```

- Cao 2 m, mặt hướng **+Z**, tay phải ở **+X** (Unity). Rig Generic.
- `Root` (pivot ở chân) là xương duy nhất có keyframe vị trí; mọi xương khác chỉ xoay.
- Khuỷu/gối mỗi bên là 2 xương (`Elbow` + `LowerArm`, `Knee` + `LowerLeg`), mỗi xương xoay một nửa góc gập.
- Lớp phủ ngoài (tóc, áo) là mesh bọc ngoài → material phải cutout (shader `BlockBuddy/Skin`, hoặc Lit với Alpha Clipping 0.5).

`BlockBuddyImport.cs` (tự chạy khi import): texture Point/không nén; mọi FBX Generic; file có `@` không import material; model remap material `BlockBuddy_Skin` → `Materials/BlockBuddy_Skin.mat`; `loopTime = true` trừ clip có tên kết thúc bằng `_LieDown/_GetUp/_PickUp/_PutDown`. Chỉ áp dụng lần import đầu.

---

## 3. Gói BlockGym

```
BlockGym/
  Models/<Model>.fbx            25 model, mỗi model 1 file (xem bảng)
  BlockGym_Layout.json          cách xếp 30 instance model + 8 điểm spawn thành phòng gym
  Editor/BlockGymBuilder.cs     menu  BlockGym > Build Scene Prefab  (đọc JSON → Prefabs/BlockGym.prefab)
  Editor/BlockGymImport.cs      AssetPostprocessor
  Scripts/BlockGymLayout.cs     class dữ liệu của JSON (JsonUtility)
  Scripts/BlockGymScene.cs      sinh nhân vật tại Spawn_*, nối prop
  Scripts/BlockGymActor.cs      vòng lặp của từng vai, phát clip bằng PlayableGraph
  Scripts/BlockGymBag.cs        bao cát đung đưa
  Materials/BlockGym_Palette.mat, BlockGym_Wood.mat, BlockGym_Brick.mat
  Textures/BlockGym_Palette.png (16x16, mỗi màu 1 pixel), BlockGym_Wood.png (tile 4 m), BlockGym_Brick.png (tile 2 m)
  Shaders/BlockGymFlat.shader   giống BlockBuddy/Skin, tên "BlockGym/Flat"
  README.txt
```

**Không có model "cả phòng"**: phòng được lắp từ các model dưới đây theo JSON.

| Model | Cấu trúc trong FBX | Dùng |
|---|---|---|
| `Floor` | `Floor` → `Floor_Mesh` (24×24 m, material Wood) | 1 |
| `Wall` | tường gạch 24 m + ốp chân tường | 4 (`Wall_North/West/South/East`) |
| `BoxingRing` | bục, sàn, 4 cột, dây, bậc thang (bậc ở phía +Z) | 1 |
| `HeavyBagRack` | khung + 3 object con `HeavyBag_1..3`, **pivot ở móc treo trên xà** | 1 |
| `SpeedBag` | bệ gắn tường + object con `SpeedBag_Ball` (pivot ở trục xoay) | 1 |
| `Treadmill` | 1 mesh, băng chạy dọc trục Z, bảng điều khiển ở -Z | 2 |
| `BenchPress` | ghế + giá + object con `Barbell` (pivot ở tâm thanh, đang nằm trên giá) | 1 |
| `Dumbbells` | 2 object con `Dumbbell_1`, `Dumbbell_2` (tạ đơn rời dưới sàn, đúng chỗ tay người tập với tới) | 1 |
| `DumbbellRack`, `SquatRack`, `PlateTree`, `ExerciseBike`, `Lockers`, `Bench`, `WaterCooler`, `MedicineBalls`, `SparePlates`, `RubberMat`, `Clock`, `Window`, `Poster_Red/Blue/Black` | 1 mesh | 1 mỗi loại |
| `Kettlebells` | 3 object con `Kettlebell_1..3` | 1 |
| `StretchMat` | 1 mesh | 2 |

Mỗi model: root empty (rotation 0) → các object con mang rotation -90° X (chủ ý, giống BlockBuddy). Material trong FBX tên `BlockGym_Palette` / `BlockGym_Wood` / `BlockGym_Brick`, được `BlockGymImport` remap sang 3 file `.mat`.

### 3.1 BlockGym_Layout.json

```json
{
  "root": "BlockGym",
  "placements": [
    { "model": "Treadmill", "name": "Treadmill_1",
      "unity": { "position": [-9.5, 0, -1.2], "rotationY": -90 },
      "fbx":   { "position": [ 9.5, 0, -1.2], "rotationY":  90 } },
    ...
  ],
  "spawns": [
    { "name": "Spawn_BagBoxer_1@HeavyBag_1", "role": "BagBoxer", "target": "HeavyBag_1",
      "unity": { ... }, "fbx": { ... } },
    ...
  ]
}
```

- `placements`: 30 instance. `model` = tên file FBX trong `Models/`, `name` = tên GameObject trong prefab.
- `spawns`: 8 điểm đặt nhân vật. Tên theo mẫu `Spawn_<Role>_<n>[@<Target>]`; `role` ∈ `RingBoxer, BagBoxer, BenchPress, DumbbellCurl, Treadmill`; `target` là tên bao cát người đó đấm.
- Đơn vị mét, `rotationY` là góc quay quanh Y (độ).
- `unity` = toạ độ đã đổi sang Unity theo quy tắc **Unity lật trục X khi import FBX**: `unity = (-x, y, z)`, `yawUnity = -yawFbx`. `fbx` = toạ độ gốc trong file FBX (Y lên, tay phải). **Mặc định dùng `unity`.** Chỉ khi kiểm tra ở mục 5 cho thấy phòng bị lệch với model thì chuyển sang `fbx` (menu `BlockGym > Toggle coordinate space`, rồi Build lại).

### 3.2 Prefab được dựng như thế nào

`BlockGym > Build Scene Prefab` (file `Editor/BlockGymBuilder.cs`):
1. Tìm `BlockGym_Layout.json`, tạo GameObject root `BlockGym`.
2. Với mỗi `placement`: `PrefabUtility.InstantiatePrefab(Models/<model>.fbx)` làm con của root, đặt `localPosition`/`localRotation` theo `unity`, đổi tên theo `name`.
3. Với mỗi `spawn`: tạo GameObject rỗng tên `name`, đặt vị trí/xoay.
4. `AddComponent<BlockGymScene>()`, gọi `FindAssets()` để điền `blockBuddy`, `clips` (21), `skins` (2).
5. Lưu `Prefabs/BlockGym.prefab`. Chạy lại sẽ ghi đè.

Nếu menu không chạy được (lỗi compile, Unity đời khác), làm tay đúng 5 bước trên bằng MCP/editor script; mọi số liệu đều trong JSON.

### 3.3 Runtime

`BlockGymScene` (trên root prefab): khi Play, tìm mọi `Spawn_*` trong con → `Instantiate(blockBuddy, spawn.position, spawn.rotation)`, gán skin luân phiên (clone material, đổi `mainTexture`), `AddComponent<BlockGymActor>()`, nối `Barbell` cho BenchPress, `Dumbbell_1/2` cho DumbbellCurl, `HeavyBag_<n>` (theo `@Target`) cho BagBoxer, thêm `BlockGymBag` vào mọi `HeavyBag_*`. Có `[ContextMenu] Build characters`, `Find BlockBuddy assets`; trong Editor `OnValidate` tự điền tham chiếu trống.

`BlockGymActor`: PlayableGraph + `AnimationMixerPlayable` 2 input, crossfade theo từng bước. Vòng lặp (`DefaultSteps`):

| Vai | Vòng lặp (clip, giây) | Prop |
|---|---|---|
| RingBoxer, BagBoxer | `Idle` 2.5 → `Box_Random` 9 (guard + mỗi 1.3 s một cú đấm ngẫu nhiên) → lặp; fade 0.6 | BagBoxer gọi `bag.Punch()` 0.3 s sau khi bắt đầu cú đấm |
| Treadmill | `Idle` 2 → `Run` 8 → lặp; fade 1.0 | — |
| BenchPress | `Idle` 2 → `Bench_LieDown` 3.8 → `Bench_Press` 3.2 → `Bench_GetUp` 3.7 → `Idle` 1.5 | tạ đòn theo tay từ 3.2 s của LieDown đến 0.6 s của GetUp, còn lại lerp về giá |
| DumbbellCurl | `Idle` 2.2 → `Curl_PickUp` 1.8 → `Curl_Loop` 7 → `Curl_PutDown` 1.8 → `Idle` 1 | tạ đơn theo tay từ 0.9 s của PickUp đến 0.9 s của PutDown |

Prop theo tay: `LateUpdate` đặt `Barbell` ở trung điểm bone `Hand.R`/`Hand.L` (xoay theo nhân vật), tạ đơn ở từng tay. `Hand.*` là transform của bone, tìm bằng `FindDeep` theo tên.

---

## 4. Những điểm CHƯA được xác minh

1. **Code C# chưa compile trong Unity.** API dùng: `UnityEngine.Playables`, `UnityEngine.Animations` (AnimationClipPlayable, AnimationMixerPlayable, AnimationPlayableOutput), `PrefabUtility.InstantiatePrefab/SaveAsPrefabAsset`, `AssetImporter.SourceAssetIdentifier`, `ModelImporter.AddRemap`, `AssetImportContext.DependsOnSourceAsset` (có `#if UNITY_2020_2_OR_NEWER`), `JsonUtility`.
2. **File `.meta` là bản tối giản** (guid + tên importer, `ModelImporter.serializedVersion: 22200`). Unity tự bổ sung phần còn lại. GUID quan trọng: 3 texture + shader BlockGym (material trỏ tới), 2 skin + shader BlockBuddy. Nếu Unity từ chối một `.meta`: xoá file `.meta` đó cho Unity sinh lại, rồi gán lại texture/shader cho material bằng tay.
3. **Hướng trong Unity.** Model và layout đi qua cùng một pipeline; đã kiểm tra bằng cách lắp lại phòng trong Blender từ chính 25 FBX + JSON (khớp với bản web). Phần chưa chắc là **dấu lật trục** khi Unity import. JSON đã tính sẵn theo giả định "lật X"; mục 5 có cách kiểm tra.
4. **Thời lượng clip** sau import có thể lệch ±1 frame (30 fps). Nếu thấy giật ở chỗ nối thì chỉnh `duration` trong `DefaultSteps` theo `clip.length` thực.
5. **URP**: shader custom là CG unlit nên chạy được trong URP nhưng không nhận ánh sáng/bóng. Nếu cần Lit: đổi material sang URP/Lit; riêng `BlockBuddy_Skin` bật Alpha Clipping 0.5.

---

## 5. Việc cần làm (theo thứ tự)

1. **Import**: chép 2 thư mục vào `Assets/_Game/3D Assets/`. Ghi lại warning/error trong Console (đặc biệt từ `BlockBuddyImport`/`BlockGymImport`).
2. **Compile**: sửa lỗi C# nếu có, giữ nguyên logic.
3. **Kiểm tra import**:
   - `BlockBuddy.fbx`: Rig Generic, root rotation 0, `Body` có SkinnedMeshRenderer với material `BlockBuddy_Skin` (không phải material xám mặc định).
   - 3 file clip bất kỳ: đúng 1 clip, tên đúng, Loop Time đúng bảng ở mục 2.
   - 3 model bất kỳ trong `BlockGym/Models`: root rotation 0, material đã remap (không xám/hồng), `HeavyBagRack.fbx` có 3 con `HeavyBag_1..3`, `BenchPress.fbx` có con `Barbell`.
   - Texture: Filter Point, Compression None; Wood/Brick wrap Repeat.
4. **Dựng prefab**: menu `BlockGym > Build Scene Prefab`. Kiểm tra `Prefabs/BlockGym.prefab`: 30 con model + 8 con `Spawn_*`, component `BlockGymScene` có `blockBuddy` ≠ None, `clips` = 21, `skins` = 2.
5. **Kiểm tra hệ trục** (mở prefab trong Scene view, nhìn từ trên xuống):
   - 4 tường tạo thành khung kín quanh sàn 24×24, không chồng lên nhau, ốp chân tường quay vào trong phòng.
   - `Treadmill_1/2` nằm sát tường phía +X (Unity) hoặc -X, **bảng điều khiển quay vào tường**, người chạy (`Spawn_Treadmill_*`) đứng trên băng và nhìn về bảng điều khiển.
   - `Spawn_BenchPress` nằm đúng trên ghế `BenchPress`, `Spawn_DumbbellCurl` đứng ngay cạnh 2 tạ đơn `Dumbbells`, `Spawn_BagBoxer_*` đứng trước bao `HeavyBag_1`/`HeavyBag_3`, cách bao ~1.25 m.
   - Nếu các cặp trên **không khớp nhau** (ví dụ người chạy đứng ngoài máy, máy quay ngược) thì layout đang dùng sai dấu: chạy `BlockGym > Toggle coordinate space (unity <-> fbx)` rồi `Build Scene Prefab` lại, kiểm tra lại. Nếu chỉ là cả phòng bị lật gương so với bản web thì **không phải lỗi**.
6. **Play mode** (thêm Directional Light, camera isometric rotation ~(35, 45, 0), Orthographic size ~14), quan sát ≥ 20 s:
   - 8 BlockBuddy xuất hiện, đứng thẳng, chân chạm sàn (2 người trên sàn đấu ở cao độ 0.87, người chạy ở 0.36).
   - 2 người trên sàn đấu đối mặt nhau; 2 người đấm bao quay mặt vào bao; bao 1 và 3 đung đưa khi bị đấm rồi tự dừng.
   - Người đẩy tạ: đứng → nằm → nhấc tạ khỏi giá → đẩy 2 cái → gác lại → đứng dậy; tạ đòn luôn ở tay khi đẩy, về đúng giá khi gác.
   - Người tập tạ đơn: cúi nhặt 2 tạ → cuốn tay → đặt lại đúng chỗ cũ.
   - Không có log lỗi lặp mỗi frame (vd `missing clip`).
7. **Sửa lỗi** và ghi rõ đã sửa gì. Chỗ hay cần chỉnh:
   - nhân vật nghiêng -90° → bật `Bake Axis Conversion` cho **cả** model lẫn mọi clip (phải giống nhau);
   - tạ đòn/tạ đơn lệch khỏi tay → `Hand.R/L` nằm ở cổ tay, cộng thêm ~0.06 m dọc theo xương nếu cần;
   - clip không chạy mà không báo lỗi → kiểm tra `Animator` trên root đang bật và `PlayableGraph.Play()` đã gọi.
8. **Báo cáo**: kết quả Pass/Fail cho bước 3–6, danh sách thay đổi, ảnh Game view nếu chụp được.

---

## 6. Không nên làm

- Không đổi tên thư mục `BlockBuddy`, `BlockGym`; tên xương; tên clip; tên object `Spawn_*`, `HeavyBag_*`, `Barbell`, `Dumbbell_*` (script tìm theo tên).
- Không gộp các model thành một mesh lớn, không gộp clip vào file model.
- Không bật `Optimize Game Objects` cho `BlockBuddy.fbx` (cần transform `Hand.R/L`).
- Không tạo Animator Controller cho nhân vật trừ khi được yêu cầu.
- Không sửa animation bằng tay trong Unity; cần đổi động tác thì báo lại để sinh lại clip từ nguồn.
