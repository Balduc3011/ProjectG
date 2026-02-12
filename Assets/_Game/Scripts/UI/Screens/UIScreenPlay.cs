using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Balduu
{
    public class UIScreenPlay : ZBase.UnityScreenNavigator.Core.Screens.Screen
    {
        public static class Events
        {
            
        }
        [field: SerializeField] public static string ScreenName { get; private set; } = "UIScreenPlay";
        [field: SerializeField] public UIScreenPlayModel Model { get; private set; }
        [field: SerializeField] public UIScreenPlayView View { get; private set; }

        public override async UniTask Initialize(Memory<object> args)
        {
            await Model.Initialize(args);
            await View.Initialize(args);
            View.PauseBtn.AddListenerUI(OnClickPauseBtn);
        }

        public override UniTask Cleanup(Memory<object> args)
        {
            return UniTask.CompletedTask;
        }
        
        private async UniTask CloseScreen()
        {
            ClearAction();
            await UIManager.CloseScreen(ScreenName, null);
        }

        private void ClearAction()
        {
            View.PauseBtn.onClick.RemoveAllListeners();
        }
        
        private void OnClickPauseBtn()
        {
            GamePlayManager.Instance.PauseGame(true);
            ShowUIModalPause().Forget();
            
        }
        private async UniTask ShowUIModalPause()
        {
            await UIManager.ShowModal(UIModalPause.ModalName, null);
        }
    }

    [System.Serializable]
    public class UIScreenPlayModel
    {
        public async UniTask Initialize(Memory<object> args)
        {
            await UniTask.CompletedTask;
        }
    }

    [System.Serializable]
    public class UIScreenPlayView
    {
        [field: SerializeField] public CanvasGroup MainView {get; private set;}
        [field: SerializeField] public Button PauseBtn { get; private set; }

        public async UniTask Initialize(Memory<object> args)
        {
            await UniTask.CompletedTask;
        }
    }
}
