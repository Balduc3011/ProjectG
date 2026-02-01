using System.Collections.Generic;
using MemoryPack;
using UnityEngine;
using UnityEngine.Serialization;

namespace DataSave
{
    [System.Serializable]
    [MemoryPackable]
    public partial class ResourceDataSave
    {
        public static ResourceDataSave Instance => InGameDataManager.Instance.InGameData.ResourceDataSave;
        [MemoryPackOrder(0)]
        public List<GameResource> gameResources;

        [MemoryPackConstructor]
        public ResourceDataSave()
        {
            gameResources = new()
            {
                new GameResource(ResourceType.Booster, (int)BoosterType.Booster1, 0),
                new GameResource(ResourceType.Booster, (int)BoosterType.Booster2, 0),
                new GameResource(ResourceType.Booster, (int)BoosterType.Booster3, 0),
                new GameResource(ResourceType.Booster, (int)BoosterType.Booster4, 0),
            };
        }
        
        public static class Function
        {
            private static ResourceDataSave ResourceDataSave => InGameDataManager.Instance.InGameData.ResourceDataSave;

            public static List<GameResource> GameResources()
            {
                return ResourceDataSave.gameResources;
            }
            
            public static GameResource GetGameResource(ResourceType type, int defineValue)
            {
                for (var i = 0; i < ResourceDataSave.gameResources.Count; i++)
                {
                    var resource = ResourceDataSave.gameResources[i];
                    if (resource.Type == type && resource.DefineValue == defineValue)
                    {
                        return resource;
                    }
                }
                return AddNewGameResource(type, defineValue, 0);
            }
            
            public static GameResource AddNewGameResource(ResourceType type, int defineValue, float value)
            {
                GameResource resource = new GameResource(type, defineValue, value);
                ResourceDataSave.gameResources.Add(resource);
                return resource;
            }

            public static GameResource AddGameResource(ResourceType type, int defineValue, float value)
            {
                GameResource resource = GetGameResource(type, defineValue);
                resource.Value += value;
                return resource;
            }
        }
    }

    [System.Serializable]
    [MemoryPackable]
    public partial class GameResource
    {
        [MemoryPackOrder(0)]
        [field: SerializeField] public ResourceType Type { get; set; }
        [MemoryPackOrder(1)]
        [field: SerializeField] public int DefineValue { get; set; }
        [MemoryPackOrder(2)]
        [field: SerializeField] public float Value { get; set; } = 0;

        [MemoryPackConstructor]
        public GameResource(ResourceType type, int defineValue, float value)
        {
            Type = type;
            DefineValue = defineValue;
            Value = value;
        }
    }
}