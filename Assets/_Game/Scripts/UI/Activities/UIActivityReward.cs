using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DataSave;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Baseduu.UINavigator.Core.Activities;
using Baseduu.UINavigator.MVPPattern;

namespace Balduu
{
    // Thin view class. It only holds the presenter and forwards the lifecycle to it.
    public class UIActivityReward : Activity
    {
        [field: SerializeField] public bool ShowOnTop { get; private set; }
        [field: SerializeField] public static string ActivityName { get; private set; } = "UIActivityReward";
        [field: SerializeField] public UIActivityRewardContext.UIPresenter UIPresenter { get; private set; }

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
    public class UIActivityRewardContext
    {
        public static class Events
        {
        }

        [HideLabel]
        [Serializable]
        public class UIModel : IAModel
        {
            public List<GameResource> GameResources { get; private set; }

            public UniTask Initialize(Memory<object> args)
            {
                var model = args.Length > 0 ? args.Span[0] as UIActivityRewardModel : null;
                if (model != null)
                {
                    GameResources = model.GameResources;
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
            [field: SerializeField] public List<UIItemShow> ItemShows { get; private set; }

            public UniTask Initialize(Memory<object> args)
            {
                return UniTask.CompletedTask;
            }

            public void InitRewardUI(List<GameResource> gameResources)
            {
                for (var i = 0; i < ItemShows.Count; i++)
                {
                    if (i < gameResources.Count)
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
                View.InitRewardUI(Model.GameResources);
            }

            public UniTask Cleanup(Memory<object> args)
            {
                View.CloseBtn?.onClick.RemoveAllListeners();
                return UniTask.CompletedTask;
            }

            private void OnClickCloseActivity()
            {
                UIManager.CloseActivity(UIActivityReward.ActivityName, null).Forget();
            }
        }
    }

    // External DTO passed through the activity args. Kept top-level for callers
    // (RewardManager) that construct it to supply the rewarded resources.
    [Serializable]
    public class UIActivityRewardModel
    {
        public List<GameResource> GameResources { get; set; }
    }
}
