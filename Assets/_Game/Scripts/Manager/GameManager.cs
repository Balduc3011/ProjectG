using System;
using System.Threading;
using Balduu.Utility.CustomComponent;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Scope GameInstance (≈ UGameInstanceSubsystem của Unreal): sống từ lúc mở app tới lúc tắt app.
/// Tự thu thập mọi BaseManager là con của GameObject này và khởi tạo theo thứ tự phụ thuộc
/// (xem <see cref="ManagerCollection{TBase}"/>). Thêm manager mới: tạo class kế thừa BaseManager,
/// đặt làm con của GameManager, lấy ra bằng GameManager.Get&lt;T&gt;(). Không sửa file này.
///
/// Phụ thuộc một chiều: World → GameInstance. Manager ở đây không được gọi tới WorldSubsystem.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class GameManager : AwaitableCachedMonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>True khi mọi manager đã LoadData + PostLoad xong.</summary>
    public static bool IsReady => Instance != null && Instance.isReady;

    private ManagerCollection<BaseManager> managers;
    private bool isReady;

    public static T Get<T>() where T : BaseManager
    {
        if (Instance == null)
            throw new InvalidOperationException($"GameManager chưa tồn tại khi lấy {typeof(T).Name}.");
        return Instance.managers.Get<T>();
    }

    public static bool TryGet<T>(out T manager) where T : BaseManager
    {
        manager = null;
        if (Instance == null)
        {
            Debug.LogError($"GameManager chưa tồn tại khi lấy {typeof(T).Name}.");
            return false;
        }
        if (!Instance.managers.TryGet(out manager))
        {
            Debug.LogError($"[GameInstance] Không tìm thấy {typeof(T).Name}. Hãy thêm nó làm con của GameManager.", Instance);
            return false;
        }
        return true;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        managers = new ManagerCollection<BaseManager>("GameInstance", GetComponentsInChildren<BaseManager>(true));
        managers.InitializeAll();
    }

    private void Start()
    {
        if (Instance != this) return;
        BootAsync(OnDestroyCancellationToken).Forget();
    }

    private async UniTaskVoid BootAsync(CancellationToken ct)
    {
        try
        {
            await managers.LoadAll(ct);
            await UniTask.DelayFrame(1, cancellationToken: ct);
            await managers.PostLoadAll(ct);
            Application.targetFrameRate = 60;
            isReady = true;
        }
        catch (OperationCanceledException)
        {
            // GameManager bị huỷ giữa chừng (tắt app / thoát Play Mode).
        }
    }

    protected override void OnDestroy()
    {
        if (Instance == this)
        {
            isReady = false;
            managers?.DeinitializeAll();
            Instance = null;
        }
        base.OnDestroy();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        // Giữ đúng khi tắt Domain Reload (Enter Play Mode Options).
        Instance = null;
    }
}
