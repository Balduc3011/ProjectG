using Cysharp.Threading.Tasks;
using Sirenix.Utilities;
using UnityEngine;

[CreateAssetMenu(fileName = "DefaultGlobalConfig", menuName = "GlobalConfigs/DefaultGlobalConfig")]
[GlobalConfig("Assets/Resources/GlobalConfig/")]
public class DefaultGlobalConfig : GlobalConfig<DefaultGlobalConfig>
{
    public static string linkSheetID = "13beq-IYyBoLcBpoKEOQ_SZ2FfDqeGHHMcFoY3Mt5HOM";
    
    private async UniTask FetchData()
    {
        await UniTask.CompletedTask;
    }
}
