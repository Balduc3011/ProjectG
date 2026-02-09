using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using ZBase.UnityScreenNavigator.Core.Activities;
using UnityEngine.UI;

namespace Balduu
{
    public class UIActivityAlert : Activity
    {
        public static class Events
        {
            
        }
        [field: SerializeField] public bool ShowOnTop { get; private set; }
        [field: SerializeField] public static string ActivityName { get; private set; } = "UIActivityAlert";
        [field: SerializeField] public UIActivityAlertModel Model { get; private set; }
        [field: SerializeField] public UIActivityAlertView View { get; private set; }

        public override async UniTask Initialize(Memory<object> args)
        {
            if (ShowOnTop)
            {
                transform.SetAsLastSibling();
            }
            await Model.Initialize(args);
            await View.Initialize(args);
            View.CloseBtn?.AddListenerUI(OnClickCloseActivity);
            View.ConfirmBtn?.AddListenerUI(OnClickConfirm);
            View.Init(Model.AlertValue, Model.PositiveConfirm);
        }

        public override UniTask Cleanup(Memory<object> args)
        {
            ClearAction();
            return UniTask.CompletedTask;
        }

        private void OnClickCloseActivity()
        {
            CloseActivity().Forget();
        }

        private async UniTask CloseActivity()
        {
            await UIManager.CloseActivity(ActivityName, null);
            Model.DeclineAction?.Invoke();
        }

        private void ClearAction()
        {
            // Clear any listeners or actions here
        }

        private void OnClickConfirm()
        {
            View.MainView.interactable = false;
            ConfirmAction().Forget();
        }
        private async UniTask ConfirmAction()
        {
            await UIManager.CloseActivity(ActivityName, null);
            Model.ConfirmAction?.Invoke();
        }
    }

    [System.Serializable]
    public class UIActivityAlertModel
    {
        [field: SerializeField] public string AlertValue { get; private set; }
        [field: SerializeField] public Action ConfirmAction { get; private set; }
        [field: SerializeField] public Action DeclineAction { get; private set; }
        [field: SerializeField] public bool PositiveConfirm { get; private set; }
        public async UniTask Initialize(Memory<object> args)
        {
            if (args.Length > 0)
            {
                var data = (args.Span[0]) as UIActivityAlertModelData;
                if (data != null)
                {
                    AlertValue = data.alertValue;
                    ConfirmAction = data.confirmAction;
                    DeclineAction = data.declineAction;
                    PositiveConfirm = data.positiveConfirm;
                }
            }
            await UniTask.CompletedTask;
        }
    }

    [System.Serializable]
    public class UIActivityAlertView
    {
        [field: SerializeField] public CanvasGroup MainView { get; private set; }
        [field: SerializeField] public Button CloseBtn { get; private set; }
        [field: SerializeField] public Button ConfirmBtn { get; private set; }
        [field: SerializeField] public Image ConfirmBtnSpr { get; private set; }
        [field: SerializeField] public Sprite PositiveConfirmBtnSpr { get; private set; }
        [field: SerializeField] public Sprite NegativeConfirmBtnSpr { get; private set; }
        [field: SerializeField] public TextMeshProUGUI AlertTxt { get; private set; }

        public async UniTask Initialize(Memory<object> args)
        {
            await UniTask.CompletedTask;
        }

        public void Init(string alertValue, bool positiveConfirm)
        {
            AlertTxt.ChangeText(alertValue);
            ConfirmBtnSpr.sprite = positiveConfirm ? PositiveConfirmBtnSpr : NegativeConfirmBtnSpr;
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
