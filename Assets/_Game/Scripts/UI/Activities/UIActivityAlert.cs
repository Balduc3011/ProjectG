using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Baseduu.UINavigator.Core.Activities;
using Baseduu.UINavigator.MVPPattern;

namespace Balduu
{
    // Thin view class. It only holds the presenter and forwards the lifecycle to it.
    public class UIActivityAlert : Activity
    {
        [field: SerializeField] public bool ShowOnTop { get; private set; }
        [field: SerializeField] public static string ActivityName { get; private set; } = "UIActivityAlert";
        [field: SerializeField] public UIActivityAlertContext.UIPresenter UIPresenter { get; private set; }

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
    public class UIActivityAlertContext
    {
        public static class Events
        {
        }

        [HideLabel]
        [Serializable]
        public class UIModel : IAModel
        {
            public string AlertValue { get; private set; }
            public Action ConfirmAction { get; private set; }
            public Action DeclineAction { get; private set; }
            public bool PositiveConfirm { get; private set; }

            public UniTask Initialize(Memory<object> args)
            {
                if (args.Length > 0)
                {
                    var data = args.Span[0] as UIActivityAlertModelData;
                    if (data != null)
                    {
                        AlertValue = data.alertValue;
                        ConfirmAction = data.confirmAction;
                        DeclineAction = data.declineAction;
                        PositiveConfirm = data.positiveConfirm;
                    }
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
            [field: SerializeField] public Button CloseBtn { get; private set; }
            [field: SerializeField] public Button ConfirmBtn { get; private set; }
            [field: SerializeField] public Image ConfirmBtnSpr { get; private set; }
            [field: SerializeField] public Sprite PositiveConfirmBtnSpr { get; private set; }
            [field: SerializeField] public Sprite NegativeConfirmBtnSpr { get; private set; }
            [field: SerializeField] public TextMeshProUGUI AlertTxt { get; private set; }

            public UniTask Initialize(Memory<object> args)
            {
                return UniTask.CompletedTask;
            }

            public void Init(string alertValue, bool positiveConfirm)
            {
                AlertTxt.ChangeText(alertValue);
                ConfirmBtnSpr.sprite = positiveConfirm ? PositiveConfirmBtnSpr : NegativeConfirmBtnSpr;
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
                View.CloseBtn?.AddListenerUI(OnClickCloseActivity);
                View.ConfirmBtn?.AddListenerUI(OnClickConfirm);
                View.Init(Model.AlertValue, Model.PositiveConfirm);
            }

            public UniTask Cleanup(Memory<object> args)
            {
                View.CloseBtn?.onClick.RemoveAllListeners();
                View.ConfirmBtn?.onClick.RemoveAllListeners();
                return UniTask.CompletedTask;
            }

            private void OnClickCloseActivity()
            {
                CloseActivity().Forget();
            }

            private async UniTask CloseActivity()
            {
                await UIManager.CloseActivity(UIActivityAlert.ActivityName, null);
                Model.DeclineAction?.Invoke();
            }

            private void OnClickConfirm()
            {
                View.MainView.interactable = false;
                ConfirmActionAsync().Forget();
            }

            private async UniTask ConfirmActionAsync()
            {
                await UIManager.CloseActivity(UIActivityAlert.ActivityName, null);
                Model.ConfirmAction?.Invoke();
            }
        }
    }
}

[System.Serializable]
public class UIActivityAlertModelData
{
    public string alertValue;
    public Action confirmAction = null;
    public Action declineAction = null;
    public bool positiveConfirm = true;
}
