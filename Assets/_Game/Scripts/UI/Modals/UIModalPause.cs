using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Baseduu.UINavigator.Core.Modals;
using Baseduu.UINavigator.MVPPattern;

namespace Balduu
{
    // Thin view class. It only holds the presenter and forwards the lifecycle to it.
    public class UIModalPause : Modal
    {
        [field: SerializeField] public static string ModalName { get; private set; } = "UIModalPause";
        [field: SerializeField] public UIModalPauseContext.UIPresenter UIPresenter { get; private set; }

        // Container-driven UIs are initialized by the navigator (it calls Initialize/Cleanup on
        // this component); embedded UIs (e.g. a tab hosted inside a screen) are initialized by a
        // direct Initialize call. Both paths reach these overrides, so the presenter is driven
        // here instead of via AddLifecycleEvent.
        public override UniTask Initialize(Memory<object> args)
        {
            return UIPresenter.Initialize(args);
        }

        public override UniTask Cleanup(Memory<object> args)
        {
            return UIPresenter.Cleanup(args);
        }
    }

    [Serializable]
    public class UIModalPauseContext
    {
        public static class Events
        {
        }

        [HideLabel]
        [Serializable]
        public class UIModel : IAModel
        {
            [field: Title(nameof(UIModel))]
            [field: SerializeField] public bool LastPauseGame { get; private set; }

            public UniTask Initialize(Memory<object> args)
            {
                return UniTask.CompletedTask;
            }
        }

        [HideLabel]
        [Serializable]
        public class UIView : IAView
        {
            [field: Title(nameof(UIView))]
            [field: SerializeField] public CanvasGroup MainView { get; private set; }
            [field: SerializeField] public Button CloseBtn { get; private set; }
            [field: SerializeField] public Button BackToMenuBtn { get; private set; }
            [field: SerializeField] public SettingSwitch MusicSwitch { get; private set; }
            [field: SerializeField] public SettingSwitch SoundSwitch { get; private set; }
            [field: SerializeField] public SettingSwitch HapticSwitch { get; private set; }

            public UniTask Initialize(Memory<object> args)
            {
                MusicSwitch.Init(SettingType.Music, GameManager.Get<SettingManager>().MusicStatus);
                SoundSwitch.Init(SettingType.Sound, GameManager.Get<SettingManager>().SoundStatus);
                HapticSwitch.Init(SettingType.Haptic, GameManager.Get<SettingManager>().HapticStatus);
                return UniTask.CompletedTask;
            }
        }

        [HideLabel]
        [Serializable]
        public class UIPresenter : IAPresenter, IModalLifecycleEventSimple
        {
            [field: Title(nameof(UIPresenter))]
            [field: SerializeField] public UIModel Model { get; private set; } = new();
            [field: SerializeField] public UIView View { get; private set; } = new();

            public async UniTask Initialize(Memory<object> args)
            {
                await Model.Initialize(args);
                await View.Initialize(args);
                View.CloseBtn?.AddListenerUI(OnClickCloseModal);
                View.BackToMenuBtn?.AddListenerUI(OnClickBackToMenu);
            }

            public UniTask Cleanup(Memory<object> args)
            {
                View.CloseBtn?.onClick.RemoveAllListeners();
                View.BackToMenuBtn?.onClick.RemoveAllListeners();
                return UniTask.CompletedTask;
            }

            private void OnClickCloseModal()
            {
                CloseModal().Forget();
            }

            private async UniTask CloseModal()
            {
                await UIManager.CloseModal(UIModalPause.ModalName, null);
                GamePlayManager.Instance.PauseGame(false);
            }

            private void OnClickBackToMenu()
            {
                BackToMenu().Forget();
            }

            private async UniTask BackToMenu()
            {
                await UIManager.CloseModal(UIModalPause.ModalName, null);
                UIActivityAlertModelData data = new UIActivityAlertModelData();
                data.alertValue = "Back To Menu\nAll progress will be lost.";
                data.confirmAction = ConfirmBackToMenu;
                data.declineAction = DeclineBackToMenu;
                data.positiveConfirm = false;
                await UIManager.ShowActivity(nameof(UIActivityAlert), new Memory<object>(new object[] { data }));
            }

            // Back To Menu Alert
            private void ConfirmBackToMenu()
            {
                GameManager.Get<GameSceneManager>().BackToMenu().Forget();
            }

            private void DeclineBackToMenu()
            {
                UIManager.ShowModal(UIModalPause.ModalName, null).Forget();
            }
        }
    }
}
