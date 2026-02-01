using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Modals;

namespace Balduu
{
    public class SampleModal : Modal
    {
        [field: SerializeField] public string ModalName { get; private set; } = "SampleModal";
        [field: SerializeField] public SampleModalModel Model { get; private set; }
        [field: SerializeField] public SampleModalView View { get; private set; }

        public override async UniTask Initialize(Memory<object> args)
        {
            await Model.Initialize(args);
            await View.Initialize(args);
            View.CloseBtn.AddListenerUI(OnClickCloseModal);
        }

        public override UniTask Cleanup(Memory<object> args)
        {
            return UniTask.CompletedTask;
        }
        
        private void OnClickCloseModal()
        {
            CloseModal().Forget();
        }
        
        private async UniTask CloseModal()
        {
            ClearAction();
            await UIManager.CloseModal(ModalName, null);
        }

        private void ClearAction()
        {
            View.CloseBtn.onClick.RemoveAllListeners();
        }
    }

    [System.Serializable]
    public class SampleModalModel
    {
        public async UniTask Initialize(Memory<object> args)
        {
            await UniTask.CompletedTask;
        }
    }

    [System.Serializable]
    public class SampleModalView
    {
        [field: SerializeField] public CanvasGroup CanvasGroup {get; private set;}
        [field: SerializeField] public Button CloseBtn { get; private set; }
        
        public async UniTask Initialize(Memory<object> args)
        {
            await UniTask.CompletedTask;
        }
    }
}