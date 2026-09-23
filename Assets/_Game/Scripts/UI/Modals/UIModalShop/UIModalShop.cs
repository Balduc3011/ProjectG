using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Baseduu.UINavigator.Core.Modals;
using Baseduu.UINavigator.MVPPattern;

namespace Balduu
{
    // Thin view class. It only holds the presenter and forwards the lifecycle to it.
    public class UIModalShop : Modal, IMenuTab
    {
        [field: SerializeField] public static string ModalName { get; private set; } = "UIModalShop";
        [field: SerializeField] public UIModalShopContext.UIPresenter UIPresenter { get; private set; }

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

        public void OnTabLeave()
        {
            UIPresenter.OnTabLeave();
        }
    }

    [Serializable]
    public class UIModalShopContext
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
            [field: SerializeField] public Button CloseBtn { get; private set; }
            [field: SerializeField] public ScrollRect PackScrollRect { get; private set; }
            [field: SerializeField] public List<UIShopPack> UiShopPacks { get; private set; }

            public UniTask Initialize(Memory<object> args)
            {
                return UniTask.CompletedTask;
            }

            public void ResetScrollToTop()
            {
                if (PackScrollRect == null) return;
                PackScrollRect.StopMovement();
                PackScrollRect.verticalNormalizedPosition = 1f;
            }

            public void InitShopPack()
            {
                for (var i = 0; i < UiShopPacks.Count; i++)
                {
                    UiShopPacks[i].Init();
                }
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
                View.InitShopPack();
            }

            public UniTask Cleanup(Memory<object> args)
            {
                View.CloseBtn?.onClick.RemoveAllListeners();
                return UniTask.CompletedTask;
            }

            public void OnTabLeave()
            {
                View.ResetScrollToTop();
            }

            private void OnClickCloseModal()
            {
                UIManager.CloseModal(UIModalShop.ModalName, null).Forget();
            }
        }
    }
}
