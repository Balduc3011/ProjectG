using Cysharp.Threading.Tasks;
using UnityEngine;

public class BaseManager : MonoBehaviour
{
    public virtual async UniTask LoadData()
    {
        await UniTask.CompletedTask;
    }
}
