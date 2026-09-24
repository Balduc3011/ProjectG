# Manager System — GameInstance & World

Tham khảo Service/Facade của JIS-CardShop và Subsystem của Unreal, không dùng thư viện DI.

> Cách gọi manager từ từng vị trí (manager, UI, event...): xem [ManagerUsage.md](ManagerUsage.md).

## Hai scope

| Scope | Component | Base class của manager | Sống bao lâu | Lấy ra |
|---|---|---|---|---|
| GameInstance (≈ `UGameInstanceSubsystem`) | `GameManager` (MainScene) | `BaseManager` | Mở app → tắt app | `GameManager.Get<T>()` |
| World (≈ `UWorldSubsystem`) | `WorldSubsystem` (scene gameplay) | `BaseWorldManager` | Theo scene, huỷ khi unload | `WorldSubsystem.Get<T>()` |

Câu hỏi để chọn scope: **rời scene gameplay thì state này có cần giữ không?** Có → GameInstance. Không → World.

**Phụ thuộc một chiều: World → GameInstance.** `GameManager` chỉ chứa `BaseManager`, nên manager GameInstance không thể lấy manager World qua hệ thống này. Đừng gọi `WorldSubsystem` từ `BaseManager`.

Scene gameplay nên load **Additive** từ MainScene, để `GameManager` luôn tồn tại.

## Lifecycle (do scope gọi)

| Phase | Khi nào | Làm gì |
|---|---|---|
| `Initialize(managers)` | Awake của scope, đồng bộ | **Chỉ** nối dây: `managers.InitializeDependency<T>()` |
| `LoadData()` | Start, tuần tự theo thứ tự phụ thuộc | Load data |
| `PostLoad()` | Khi **mọi** manager cùng scope đã LoadData xong | Hiện UI, spawn, bind... |
| `Deinitialize()` | Scope bị huỷ, thứ tự ngược | Huỷ subscribe. Không đụng manager khác |

- `GameManager.IsReady` / `WorldSubsystem.Current.IsReady`: true sau PostLoad.
- World đợi `GameManager.IsReady` rồi mới LoadData.
- Thứ tự do **phụ thuộc** quyết định, không do vị trí trong Hierarchy. Vòng phụ thuộc hoặc thiếu manager báo lỗi ngay khi Initialize.

## Thêm manager mới

1. Tạo class kế thừa `BaseManager` (GameInstance) hoặc `BaseWorldManager` (World).
2. Đặt component làm **con** của `GameManager` hoặc `WorldSubsystem`.
3. Nếu cần manager khác phải load trước:

```csharp
private TimeManager timeManager;

protected internal override void Initialize(ManagerCollection<BaseManager> managers)
{
    timeManager = managers.InitializeDependency<TimeManager>();
}
```

Không cần sửa `GameManager.cs` hay `WorldSubsystem.cs`. Mẫu: `Scripts/World/Player/PlayerManager.cs`.

## Quy tắc

- Trong manager: phụ thuộc lấy qua `Initialize`, **không** gọi static (`GameManager.Get<T>()`) để lấy manager cùng scope. Static chỉ dành cho code ngoài hệ thống (UI, object trong scene).
- Manager World được dùng `GameManager.Get<T>()` trong `Initialize` để lấy manager GameInstance.

## File

| File | Vai trò |
|---|---|
| `Scripts/Core/ManagerBase.cs` | Base generic + lifecycle |
| `Scripts/Core/ManagerCollection.cs` | Đăng ký, thứ tự phụ thuộc, phát hiện vòng, Load/PostLoad/Deinit |
| `Scripts/Manager/GameManager.cs` | Scope GameInstance |
| `Scripts/Manager/BaseManager.cs` | Base manager GameInstance |
| `Scripts/Manager/GameSceneManager.cs` | Load/unload World scene (Additive), màn loading, đổi Screen UI |
| `Scripts/World/WorldSubsystem.cs` | Scope World |
| `Scripts/World/BaseWorldManager.cs` | Base manager World |
