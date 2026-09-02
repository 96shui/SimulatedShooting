using System;
using UnityEngine;

namespace SimulatedShooting.Scene
{
    public readonly struct MovingTargetHitInput
    {
        public MovingTargetHitInput(
            string shotId,
            Collider hitCollider,
            Vector3 worldPoint,
            Vector3 worldNormal,
            bool isTarget)
        {
            ShotId = shotId;
            HitCollider = hitCollider;
            WorldPoint = worldPoint;
            WorldNormal = worldNormal;
            IsTarget = isTarget;
        }

        public string ShotId { get; }
        public Collider HitCollider { get; }
        public Vector3 WorldPoint { get; }
        public Vector3 WorldNormal { get; }
        public bool IsTarget { get; }
    }

    [DisallowMultipleComponent]
    public sealed class MovingTargetHitAdapter : MonoBehaviour
    {
        [SerializeField] Collider targetCollider;
        [SerializeField] LayerMask targetLayerMask;
        [SerializeField] LayerMask environmentLayerMask;
        [SerializeField] MovingTargetImpactFeedback impactFeedback;

        public event Action<MovingTargetHitInput> HitReported;

        public LayerMask TargetLayerMask => targetLayerMask;
        public LayerMask EnvironmentLayerMask => environmentLayerMask;
        public Collider TargetCollider => targetCollider;

        public void Configure(
            Collider target,
            LayerMask targetMask,
            LayerMask environmentMask,
            MovingTargetImpactFeedback feedback)
        {
            targetCollider = target;
            targetLayerMask = targetMask;
            environmentLayerMask = environmentMask;
            impactFeedback = feedback;
        }

        public bool IsTargetCollider(Collider candidate)
        {
            return candidate != null
                   && candidate == targetCollider
                   && (targetLayerMask.value & (1 << candidate.gameObject.layer)) != 0;
        }

        public bool TryReportConfirmedHit(
            string shotId,
            Collider hitCollider,
            Vector3 worldPoint,
            Vector3 worldNormal,
            out MovingTargetHitInput input)
        {
            if (string.IsNullOrWhiteSpace(shotId) || hitCollider == null)
            {
                input = default;
                return false;
            }

            var isTarget = IsTargetCollider(hitCollider);
            input = new MovingTargetHitInput(shotId, hitCollider, worldPoint, worldNormal, isTarget);
            HitReported?.Invoke(input);
            if (isTarget && impactFeedback != null)
                impactFeedback.TryConsume(input);
            return true;
        }
    }

}
