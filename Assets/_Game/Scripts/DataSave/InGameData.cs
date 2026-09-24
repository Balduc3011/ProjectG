using Balduu.Utility;
using MemoryPack;
using UnityEngine;

namespace DataSave
{
    [MemoryPackable]
    [System.Serializable]
    public partial class InGameData
    {
        [field: SerializeField] public SettingDataSave SettingDataSave { get; set; }= new();
        [field: SerializeField] public ProfileDataSave ProfileDataSave { get; set; }= new();
        [field: SerializeField] public ResourceDataSave ResourceDataSave { get; set; }= new();
        [field: SerializeField] public ShopDataSave ShopDataSave { get; set; }= new();
        
        [MemoryPackOnSerialized]
        void OnSerialized()
        {
            DebugAds.Log("---------------------------InGameData OnSerialized----------------------------");
            SettingDataSave ??= new SettingDataSave();
            ProfileDataSave ??= new ProfileDataSave();
            ResourceDataSave ??= new ResourceDataSave();
            ShopDataSave ??= new ShopDataSave();
        }
     
        [MemoryPackOnDeserialized]
        void OnDeserialized()
        {
            DebugAds.Log("---------------------------InGameData OnDeserialized----------------------------");
            SettingDataSave ??= new SettingDataSave();
            ProfileDataSave ??= new ProfileDataSave();
            ResourceDataSave ??= new ResourceDataSave();
            ShopDataSave ??= new ShopDataSave();
        }
        
        public void Load()
        {
            SettingDataSave ??= new SettingDataSave();
            ProfileDataSave ??= new ProfileDataSave();
            ResourceDataSave ??= new ResourceDataSave();
            ShopDataSave ??= new ShopDataSave();
        }
    }
}

