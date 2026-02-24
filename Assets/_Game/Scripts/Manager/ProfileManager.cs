using Cysharp.Threading.Tasks;
using UnityEngine;

public class ProfileManager : BaseManager
{
    public override async UniTask LoadData()
    {
        await UniTask.CompletedTask;
    }
}
