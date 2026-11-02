BlockBuddy – nhân vật khối hộp cho Unity

Models/BlockBuddy.fbx          mesh + rig Generic (20 xương, có khuỷu tay / đầu gối), không chứa animation
Animations/Base/BlockBuddy@*   9 clip cơ bản: Idle, Walk, Run, Sneak, Wave, Jump, Attack, Dance, Sit (đều lặp)
Animations/Gym/BlockBuddy@*    12 clip cho gói BlockGym:
                                 Box_Guard, Box_JabLeft, Box_CrossRight, Box_HookLeft, Box_UppercutRight, Box_Combo (lặp)
                                 Bench_LieDown (3.8s, một lần), Bench_Press (lặp), Bench_GetUp (3.7s, một lần)
                                 Curl_PickUp (1.8s, một lần), Curl_Loop (lặp), Curl_PutDown (1.8s, một lần)
Materials/BlockBuddy_Skin.mat  shader BlockBuddy/Skin; đổi skin bằng cách thay texture (PNG 64x64 kiểu tay 4px)
Editor/BlockBuddyImport.cs     tự đặt Generic rig, Point filter cho texture, loop cho clip (trừ các clip một lần ở trên)

Idle -> boxing / chạy: chỉ cần crossfade. Nằm xuống ghế / nhặt tạ: dùng clip chuyển tiếp ở trên.
Nhân vật cao 2 m, quay mặt về +Z. Xương Root (ở chân) mang offset/xoay toàn thân của clip.
