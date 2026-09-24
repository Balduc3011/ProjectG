using System;
using System.Collections.Generic;
using Balduu;
using Cysharp.Threading.Tasks;
using DataSave;
using UnityEngine;

public class RewardManager : BaseManager
{
    private UIActivityRewardModel uiActivityRewardModel;
    private Memory<object> uiActivityRewardModelArgs;
    private ResourceManager resourceManager;

    protected internal override void Initialize(ManagerCollection<BaseManager> managers)
    {
        resourceManager = managers.InitializeDependency<ResourceManager>();
    }

    public void AddReward(List<GameResource> rewards, bool showRewardActivity = true)
    {
        resourceManager.AddGameResources(rewards);
        if (showRewardActivity)
        {
            if(uiActivityRewardModel == null)
                uiActivityRewardModel = new UIActivityRewardModel();
            uiActivityRewardModel.GameResources = rewards;
            if(uiActivityRewardModelArgs.IsEmpty)
                uiActivityRewardModelArgs = new Memory<object>(new object[] { uiActivityRewardModel });
            UIManager.ShowActivity(UIActivityReward.ActivityName, uiActivityRewardModelArgs).Forget();
        }
    }
}
