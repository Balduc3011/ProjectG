using System;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZBase.UnityScreenNavigator.Core.Activities;

namespace Balduu
{
    public class UIActivityLoading : Activity
    {
        [field: SerializeField] public bool ShowOnTop { get; private set; }
        [field: SerializeField] public static string ActivityName { get; private set; } = "UIActivityLoading";
        [field: SerializeField] public UIActivityLoadingModel Model { get; private set; }
        [field: SerializeField] public UIActivityLoadingView View { get; private set; }

        public override async UniTask Initialize(Memory<object> args)
        {
            if (ShowOnTop)
            {
                transform.SetAsLastSibling();
            }
            await Model.Initialize(args);
            await View.Initialize(args);
        }

        public override UniTask Cleanup(Memory<object> args)
        {
            ClearAction();
            return UniTask.CompletedTask;
        }

        public override void DidEnter(Memory<object> args)
        {
            base.DidEnter(args);
            RunLoading().Forget();
        }

        private async UniTask RunLoading()
        {
            if (Model.WaitAction != null)
            {
                await Model.WaitAction.Invoke();
            }
            await View.RunLoading();
            await CloseActivity();
            if (Model.DoneAction != null)
            {
                await Model.DoneAction.Invoke();
            }
        }

        private async UniTask CloseActivity()
        {
            await UIManager.CloseActivity(ActivityName, null);
        }

        private void ClearAction()
        {
            // Clear any listeners or actions here
        }
    }

    [System.Serializable]
    public class UIActivityLoadingModel
    {
        [field: SerializeField] public Func<UniTask> WaitAction { get; set; }
        [field: SerializeField] public Func<UniTask> DoneAction { get; set; }
        
        public async UniTask Initialize(Memory<object> args)
        {
            UIActivityLoadingModel model = args.Length > 0 ? args.Span[0] as UIActivityLoadingModel : null;
            if (model != null)
            {
                WaitAction = model.WaitAction;
                DoneAction = model.DoneAction;
            }
            await UniTask.CompletedTask;
        }
    }

    [System.Serializable]
    public class UIActivityLoadingView
    {
        [field: SerializeField] public CanvasGroup MainView { get; private set; }
        [field: SerializeField] public Slider LoadingSlider { get; private set; }
        [field: SerializeField] public TextMeshProUGUI LoadingValueTxt { get; private set; }
        
        public async UniTask Initialize(Memory<object> args)
        {
            await UniTask.CompletedTask;
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
}
