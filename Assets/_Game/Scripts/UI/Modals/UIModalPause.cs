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
            GameManager.GamePlayManager.PauseGame(false);
        }

        private void ClearAction()
        {
            View.CloseBtn.onClick.RemoveAllListeners();
        }
        
        private void OnClickBackToMenu()
        {
            CloseModal().Forget();
        }
    }

    [System.Serializable]
    public class UIModalPauseModel
    {
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
