using System;
using DataSave;

namespace DataSave
{
     public static class UserDataSaveManager
     {
          private static class Keys
          {
               public const string profile = "ProflieData";
               public const string resource = "ResourceData";
               public const string setting = "SettingData";
               public const string shop = "ShopData";
          }

          public static void SaveProfileData(ProfileDataSave data)                { DataSave.DataFactory.SaveData(Keys.profile, data); }
          public static void SaveResourceData(ResourceDataSave data)                { DataSave.DataFactory.SaveData(Keys.resource, data); }
          public static void SaveSettingData(SettingDataSave data)                  { DataSave.DataFactory.SaveData(Keys.setting, data); }
          public static void SaveShopData(ShopDataSave data)                        { DataSave.DataFactory.SaveData(Keys.shop, data); }

          public static ProfileDataSave        LoadProfileData()         { return DataSave.DataFactory.LoadData(Keys.profile,           new ProfileDataSave()); }
          public static ResourceDataSave       LoadResourceData()         { return DataSave.DataFactory.LoadData(Keys.resource,          new ResourceDataSave()); }
          public static SettingDataSave        LoadSettingData()          { return DataSave.DataFactory.LoadData(Keys.setting,           new SettingDataSave()); }
          public static ShopDataSave           LoadShopData()             { return DataSave.DataFactory.LoadData(Keys.shop,              new ShopDataSave()); }

          public static InGameData LoadAllData()
          {
               return new InGameData
               {
                    ProfileDataSave    = LoadProfileData(),
                    SettingDataSave    = LoadSettingData(),
                    ResourceDataSave   = LoadResourceData(),
                    ShopDataSave       = LoadShopData(),
               };
          }

          

          public static void SaveAllData(InGameData data)
          {
               SaveProfileData(data.ProfileDataSave);
               SaveSettingData(data.SettingDataSave);
               SaveResourceData(data.ResourceDataSave);
               SaveShopData(data.ShopDataSave);
          }

          public static void DeleteData(string key)
          {
               DataFactory.DeleteKey(key);
          }

          public static void DeleteAllData()
          {
               DataFactory.DeleteKey(Keys.profile);
               DataFactory.DeleteKey(Keys.setting);
               DataFactory.DeleteKey(Keys.resource);
               DataFactory.DeleteKey(Keys.shop);
          }
     }
}