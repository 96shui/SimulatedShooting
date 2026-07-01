using UnityEngine;

public class GameMain : MonoBehaviour
{
    public static GameMain Instance { get; private set; }

    public GameStateManager GameState { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitManagers();
    }

    void InitManagers()
    {
        GameState = GameStateManager.Instance;
    }

    void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        GameStateManager.DestroyInstance();
        GameState = null;
        Instance = null;
    }
}
