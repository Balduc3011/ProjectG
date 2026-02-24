using Cysharp.Threading.Tasks;
using UnityEngine;

public class TimeManager : BaseManager
{
    public override async UniTask LoadData()
    {
        await UniTask.CompletedTask;
    }
}
