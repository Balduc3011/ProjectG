# Cách gọi Manager — hướng dẫn sử dụng

Kiến trúc tổng quan (scope, lifecycle): xem [ManagerSystem.md](ManagerSystem.md). File này trả lời hai câu hỏi: **setup World scene thế nào** và **đang ở chỗ X thì gọi manager Y ra sao**.

## 0. Setup World scene (lần đầu)

MainScene đã có sẵn `GameManager` (GameInstance), không cần làm gì thêm ở đó. Phần này là để tạo scene gameplay đầu tiên.

### Bước 1 — Tạo scene

1. `Assets/_Game/Scenes` → chuột phải → **Create → Scene**, đặt tên ví dụ `WorldScene`.
2. Xoá **Main Camera** và **Directional Light** mặc định nếu MainScene đã có (MainScene đang có Main Camera, Directional Light, EventSystem). Hai scene cùng load thì chỉ được có **một** AudioListener và **một** EventSystem.
3. Thêm scene vào **File → Build Profiles → Scene List** (Unity 6), đặt **MainScene ở vị trí 0**, `WorldScene` ở sau. Không có trong danh sách thì không load được lúc runtime.

### Bước 2 — Dựng Hierarchy

```
WorldScene
├── ========WORLD==========        ← GameObject rỗng, gắn component WorldSubsystem
│   ├── PlayerManager               ← GameObject rỗng, gắn component PlayerManager
│   └── (các BaseWorldManager khác, mỗi cái một GameObject con)
└── ========ENVIRONMENT====        ← map, prop, spawn point... (không phải manager)
```

1. Tạo GameObject rỗng, đặt tên dễ thấy (ví dụ `========WORLD==========`), **Add Component → WorldSubsystem**.
2. Tạo GameObject con của nó, **Add Component → PlayerManager**.
3. Manager World mới: làm tương tự, mỗi manager một GameObject **con** của `WorldSubsystem`. Lồng nhiều cấp cũng được, gắn thẳng lên chính GameObject có `WorldSubsystem` cũng được.

### Có cần kéo reference các manager không? — **Không.**

- `WorldSubsystem` tự tìm mọi `BaseWorldManager` là con của nó (`GetComponentsInChildren`) lúc Awake. Không có field nào để kéo, và không phải sửa `WorldSubsystem.cs`.
- Manager lấy nhau bằng code trong `Initialize()` (xem mục 1–3), không kéo reference manager ↔ manager trong Inspector.
- Chỉ kéo reference Inspector cho **asset / object của riêng manager đó**, ví dụ `PlayerManager` giữ prefab player, spawn point:

```csharp
public class PlayerManager : BaseWorldManager
{
    [SerializeField] private GameObject playerPrefab;   // kéo prefab vào đây
    [SerializeField] private Transform spawnPoint;       // kéo object trong WorldScene vào đây
}
```

Lưu ý:

- Manager **phải là con** của `WorldSubsystem`. Đặt ở chỗ khác trong scene sẽ không được đăng ký, và `Get<T>()` sẽ báo "Không tìm thấy".
- GameObject con **bị tắt (inactive) vẫn được đăng ký** và vẫn chạy lifecycle. Muốn bỏ một manager thì xoá component, đừng chỉ tắt GameObject.
- Mỗi lúc chỉ có **một** `WorldSubsystem` active. Cái thứ hai sẽ bị tắt và báo lỗi.
- **Không** đặt `BaseManager` (GameInstance) vào WorldScene. Chúng thuộc về `GameManager` trong MainScene.

### Bước 3 — Load / unload World scene từ MainScene

Việc này do **`GameSceneManager`** lo. Nó là con của `GameManager` trong prefab `Prefab/Manager/GameManager.prefab`.

1. Chọn `GameManager` → `GameSceneManager` trong prefab, điền **World Scene Name** (mặc định `SampleWorldScene`). Scene phải có trong **Build Profiles → Scene List**.
2. Gọi từ UI:

```csharp
// Vào gameplay: màn loading → load World (Additive) → đợi World sẵn sàng → bật UIScreenPlay
GameManager.Get<GameSceneManager>().OpenWorldScene().Forget();

// Về menu: màn loading → đóng UIScreenPlay → unload World
GameManager.Get<GameSceneManager>().BackToMenu().Forget();
```

`GameSceneManager` tự làm những việc sau, không cần viết lại:

- Load **Additive** (không dùng `LoadSceneMode.Single`, vì nó sẽ unload MainScene, làm mất `GameManager` và UI).
- **`SetActiveScene`** sang World sau khi load. Đây là bước bắt buộc: `Instantiate` luôn tạo object vào scene active, và lighting / skybox cũng theo scene active. Khi unload thì trả scene active về MainScene.
- Đợi `WorldSubsystem.Current.IsReady` rồi mới tắt màn loading.
- Chặn bấm Play / Back liên tục khi đang chuyển scene.
- World đã mở sẵn (test multi-scene) thì không load lại.

