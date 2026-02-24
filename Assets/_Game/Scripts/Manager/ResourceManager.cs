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
    public ReactiveGameResource AddGameResource(ResourceType type, int defineValue, float value)
    {
        ResourceDataSave.Function.AddGameResource(type, defineValue, value);
        ReactiveGameResource resource = GetGameResource(type, defineValue);
        resource.Value.Value += value;
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
}