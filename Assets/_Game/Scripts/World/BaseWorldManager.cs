/// <summary>
/// Manager thuộc scope World (một scene gameplay). Đặt làm con của <see cref="WorldSubsystem"/> để được tự đăng ký.
/// Được phép dùng <c>GameManager.Get&lt;T&gt;()</c> (World → GameInstance).
/// Chiều ngược lại bị chặn bởi kiểu: GameManager chỉ chứa BaseManager, không bao giờ thấy BaseWorldManager.
/// </summary>
public abstract class BaseWorldManager : ManagerBase<BaseWorldManager>
{
    public WorldSubsystem World { get; internal set; }
}
