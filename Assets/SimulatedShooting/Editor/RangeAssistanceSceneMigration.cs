using System;
using System.Collections.Generic;
using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRShooting.Application;

namespace SimulatedShooting.Editor
{
    // BDD29: move target assemblies, never tracked player/camera/controller transforms.
    public static class RangeAssistanceSceneMigration
    {
        [MenuItem("Tools/Simulated Shooting/Apply Shorter Training Distances")]
        public static void Apply()
        {
            foreach (var path in new[] { "Assets/Scenes/ZeroingRangeScene.unity", "Assets/Scenes/MovingTargetRangeScene.unity" })
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                PatchScene(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        public static void PatchScene(UnityEngine.SceneManagement.Scene scene)
        {
            var nodes = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SceneTestId>(true)).ToArray();
            var zeroing = nodes.Any(n => n.Id == "ZeroingRange.Root");
            var prefix = zeroing ? "ZeroingRange" : "MovingTargetRange";
            Transform At(string id) => nodes.FirstOrDefault(n => n.Id == id)?.transform;
            var station = At(prefix + ".FiringStation.Root")?.GetComponent<TrainingRangeSceneBindings>();
            var target = At(prefix + (zeroing ? ".Target.Primary" : ".Route.Root"));
            if (station == null || target == null)
                throw new InvalidOperationException("Missing range station/target bindings: " + scene.path);
            var aimPlane = zeroing ? At(prefix + ".Target.Center") : target;
            var forward = station.PlayerRootAnchor.forward;
            var distance = zeroing ? ZeroingRules.DistanceMeters : 50f;
            var delta = forward * (distance - Vector3.Dot(aimPlane.position - station.PlayerRootAnchor.position, forward));
            var moved = new HashSet<Transform> { target };
            var panels = At(prefix + ".Visual.TargetBayPanels");
            if (panels != null) moved.Add(panels);
            if (!zeroing)
            {
                var moving = At(prefix + ".Target");
                if (moving != null) moved.Add(moving);
            }
            foreach (var node in moved)
                if (!moved.Any(parent => parent != node && node.IsChildOf(parent))) node.position += delta;
            station.TargetRootAnchor.position = aimPlane.position;
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[RangeAssistance] " + prefix + ": " + distance + "m; player and nearby interaction anchors preserved.");
        }
    }
}
