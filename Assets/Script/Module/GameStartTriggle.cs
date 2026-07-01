using UnityEngine;
using UnityEngine.InputSystem;

public class GameStartTriggle : MonoBehaviour
{
    const string CanvasChildName = "canvas";

    GameObject canvasChild;
    bool isPlayerInside;

    void Awake()
    {
        var childTransform = transform.Find(CanvasChildName);
        if (childTransform == null)
        {
            Debug.LogWarning($"[{nameof(GameStartTriggle)}] 未找到名为 {CanvasChildName} 的子物体。", this);
            return;
        }

        canvasChild = childTransform.gameObject;
        canvasChild.SetActive(false);
    }

    void Update()
    {
        if (!isPlayerInside)
        {
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.kKey.wasPressedThisFrame)
        {
            return;
        }

        GameStateManager.Instance.ChangeState(GameState.InGame);
    }

    void OnTriggerEnter(Collider other)
    {
        isPlayerInside = true;

        if (canvasChild != null)
        {
            canvasChild.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        isPlayerInside = false;

        if (canvasChild != null)
        {
            canvasChild.SetActive(false);
        }
    }
}
