using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VRShooting.Common;

namespace VRShooting.Unity.UI
{
    [Serializable]
    public struct TrainingPresentationPanelBinding
    {
        [SerializeField]
        ScreenId screen;

        [SerializeField]
        GameObject panel;

        public TrainingPresentationPanelBinding(ScreenId screen, GameObject panel)
        {
            this.screen = screen;
            this.panel = panel;
        }

        public ScreenId Screen => screen;

        public GameObject Panel => panel;
    }

    /// <summary>
    /// Renders a complete presentation snapshot without deriving gameplay state from UI objects.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrainingPresentationView : MonoBehaviour
    {
        static readonly Rect CenterAimReserve = Rect.MinMaxRect(0.35f, 0.30f, 0.65f, 0.70f);

        [SerializeField]
        GameObject largePanelRoot;

        [SerializeField]
        GameObject minimalHudRoot;

        [SerializeField]
        TMP_Text pickupPrompt;

        [SerializeField]
        TMP_Text firingStationState;

        [SerializeField]
        Button startButton;

        [SerializeField]
        Button nextRoundButton;

        [SerializeField]
        Button retryButton;

        [SerializeField]
        List<TrainingPresentationPanelBinding> panels = new List<TrainingPresentationPanelBinding>();

        public GameObject LargePanelRoot => largePanelRoot;

        public GameObject MinimalHudRoot => minimalHudRoot;

        public Button NextRoundButton => nextRoundButton;

        public Button RetryButton => retryButton;

        public TrainingPresentationDto LastSnapshot { get; private set; }

        public void Configure(
            GameObject largeRoot,
            GameObject minimalRoot,
            TMP_Text pickupPromptText,
            TMP_Text firingStationStateText,
            Button start,
            Button nextRound,
            Button retry,
            IEnumerable<TrainingPresentationPanelBinding> panelBindings)
        {
            largePanelRoot = largeRoot;
            minimalHudRoot = minimalRoot;
            pickupPrompt = pickupPromptText;
            firingStationState = firingStationStateText;
            startButton = start;
            nextRoundButton = nextRound;
            retryButton = retry;
            panels.Clear();
            if (panelBindings != null)
            {
                panels.AddRange(panelBindings);
            }
        }

        public void Apply(TrainingPresentationDto snapshot)
        {
            LastSnapshot = snapshot;
            ApplyRootState(largePanelRoot, snapshot.LargePanelVisible, true);
            ApplyRootState(minimalHudRoot, snapshot.MinimalHudVisible, false);

            for (var index = 0; index < panels.Count; index++)
            {
                var binding = panels[index];
                if (binding.Panel == null)
                {
                    continue;
                }

                var belongsToLargeRoot = largePanelRoot != null
                                         && binding.Panel.transform.IsChildOf(largePanelRoot.transform);
                var rootVisible = belongsToLargeRoot
                    ? snapshot.LargePanelVisible
                    : snapshot.MinimalHudVisible;
                binding.Panel.SetActive(rootVisible && binding.Screen == snapshot.ActiveScreen);
            }

            if (pickupPrompt != null)
            {
                pickupPrompt.text = ResolvePickupPrompt(snapshot);
            }

            if (firingStationState != null)
            {
                firingStationState.text = string.IsNullOrEmpty(snapshot.FiringStationId)
                    ? "射击位：等待绑定"
                    : "射击位：" + snapshot.FiringStationId;
            }

            if (nextRoundButton != null)
            {
                nextRoundButton.interactable = snapshot.LargePanelVisible
                                               && snapshot.Phase == TrainingPresentationPhase.RoundReview;
            }

            if (startButton != null)
            {
                startButton.interactable = snapshot.LargePanelVisible
                                           && (snapshot.Phase == TrainingPresentationPhase.ModeEntry
                                               || snapshot.Phase == TrainingPresentationPhase.AwaitingStartConfirmation);
            }

            if (retryButton != null)
            {
                retryButton.interactable = snapshot.LargePanelVisible
                                           && snapshot.Phase == TrainingPresentationPhase.SessionResults;
            }

            FocusPrimaryAction(snapshot);
        }

        public void SetCommandInFlight(bool inFlight)
        {
            if (nextRoundButton != null && LastSnapshot.Phase == TrainingPresentationPhase.RoundReview)
            {
                nextRoundButton.interactable = !inFlight;
            }

            if (retryButton != null && LastSnapshot.Phase == TrainingPresentationPhase.SessionResults)
            {
                retryButton.interactable = !inFlight;
            }
        }

        public bool HasCenterAimObstruction()
        {
            if (minimalHudRoot == null)
            {
                return false;
            }

            var rootRect = minimalHudRoot.transform as RectTransform;
            if (rootRect == null || rootRect.rect.width <= 0f || rootRect.rect.height <= 0f)
            {
                return false;
            }

            var graphics = minimalHudRoot.GetComponentsInChildren<Graphic>(true);
            for (var index = 0; index < graphics.Length; index++)
            {
                var graphic = graphics[index];
                if (graphic == null || !graphic.gameObject.activeSelf || graphic.color.a <= 0.001f)
                {
                    continue;
                }

                if (!(graphic.transform is RectTransform graphicRect))
                {
                    continue;
                }

                var corners = new Vector3[4];
                graphicRect.GetWorldCorners(corners);
                var min = rootRect.InverseTransformPoint(corners[0]);
                var max = rootRect.InverseTransformPoint(corners[2]);
                var normalized = Rect.MinMaxRect(
                    Mathf.InverseLerp(rootRect.rect.xMin, rootRect.rect.xMax, min.x),
                    Mathf.InverseLerp(rootRect.rect.yMin, rootRect.rect.yMax, min.y),
                    Mathf.InverseLerp(rootRect.rect.xMin, rootRect.rect.xMax, max.x),
                    Mathf.InverseLerp(rootRect.rect.yMin, rootRect.rect.yMax, max.y));
                if (normalized.Overlaps(CenterAimReserve))
                {
                    return true;
                }
            }

            return false;
        }

        static void ApplyRootState(GameObject root, bool visible, bool interactive)
        {
            if (root == null)
            {
                return;
            }

            var group = root.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = root.AddComponent<CanvasGroup>();
            }

            group.alpha = visible ? 1f : 0f;
            group.interactable = visible && interactive;
            group.blocksRaycasts = visible && interactive;
            root.SetActive(visible);
        }

