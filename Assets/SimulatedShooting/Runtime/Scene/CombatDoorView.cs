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
        public bool IsOpen { get; private set; }
        readonly HashSet<string> events = new HashSet<string>();

        public void Apply(string eventId, bool open)
        {
            if (string.IsNullOrEmpty(eventId) || !events.Add(eventId)) return;
            IsOpen = open;
            // Physical leaf stays solid; navigation carving clears after the leaf has swung away.
            if (!open) Obstacle.enabled = true;
        }

        void Update()
        {
            var target = Quaternion.Euler(0, IsOpen ? 100 : 0, 0);
            Hinge.localRotation = Quaternion.RotateTowards(Hinge.localRotation, target, 150 * Time.deltaTime);
            if (IsOpen && Quaternion.Angle(Hinge.localRotation, target) < 1) Obstacle.enabled = false;
        }

        public void ResetView()
        {
            events.Clear();
            IsOpen = false;
            Hinge.localRotation = Quaternion.identity;
            Obstacle.enabled = true;
        }
    }
}
