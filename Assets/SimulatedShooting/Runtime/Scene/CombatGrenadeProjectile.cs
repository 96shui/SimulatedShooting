using System;
using UnityEngine;
using VRShooting.Common;

namespace SimulatedShooting.Scene
{
    public sealed class CombatGrenadeProjectile : MonoBehaviour
    {
        GrenadeThrowPlanDto plan;
        float elapsed;
        Action expired;
        Func<Vector3> positionProvider;
        bool launched;
        Vector3 previousPosition;

        public void Launch(GrenadeThrowPlanDto throwPlan, Action onExpired, Func<Vector3> actualPosition)
        {
            plan = throwPlan; expired = onExpired; positionProvider = actualPosition; elapsed = 0; launched = true;
            transform.position = plan.Origin;
            previousPosition=plan.Origin;
            transform.rotation = Quaternion.LookRotation((plan.Target - plan.Origin).normalized);
        }

        void Update()
        {
            if (!launched) return;
            elapsed += Time.deltaTime;
            var duration = Mathf.Max(.08f, (float)(plan.ExplosionTime - plan.ThrowTime));
            var now = plan.ThrowTime + elapsed;
            var t = Mathf.Clamp01(elapsed / duration);
            transform.position = positionProvider?.Invoke() ?? GrenadeTrajectory.Position(plan, now);
            var movement=transform.position-previousPosition;
            if(movement.sqrMagnitude>.000001f)
                transform.Rotate(420f * Time.deltaTime,280f * Time.deltaTime,190f * Time.deltaTime,Space.Self);
            else if(elapsed>plan.WindupSeconds+.05f)
                // A tumbling model can have a taller vertical bound than its collision radius.
                // Settle upright once the authoritative position stops so it stays above ground.
                transform.rotation=Quaternion.identity;
            previousPosition=transform.position;
            // A service-driven projectile lives until GrenadeExploded/GrenadeCancelled.
            // Wall-clock time must not hide it while the combat clock is stationary.
            if (positionProvider == null && t >= 1f)
            {
                launched = false;
                expired?.Invoke();
            }
        }
    }
}
