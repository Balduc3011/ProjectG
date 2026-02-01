using System;
using System.Collections.Generic;
using MemoryPack;
using Newtonsoft.Json;
using UnityEngine;

namespace DataSave
{
    public static class DataFactory
    {
        private const bool EnableDebugAds = false;
        
        private static readonly Dictionary<string, Hash128> LastSavedHashByKey = new Dictionary<string, Hash128>(128);

        public static void SaveData<T>(string key, T data)
        {
            try
            {
                if (data == null)
                {
                    DeleteDataInternal(key);
                    LastSavedHashByKey.Remove(key);
                    return;
                }

                var bytes = MemoryPackSerializer.Serialize(data);
                var newHash = Hash128.Compute(bytes);
                
                if (HasDataChanged(key, newHash))
                {
                    var base64 = Convert.ToBase64String(bytes);
                    SecurePlayerPrefsV2.SetString(key, base64);
                    SecurePlayerPrefsV2.Save();
                    LastSavedHashByKey[key] = newHash;
                }
            }
            catch (Exception e)
            {
                LogError($"DataFactory.SaveData key={key} error: {e.Message}");
            }
        }

        public static T LoadData<T>(string key, T defaultValue = default)
        {
            Log($"DataFactory.LoadData key={key}");

            try
            {
                Log($"DataFactory.LoadData key={key}");

                var v2Data = TryLoadV2Data<T>(key);
                if (v2Data != null && !v2Data.Equals(default(T)))
                {
                    return v2Data;
                }

                Log($"DataFactory.LoadData key={key} V2 format failed / not found");
            }
            catch (Exception e)
            {
                LogError($"DataFactory.LoadData key={key} error: {e.Message}");
            }

            return defaultValue;
        }

        public static void DeleteKey(string key)
        {
            SecurePlayerPrefsV2.DeleteKey(key);
#if UNITY_EDITOR
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
#endif
        }

        public static string ConvertToJsonString<T>(T dataObject)
        {
            return JsonConvert.SerializeObject(dataObject, Formatting.Indented);
        }

        public static T ConvertFromJsonString<T>(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
                return default;

            try
            {
                return JsonConvert.DeserializeObject<T>(jsonString);
            }
            catch
            {
                return default;
            }
        }

        private static bool HasDataChanged(string key, Hash128 newHash)
        {
            return !LastSavedHashByKey.TryGetValue(key, out var oldHash) || !oldHash.Equals(newHash);
        }

        private static void DeleteDataInternal(string key)
        {
            if (SecurePlayerPrefsV2.HasKey(key))
                SecurePlayerPrefsV2.DeleteKey(key);
#if UNITY_EDITOR
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
#endif
            SecurePlayerPrefsV2.Save();
        }

        private static T TryLoadV2Data<T>(string key)
        {
            var v2 = SecurePlayerPrefsV2.GetString(key, string.Empty);
            Log($"DataFactory.LoadData key={key} v2={v2}");

            if (!string.IsNullOrEmpty(v2))
            {
                Log($"DataFactory.LoadData key={key} is V2 format");
                var bytes = Convert.FromBase64String(v2);
                LastSavedHashByKey[key] = Hash128.Compute(bytes);
                return MemoryPackSerializer.Deserialize<T>(bytes);
            }

            return default;
        }

        private static void Log(string message)
        {
#if UNITY_EDITOR
            Debug.Log(message);
#endif
        }

        private static void LogError(string message)
        {
#if UNITY_EDITOR
            Debug.Log(message);
#endif
        }
    }
}