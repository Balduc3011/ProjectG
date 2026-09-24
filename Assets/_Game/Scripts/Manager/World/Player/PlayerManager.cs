using Cysharp.Threading.Tasks;

/// <summary>
/// Mẫu manager scope World: quản lý Player trong scene gameplay. Chưa có logic.
/// Đặt làm con của WorldSubsystem. Lấy ra bằng <c>WorldSubsystem.Get&lt;PlayerManager&gt;()</c>.
/// </summary>
public class PlayerManager : BaseWorldManager
{
    // Ví dụ khai báo phụ thuộc:
    // private ResourceManager resourceManager;   // GameInstance — World → GameInstance là hợp lệ
    // private CameraManager cameraManager;       // cùng World

    protected internal override void Initialize(ManagerCollection<BaseWorldManager> managers)
    {
        // Chỉ nối dây. Không load data, không gọi logic của manager khác ở đây.
        // cameraManager = managers.InitializeDependency<CameraManager>();   // CameraManager sẽ LoadData trước PlayerManager
        // resourceManager = GameManager.Get<ResourceManager>();
    }

    public override UniTask LoadData()
    {
        // Load data của player (vd từ save). Lúc này GameManager đã sẵn sàng.
        return UniTask.CompletedTask;
    }

    protected internal override UniTask PostLoad()
    {
        // Mọi manager trong World đã LoadData xong: spawn player, bind camera, bật input...
        return UniTask.CompletedTask;
    }

    protected internal override void Deinitialize()
    {
        // Scene bị unload: huỷ subscribe event, trả object về pool...
    }
}
