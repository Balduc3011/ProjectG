using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Modals;

namespace Balduu
{
    public class UIModalSetting : Modal
    {
        public static class Events
        {
            
        }
        [field: SerializeField] public string ModalName { get; private set; } = "UIModalSetting";
        [field: SerializeField] public UIModalSettingModel Model { get; private set; }
        [field: SerializeField] public UIModalSettingView View { get; private set; }

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
    public class UIModalSettingModel
    {
        public async UniTask Initialize(Memory<object> args)
        {
            await UniTask.CompletedTask;
        }
    }

    [System.Serializable]
    public class UIModalSettingView
    {
        [field: SerializeField] public CanvasGroup MainView { get; private set; }
        [field: SerializeField] public Button CloseBtn { get; private set; }
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
