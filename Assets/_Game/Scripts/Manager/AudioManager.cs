using Cysharp.Threading.Tasks;
using UnityEngine;

public class AudioManager : BaseManager
{
    public override async UniTask LoadData()
    {
        await UniTask.CompletedTask;
    }
}
