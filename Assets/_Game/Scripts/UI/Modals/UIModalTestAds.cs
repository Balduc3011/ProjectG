using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Modals;

namespace Balduu
{
    public class UIModalTestAds : Modal
    {
        [field: SerializeField] public string ModalName { get; private set; } = "UIModalTestAds";
        [field: SerializeField] public UIModalTestAdsModel Model { get; private set; }
        [field: SerializeField] public UIModalTestAdsView View { get; private set; }

        public override async UniTask Initialize(Memory<object> args)
        {
            await Model.Initialize(args);
            await View.Initialize(args);
            View.CloseBtn.AddListenerUI(OnClickCloseModal);
            View.ShowInterBtn.AddListenerUI(ShowInterstitialAd);
            View.ShowRewardBtn.AddListenerUI(ShowRewardedAd);
            View.ShowBannerBtn.AddListenerUI(ShowBannerAd);
            View.HideBannerBtn.AddListenerUI(HideBannerAd);
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
        
        private void ShowInterstitialAd()
        {
            // Implement ad showing logic here
        }
        
        private void ShowRewardedAd()
        {
            // Implement ad showing logic here
        }
        
        private void ShowBannerAd()
        {
            // Implement ad showing logic here
        }
        
        private void HideBannerAd()
        {
            // Implement ad hiding logic here
        }
    }

    [System.Serializable]
    public class UIModalTestAdsModel
    {
        public async UniTask Initialize(Memory<object> args)
        {
            await UniTask.CompletedTask;
        }
    }

    [System.Serializable]
    public class UIModalTestAdsView
    {
        [field: SerializeField] public CanvasGroup MainView { get; private set; }
        [field: SerializeField] public Button CloseBtn { get; private set; }
        [field: SerializeField] public Button ShowInterBtn { get; private set; }
        [field: SerializeField] public Button ShowRewardBtn { get; private set; }
        [field: SerializeField] public Button ShowBannerBtn { get; private set; }
        [field: SerializeField] public Button HideBannerBtn { get; private set; }

        public async UniTask Initialize(Memory<object> args)
        {
            await UniTask.CompletedTask;
        }
    }
}
