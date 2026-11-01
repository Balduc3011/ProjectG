BlockGym – phòng gym khối hộp cho Unity (cần cả gói BlockBuddy)

Cài đặt
1. Chép BlockBuddy/ và BlockGym/ vào Assets/_Game/3D Assets/ (giữ nguyên tên hai thư mục).
2. Menu Unity: BlockGym > Build Scene Prefab  -> tạo Prefabs/BlockGym.prefab từ BlockGym_Layout.json.
3. Kéo prefab vào scene, bấm Play: 8 BlockBuddy xuất hiện ở các điểm Spawn_* và tập.

Trong gói
- Models/*.fbx            25 model dùng lại được, mỗi model 1 file. Model nhiều phần có các object con có pivot riêng:
                            HeavyBagRack (khung + HeavyBag_1..3), BenchPress (ghế + Barbell), SpeedBag (bệ + SpeedBag_Ball),
                            Kettlebells (3 quả), Dumbbells (2 tạ đơn rời)
- BlockGym_Layout.json    vị trí + góc xoay của 30 lần đặt model và 8 điểm Spawn_* (có sẵn toạ độ Unity và toạ độ gốc FBX)
- Editor/BlockGymBuilder.cs  menu BlockGym > Build Scene Prefab (đọc json, xếp model, tạo prefab)
- Materials/Textures/Shaders 1 material palette cho đồ đạc, gỗ cho sàn, gạch cho tường (shader BlockGym/Flat, Built-in lẫn URP)
- Scripts/BlockGymScene.cs   sinh nhân vật tại Spawn_*, nối tạ đòn / tạ đơn / bao cát cho đúng người
- Scripts/BlockGymActor.cs   vòng lặp Idle -> làm -> Idle của từng vai, phát clip bằng PlayableGraph (không cần Animator Controller)
- Scripts/BlockGymBag.cs     bao cát đung đưa khi bị đấm

Điểm spawn: Spawn_<Vai>_<n>[@<tên bao cát>]   Vai: RingBoxer, BagBoxer, BenchPress, DumbbellCurl, Treadmill
