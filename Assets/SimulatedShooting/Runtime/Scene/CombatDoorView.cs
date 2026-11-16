using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SimulatedShooting.Scene
{
    public sealed class CombatDoorView : MonoBehaviour
    {
        public string RoomId;
        public Transform Hinge;
        public NavMeshObstacle Obstacle;
        public bool InitiallyOpen;
        public bool IsOpen { get; private set; }
        readonly HashSet<string> events = new HashSet<string>();
        readonly Dictionary<Collider, bool> blockers = new Dictionary<Collider, bool>();

        void ApplyBlocking()
        {
            // Only the moving leaf belongs to Hinge; frames and surrounding walls stay solid.
            if (Hinge != null)
                foreach (var collider in Hinge.GetComponentsInChildren<Collider>(true))
                {
                    if (collider.isTrigger) continue;
                    if (!blockers.ContainsKey(collider)) blockers.Add(collider, collider.enabled);
                    collider.enabled = !IsOpen && blockers[collider];
                }
            if (Obstacle != null) Obstacle.enabled = !IsOpen;
        }

        public void Apply(string eventId, bool open)
        {
            if (string.IsNullOrEmpty(eventId) || !events.Add(eventId)) return;
            IsOpen = open;
            // Clear the navigation gate on the same command that starts the leaf animation.
            ApplyBlocking();
        }

        void Update()
        {
            var target = Quaternion.Euler(0, IsOpen ? 100 : 0, 0);
            Hinge.localRotation = Quaternion.RotateTowards(Hinge.localRotation, target, 150 * Time.deltaTime);
            if (IsOpen && Quaternion.Angle(Hinge.localRotation, target) < 1 && Obstacle != null) Obstacle.enabled = false;
        }

        public void ResetView()
        {
            events.Clear();
            IsOpen = InitiallyOpen;
            Hinge.localRotation = Quaternion.Euler(0, IsOpen ? 100 : 0, 0);
            ApplyBlocking();
        }
    }
}
