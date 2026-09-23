using System;
using System.Collections.Generic;
using Baseduu.ReactiveFields;
using Cysharp.Threading.Tasks;
using DataSave;
using NUnit.Framework;
using R3;
using Sirenix.OdinInspector;
using UnityEngine;

public class ResourceManager : BaseManager
{
    public List<ReactiveGameResource> gameResources;

    public override async UniTask LoadData()
    {
        ResourceDataSave.Function.FormatDuplicateGameResource();
        List<GameResource> savedResources = ResourceDataSave.Function.GameResources();
        gameResources = new List<ReactiveGameResource>();
        for (var i = 0; i < savedResources.Count; i++)
        {
            var resource = savedResources[i];
            ReactiveGameResource reactiveResource = new ReactiveGameResource(resource.Type, resource.DefineValue, resource.Value);
            gameResources.Add(reactiveResource);
        }
        await UniTask.CompletedTask;
    }
    
    public ReactiveGameResource GetGameResource(ResourceType type, int defineValue)
    {
        for (var i = 0; i < gameResources.Count; i++)
        {
            var resource = gameResources[i];
            if (resource.Type == type && resource.DefineValue == defineValue)
            {
                return resource;
            }
        }
        return AddNewGameResource(type, defineValue, 0);
    }
    
    [Button]
    private ReactiveGameResource AddNewGameResource(ResourceType type, int defineValue, float value)
    {
        ResourceDataSave.Function.AddNewGameResource(type, defineValue, value);
        ReactiveGameResource resource = new ReactiveGameResource(type, defineValue, value);
        gameResources.Add(resource);
        return resource;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="type">ResourceType</param>
    /// <param name="defineValue"></param>
    /// <param name="value">Positive to Add, Negative to Subtract</param>
    /// <returns></returns>
    [Button]
    public ReactiveGameResource AddGameResource(ResourceType type, int defineValue, float value)
    {
        if (IsExpireDurationResource(type, defineValue))
        {
            return AddExpireDurationResource(type, defineValue, value);
        }
        ResourceDataSave.Function.AddGameResource(type, defineValue, value);
        ReactiveGameResource resource = GetGameResource(type, defineValue);
        resource.Value.Value += value;
        return resource;
    }

    /// <summary>
    /// Resource mà value cộng vào là số giây thời hạn chứ không phải số lượng. Hiện chỉ có InfiniteLife.
    /// </summary>
    private static bool IsExpireDurationResource(ResourceType type, int defineValue)
    {
        return type == ResourceType.Special && defineValue == (int)SpecialType.InfiniteLife;
    }

    private ReactiveGameResource AddExpireDurationResource(ResourceType type, int defineValue, float seconds)
    {
        DateTime nowUtc = GameManager.TimeManager.UtcNow;
        GameResource saved = ResourceDataSave.Function.AddExpireDuration(type, defineValue, nowUtc, seconds);
        ReactiveGameResource resource = GetGameResource(type, defineValue);
        resource.Value.Value = saved.Value;
        return resource;
    }
    
    public void AddGameResources(List<GameResource> resources)
    {
        for (var i = 0; i < resources.Count; i++)
        {
            var res = resources[i];
            AddGameResource(res.Type, res.DefineValue, res.Value);
        }
    }

    private const float EXPIRE_CHECK_INTERVAL = 1f;
    // Khởi tạo bằng interval để tick đầu tiên chạy ngay, bắt luôn resource hết hạn lúc app đang đóng.
    private float expireCheckTimer = EXPIRE_CHECK_INTERVAL;

    private void Update()
    {
        if (gameResources == null)
        {
            return;
        }
        expireCheckTimer += Time.deltaTime;
        if (expireCheckTimer < EXPIRE_CHECK_INTERVAL)
        {
            return;
        }
        expireCheckTimer = 0f;
        CheckExpiredResources();
    }

    private void CheckExpiredResources()
    {
        DateTime nowUtc = GameManager.TimeManager.UtcNow;
        List<GameResource> savedResources = ResourceDataSave.Function.GameResources();
        for (var i = 0; i < savedResources.Count; i++)
        {
            var saved = savedResources[i];
            if (!saved.HasExpireTime() || saved.GetExpireTime() > nowUtc)
            {
                continue;
            }
            ResourceDataSave.Function.ExpireResource(saved);
            GetGameResource(saved.Type, saved.DefineValue).Value.Value = saved.Value;
        }
    }
}

[System.Serializable]
public partial class ReactiveGameResource
{
    [field: SerializeField] public ResourceType Type { get; set; }
    [field: SerializeField] public int DefineValue { get; set; }
    [field: SerializeField] public ReactiveValue<float> Value { get; set; }

    public ReactiveGameResource(ResourceType type, int defineValue, float value)
    {
        Type = type;
        DefineValue = defineValue;
        Value = new ReactiveValue<float>(value);
    }
}

public enum ResourceType
{
    None = 0,
    Currency = 1,
    Booster = 2,
    Special = 3,
}

public enum CurrencyType
{
    None = 0,
    Money = 1,
    Energy = 2,
    Life = 3,
}

public enum BoosterType
{
    None = 0,
    Booster1 = 1,
    Booster2 = 2,
    Booster3 = 3,
    Booster4 = 4,
}

public enum SpecialType
{
    None = 0,
    NoAds = 1,
    IAP = 2,
    Ads = 3,
    InfiniteLife = 4,
}