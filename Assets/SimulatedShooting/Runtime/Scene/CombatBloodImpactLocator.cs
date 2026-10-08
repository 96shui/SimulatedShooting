using UnityEngine;

namespace SimulatedShooting.Scene
{
    public static class CombatBloodImpactLocator
    {
        // A death snapshot can disable the hit collider before EnemyHit is presented.
        // Collider.bounds/ClosestPoint must not be used on that disabled collider.
        public static Vector3 Resolve(CombatActorView actor, Ray shot, out Vector3 normal)
        {
            var collider = actor.HitCollider;
            if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy &&
                collider.Raycast(shot, out var hit, 100f))
            {
                normal = hit.normal;
                return hit.point;
            }

            Bounds local;
            if (collider is CapsuleCollider capsule)
            {
                var size = Vector3.one * capsule.radius * 2f;
                size[capsule.direction] = Mathf.Max(capsule.height, size[capsule.direction]);
                local = new Bounds(capsule.center, size);
            }
            else if (collider is BoxCollider box) local = new Bounds(box.center, box.size);
            else if (collider is SphereCollider sphere)
                local = new Bounds(sphere.center, Vector3.one * sphere.radius * 2f);
            else
            {
                var body = new Bounds(actor.transform.position + Vector3.up * 1.1f, new Vector3(.6f, 1.6f, .6f));
                return ResolveBounds(body, shot, out normal);
            }

            var transform = collider.transform;
            var bounds = new Bounds(transform.TransformPoint(local.center), Vector3.zero);
            for (var i = 0; i < 8; i++)
            {
                var corner = local.center + Vector3.Scale(local.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                bounds.Encapsulate(transform.TransformPoint(corner));
            }
            return ResolveBounds(bounds, shot, out normal);
        }

        static Vector3 ResolveBounds(Bounds body, Ray shot, out Vector3 normal)
        {
            var point = body.IntersectRay(shot, out var distance) && distance >= 0 && distance <= 100f
                ? shot.GetPoint(distance) : body.ClosestPoint(shot.origin);
            normal = (shot.origin - point).normalized;
            if (normal.sqrMagnitude < .001f) normal = -shot.direction.normalized;
            return point;
        }
    }
}
