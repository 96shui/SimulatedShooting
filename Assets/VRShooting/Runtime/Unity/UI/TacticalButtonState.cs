using UnityEngine;
using UnityEngine.UI;

namespace VRShooting.Unity.UI
{
    // SpriteSwap does not apply Selectable.disabledColor. Keep disabled actions visibly distinct.
    [RequireComponent(typeof(Button), typeof(CanvasGroup))]
    public sealed class TacticalButtonState : MonoBehaviour
    {
        Button button;
        CanvasGroup group;
        void Awake() { button = GetComponent<Button>(); group = GetComponent<CanvasGroup>(); }
        void OnEnable() { if (button == null) Awake(); Refresh(); }
        void LateUpdate() => Refresh();
        void Refresh() { group.alpha = button.IsInteractable() ? 1f : 0.38f; }
    }
}
