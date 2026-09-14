using System;
using UnityEngine;
using VRShooting.Application;
using VRShooting.Common;

namespace VRShooting.Unity.Combat
{
    /// <summary>One first-hit ray includes both cover and targets. No target-only ray through walls.</summary>
    public sealed class CombatPhysicsShotQuery : ICombatShotQuery
    {
        readonly int hitMask, environmentMask;
        readonly float range;
        public CombatPhysicsShotQuery(int hitMask, int environmentMask, float range = 100f)
        {
            if (hitMask == 0 || environmentMask == 0 || (hitMask & environmentMask) != environmentMask ||
                float.IsNaN(range) || float.IsInfinity(range) || range <= 0) throw new ArgumentException("Include environment layers and a finite positive range");
            this.hitMask = hitMask; this.environmentMask = environmentMask; this.range = range;
        }
        public CombatRayHitDto Cast(Vector3 origin, Vector3 direction)
        {
            if (!Finite(origin) || !Finite(direction) || !Finite(direction.sqrMagnitude) || direction.sqrMagnitude < 1e-8) return default;
            if (Physics.CheckSphere(origin, .01f, environmentMask, QueryTriggerInteraction.Ignore))
                return new CombatRayHitDto { Hit = true, Point = origin };
            if (!Physics.Raycast(origin, direction.normalized, out var hit, range, hitMask, QueryTriggerInteraction.Ignore)) return default;
            var binding = hit.collider.GetComponentInParent<CombatEntityBinding>();
            return new CombatRayHitDto { Hit = true, EntityId = binding != null ? binding.EntityId : "", Point = hit.point };
        }
        static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
        static bool Finite(Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z);
    }
}
