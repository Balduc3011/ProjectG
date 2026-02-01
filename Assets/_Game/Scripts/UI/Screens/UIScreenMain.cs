using System;
using Cysharp.Threading.Tasks;
using Demo.Scripts;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Modals;
using ZBase.UnityScreenNavigator.Core.Views;

namespace Balduu
{
    public class UIScreenMain : ZBase.UnityScreenNavigator.Core.Screens.Screen
    {
        public static class Events
        {
            
        }
        [field: SerializeField] public static string ScreenName { get; private set; } = "UIScreenMain";
        [field: SerializeField] public UIScreenMainModel Model { get; private set; }
        [field: SerializeField] public UIScreenMainView View { get; private set; }

        public override async UniTask Initialize(Memory<object> args)
        {
            await Model.Initialize(args);
            await View.Initialize(args);
            View.SettingBtn.AddListenerUI(OnClickSettingBtn);
            View.TestAdsBtn.AddListenerUI(OnClickTestAdsBtn);
            View.PlayGameBtn.AddListenerUI(OnClickPlayGameBtn);
        }

        public override UniTask Cleanup(Memory<object> args)
        {
            View.SettingBtn.onClick.RemoveAllListeners();
            View.TestAdsBtn.onClick.RemoveAllListeners();
            View.PlayGameBtn.onClick.RemoveAllListeners();
            return UniTask.CompletedTask;
        }

        private async UniTask OnClickSettingBtn()
        {
            await UIManager.ShowModal("UIModalSetting", null);
        }
        private async UniTask OnClickTestAdsBtn()
        {
            await UIManager.ShowModal("UIModalTestAds", null);
        }
        
        private async UniTask OnClickPlayGameBtn()
        {
            UIActivityLoadingModel loadingModel = new UIActivityLoadingModel
            {
                WaitAction = OpenUIScreenPlay,
                DoneAction = null
            };
            UIActivityResource.Events.ShowContentEvent?.Invoke(false, false);
            await UIManager.ShowActivity(UIActivityLoading.ActivityName, new Memory<object>(new object[] { loadingModel }));
        }
        
        private async UniTask OpenUIScreenPlay()
        {
            UIActivityResource.Events.ShowContentEvent?.Invoke(true, false);
            await UIManager.ShowScreen(UIScreenPlay.ScreenName, null);
        }
    }

    [System.Serializable]
    public class UIScreenMainModel
    {
        public async UniTask Initialize(Memory<object> args)
        {
            await UniTask.CompletedTask;
        }
    }

    [System.Serializable]
    public class UIScreenMainView
    {
        [field: SerializeField] public CanvasGroup MainView {get; private set;}
        [field: SerializeField] public Button SettingBtn {get; private set;}
        [field: SerializeField] public Button TestAdsBtn {get; private set;}
        [field: SerializeField] public Button PlayGameBtn {get; private set;}
        
        public async UniTask Initialize(Memory<object> args)
        {
            await UniTask.CompletedTask;
        }
    }
}