Unload World sẽ tự gọi `Deinitialize` của mọi manager World, rồi `WorldSubsystem.Current` về `null`.

### Bước 4 — Test trong Editor

| Cách mở | Kết quả |
|---|---|
| Mở MainScene → Play → vào gameplay (load Additive) | ✅ Đúng luồng thật |
| Mở MainScene, kéo thêm WorldScene vào Hierarchy (multi-scene editing) → Play | ✅ Tiện để test nhanh. `GameManager` khởi tạo trước `WorldSubsystem` nhờ execution order |
| Chỉ mở WorldScene → Play | ⚠️ Console báo "Chưa có GameManager", World đứng chờ mãi, không LoadData |

Kiểm tra setup đúng: Play xong, gõ vào một script test bất kỳ:

```csharp
Debug.Log(WorldSubsystem.Current.IsReady);                 // true
Debug.Log(WorldSubsystem.Get<PlayerManager>() != null);    // true
```

## Bảng tra nhanh

| Gọi từ ↓ / tới → | Manager GameInstance (`BaseManager`) | Manager World (`BaseWorldManager`) |
|---|---|---|
| Manager GameInstance | `managers.InitializeDependency<T>()` trong `Initialize` | ✗ Cấm, dùng event (mục 4) |
| Manager World | `GameManager.Get<T>()` trong `Initialize` | `managers.InitializeDependency<T>()` hoặc `managers.Get<T>()` trong `Initialize` |
| UI / object trong scene | `GameManager.Get<T>()` | `WorldSubsystem.Get<T>()` / `WorldSubsystem.TryGet<T>()` |

Quy tắc chung:

- Bên trong manager: lấy phụ thuộc **một lần trong `Initialize()`**, lưu vào field, dùng lại sau đó. Không gọi static mỗi lần dùng.
- Bên ngoài hệ thống (UI, prefab, object trong scene): gọi qua static `GameManager.Get<T>()` / `WorldSubsystem.Get<T>()`.

## 1. Manager GameInstance gọi manager GameInstance khác

```csharp
public class ShopManager : BaseManager
{
    private ResourceManager resourceManager;

    protected internal override void Initialize(ManagerCollection<BaseManager> managers)
    {
        // ResourceManager chắc chắn LoadData trước ShopManager.
        resourceManager = managers.InitializeDependency<ResourceManager>();
    }

    public void BuyPack()
    {
        resourceManager.AddGameResource(ResourceType.Currency, (int)CurrencyType.Money, -100);
    }
}
```

`InitializeDependency<T>()` hay `Get<T>()`?

- `InitializeDependency<T>()`: khi **LoadData / PostLoad** của manager này cần T đã load xong.
- `managers.Get<T>()`: khi chỉ gọi T lúc runtime (bấm nút, gameplay), không cần thứ tự load.
- Nếu không chắc, dùng `InitializeDependency`. Nếu hai manager cần nhau, hệ thống báo lỗi vòng phụ thuộc: tách phần dùng chung ra, hoặc một bên đổi sang `Get<T>()`.

## 2. Manager World gọi manager GameInstance

World được phép gọi lên GameInstance.

```csharp
public class PlayerManager : BaseWorldManager
{
    private ResourceManager resourceManager;

    protected internal override void Initialize(ManagerCollection<BaseWorldManager> managers)
    {
        resourceManager = GameManager.Get<ResourceManager>();
    }

    public void OnPlayerCollectCoin(int amount)
    {
        resourceManager.AddGameResource(ResourceType.Currency, (int)CurrencyType.Money, amount);
    }
}
```

Không cần `InitializeDependency`: World luôn đợi `GameManager.IsReady` rồi mới `LoadData`, nên data của GameInstance đã sẵn sàng.

## 3. Manager World gọi manager World khác

```csharp
public class EnemyManager : BaseWorldManager
{
    private PlayerManager playerManager;

    protected internal override void Initialize(ManagerCollection<BaseWorldManager> managers)
    {
        playerManager = managers.InitializeDependency<PlayerManager>(); // PlayerManager load trước
        // Hoặc managers.Get<PlayerManager>() nếu chỉ gọi lúc runtime.
    }

    private void OnPlayerHit() => playerManager.Respawn();
}
```

## 4. Manager GameInstance cần báo cho World → dùng event

**Không gọi trực tiếp manager World** từ `BaseManager`: khi đang ở menu, scene gameplay chưa tồn tại. GameInstance phát event, còn World tự đăng ký và tự huỷ đăng ký.

