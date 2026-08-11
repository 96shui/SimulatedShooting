using UnityEngine;
using UnityEngine.SceneManagement;
using VRShooting.Infrastructure;

namespace VRShooting.Application
{
    public enum GameState
    {
        /// <summary>主界面</summary>
        MainMenu,

        /// <summary>游戏中</summary>
        InGame,

        /// <summary>结算</summary>
        Settlement,
    }

    /// <summary>
    /// 临时场景状态管理。task002 将引入 IUIRouter 与 ITrainingSessionService 后逐步替换。
    /// </summary>
    public class GameStateManager : BaseManager<GameStateManager>
    {
        public const string MainSceneName = "MainScene";
        public const string ZeroingRangeSceneName = "ZeroingRangeScene";
        /// <summary>移动靶场景资源名（仓库当前文件名为 MovingargetScene）。</summary>
        public const string MovingTargetSceneName = "MovingargetScene";

        public GameState CurrentState { get; private set; }

        string activeTrainingSceneName = ZeroingRangeSceneName;

        protected override void Init()
        {
            base.Init();

            var activeSceneName = SceneManager.GetActiveScene().name;
            if (activeSceneName == ZeroingRangeSceneName || activeSceneName == MovingTargetSceneName)
            {
                activeTrainingSceneName = activeSceneName;
                CurrentState = GameState.InGame;
                Debug.Log($"[{nameof(GameStateManager)}] 状态初始化为: {CurrentState}");
                return;
            }

            ChangeState(GameState.MainMenu);
        }

        public void ChangeState(GameState newState)
        {
            ChangeState(newState, null);
        }

        /// <summary>
        /// 切换游戏状态。进入 InGame 时可指定训练场景名；未指定时沿用最近一次训练场景。
        /// </summary>
        public void ChangeState(GameState newState, string trainingSceneName)
        {
            if (newState == GameState.InGame && !string.IsNullOrWhiteSpace(trainingSceneName))
            {
                activeTrainingSceneName = trainingSceneName;
            }

            CurrentState = newState;
            Debug.Log($"[{nameof(GameStateManager)}] 状态改变为: {newState}");

            switch (newState)
            {
                case GameState.MainMenu:
                    LoadSceneIfNeeded(MainSceneName);
                    break;
                case GameState.InGame:
                    LoadSceneIfNeeded(string.IsNullOrWhiteSpace(activeTrainingSceneName)
                        ? ZeroingRangeSceneName
                        : activeTrainingSceneName);
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
}
