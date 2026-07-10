using System;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Baseduu.UINavigator.Core.Activities;
using Baseduu.UINavigator.MVPPattern;

namespace Balduu
{
    // Thin view class. It only holds the presenter and forwards the lifecycle to it.
    public class UIActivityLoading : Activity
    {
        [field: SerializeField] public bool ShowOnTop { get; private set; }
        [field: SerializeField] public static string ActivityName { get; private set; } = "UIActivityLoading";
        [field: SerializeField] public UIActivityLoadingContext.UIPresenter UIPresenter { get; private set; }

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

        public override void DidEnter(Memory<object> args)
        {
            UIPresenter.DidEnter(args);
        }
    }

    [Serializable]
    public class UIActivityLoadingContext
    {
        public static class Events
        {
        }

        [HideLabel]
        [Serializable]
        public class UIModel : IAModel
        {
            public Func<UniTask> WaitAction { get; private set; }
            public Func<UniTask> DoneAction { get; private set; }

            public UniTask Initialize(Memory<object> args)
            {
                UIActivityLoadingModel model = args.Length > 0 ? args.Span[0] as UIActivityLoadingModel : null;
                if (model != null)
                {
                    WaitAction = model.WaitAction;
                    DoneAction = model.DoneAction;
                }
                return UniTask.CompletedTask;
            }
        }

        [HideLabel]
        [Serializable]
        public class UIView : IAView
        {
            [field: Title(nameof(UIView))]
            [field: SerializeField] public CanvasGroup MainView { get; private set; }
            [field: SerializeField] public Slider LoadingSlider { get; private set; }
            [field: SerializeField] public TextMeshProUGUI LoadingValueTxt { get; private set; }

            public UniTask Initialize(Memory<object> args)
            {
                return UniTask.CompletedTask;
            }

            public async UniTask RunLoading()
            {
                await LMotion.Create(0.05f, 1f, 1f).Bind(SetLoadingSliderValue);
            }

            private void SetLoadingSliderValue(float value)
            {
                LoadingSlider.value = value;
                LoadingValueTxt.ChangeText($"{value * 100f:0}%");
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
            }

            public void DidEnter(Memory<object> args)
            {
                RunLoading().Forget();
            }

            private async UniTask RunLoading()
            {
                if (Model.WaitAction != null)
                {
                    await Model.WaitAction.Invoke();
                }
                await View.RunLoading();
                await UIManager.CloseActivity(UIActivityLoading.ActivityName, null);
                if (Model.DoneAction != null)
                {
                    await Model.DoneAction.Invoke();
                }
            }
        }
    }

    // External DTO passed through the activity args. Kept top-level for callers
    // (GameManager, UIManager) that construct it to drive the loading flow.
    [Serializable]
    public class UIActivityLoadingModel
    {
        public Func<UniTask> WaitAction { get; set; }
        public Func<UniTask> DoneAction { get; set; }
    }
}
