using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Scope World (≈ UWorldSubsystem của Unreal): sống theo scene gameplay, bị huỷ khi scene unload.
/// Gắn component này vào 1 GameObject trong scene gameplay, các BaseWorldManager đặt làm con của nó.
///
/// Khởi động:
///   Awake  → Initialize mọi manager (nối dây phụ thuộc).
///   Start  → đợi GameManager sẵn sàng → LoadData → PostLoad → IsReady = true.
///
/// Scene gameplay nên được load Additive từ MainScene, để GameManager (GameInstance) luôn tồn tại.
/// </summary>
[DefaultExecutionOrder(-900)]
public class WorldSubsystem : MonoBehaviour
{
    public static WorldSubsystem Current { get; private set; }

    public bool IsReady { get; private set; }

    private ManagerCollection<BaseWorldManager> managers;

    public static T Get<T>() where T : BaseWorldManager
    {
        if (Current == null)
            throw new InvalidOperationException($"Chưa có WorldSubsystem nào active khi lấy {typeof(T).Name}.");
        return Current.managers.Get<T>();
    }

    public static bool TryGet<T>(out T manager) where T : BaseWorldManager
    {
        manager = null;
        return Current != null && Current.managers.TryGet(out manager);
    }

    private void Awake()
    {
        if (Current != null && Current != this)
        {
            Debug.LogError($"[World] Chỉ được có 1 WorldSubsystem. '{name}' bị tắt vì '{Current.name}' đang active.", this);
            enabled = false;
            return;
        }
        Current = this;

        var found = GetComponentsInChildren<BaseWorldManager>(true);
        for (var i = 0; i < found.Length; i++)
        {
            found[i].World = this;
        }
        managers = new ManagerCollection<BaseWorldManager>($"World:{gameObject.scene.name}", found);
        managers.InitializeAll();
    }

    private void Start()
    {
        BootAsync(destroyCancellationToken).Forget();
    }

    private async UniTaskVoid BootAsync(CancellationToken ct)
    {
        try
        {
            if (!GameManager.IsReady)
            {
                if (GameManager.Instance == null)
                    Debug.LogWarning("[World] Chưa có GameManager. Hãy mở scene này từ MainScene (load Additive).", this);
                await UniTask.WaitUntil(() => GameManager.IsReady, cancellationToken: ct);
            }

            await managers.LoadAll(ct);
            await managers.PostLoadAll(ct);
            IsReady = true;
        }
        catch (OperationCanceledException)
        {
            // Scene bị unload giữa chừng.
        }
    }

    private void OnDestroy()
    {
        if (Current != this) return;
        IsReady = false;
        managers?.DeinitializeAll();
        Current = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        // Giữ đúng khi tắt Domain Reload (Enter Play Mode Options).
        Current = null;
    }
}
