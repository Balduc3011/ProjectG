using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Danh sách manager của một scope. Thứ tự khởi tạo được suy ra từ phụ thuộc khai báo trong Initialize(),
/// không phụ thuộc thứ tự dòng code hay thứ tự trong Hierarchy. Vòng phụ thuộc báo lỗi ngay khi khởi tạo.
/// </summary>
public sealed class ManagerCollection<TBase> where TBase : ManagerBase<TBase>
{
    private enum Phase { Created, Initializing, Initialized, Deinitialized }

    private readonly string scopeName;
    private readonly List<TBase> registered = new List<TBase>();
    private readonly Dictionary<Type, TBase> byType = new Dictionary<Type, TBase>();
    private readonly List<TBase> initOrder = new List<TBase>();   // cũng là thứ tự LoadData / PostLoad
    private readonly List<TBase> initStack = new List<TBase>();   // chuỗi đang khởi tạo, để báo vòng phụ thuộc
    private Phase phase = Phase.Created;

    public ManagerCollection(string scopeName, IEnumerable<TBase> managers)
    {
        this.scopeName = scopeName;
        foreach (var manager in managers)
        {
            var type = manager.GetType();
            if (byType.TryGetValue(type, out var existing))
            {
                Debug.LogError($"[{scopeName}] Trùng manager {type.Name} trên '{manager.name}' (đã có trên '{existing.name}'), bỏ qua.", manager);
                continue;
            }
            byType.Add(type, manager);
            registered.Add(manager);
        }
    }

    public T Get<T>() where T : TBase
    {
        if (TryGet(out T manager)) return manager;
        throw new InvalidOperationException($"[{scopeName}] Không tìm thấy {typeof(T).Name}. Hãy thêm nó làm con của object scope.");
    }

    public bool TryGet<T>(out T manager) where T : TBase
    {
        if (byType.TryGetValue(typeof(T), out var exact))
        {
            manager = (T)exact;
            return true;
        }
        // T là base class / abstract của một manager đã đăng ký.
        for (var i = 0; i < registered.Count; i++)
        {
            if (registered[i] is T match)
            {
                manager = match;
                return true;
            }
        }
        manager = null;
        return false;
    }

    /// <summary>Chỉ gọi trong Initialize(). Bảo đảm T được Initialize (và vì thế LoadData) trước manager đang gọi.</summary>
    public T InitializeDependency<T>() where T : TBase
    {
        if (phase != Phase.Initializing)
            throw new InvalidOperationException($"[{scopeName}] InitializeDependency<{typeof(T).Name}> chỉ được gọi trong Initialize().");
        var dependency = Get<T>();
        InitializeOne(dependency);
        return dependency;
    }

    public void InitializeAll()
    {
        if (phase != Phase.Created) return;
        phase = Phase.Initializing;
        for (var i = 0; i < registered.Count; i++)
        {
            InitializeOne(registered[i]);
        }
        phase = Phase.Initialized;
    }

    public UniTask LoadAll(CancellationToken ct) => RunInOrder(m => m.LoadData(), nameof(ManagerBase<TBase>.LoadData), ct);

    public UniTask PostLoadAll(CancellationToken ct) => RunInOrder(m => m.PostLoad(), "PostLoad", ct);

    public void DeinitializeAll()
    {
        if (phase == Phase.Deinitialized) return;
        phase = Phase.Deinitialized;
        for (var i = initOrder.Count - 1; i >= 0; i--)
        {
            try
            {
                initOrder[i].Deinitialize();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    private void InitializeOne(TBase manager)
    {
        if (initOrder.Contains(manager)) return;
        if (initStack.Contains(manager))
        {
            var chain = string.Join(" -> ", initStack.ConvertAll(m => m.GetType().Name)) + " -> " + manager.GetType().Name;
            throw new InvalidOperationException($"[{scopeName}] Vòng phụ thuộc manager: {chain}");
        }

        initStack.Add(manager);
        try
        {
            manager.Initialize(this);
        }
        finally
        {
            initStack.RemoveAt(initStack.Count - 1);
        }
        initOrder.Add(manager);
    }

    private async UniTask RunInOrder(Func<TBase, UniTask> step, string stepName, CancellationToken ct)
    {
        if (phase != Phase.Initialized)
            throw new InvalidOperationException($"[{scopeName}] {stepName} gọi khi collection đang ở trạng thái {phase}.");

        for (var i = 0; i < initOrder.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var manager = initOrder[i];
            try
            {
                await step(manager);
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                Debug.LogError($"[{scopeName}] {manager.GetType().Name}.{stepName} lỗi, dừng khởi động scope.", manager);
                throw;
            }
        }
    }
}
