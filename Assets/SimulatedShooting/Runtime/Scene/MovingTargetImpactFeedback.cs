using System.Collections.Generic;
using UnityEngine;

namespace SimulatedShooting.Scene
{
    [DisallowMultipleComponent]
    public sealed class MovingTargetImpactFeedback : MonoBehaviour
    {
        [SerializeField] Transform marker;
        readonly HashSet<string> consumedShotIds = new HashSet<string>();

        public int ConsumedShotCount => consumedShotIds.Count;

        public void Configure(Transform feedbackMarker)
        {
            marker = feedbackMarker;
            if (marker != null)
                marker.gameObject.SetActive(false);
        }

        public bool TryConsume(MovingTargetHitInput input)
        {
            if (!input.IsTarget || string.IsNullOrWhiteSpace(input.ShotId) || !consumedShotIds.Add(input.ShotId))
                return false;

            if (marker != null)
            {
                marker.position = input.WorldPoint + input.WorldNormal.normalized * 0.003f;
                marker.gameObject.SetActive(true);
            }
            return true;
        }

        public void ResetFeedback()
        {
            consumedShotIds.Clear();
            if (marker != null)
                marker.gameObject.SetActive(false);
        }
    }
}
