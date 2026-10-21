using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Tests.EditMode
{
    // P1 task005/task013, BDD05/06: the ray and tracer must originate at the actual barrel rim.
    public sealed class TrainingRifleMuzzleAlignmentTests
    {
        const string PrefabPath = "Assets/SimulatedShooting/Prefabs/Weapons/Weapon_training-rifle_Blockout.prefab";

        [Test]
        public void SharedPrefabMuzzleMatchesActualMeshOpening()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            AssertMuzzleAtOpening(prefab.GetComponent<WeaponPrefabBinding>());
        }

        [TestCase("Assets/Scenes/ZeroingRangeScene.unity")]
        [TestCase("Assets/Scenes/MovingTargetRangeScene.unity")]
        public void SavedRangeInheritsCorrectMuzzleWithoutChangingItsUiOrTarget(string path)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var bindings = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<WeaponPrefabBinding>(true)).ToArray();
                Assert.That(bindings, Is.Not.Empty);
                foreach (var binding in bindings) AssertMuzzleAtOpening(binding);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        static void AssertMuzzleAtOpening(WeaponPrefabBinding binding)
        {
            Assert.That(binding, Is.Not.Null);
            var points = new List<Vector3>();
            foreach (var filter in binding.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                foreach (var point in filter.sharedMesh.vertices)
                    points.Add(binding.RecoilRoot.InverseTransformPoint(filter.transform.TransformPoint(point)));
            }
            Assert.That(points.Count, Is.GreaterThan(1000), "Inspect the actual imported rifle mesh.");
            var front = points.Max(point => point.z);
            var rim = points.Where(point => point.z >= front - .00025f).ToArray();
            var center = new Vector3(
                (rim.Min(point => point.x) + rim.Max(point => point.x)) * .5f,
                (rim.Min(point => point.y) + rim.Max(point => point.y)) * .5f,
                front + .001f);
            var muzzle = binding.RecoilRoot.InverseTransformPoint(binding.MuzzlePoint.position);
            Assert.That(Vector3.Distance(muzzle, center), Is.LessThan(.0002f),
                $"Muzzle {muzzle:F6} must sit at the mesh rim centre plus 1mm clearance {center:F6}.");
        }
    }
}
