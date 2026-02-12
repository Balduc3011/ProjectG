using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Modals;

namespace Balduu
{
    public class UIModalPause : Modal
    {
        public static class Events
        {
            
        }
        [field: SerializeField] public static string ModalName { get; private set; } = "UIModalPause";
        [field: SerializeField] public UIModalPauseModel Model { get; private set; }
        [field: SerializeField] public UIModalPauseView View { get; private set; }

        public override async UniTask Initialize(Memory<object> args)
        {
            await Model.Initialize(args);
            await View.Initialize(args);
            View.CloseBtn?.AddListenerUI(OnClickCloseModal);
            View.BackToMenuBtn?.AddListenerUI(OnClickBackToMenu);
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
            GamePlayManager.Instance.PauseGame(false);
        }

        private void ClearAction()
        {
            View.CloseBtn.onClick.RemoveAllListeners();
        }
        
        private void OnClickBackToMenu()
        {
            BackToMenu().Forget();
        }
        
        private async UniTask BackToMenu()
        {
            ClearAction();
            await UIManager.CloseModal(ModalName, null);
            UIActivityAlertModelData data = new UIActivityAlertModelData();
            data.alertValue = "Back To Menu\nAll progress will be lost.";
            data.confirmAction = ConfirmBackToMenu;
            data.declineAction = DeclineBackToMenu;
            data.positiveConfirm = false;
            await UIManager.ShowActivity(nameof(UIActivityAlert), new Memory<object>(new object[] { data }));
        }
        
        // Back To Menu Alert
        public void ConfirmBackToMenu()
        {
            GameManager.Instance.BackToMenu().Forget();
        }

        public void DeclineBackToMenu()
        {
            UIManager.ShowModal(UIModalPause.ModalName, null).Forget();
        }
    }

    [System.Serializable]
    public class UIModalPauseModel
    {
        [field: SerializeField] public bool LastPauseGame { get; private set; }
        public async UniTask Initialize(Memory<object> args)
        {
            await UniTask.CompletedTask;
        }
    }

    [System.Serializable]
    public class UIModalPauseView
    {
        [field: SerializeField] public CanvasGroup MainView { get; private set; }
        [field: SerializeField] public Button CloseBtn { get; private set; }
        [field: SerializeField] public Button BackToMenuBtn { get; private set; }
        [field: SerializeField] public SettingSwitch MusicSwitch { get; private set; }
        [field: SerializeField] public SettingSwitch SoundSwitch { get; private set; }
        [field: SerializeField] public SettingSwitch HapticSwitch { get; private set; }

        public async UniTask Initialize(Memory<object> args)
        {
            MusicSwitch.Init(SettingType.Music, GameManager.SettingManager.MusicStatus);
            SoundSwitch.Init(SettingType.Sound, GameManager.SettingManager.SoundStatus);
            HapticSwitch.Init(SettingType.Haptic, GameManager.SettingManager.HapticStatus);
            await UniTask.CompletedTask;
        }
    }
}
