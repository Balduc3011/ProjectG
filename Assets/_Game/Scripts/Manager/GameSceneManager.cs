using System;
using Balduu;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý chuyển scene: load/unload World scene (Additive) chồng lên MainScene, kèm màn loading và đổi Screen UI.
/// MainScene luôn được giữ (GameManager, UI). Mỗi lúc chỉ có 1 World scene.
/// Gọi: <c>GameManager.Get&lt;GameSceneManager&gt;().OpenWorldScene()</c> / <c>.BackToMenu()</c>.
/// </summary>
public class GameSceneManager : BaseManager
{
    [SerializeField, Tooltip("Tên scene World sẽ load khi vào gameplay. Scene phải có trong Build Profiles → Scene List.")]
    private string worldSceneName = "SampleWorldScene";

    private UIActivityLoadingModel openWorldModel;
    private Memory<object> openWorldModelArgs;
    private UIActivityLoadingModel backToMenuModel;
    private Memory<object> backToMenuModelArgs;
    private bool loadedUIScreenMain;
    private bool isTransitioning;

    /// <summary>Scene World đang được load (tên theo field <c>worldSceneName</c>).</summary>
    public bool IsWorldLoaded => SceneManager.GetSceneByName(worldSceneName).isLoaded;

    public async UniTask OpenWorldScene()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        GamePlayManager.Instance.OpenBattleScene();
        openWorldModel ??= new UIActivityLoadingModel
        {
            WaitAction = LoadWorldAndShowPlayScreen,
            DoneAction = OnTransitionDone
        };
        if (openWorldModelArgs.IsEmpty)
            openWorldModelArgs = new Memory<object>(new object[] { openWorldModel });

        UIActivityResourceContext.Events.ShowContentEvent?.Invoke(false, false);
        await UIManager.ShowActivity(UIActivityLoading.ActivityName, openWorldModelArgs);
    }

    public async UniTask BackToMenu()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        GamePlayManager.Instance.OpenBattleScene();
        loadedUIScreenMain = false;
        backToMenuModel ??= new UIActivityLoadingModel
        {
            WaitAction = WaitForFirstUI,
            DoneAction = OnTransitionDone
        };
        if (backToMenuModelArgs.IsEmpty)
            backToMenuModelArgs = new Memory<object>(new object[] { backToMenuModel });

        await UIManager.ShowActivity(UIActivityLoading.ActivityName, backToMenuModelArgs);
        await UIManager.CloseScreen(UIScreenPlay.ScreenName, null);
        await UnloadWorldScene();
        loadedUIScreenMain = true;
    }

    // Chạy trong lúc màn loading đang che.
    private async UniTask LoadWorldAndShowPlayScreen()
    {
        await LoadWorldScene();
        UIActivityResourceContext.Events.ShowContentEvent?.Invoke(true, false);
        await UIManager.ShowScreen(UIScreenPlay.ScreenName, null);
    }

    private async UniTask WaitForFirstUI()
    {
        await UniTask.WaitUntil(() => loadedUIScreenMain, cancellationToken: destroyCancellationToken);
    }

    private UniTask OnTransitionDone()
    {
        isTransitioning = false;
        return UniTask.CompletedTask;
    }

    private async UniTask LoadWorldScene()
    {
        if (IsWorldLoaded)
        {
            // Vd đang test multi-scene trong Editor: World đã mở sẵn.
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(worldSceneName));
            return;
        }
        if (!Application.CanStreamedLevelBeLoaded(worldSceneName))
        {
            Debug.LogError($"[GameSceneManager] Scene '{worldSceneName}' chưa có trong Build Profiles → Scene List.", this);
            return;
        }

        await SceneManager.LoadSceneAsync(worldSceneName, LoadSceneMode.Additive).ToUniTask(cancellationToken: destroyCancellationToken);
        // Bắt buộc: Instantiate luôn tạo object vào scene active; lighting/skybox cũng theo scene active.
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(worldSceneName));

        // Chỉ đợi trạng thái sẵn sàng của World, không gọi manager World (giữ phụ thuộc một chiều).
        if (WorldSubsystem.Current == null)
        {
            Debug.LogWarning($"[GameSceneManager] Scene '{worldSceneName}' không có WorldSubsystem.", this);
            return;
        }
        await UniTask.WaitUntil(() => WorldSubsystem.Current == null || WorldSubsystem.Current.IsReady,
            cancellationToken: destroyCancellationToken);
    }

    private async UniTask UnloadWorldScene()
    {
        var scene = SceneManager.GetSceneByName(worldSceneName);
        if (!scene.isLoaded) return;

        SceneManager.SetActiveScene(gameObject.scene);
        await SceneManager.UnloadSceneAsync(scene).ToUniTask(cancellationToken: destroyCancellationToken);
    }
}
