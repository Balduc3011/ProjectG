using UnityEngine;

public class GamePlayManager : MonoBehaviour
{
    public bool pause = true;
    public bool IsGamePaused => pause;
    public void PauseGame(bool isPause)
    {
        pause = isPause;
    }
}
