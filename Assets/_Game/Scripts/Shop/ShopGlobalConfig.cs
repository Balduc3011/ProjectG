using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
}

