using Balduu.Utility.DesignPattern;
using Baseduu.ReactiveFields;
using UnityEngine;

public class GamePlayManager : Singleton<GamePlayManager>
{
    public bool gameStarted = false;
    [field: SerializeField] public ReactiveValue<bool> IsGamePaused { get; private set; } = new(true);
    
    public void OpenBattleScene()
    {
        gameStarted = false;
        IsGamePaused.Value = true;
    }
    
    public void StartGame()
    {
        gameStarted = true;
        IsGamePaused.Value = false;
    }
    
    public void PauseGame(bool isPause)
    {
        if(!gameStarted)
            return;
        IsGamePaused.Value = isPause;
    }
}
