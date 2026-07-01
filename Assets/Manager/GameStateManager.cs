using BaseUtil;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    /// <summary>主界面</summary>
    MainMenu,

    /// <summary>游戏中</summary>
    InGame,

    /// <summary>结算</summary>
    Settlement,
}

public class GameStateManager : BaseManager<GameStateManager>
{
    const string MainSceneName = "MainScene";
    const string GameSceneName = "GameScene";

    public GameState CurrentState { get; private set; }

    protected override void Init()
    {
        base.Init();
        ChangeState(GameState.MainMenu);
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        Debug.Log($"[{nameof(GameStateManager)}] 状态改变为: {newState}");

        switch (newState)
        {
            case GameState.MainMenu:
                LoadSceneIfNeeded(MainSceneName);
                break;
            case GameState.InGame:
                LoadSceneIfNeeded(GameSceneName);
                break;
        }
    }

    static void LoadSceneIfNeeded(string sceneName)
    {
        if (SceneManager.GetActiveScene().name == sceneName)
        {
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
