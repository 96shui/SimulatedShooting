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

        public void Launch(GrenadeThrowPlanDto throwPlan, Action onExpired, Func<Vector3> actualPosition)
        {
            plan = throwPlan; expired = onExpired; positionProvider = actualPosition; elapsed = 0; launched = true;
            transform.position = plan.Origin;
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
            var ahead = GrenadeTrajectory.Position(plan, now + .03);
            if ((ahead-transform.position).sqrMagnitude > .0001f) transform.rotation = Quaternion.LookRotation(ahead-transform.position);
            transform.Rotate(420f * Time.deltaTime, 280f * Time.deltaTime, 190f * Time.deltaTime, Space.Self);
            if (t >= 1f)
            {
                launched = false;
                expired?.Invoke();
            }
        }
    }
}
