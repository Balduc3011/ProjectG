using System;
using Balduu;
using Cysharp.Threading.Tasks;
using Demo.Scripts;
using Sirenix.OdinInspector;
using UnityEngine;
using Baseduu.UINavigator.Core.Activities;
using Baseduu.UINavigator.Core.Modals;
using Baseduu.UINavigator.Core.Screens;
using Baseduu.UINavigator.Core.Views;

public class UIManager : BaseManager
{
    #region Statup
    [BoxGroup("Loading")] public bool loadedUIScreenMain = false;
    [BoxGroup("Loading")] public GameObject tempLoadingObj;
    [BoxGroup("FirstScreen")] public string firstScene = "UIScreenMain";
    // Hiện UI đầu tiên khi MỌI manager đã load xong (trước đây dựa vào việc được gọi cuối trong GameManager).
    protected internal override async UniTask PostLoad()
    {
        UIActivityLoadingModel loadingModel = new UIActivityLoadingModel
        {
            WaitAction = WaitForFirstUI,
            DoneAction = OpenUIActivityResource
        };
        await ShowActivity(UIActivityLoading.ActivityName, new Memory<object>(new object[] { loadingModel }));
        tempLoadingObj.SetActive(false);
        await ShowScreen(firstScene, null);
        loadedUIScreenMain = true;
    }

    private async UniTask WaitForFirstUI()
    {
        await UniTask.WaitUntil(() => loadedUIScreenMain);
    }

    public async UniTask OpenUIActivityResource()
    {
        await ShowActivity(UIActivityResource.ActivityName, null);
    }

    #endregion
    
    #region Function
    public static async UniTask ShowScreen(string screenName, Memory<object> args)
    {
        Debug.Log($"Opening screen: {screenName}");
        var options = new ViewOptions(screenName, false, loadAsync: false);
        await ScreenContainer.Find(ContainerKey.Screens).PushAsync(options, args);
    }
    
    public static async UniTask CloseScreen(string screenName, Memory<object> args)
    {
        Debug.Log($"Closing screen: {screenName}");
        await ScreenContainer.Find("Screens").PopAsync(true);
    }
    
    public static async UniTask ShowModal(string modalName, Memory<object> args)
    {
        Debug.Log($"Opening modal: {modalName}");
        var options = new ViewOptions(modalName);
        await ModalContainer.Find(ContainerKey.Modals).PushAsync(options, args);
    }
    
    public static async UniTask CloseModal(string modalName, Memory<object> args)
    {
        Debug.Log($"Closing modal: {modalName}");
        await ModalContainer.Find("Modals").PopAsync(true);
    }
    
    public static async UniTask ShowActivity(string activityName, Memory<object> args)
    {
        Debug.Log($"Opening activity: {activityName}");
        var options = new ActivityOptions(activityName, false);
        await ActivityContainer.Find(ContainerKey.Activities).ShowAsync(options, args);
    }
    
    public static async UniTask CloseActivity(string activityName, Memory<object> args)
    {
        Debug.Log($"Closing activity: {activityName}");
        await ActivityContainer.Find(ContainerKey.Activities).HideAsync(activityName);
    }

    #endregion

    
}
