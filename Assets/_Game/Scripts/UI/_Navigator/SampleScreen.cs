using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Balduu
{
    public class SampleScreen : ZBase.UnityScreenNavigator.Core.Screens.Screen
    {
        [field: SerializeField] public SampleScreenModel Model { get; private set; }
        [field: SerializeField] public SampleScreenView View { get; private set; }

        public override async UniTask Initialize(Memory<object> args)
        {
            await Model.Initialize(args);
            await View.Initialize(args);
        }

        public override UniTask Cleanup(Memory<object> args)
        {
            return UniTask.CompletedTask;
        }
    }
    
    [System.Serializable]
    public class SampleScreenModel
    {
        public async UniTask Initialize(Memory<object> args)
        {
            await UniTask.CompletedTask;
        }
    }
      
    [System.Serializable]
    public class SampleScreenView
    {
        [field: SerializeField] public CanvasGroup CanvasGroup {get; private set;}
        public async UniTask Initialize(Memory<object> args)
        {
            await UniTask.CompletedTask;
        }
    }
}

