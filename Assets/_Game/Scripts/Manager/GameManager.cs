using System;
using Balduu;
using Balduu.Utility.DesignPattern;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public UIManager uiManager;
    public static UIManager UIManager => Instance.uiManager;
    public AudioManager audioManager;
    public static AudioManager AudioManager => Instance.audioManager;
    public SettingManager settingManager;
    public static SettingManager SettingManager => Instance.settingManager;
    public LocalizationManager localizationManager;
    public static LocalizationManager LocalizationManager => Instance.localizationManager;
    public TimeManager timeManager;
    public static TimeManager TimeManager => Instance.timeManager;
    public ProfileManager profileManager;
    public static ProfileManager ProfileManager => Instance.profileManager;
    public ResourceManager resourceManager;
    public static ResourceManager ResourceManager => Instance.resourceManager;
    public RewardManager rewardManager;
    public static RewardManager RewardManager => Instance.rewardManager;
    private bool loadedUIScreenMain = false;

    private void Start()
    {
        LoadData().Forget();
    }

    private async UniTask LoadData()
    {
        await AudioManager.LoadData();
        await SettingManager.LoadData();
        await LocalizationManager.LoadData();
        await TimeManager.LoadData();
        await ProfileManager.LoadData();
        await ResourceManager.LoadData();
        await RewardManager.LoadData();
        await UniTask.DelayFrame(1);
        await UIManager.LoadData();
        Application.targetFrameRate = 60;
    }

    #region Navigation
    private UIActivityLoadingModel loadingBattleModel;
    private Memory<object> loadingBattleModelArgs;
    private UIActivityLoadingModel loadingBackModel;
    private Memory<object> loadingBackModelArgs;
    
    public async UniTask OpenBattleScene()
    {
        GamePlayManager.Instance.OpenBattleScene();
        if (loadingBattleModel == null)
        {
            loadingBattleModel = new UIActivityLoadingModel
            {
                WaitAction = OpenUIScreenPlay,
                DoneAction = null
            };
        }
        if(loadingBattleModelArgs.IsEmpty)
            loadingBattleModelArgs = new Memory<object>(new object[] { loadingBattleModel });
        UIActivityResourceContext.Events.ShowContentEvent?.Invoke(false, false);
        await UIManager.ShowActivity(UIActivityLoading.ActivityName, loadingBattleModelArgs);
    }
        
    private async UniTask OpenUIScreenPlay()
    {
        UIActivityResourceContext.Events.ShowContentEvent?.Invoke(true, false);
        await UIManager.ShowScreen(UIScreenPlay.ScreenName, null);
    }
    
    public async UniTaskVoid BackToMenu()
    {
        GamePlayManager.Instance.OpenBattleScene();
        loadedUIScreenMain = false;
        if (loadingBackModel == null)
        {
            loadingBackModel = new UIActivityLoadingModel
            {
                WaitAction = WaitForFirstUI,
                DoneAction = null
            };
        }
        if(loadingBackModelArgs.IsEmpty) 
            loadingBackModelArgs = new Memory<object>(new object[] { loadingBackModel });
        await UIManager.ShowActivity(UIActivityLoading.ActivityName, loadingBackModelArgs);
        await UIManager.CloseScreen(UIScreenPlay.ScreenName, null);
        loadedUIScreenMain = true;
    }
    
    private async UniTask WaitForFirstUI()
    {
        await UniTask.WaitUntil(() => loadedUIScreenMain);
    }

    #endregion
}
