using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Baseduu.UINavigator.Core.Screens;
using Baseduu.UINavigator.MVPPattern;
using Screen = Baseduu.UINavigator.Core.Screens.Screen;

namespace Balduu
{
    // Thin view class. It only holds the presenter and forwards the lifecycle to it.
    public class UIScreenPlay : Screen
    {
        [field: SerializeField] public static string ScreenName { get; private set; } = "UIScreenPlay";
        [field: SerializeField] public UIScreenPlayContext.UIPresenter UIPresenter { get; private set; }

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
    public class UIScreenPlayContext
    {
        public static class Events
        {
        }

        [HideLabel]
        [Serializable]
        public class UIModel : IAModel
        {
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
            [field: SerializeField] public Button PauseBtn { get; private set; }

            public UniTask Initialize(Memory<object> args)
            {
                return UniTask.CompletedTask;
            }
        }

        [HideLabel]
        [Serializable]
        public class UIPresenter : IAPresenter, IScreenLifecycleEventSimple
        {
            [field: Title(nameof(UIPresenter))]
            [field: SerializeField] public UIModel Model { get; private set; } = new();
            [field: SerializeField] public UIView View { get; private set; } = new();

            public async UniTask Initialize(Memory<object> args)
            {
                await Model.Initialize(args);
                await View.Initialize(args);
                View.PauseBtn?.AddListenerUI(OnClickPauseBtn);
            }

            public UniTask Cleanup(Memory<object> args)
            {
                View.PauseBtn?.onClick.RemoveAllListeners();
                return UniTask.CompletedTask;
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
    }
}
