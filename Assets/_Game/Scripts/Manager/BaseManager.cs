/// <summary>
/// Manager thuộc scope GameInstance (sống suốt thời gian mở app).
/// Chỉ cần đặt làm con của GameManager là được tự đăng ký, không phải sửa GameManager.cs.
/// Lấy ra bằng <c>GameManager.Get&lt;T&gt;()</c>.
/// </summary>
public class BaseManager : ManagerBase<BaseManager>
{
}