        static string ResolvePickupPrompt(TrainingPresentationDto snapshot)
        {
            if (snapshot.AwaitingWeaponPickup)
            {
                return "请用右手近距取枪";
            }

            if (snapshot.VisibilityReason == "PickupBeforeStart")
            {
                return "请先确认当前训练设置";
            }

            if (snapshot.Phase == TrainingPresentationPhase.AwaitingStartConfirmation)
            {
                return "确认设置后开始训练";
            }

            return string.Empty;
        }

        void FocusPrimaryAction(TrainingPresentationDto snapshot)
        {
            if (!snapshot.LargePanelVisible || EventSystem.current == null)
            {
                return;
            }

            GameObject focus = null;
            if ((snapshot.Phase == TrainingPresentationPhase.ModeEntry
                 || snapshot.Phase == TrainingPresentationPhase.AwaitingStartConfirmation)
                && startButton != null)
            {
                focus = startButton.gameObject;
            }
            else if (snapshot.Phase == TrainingPresentationPhase.RoundReview && nextRoundButton != null)
            {
                focus = nextRoundButton.gameObject;
            }
            else if (snapshot.Phase == TrainingPresentationPhase.SessionResults && retryButton != null)
            {
                focus = retryButton.gameObject;
            }

            if (focus != null && focus.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(focus);
            }
        }
    }
}
