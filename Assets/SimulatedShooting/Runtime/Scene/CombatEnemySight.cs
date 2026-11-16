using UnityEngine;
using VRShooting.Common;

namespace SimulatedShooting.Scene
{
    // Physical perception only. CombatCoreService owns state, aim threshold and attack deadlines.
    public readonly struct CombatEnemySight
    {
        public readonly float Distance, VerticalOffset;
        public readonly bool InCircle, Visible;
        public readonly Collider HeadBlocker, ChestBlocker, FootBlocker;

        CombatEnemySight(float distance, float verticalOffset, bool inCircle, bool visible,
            Collider head, Collider chest, Collider foot)
        {
            Distance = distance; VerticalOffset = verticalOffset; InCircle = inCircle; Visible = visible;
            HeadBlocker = head; ChestBlocker = chest; FootBlocker = foot;
        }

        public static CombatEnemySight Sample(Vector3 origin, Vector3 playerFeet, Transform enemy,
            Transform player, Transform weapon, CombatConfigDto config)
        {
            var delta = playerFeet - origin;
            var distance = Vector3.ProjectOnPlane(delta, Vector3.up).magnitude;
            var vertical = Mathf.Abs(playerFeet.y + config.EnemyChestSampleHeight - origin.y);
            var inside = distance <= config.EnemyRange && vertical <= config.EnemyVerticalTolerance;
            if (!inside) return new CombatEnemySight(distance, vertical, false, false, null, null, null);

            // RaycastAll does not report a collider containing the origin. Detect an embedded
            // origin explicitly, after filtering the actor's own capsule and the player's rig.
            foreach (var overlap in Physics.OverlapSphere(origin, .02f, ~0, QueryTriggerInteraction.Ignore))
                if (!Ignore(overlap, enemy, player, weapon))
                    return new CombatEnemySight(distance, vertical, true, false, overlap, overlap, overlap);
            var head = Blocker(origin, playerFeet + Vector3.up * config.EnemyHeadSampleHeight, enemy, player, weapon);
            var chest = Blocker(origin, playerFeet + Vector3.up * config.EnemyChestSampleHeight, enemy, player, weapon);
            var foot = Blocker(origin, playerFeet + Vector3.up * config.EnemyFootSampleHeight, enemy, player, weapon);
            return new CombatEnemySight(distance, vertical, true, head == null || chest == null || foot == null, head, chest, foot);
        }

        static Collider Blocker(Vector3 origin, Vector3 target, Transform enemy, Transform player, Transform weapon)
        {
            var offset = target - origin;
            if (offset.sqrMagnitude < .000001f) return null;
            Collider nearest = null;
            var distance = float.PositiveInfinity;
            foreach (var hit in Physics.RaycastAll(origin, offset.normalized, offset.magnitude, ~0, QueryTriggerInteraction.Ignore))
                if (hit.distance < distance && !Ignore(hit.collider, enemy, player, weapon))
                { nearest = hit.collider; distance = hit.distance; }
            return nearest;
        }

        static bool Ignore(Collider collider, Transform enemy, Transform player, Transform weapon)
            => (enemy != null && collider.transform.IsChildOf(enemy)) ||
               (player != null && collider.transform.IsChildOf(player)) ||
               (weapon != null && collider.transform.IsChildOf(weapon));
    }
}
