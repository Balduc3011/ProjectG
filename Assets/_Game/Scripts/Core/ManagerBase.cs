using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Base chung cho mọi Manager thuộc một scope (GameInstance hoặc World).
/// <typeparamref name="TBase"/> là base class của scope (BaseManager / BaseWorldManager), nên manager
/// của scope này không thể lấy nhầm manager của scope khác.
///
/// Lifecycle do scope gọi, theo thứ tự phụ thuộc:
///   1. Initialize(managers) — đồng bộ, chạy trong Awake của scope. CHỈ nối dây phụ thuộc.
///   2. LoadData()           — async, tuần tự theo thứ tự Initialize (dependency load trước).
///   3. PostLoad()           — async, khi MỌI manager trong scope đã LoadData xong.
///   4. Deinitialize()       — khi scope bị huỷ, thứ tự ngược lại. Không đụng tới manager khác.
/// </summary>
public abstract class ManagerBase<TBase> : MonoBehaviour where TBase : ManagerBase<TBase>
{
    /// <summary>
    /// Khai báo phụ thuộc bằng <c>managers.InitializeDependency&lt;T&gt;()</c>:
    /// T chắc chắn được Initialize và LoadData trước manager này. Không cần quan tâm thứ tự trong Hierarchy.
    /// </summary>
    protected internal virtual void Initialize(ManagerCollection<TBase> managers) { }

    public virtual UniTask LoadData() => UniTask.CompletedTask;

    protected internal virtual UniTask PostLoad() => UniTask.CompletedTask;

    protected internal virtual void Deinitialize() { }
}