```csharp
// RewardManager (GameInstance)
public event Action<List<GameResource>> OnRewardAdded;

public void AddReward(List<GameResource> rewards, bool showRewardActivity = true)
{
    resourceManager.AddGameResources(rewards);
    OnRewardAdded?.Invoke(rewards);
    // ...
}
```

```csharp
// PlayerManager (World)
private RewardManager rewardManager;

protected internal override void Initialize(ManagerCollection<BaseWorldManager> managers)
{
    rewardManager = GameManager.Get<RewardManager>();
    rewardManager.OnRewardAdded += HandleReward;
}

protected internal override void Deinitialize()
{
    rewardManager.OnRewardAdded -= HandleReward; // BẮT BUỘC: scene World sẽ bị unload
}

private void HandleReward(List<GameResource> rewards) { /* ... */ }
```

Quên `-=` trong `Deinitialize` sẽ khiến GameInstance giữ tham chiếu tới object đã bị huỷ, và gây lỗi khi event được phát lần sau.

## 5. UI / object trong scene gọi manager

```csharp
// Manager GameInstance
GameManager.Get<ResourceManager>().AddGameResource(ResourceType.Booster, (int)BoosterType.Booster1, 1);

// Manager World
WorldSubsystem.Get<PlayerManager>().Respawn();

// UI dùng chung cho cả menu và gameplay (có thể chưa có World):
if (WorldSubsystem.TryGet<PlayerManager>(out var player))
    player.Respawn();
```

Nếu gọi nhiều lần (trong `Update`, trong list item...), hãy cache vào field ở `Start()` thay vì gọi `Get<T>()` mỗi frame.

## 6. Thời điểm được phép gọi

| Phase / thời điểm | Được làm | Không được làm |
|---|---|---|
| `Initialize()` | Lấy reference, subscribe event | Đọc data, gọi logic của manager khác |
| `LoadData()` | Dùng các manager đã khai báo bằng `InitializeDependency` | Dùng manager chỉ lấy bằng `Get<T>()` (có thể chưa load) |
| `PostLoad()` | Dùng mọi manager cùng scope | — |
| Runtime, bên ngoài hệ thống | Gọi khi `GameManager.IsReady` / `WorldSubsystem.Current.IsReady` là `true` | Gọi trước khi ready (data còn null) |
| `Deinitialize()` | Huỷ subscribe, dọn state của chính mình | Gọi logic của manager khác (có thể đã bị huỷ) |

Đợi sẵn sàng từ UI hoặc object:

```csharp
await UniTask.WaitUntil(() => GameManager.IsReady, cancellationToken: destroyCancellationToken);
```

## 7. Ví dụ: ResourceManager

| Hàm | Dùng khi |
|---|---|
| `GetGameResource(type, defineValue)` | Lấy resource (dạng reactive, để bind UI). Chưa có thì tự tạo với giá trị 0 |
| `AddGameResource(type, defineValue, value)` | Cộng hoặc trừ (giá trị âm để trừ). Chưa có thì tự tạo |
| `AddGameResources(list)` | Cộng nhiều resource cùng lúc |

`AddNewGameResource` là `private` (chỉ để hiện nút trong Inspector qua `[Button]`). Thường không cần gọi, vì hai hàm trên đã tự tạo resource khi chưa có. Nếu thật sự cần tạo với giá trị khởi đầu khác 0, đổi nó thành `public`.

## 8. Lỗi hay gặp

| Lỗi | Nguyên nhân | Cách sửa |
|---|---|---|
| `Không tìm thấy XxxManager` | Component chưa phải con của `GameManager` / `WorldSubsystem`, hoặc sai base class | Kéo vào làm con, kiểm tra kế thừa `BaseManager` / `BaseWorldManager` |
| `Vòng phụ thuộc manager: A -> B -> A` | A và B cùng `InitializeDependency` lẫn nhau | Một bên đổi sang `Get<T>()`, hoặc tách phần dùng chung ra |
| `InitializeDependency chỉ được gọi trong Initialize()` | Gọi trong `LoadData` hoặc lúc runtime | Chuyển vào `Initialize()` |
| `GameManager chưa tồn tại` | Gọi trong `Awake` của object ở scene khác trước khi MainScene load, hoặc mở thẳng scene gameplay | Mở từ MainScene, load scene gameplay Additive |
| `Chưa có WorldSubsystem nào active` | Gọi manager World khi đang ở menu | Dùng `WorldSubsystem.TryGet` |
| NullReference trên data (`gameResources`...) | Gọi trước khi `IsReady` | Đợi `GameManager.IsReady` |
