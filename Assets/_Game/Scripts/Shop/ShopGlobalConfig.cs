using System;
using System.Collections.Generic;
using Balduu;
using Cysharp.Threading.Tasks;
using DataSave;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopGlobalConfig", menuName = "GlobalConfigs/ShopGlobalConfig")]
[GlobalConfig("Assets/Resources/GlobalConfig/")]
public class ShopGlobalConfig : GlobalConfig<ShopGlobalConfig>
{
    [BoxGroup("Packages Config")]
    public List<IAPPackage> iapPackages = new List<IAPPackage>();
    [BoxGroup("Packages Config"), Button]
    private async void FetchData()
    {
        iapPackages.Clear();
        await UniTask.CompletedTask;
    }

    public IAPPackage GetIAPPackage(string packageId)
    {
        for (var i = 0; i < iapPackages.Count; i++)
        {
            if(iapPackages[i].packageID == packageId)
                return iapPackages[i];
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

