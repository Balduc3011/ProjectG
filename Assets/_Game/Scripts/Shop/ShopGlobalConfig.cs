using System;
using System.Collections.Generic;
using System.Globalization;
using Balduu;
using Cysharp.Threading.Tasks;
using DataSave;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "ShopGlobalConfig", menuName = "GlobalConfigs/ShopGlobalConfig")]
[GlobalConfig("Assets/Resources/GlobalConfig/")]
public class ShopGlobalConfig : GlobalConfig<ShopGlobalConfig>
{
    [BoxGroup("Packages Config")]
    [FormerlySerializedAs("iapPackages")]
    public List<ShopPackage> shopPackages = new List<ShopPackage>();
    [BoxGroup("Packages Config"), Button]
    private async void FetchData()
    {
        try
        {
            List<Dictionary<string, string>> rows = await GoogleSheetHelper.FetchTableAsync(DefaultGlobalConfig.linkSheetID, "ShopData");
            List<ShopPackage> packages = new List<ShopPackage>();
            Dictionary<string, ShopPackage> packageById = new Dictionary<string, ShopPackage>();

            for (int i = 0; i < rows.Count; i++)
            {
                Dictionary<string, string> row = rows[i];
                string packageId = row["PackageID"].Trim();
                if (string.IsNullOrEmpty(packageId)) continue;

                // Dòng đầu tiên của 1 PackageID chứa thông tin gói, các dòng sau chỉ thêm reward
                if (!packageById.TryGetValue(packageId, out ShopPackage package))
                {
                    ResourceType priceType = ParseEnum<ResourceType>(row["PriceResourceType"]);
                    package = new ShopPackage
                    {
                        packageID = packageId,
                        consumeType = ParseEnum<ConsumeType>(row["ConsumeType"]),
                        title = row["Title"],
                        description = row["Description"],
                        price = new GameResource(priceType, ParseDefineValue(priceType, row["PriceDefineValue"]), ParseFloat(row["PriceValue"])),
                        gameResources = new List<GameResource>(),
                    };
                    packageById.Add(packageId, package);
                    packages.Add(package);
                }

                if (string.IsNullOrWhiteSpace(row["RewardResourceType"])) continue;
                ResourceType rewardType = ParseEnum<ResourceType>(row["RewardResourceType"]);
                package.gameResources.Add(new GameResource(rewardType, ParseDefineValue(rewardType, row["RewardDefineValue"]), ParseFloat(row["RewardValue"])));
            }

            shopPackages = packages;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            Debug.Log($"[ShopGlobalConfig] Fetched {shopPackages.Count} packages");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ShopGlobalConfig] FetchData failed: {e}");
        }
    }

    private static T ParseEnum<T>(string value) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return default;
        if (Enum.TryParse(value.Trim(), true, out T result)) return result;
        throw new FormatException($"Invalid {typeof(T).Name}: '{value}'");
    }

    private static int ParseDefineValue(ResourceType type, string value)
    {
        return type switch
        {
            ResourceType.Currency => (int)ParseEnum<CurrencyType>(value),
            ResourceType.Booster => (int)ParseEnum<BoosterType>(value),
            ResourceType.Special => (int)ParseEnum<SpecialType>(value),
            _ => 0,
        };
    }

    private static float ParseFloat(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        return float.Parse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    public ShopPackage GetShopPackage(string packageId)
    {
        for (var i = 0; i < shopPackages.Count; i++)
        {
            if(shopPackages[i].packageID == packageId)
                return shopPackages[i];
        }

        return null;
    }
    
    public string GetResourceTextValue(GameResource resource, bool showIcon = false)
    {
        string value = "";
        value += resource.Value.ToString();
        switch (resource.Type)
        {
            case ResourceType.None:
                break;
            case ResourceType.Currency:
                if (resource.DefineValue == (int)CurrencyType.Money)
                    value = resource.Value.ToStringUI(2);
                else
                    value = $"{resource.Value}";
                break;
            case ResourceType.Booster:
                value = $"{resource.Value}";
                break;
            case ResourceType.Special:
                SpecialType specialType = (SpecialType)resource.DefineValue;
                switch (specialType)
                {
                    case SpecialType.None:
                        break;
                    case SpecialType.NoAds:
                        break;
                    case SpecialType.Ads:
                        value = "Watch";
                        break;
                    case SpecialType.IAP:
                        value = $"${resource.Value}";
                        break;
                    case SpecialType.InfiniteLife:
                        // Value của InfiniteLife là số giây thời hạn, không phải số lượng.
                        TimeSpan duration = TimeSpan.FromSeconds(resource.Value);
                        value = duration.TotalHours >= 1 ? $"{(int)duration.TotalHours}h" : $"{(int)duration.TotalMinutes}m";
                        break;
                    default:
                        break;
                }
                break;
            default:
                break;
        }
        return value;
    }
}

