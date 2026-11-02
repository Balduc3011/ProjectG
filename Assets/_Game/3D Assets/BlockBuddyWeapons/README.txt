BlockBuddyWeapons – bộ animation vũ khí cho BlockBuddy (Kiếm / Cung / Thương / Găng đấm)

Chỉ có animation + model vũ khí, KHÔNG có nhân vật. Nhân vật dùng Assets/_Game/3D Assets/BlockBuddy/Models/BlockBuddy.fbx.
Mọi clip có cùng hierarchy BlockBuddy/Rig/Root/... với BlockBuddy.fbx (rig Generic 20 xương), mỗi file 1 clip, tên clip = phần sau '@'.

Animations/Sword/   Sword_Idle (lặp 3.0s), Sword_Run (lặp 0.7s), Sword_Attack1 (chém chéo 0.9s), Sword_Attack2 (chém ngang 0.93s), Sword_Skill (combo hất lên + bổ xuống 1.3s)
Animations/Bow/     Bow_Idle (lặp), Bow_Run (lặp), Bow_Shoot (1 mũi tên 1.8s), Bow_Skill (bắn liên 3 mũi 2.03s)
Animations/Spear/   Spear_Idle (lặp), Spear_Run (lặp), Spear_Attack1 (đâm 1.0s), Spear_Attack2 (quét ngang 1.07s), Spear_Skill (đâm nhanh 3 phát 1.33s)
Animations/Fist/    Fist_Idle (thế thủ, lặp 3.0s), Fist_Run (lặp), Fist_Jab (0.7s), Fist_Cross (0.8s), Fist_Skill (jab-cross-móc trái 1.8s)
Models/             Weapon_Sword, Weapon_Bow, Weapon_Spear, Weapon_Arrow, Weapon_Gauntlet_R, Weapon_Gauntlet_L (pivot = chỗ cầm, +Z = mũi / cánh cung trên)
Data/BlockBuddyWeapons_Clips.json   độ dài, loop, thời điểm trúng đòn (hits), cửa sổ mũi tên (nock -> release)
Editor/BlockBuddyWeaponsImport.cs   tự đặt: clip Generic + loop cho *_Idle/*_Run, model vũ khí remap sang Materials/BlockBuddyWeapons_Mat.mat, texture Point/không nén

Cầm vũ khí: tạo socket con của Hand.R (kiếm, thương, găng phải) / Hand.L (cung, găng trái) Ở TƯ THẾ BIND:
  socket.position = Hand.X.position + nhân_vật.up * 0.0625 ;  socket.rotation = nhân_vật.rotation
rồi đặt model vũ khí làm con của socket với localPosition 0, localRotation identity.
Mũi tên không gắn vào tay: đặt bằng script từ tay phải tới tay cầm cung (xem Data json).
Mọi clip một lần bắt đầu và kết thúc ở tư thế Idle của chính vũ khí đó -> crossfade liền mạch.
Nguồn sinh lại: ASSETHELPER/BlockBuddyWeapons_Source (không sửa clip bằng tay trong Unity).
