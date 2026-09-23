using System;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;
using Baseduu.UINavigator.Core.Activities;
using Baseduu.UINavigator.MVPPattern;

namespace Balduu
{
    // Thin view class. It only holds the presenter and forwards the lifecycle to it.
    public class UIActivityResource : Activity
    {
        [field: SerializeField] public bool ShowOnTop { get; private set; }
        [field: SerializeField] public static string ActivityName { get; private set; } = "UIActivityResource";
        [field: SerializeField] public UIActivityResourceContext.UIPresenter UIPresenter { get; private set; }

        // Container-driven UIs are initialized by the navigator (it calls Initialize/Cleanup on
        // this component); embedded UIs (e.g. a tab hosted inside a screen) are initialized by a
        // direct Initialize call. Both paths reach these overrides, so the presenter is driven
        // here instead of via AddLifecycleEvent.
        public override UniTask Initialize(Memory<object> args)
        {
            if (ShowOnTop)
            {
                transform.SetAsLastSibling();
            }
            return UIPresenter.Initialize(args);
        }

        public override UniTask Cleanup(Memory<object> args)
        {
            return UIPresenter.Cleanup(args);
        }
    }

    [Serializable]
    public class UIActivityResourceContext
    {
        public static class Events
        {
            public static Action<bool, bool> ShowContentEvent { get; set; }
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
            [field: SerializeField] public UIResourceBar UIResourceBarMoney { get; private set; }
            [field: SerializeField] public UIResourceBar UIResourceBarLife { get; private set; }

            public UniTask Initialize(Memory<object> args)
            {
                return UniTask.CompletedTask;
            }

            public void ShowContent(bool show, bool anim)
            {
                if (anim)
                {
                    float currentAlpha = MainView.alpha;
                    float targetAlpha = show ? 1 : 0;
                    LMotion.Create(currentAlpha, targetAlpha, 0.15f)
                        .BindToAlpha(MainView)
                        .AddTo(MainView);
                }
                else
                {
                    MainView.alpha = show ? 1 : 0;
                }
            }
        }

        [HideLabel]
        [Serializable]
        public class UIPresenter : IAPresenter, IActivityLifecycleEventSimple
        {
            [field: Title(nameof(UIPresenter))]
            [field: SerializeField] public UIModel Model { get; private set; } = new();
            [field: SerializeField] public UIView View { get; private set; } = new();

            public async UniTask Initialize(Memory<object> args)
            {
                await Model.Initialize(args);
                await View.Initialize(args);

                View.UIResourceBarMoney.Init(GameManager.ResourceManager.GetGameResource(ResourceType.Currency, (int)CurrencyType.Money).Value, OnClickOpenShopMoney);
                View.UIResourceBarLife.Init(GameManager.ResourceManager.GetGameResource(ResourceType.Currency, (int)CurrencyType.Life).Value, OnClickOpenShopLife);

                Events.ShowContentEvent += View.ShowContent;
            }

            public UniTask Cleanup(Memory<object> args)
            {
                Events.ShowContentEvent -= View.ShowContent;
                return UniTask.CompletedTask;
            }

            private void OnClickOpenShopMoney()
            {
                MenuNav.SetSelectedTabEvent?.Invoke(0);
                UIScreenMainContext.Events.MoveSwipeAreaEvent?.Invoke(0, true);
            }

            private void OnClickOpenShopLife()
            {
                MenuNav.SetSelectedTabEvent?.Invoke(0);
                UIScreenMainContext.Events.MoveSwipeAreaEvent?.Invoke(0, true);
            }
        }
    }
}
