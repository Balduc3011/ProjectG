using System;
using System.Collections.Generic;
using System.Globalization;
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
        [MemoryPackOrder(0)] public List<GameResource> gameResources;

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
                InGameDataManager.Instance.SaveData();
                return resource;
            }

            public static GameResource AddGameResource(ResourceType type, int defineValue, float value)
            {
                GameResource resource = GetGameResource(type, defineValue);
                resource.Value += value;
                InGameDataManager.Instance.SaveData();
                return resource;
            }

            /// <summary>
            /// Dùng cho resource mà value mua được là số giây thời hạn chứ không phải số lượng.
            /// Mua thêm chỉ cộng dồn vào expire time, Value luôn giữ ở 1 để khi hết hạn trừ 1 là về 0.
            /// <paramref name="nowUtc"/> phải là giờ server (TimeManager.UtcNow) để không cheat được bằng clock device.
            /// </summary>
            public static GameResource AddExpireDuration(ResourceType type, int defineValue, DateTime nowUtc, float seconds)
            {
                GameResource resource = GetGameResource(type, defineValue);
                DateTime from = resource.HasExpireTime() ? resource.GetExpireTime() : nowUtc;
                if (from < nowUtc)
                {
                    from = nowUtc;
                }
                resource.SetExpireTime(from.AddSeconds(seconds));
                resource.Value = 1;
                InGameDataManager.Instance.SaveData();
                return resource;
            }

            /// <summary>
            /// Resource hết hạn: trừ 1 value và xoá expire time.
            /// </summary>
            public static void ExpireResource(GameResource resource)
            {
                resource.Value = Mathf.Max(0, resource.Value - 1);
                resource.ExpireTime = null;
                InGameDataManager.Instance.SaveData();
            }

            public static void FormatDuplicateGameResource()
            {
                Dictionary<string, GameResource> uniqueResources = new Dictionary<string, GameResource>();
                List<GameResource> duplicates = new List<GameResource>();

                for (var i = 0; i < ResourceDataSave.gameResources.Count; i++)
                {
                    var resource = ResourceDataSave.gameResources[i];
                    string key = $"{resource.Type}_{resource.DefineValue}";
                    if (uniqueResources.ContainsKey(key))
                    {
                        uniqueResources[key].Value += resource.Value;
                        duplicates.Add(resource);
                    }
                    else
                    {
                        uniqueResources[key] = resource;
                    }
                }

                for (var i = 0; i < duplicates.Count; i++)
                {
                    ResourceDataSave.gameResources.Remove(duplicates[i]);
                }

                InGameDataManager.Instance.SaveData();

            }
        }
    }
    [System.Serializable]
    [MemoryPackable]
    public partial class GameResource
    {
        [MemoryPackOrder(0)]
        [field: SerializeField]
        public ResourceType Type { get; set; }

        [MemoryPackOrder(1)]
        [field: SerializeField]
        public int DefineValue { get; set; }

        [MemoryPackOrder(2)]
        [field: SerializeField]
        public float Value { get; set; } = 0;

        /// <summary>
        /// Thời điểm hết hạn của resource (UTC), rỗng nghĩa là không hết hạn.
        /// Lưu/parse bằng InvariantCulture format "o" để không phụ thuộc máy người chơi.
        /// </summary>
        [MemoryPackOrder(3)]
        [field: SerializeField]
        public string ExpireTime { get; set; }

        [MemoryPackConstructor]
        public GameResource(ResourceType type, int defineValue, float value, string expireTime = null)
        {
            Type = type;
            DefineValue = defineValue;
            Value = value;
            ExpireTime = expireTime;
        }

        public bool HasExpireTime()
        {
            return !string.IsNullOrEmpty(ExpireTime);
        }

        /// <summary>Luôn trả về UTC, kể cả khi chuỗi lưu là giờ local.</summary>
        public DateTime GetExpireTime()
        {
            if (DateTime.TryParse(ExpireTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime time))
            {
                return time.ToUniversalTime();
            }
            return DateTime.MinValue;
        }

        public void SetExpireTime(DateTime timeUtc)
        {
            ExpireTime = timeUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
        }
    }
}