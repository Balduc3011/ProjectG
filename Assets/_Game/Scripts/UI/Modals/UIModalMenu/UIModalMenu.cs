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
    public class UIModalMenu : Modal
    {
        [field: SerializeField] public static string ModalName { get; private set; } = "UIModalMenu";
        [field: SerializeField] public UIModalMenuContext.UIPresenter UIPresenter { get; private set; }

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
    public class UIModalMenuContext
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
            [field: SerializeField] public Button SettingBtn { get; private set; }
            [field: SerializeField] public Button TestAdsBtn { get; private set; }
            [field: SerializeField] public Button PlayGameBtn { get; private set; }

            public UniTask Initialize(Memory<object> args)
            {
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
                View.SettingBtn?.AddListenerUI(OnClickSettingBtn);
                View.TestAdsBtn?.AddListenerUI(OnClickTestAdsBtn);
                View.PlayGameBtn?.AddListenerUI(GameManager.Instance.OpenBattleScene);
            }

            public UniTask Cleanup(Memory<object> args)
            {
                View.SettingBtn?.onClick.RemoveAllListeners();
                View.TestAdsBtn?.onClick.RemoveAllListeners();
                View.PlayGameBtn?.onClick.RemoveAllListeners();
                return UniTask.CompletedTask;
            }

            private async UniTask OnClickSettingBtn()
            {
                Debug.Log("OnClickSettingBtn");
                await UIManager.ShowModal("UIModalSetting", null);
            }

            private async UniTask OnClickTestAdsBtn()
            {
                await UIManager.ShowModal("UIModalTestAds", null);
            }
        }
    }
}
