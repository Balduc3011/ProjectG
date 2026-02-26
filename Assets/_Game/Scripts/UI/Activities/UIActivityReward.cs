using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DataSave;
using UnityEngine;
using ZBase.UnityScreenNavigator.Core.Activities;
using UnityEngine.UI;

namespace Balduu
{
    public class UIActivityReward : Activity
    {
        public static class Events
        {
            
        }
        [field: SerializeField] public bool ShowOnTop { get; private set; }
        [field: SerializeField] public static string ActivityName { get; private set; } = "UIActivityReward";
        [field: SerializeField] public UIActivityRewardModel Model { get; private set; }
        [field: SerializeField] public UIActivityRewardView View { get; private set; }

        public override async UniTask Initialize(Memory<object> args)
        {
            if (ShowOnTop)
            {
                transform.SetAsLastSibling();
            }
            await Model.Initialize(args);
            await View.Initialize(args);
            View.CloseBtn?.AddListenerUI(OnClickCloseActivity);
            View.InitRewardUI(Model.GameResources);
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
        }

        private void ClearAction()
        {
            // Clear any listeners or actions here
        }
    }

    [System.Serializable]
    public class UIActivityRewardModel
    {
        [field: SerializeField] public List<GameResource> GameResources { get; set; }
        public async UniTask Initialize(Memory<object> args)
        {
            var model = args.Span[0] as UIActivityRewardModel;
            if(model != null)
                GameResources = model.GameResources;
            await UniTask.CompletedTask;
        }
    }

    [System.Serializable]
    public class UIActivityRewardView
    {
        [field: SerializeField] public CanvasGroup MainView { get; private set; }
        [field: SerializeField] public Button CloseBtn { get; private set; }
        [field: SerializeField] public List<UIItemShow> ItemShows { get; private set; }

        public async UniTask Initialize(Memory<object> args)
        {
            await UniTask.CompletedTask;
        }
        
        public void InitRewardUI(List<GameResource> gameResources)
        {
            for (var i = 0; i < ItemShows.Count; i++)
            {
                if(i < gameResources.Count)
                {
                    var sprite = SpriteGlobalConfig.Instance.GetResourceSprite(gameResources[i].Type, gameResources[i].DefineValue);
                    var value = ShopGlobalConfig.Instance.GetResourceTextValue(gameResources[i]);
                    ItemShows[i].Init(sprite, value);
                }
                else
                {
                    ItemShows[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
