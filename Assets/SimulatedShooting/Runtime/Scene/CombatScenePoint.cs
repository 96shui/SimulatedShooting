using System;
using UnityEngine;

namespace SimulatedShooting.Scene
{
    public enum CombatPointKind { SearchNode, EnemySpawn, EstimateArea, Corner, Entrance, Room, Floor, Route }

    [DisallowMultipleComponent]
    public sealed class CombatScenePoint : MonoBehaviour
    {
        public string Id;
        public string RegionId;
        public string FloorId;
        public string RoomId;
        public CombatPointKind Kind;
        public BoxCollider Volume;
        public CombatDoorView Door;
        public bool RequiresNavigation => Kind != CombatPointKind.EstimateArea;
        public event Action<CombatScenePoint, Transform, string> FactReported;

        public bool Confirm(Transform actor, Ray ray, float reach = 3f)
        {
            if (actor == null || Volume == null || !Volume.bounds.Contains(actor.position + Vector3.up)) return false;
            if (Door != null)
            {
                if (!Physics.Raycast(ray, out var doorHit, reach, ~0, QueryTriggerInteraction.Ignore)
                    || !doorHit.transform.IsChildOf(Door.transform)) return false;
            }
            else
            {
                float distance = Vector3.Dot(transform.position + Vector3.up - ray.origin, ray.direction);
                if (distance < 0 || distance > reach || !Volume.bounds.Contains(ray.GetPoint(distance))) return false;
                if (Physics.Raycast(ray, distance, ~0, QueryTriggerInteraction.Ignore)) return false;
            }
            FactReported?.Invoke(this, actor, "confirm");
            return true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<CombatSceneWalker>() != null) FactReported?.Invoke(this, other.transform, "enter");
        }

        void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<CombatSceneWalker>() != null) FactReported?.Invoke(this, other.transform, "exit");
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Kind == CombatPointKind.EnemySpawn ? Color.red : Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * .15f, .25f);
            if (Volume != null) Gizmos.DrawWireCube(Volume.bounds.center, Volume.bounds.size);
        }
    }
}
