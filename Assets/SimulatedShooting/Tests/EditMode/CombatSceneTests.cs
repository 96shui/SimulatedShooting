using System.Linq;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SimulatedShooting.Tests.EditMode
{
    public class CombatSceneTests
    {
        [Test]
        public void Bdd14_19_20_21_SavedSceneHasCompleteBindingsAndFiveRooms()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity", OpenSceneMode.Single);
            var bindings = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<CombatSceneBindings>()).Single();
            Assert.That(bindings.ValidateBindings(), Is.Empty);
            Assert.That(bindings.Points.Count(p => p.Kind == CombatPointKind.Room), Is.EqualTo(5));
            Assert.That(bindings.Points.Where(p => p.Kind == CombatPointKind.Room).Select(p => p.FloorId).Distinct().Count(), Is.EqualTo(3));
            Assert.That(bindings.Points.Count(p => p.Kind == CombatPointKind.EnemySpawn), Is.EqualTo(13));
            Assert.That(bindings.TrenchEntry, Is.Not.Null);
            Assert.That(bindings.UrbanEntry, Is.Not.Null);
            Assert.That(bindings.NavigationData, Is.Not.Null);
        }

        [Test]
        public void Bdd19_20_ValidatorReportsDuplicateIdsAndMissingAnchor()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity", OpenSceneMode.Single);
            var bindings = Object.FindObjectOfType<CombatSceneBindings>();
            var original = bindings.Points[1].Id;
            bindings.Points[1].Id = bindings.Points[0].Id;
            bindings.WeaponAnchor = null;
            var errors = bindings.ValidateBindings();
            Assert.That(errors.Any(e => e.Contains("Duplicate")), Is.True);
            Assert.That(errors.Any(e => e.Contains("WeaponAnchor")), Is.True);
            bindings.Points[1].Id = original;
        }

        [Test]
        public void Bdd19_TrenchTownTransitionHasNoCoplanarWalkableSurfaces()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity", OpenSceneMode.Single);
            var geometry = Object.FindObjectOfType<CombatSceneBindings>().GeometryRoot;
            var street = geometry.GetComponentsInChildren<BoxCollider>().Single(c => c.name == "Street").bounds;
            var sidewalk = geometry.GetComponentsInChildren<BoxCollider>().Single(c => c.name == "Sidewalk").bounds;
            var firstFloor = geometry.GetComponentsInChildren<BoxCollider>().Single(c => c.name == "Floor_1").bounds;
            Assert.That(Mathf.Abs(street.max.y - sidewalk.max.y), Is.GreaterThan(.005f));
            Assert.That(Mathf.Abs(street.max.y - firstFloor.max.y), Is.GreaterThan(.005f));
            var platform = geometry.GetComponentsInChildren<BoxCollider>().Single(c => c.name == "StairBase").bounds;
            Assert.That(Mathf.Abs(street.max.y - platform.max.y), Is.GreaterThan(.005f), "Screenshot: stair apron overlaps the road");
            foreach (var floor in geometry.GetComponentsInChildren<BoxCollider>().Where(c => c.name == "TrenchFloor"))
                Assert.That(floor.bounds.max.z, Is.LessThanOrEqualTo(street.min.z + .001f), "Trench exit overlaps road");
        }

        [Test]
        public void Bdd19_TrenchUsesImportedSandbagsAndDetailedTimber()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity", OpenSceneMode.Single);
            var geometry = Object.FindObjectOfType<CombatSceneBindings>().GeometryRoot;
            var placeholders = geometry.GetComponentsInChildren<Transform>().Where(t => t.name == "Sandbag").ToArray();
            var details = GameObject.Find("VisualPolish_TrenchDetails");
            Assert.That(details, Is.Not.Null);
            var bags = details.GetComponentsInChildren<MeshFilter>().Where(t => t.name == "Sandbag_Imported").ToArray();
            Assert.That(bags.Length, Is.EqualTo(placeholders.Length).And.GreaterThan(0));
            Assert.That(placeholders.All(t => !t.GetComponent<Renderer>().enabled), Is.True);
            Assert.That(bags.All(t => t.sharedMesh.vertexCount > 24), Is.True);
            Assert.That(bags.All(t => t.GetComponent<Renderer>().sharedMaterial.GetTexture("_BumpMap") != null), Is.True);
            Assert.That(details.GetComponentsInChildren<Collider>(), Is.Empty, "Visual detail must not block the existing route");
        }

        [Test]
        public void Bdd15_19_EnemyPrefabUsesImportedSoldierVisual()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimulatedShooting/Prefabs/Combat/Actor_Enemy.prefab");
            var view = prefab.GetComponent<CombatActorView>();
            var soldier = view.VisualRoot.Find("CC0_SoldierModel");
            Assert.That(soldier, Is.Not.Null);
            Assert.That(soldier.GetComponentsInChildren<Renderer>(true), Is.Not.Empty);
        }
    }
}
