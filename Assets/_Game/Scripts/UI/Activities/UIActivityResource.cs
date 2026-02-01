using System;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using ZBase.UnityScreenNavigator.Core.Activities;

namespace Balduu
{
    public class UIActivityResource : Activity
    {
        public static class Events
        {
            public static Action<bool, bool> ShowContentEvent { get; set; }
        }
        [field: SerializeField] public bool ShowOnTop { get; private set; }
        [field: SerializeField] public static string ActivityName { get; private set; } = "UIActivityResource";
        [field: SerializeField] public UIActivityResourceModel Model { get; private set; }
        [field: SerializeField] public UIActivityResourceView View { get; private set; }

        public override async UniTask Initialize(Memory<object> args)
        {
            if (ShowOnTop)
            {
                transform.SetAsLastSibling();
            }
            await Model.Initialize(args);
            await View.Initialize(args);
            Events.ShowContentEvent += ShowContent;
        }

        public override UniTask Cleanup(Memory<object> args)
        {
            ClearAction();
            return UniTask.CompletedTask;
        }

        private async UniTask CloseActivity()
        {
            await UIManager.CloseActivity(ActivityName, null);
        }

        private void ClearAction()
        {
            Events.ShowContentEvent -= ShowContent;
        }

        private void ShowContent(bool show, bool anim)
        {
            if (anim)
            {
                float currentAlpha = View.MainView.alpha;
                float targetAlpha = show ? 1 : 0;
                LMotion.Create(currentAlpha, targetAlpha, 0.15f)
                    .BindToAlpha(View.MainView)
                    .AddTo(View.MainView);
            }
            else
            {
                View.MainView.alpha = show ? 1 : 0;
            }
        }
    }

    [System.Serializable]
    public class UIActivityResourceModel
    {
        public async UniTask Initialize(Memory<object> args)
        {
            await UniTask.CompletedTask;
        }
    }

    [System.Serializable]
    public class UIActivityResourceView
    {
        [field: SerializeField] public CanvasGroup MainView { get; private set; }
        public async UniTask Initialize(Memory<object> args)
        {
            await UniTask.CompletedTask;
        }
    }
}
