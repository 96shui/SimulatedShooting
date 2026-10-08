using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VRShooting.Unity.UI
{
    /// <summary>One step per click, or repeated steps while a UI pointer remains held.</summary>
    public sealed class RepeatingAdjustmentButton : MonoBehaviour, IPointerDownHandler,
        IPointerUpHandler, IPointerExitHandler
    {
        Button button;
        Action step;
        bool held;
        bool repeated;
        float nextStepTime;

        public void Configure(Button target, Action action)
        {
            button = target;
            step = action;
            button.onClick.AddListener(OnClick);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !CanAdjust()) return;
            held = true;
            repeated = false;
            nextStepTime = Time.unscaledTime + .35f;
        }

        void Update()
        {
            if (!held) return;
            if (!CanAdjust()) { held = false; return; }
            if (Time.unscaledTime < nextStepTime) return;
            repeated = true;
            nextStepTime = Time.unscaledTime + .06f;
            step?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData) => held = false;
        public void OnPointerExit(PointerEventData eventData) { held = false; repeated = true; }
        void OnClick()
        {
            if (!repeated && CanAdjust())
                step?.Invoke();
            repeated = false;
        }
        void OnDisable() { held = false; repeated = false; }
        bool CanAdjust() => button != null && button.IsActive() && button.IsInteractable();
    }
}
