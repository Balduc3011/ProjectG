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
    public GamePlayManager gamePlayManager;
    public static GamePlayManager GamePlayManager => Instance.gamePlayManager;
    public TimeManager timeManager;
    public static TimeManager TimeManager => Instance.timeManager;
    public ProfileManager profileManager;
    public static ProfileManager ProfileManager => Instance.profileManager;
    public ResourceManager resourceManager;
    public static ResourceManager ResourceManager => Instance.resourceManager;
    private bool loadedUIScreenMain = false;

    private void Start()
    {
        LoadData().Forget();
    }

    private async UniTask LoadData()
    {
        SettingManager.LoadData();
        ResourceManager.LoadData();
        await UniTask.DelayFrame(1);
        UIManager.LoadData().Forget();
    }

    public async UniTaskVoid BackToMenu()
    {
        loadedUIScreenMain = false;
        UIActivityLoadingModel loadingModel = new UIActivityLoadingModel
        {
            WaitAction = WaitForFirstUI,
            DoneAction = null
        };
        await UIManager.ShowActivity(UIActivityLoading.ActivityName, new Memory<object>(new object[] { loadingModel }));
        await UIManager.CloseScreen(UIScreenPlay.ScreenName, null);
        loadedUIScreenMain = true;
    }

    private async UniTask WaitForFirstUI()
    {
        await UniTask.WaitUntil(() => loadedUIScreenMain);
    }
}
