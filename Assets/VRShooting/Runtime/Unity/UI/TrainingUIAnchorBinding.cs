using UnityEngine;

namespace VRShooting.Unity.UI
{
    public enum TrainingUIAnchorSlot
    {
        LargePanel,
        MinimalHud
    }

    public interface ITrainingUIAnchor
    {
        TrainingUIAnchorSlot Slot { get; }

        Transform AnchorTransform { get; }
    }

    [DisallowMultipleComponent]
    public sealed class TrainingUIAnchor : MonoBehaviour, ITrainingUIAnchor
    {
        [SerializeField]
        TrainingUIAnchorSlot slot;

        public TrainingUIAnchorSlot Slot => slot;

        public Transform AnchorTransform => transform;

        public void Configure(TrainingUIAnchorSlot value)
        {
            slot = value;
        }
    }

    public readonly struct TrainingUIAnchorPair
    {
        public TrainingUIAnchorPair(ITrainingUIAnchor largePanel, ITrainingUIAnchor minimalHud)
        {
            LargePanel = largePanel;
            MinimalHud = minimalHud;
        }

        public ITrainingUIAnchor LargePanel { get; }

        public ITrainingUIAnchor MinimalHud { get; }

        public static TrainingUIAnchorPair CreateDefaultFake(Transform parent)
        {
            var largeObject = new GameObject("FakeAnchor_Training_LargePanel");
            largeObject.transform.SetParent(parent, false);
            var large = largeObject.AddComponent<TrainingUIAnchor>();
            large.Configure(TrainingUIAnchorSlot.LargePanel);

            var minimalObject = new GameObject("FakeAnchor_Training_MinimalHud");
            minimalObject.transform.SetParent(parent, false);
            var minimal = minimalObject.AddComponent<TrainingUIAnchor>();
            minimal.Configure(TrainingUIAnchorSlot.MinimalHud);
            return new TrainingUIAnchorPair(large, minimal);
        }
    }

    /// <summary>
    /// Parents UI roots to scene-provided anchors without choosing any world-space pose.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrainingUIAnchorBinder : MonoBehaviour
    {
        [SerializeField]
        Transform largePanelRoot;

        [SerializeField]
        Transform minimalHudRoot;

        public string LastError { get; private set; } = string.Empty;

        public void Configure(Transform largeRoot, Transform minimalRoot)
        {
            largePanelRoot = largeRoot;
            minimalHudRoot = minimalRoot;
        }

        public bool Bind(ITrainingUIAnchor largePanelAnchor, ITrainingUIAnchor minimalHudAnchor)
        {
            if (!ValidateAnchor(largePanelAnchor, TrainingUIAnchorSlot.LargePanel, "LargePanel"))
            {
                return false;
            }

            if (!ValidateAnchor(minimalHudAnchor, TrainingUIAnchorSlot.MinimalHud, "MinimalHud"))
            {
                return false;
            }

            if (largePanelRoot == null || minimalHudRoot == null)
            {
                return Fail("UI root references are incomplete.");
            }

            if (largePanelAnchor.AnchorTransform == minimalHudAnchor.AnchorTransform)
            {
                return Fail("LargePanel and MinimalHud anchors must be independent.");
            }

            largePanelRoot.SetParent(largePanelAnchor.AnchorTransform, false);
            minimalHudRoot.SetParent(minimalHudAnchor.AnchorTransform, false);
            ResetLocalPose(largePanelRoot);
            ResetLocalPose(minimalHudRoot);
            LastError = string.Empty;
            return true;
        }

        bool ValidateAnchor(ITrainingUIAnchor anchor, TrainingUIAnchorSlot expected, string label)
        {
            if (anchor == null || anchor.AnchorTransform == null)
            {
                return Fail("Missing " + label + " anchor.");
            }

            return anchor.Slot == expected || Fail(label + " anchor has the wrong slot.");
        }

        bool Fail(string message)
        {
            LastError = message;
            Debug.LogError("[TrainingUIAnchorBinder] " + message, this);
            return false;
        }

        static void ResetLocalPose(Transform target)
        {
            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;
        }
    }
}
