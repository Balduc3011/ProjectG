using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ZBase.UnityScreenNavigator.Core.Activities;

namespace Balduu
{
    public class SampleActivity : Activity
    {
        [field: SerializeField] public string ActivityName { get; private set; } = "SampleActivity";
        [field: SerializeField] public SampleActivityModel Model { get; private set; }
        [field: SerializeField] public SampleActivityView View { get; private set; }

        public override async UniTask Initialize(Memory<object> args)
        {
            await Model.Initialize(args);
            await View.Initialize(args);
        }

        public override UniTask Cleanup(Memory<object> args)
        {
            ClearAction();
            return UniTask.CompletedTask;
        }
        
        private async UniTask CloseActivity()
        {
            await UIManager.CloseActivity(ActivityName, null);
        }

        private void ClearAction()
        {
        }
    }

    [System.Serializable]
    public class SampleActivityModel
    {
        public async UniTask Initialize(Memory<object> args)
        {
            await UniTask.CompletedTask;
        }
    }

    [System.Serializable]
    public class SampleActivityView
    {
        [field: SerializeField] public CanvasGroup CanvasGroup {get; private set;}
        
        public async UniTask Initialize(Memory<object> args)
        {
            await UniTask.CompletedTask;
        }
    }
}