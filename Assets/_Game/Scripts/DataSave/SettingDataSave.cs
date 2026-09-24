using System.Collections.Generic;
using MemoryPack;
using UnityEngine;
using UnityEngine.Serialization;

namespace DataSave
{
    [System.Serializable]
    [MemoryPackable]
    public partial class SettingDataSave
    {
        public static SettingDataSave Instance => InGameDataManager.Instance.InGameData.SettingDataSave;
        [MemoryPackOrder(0)]
        public List<SettingData> settingData;
        [MemoryPackOrder(1)]
        public LanguageType language;

        [MemoryPackConstructor]
        public SettingDataSave()
        {
            language = LanguageType.EN;
            settingData = new()
            {
                new SettingData(SettingType.Music, true),
                new SettingData(SettingType.Sound, true),
                new SettingData(SettingType.Haptic, true),
                new SettingData(SettingType.Effect, true),
            };
        }
        
        public static class Function
        {
            private static SettingDataSave SettingDataSave => InGameDataManager.Instance.InGameData.SettingDataSave;
        
            public static SettingData GetSettingData(SettingType settingType)
            {
                for (var i = 0; i < SettingDataSave.settingData.Count; i++)
                {
                    var settingData = SettingDataSave.settingData[i];
                    if (settingData.settingType == settingType)
                    {
                        return settingData;
                    }
                }
                return AddNewSettingData(settingType, true);
            }
            private static SettingData AddNewSettingData(SettingType settingType, bool isOn)
            {
                SettingData settingData = new SettingData(settingType, isOn);
                SettingDataSave.settingData.Add(settingData);
                InGameDataManager.Instance.SaveData();
                return settingData;
            }
            
            public static SettingData ChangeSettingData(SettingType settingType, bool isOn)
            {
                SettingData settingData = GetSettingData(settingType);
                settingData.isOn = isOn;
                InGameDataManager.Instance.SaveData();
                return settingData;
            }

            public static LanguageType GetLanguage()
            {
                return SettingDataSave.language;
            }

            public static void ChangeLanguage(LanguageType language)
            {
                SettingDataSave.language = language;
                InGameDataManager.Instance.SaveData();
            }
        }
    }
    
    [System.Serializable]
    [MemoryPackable]
    public partial class SettingData
    {
        [MemoryPackOrder(0)]
        public SettingType settingType;
        [MemoryPackOrder(1)]
        public bool isOn;
        
        [MemoryPackConstructor]
        public SettingData(SettingType settingType, bool isOn)
        {
            this.settingType = settingType;
            this.isOn = isOn;
        }
    }
}

