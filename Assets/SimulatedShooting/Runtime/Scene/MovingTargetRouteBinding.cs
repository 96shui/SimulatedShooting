using UnityEngine;

namespace SimulatedShooting.Scene
{
    [DisallowMultipleComponent]
    public sealed class MovingTargetRouteBinding : MonoBehaviour
    {
        [SerializeField] Transform rightEndpoint;
        [SerializeField] Transform leftEndpoint;
        [SerializeField] Transform targetRoot;
        [SerializeField] Collider hitSurface;
        [SerializeField] Transform targetCenter;
        [SerializeField] Transform impactFeedbackRoot;
        [SerializeField, Range(0f, 1f)] float normalizedProgress;

        public Transform RightEndpoint => rightEndpoint;
        public Transform LeftEndpoint => leftEndpoint;
        public Transform TargetRoot => targetRoot;
        public Collider HitSurface => hitSurface;
        public Transform TargetCenter => targetCenter;
        public Transform ImpactFeedbackRoot => impactFeedbackRoot;
        public float NormalizedProgress => normalizedProgress;

        public void Configure(
            Transform right,
            Transform left,
            Transform target,
            Collider surface,
            Transform center,
            Transform feedbackRoot)
        {
            rightEndpoint = right;
            leftEndpoint = left;
            targetRoot = target;
            hitSurface = surface;
            targetCenter = center;
            impactFeedbackRoot = feedbackRoot;
            ApplyNormalizedProgress(0f);
        }

        public void ApplyNormalizedProgress(float rightToLeftProgress)
        {
            normalizedProgress = Mathf.Clamp01(rightToLeftProgress);
            if (rightEndpoint == null || leftEndpoint == null || targetRoot == null)
                return;

            targetRoot.position = Vector3.Lerp(rightEndpoint.position, leftEndpoint.position, normalizedProgress);
        }
    }
}
