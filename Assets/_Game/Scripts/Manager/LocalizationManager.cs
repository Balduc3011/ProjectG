using Cysharp.Threading.Tasks;
using UnityEngine;

public class LocalizationManager : BaseManager
{
    public override async UniTask LoadData()
    {
        await UniTask.CompletedTask;
    }
    public string GetTextByKey(string key)
    {
        return key;
    }
    public string GetTextBySample(string sample)
    {
        return sample;
    }
    
    public string GetKeyBySample(string sample)
    {
        return "";
    }
}
