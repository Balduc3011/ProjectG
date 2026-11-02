BlockKit – bộ dựng cảnh khối hộp cho Unity (đi cùng BlockBuddy)

Cài đặt
1. Chép BlockBuddy/ và BlockKit/ vào Assets/_Game/3D Assets/ (giữ nguyên tên hai thư mục).
2. Menu Unity: BlockKit > Build Layout Prefabs  -> mỗi file trong Layouts/ thành một prefab trong Prefabs/ (BlockGym_Layout.json -> BlockGym.prefab).
3. Kéo prefab vào scene, bấm Play: BlockBuddy xuất hiện ở các điểm Spawn_* và làm việc của mình.

Trong gói
- Models/*.fbx              25 model dùng lại được, mỗi model 1 file. Model nhiều phần có object con với pivot riêng:
                              HeavyBagRack (khung + HeavyBag_1..3), BenchPress (ghế + Barbell), SpeedBag (bệ + SpeedBag_Ball),
                              Kettlebells (3 quả), Dumbbells (2 tạ đơn rời)
- Layouts/*.json            cách xếp model + điểm spawn thành một cảnh. Có sẵn BlockGym_Layout.json (phòng gym boxing, 30 model, 8 người).
                              Thêm cảnh mới = thêm 1 file json (có cả toạ độ Unity và toạ độ gốc FBX).
- Editor/BlockKitBuilder.cs menu BlockKit > Build Layout Prefabs; chuột phải file json > BlockKit > Build Prefab from this layout
- Materials/Textures/Shaders 1 material palette cho đồ đạc, gỗ cho sàn, gạch cho tường (shader BlockKit/Flat, Built-in lẫn URP)
- Scripts/BlockKitScene.cs  sinh BlockBuddy tại Spawn_*, nối tạ đòn / tạ đơn / bao cát cho đúng người
- Scripts/BlockKitActor.cs  vòng lặp Idle -> làm -> Idle của từng vai (RingBoxer, BagBoxer, BenchPress, DumbbellCurl, Treadmill),
                              phát clip bằng PlayableGraph (không cần Animator Controller). Thêm vai = thêm vào enum Role + DefaultSteps.
- Scripts/BlockKitBag.cs    bao cát đung đưa khi bị đấm

Điểm spawn: Spawn_<Vai>_<n>[@<tên object đích>]
