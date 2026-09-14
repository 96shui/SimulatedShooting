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
        public void Bdd19_ExteriorCollisionCoversDistrictWithoutIntersectingBuildings()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity", OpenSceneMode.Single);
            var bindings = Object.FindObjectOfType<CombatSceneBindings>();
            var landscape = bindings.transform.Find("EnvironmentBackdrop/Landscape").GetComponent<BoxCollider>();
            Assert.That(landscape, Is.Not.Null);
            Assert.That(landscape.enabled && !landscape.isTrigger, Is.True);
            var exterior = bindings.GeometryRoot.Find("Town_ExteriorCollision");
            Assert.That(exterior, Is.Not.Null);
            var ground = exterior.Find("TownGround").GetComponent<BoxCollider>();
            Physics.SyncTransforms();
            var buildings = bindings.GeometryRoot.Find("Town_PerimeterBuildings").GetComponentsInChildren<BoxCollider>();
            foreach (var building in buildings)
            {
                var min = building.bounds.min; var max = building.bounds.max;
                Assert.That(min.x, Is.GreaterThan(ground.bounds.min.x));
                Assert.That(max.x, Is.LessThan(ground.bounds.max.x));
                Assert.That(min.z, Is.GreaterThan(ground.bounds.min.z));
                Assert.That(max.z, Is.LessThan(ground.bounds.max.z));
                foreach (var wall in exterior.GetComponentsInChildren<BoxCollider>().Where(c => c != ground))
                    Assert.That(wall.bounds.Intersects(building.bounds), Is.False, wall.name + " cuts through " + building.transform.parent.name);
            }
        }

        [Test]
        public void Bdd19_PerimeterBuildingsFormStreetWithoutBlockingSearchPoints()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity", OpenSceneMode.Single);
            var bindings = Object.FindObjectOfType<CombatSceneBindings>();
            var district = bindings.GeometryRoot.Find("Town_PerimeterBuildings");
            Assert.That(district, Is.Not.Null);
            Assert.That(district.childCount, Is.EqualTo(6));
            Physics.SyncTransforms();
            var colliders = district.GetComponentsInChildren<BoxCollider>();
            Assert.That(colliders.Length, Is.GreaterThan(6));
            foreach (Transform building in district)
            {
                Assert.That(building.Find("ClosedBuildingShell"), Is.Null, building.name);
                Assert.That(building.Find("Interior/TrainingDummy"), Is.Not.Null, building.name);
            }
            foreach (var point in bindings.Points.Where(p => p.RequiresNavigation))
                foreach (var collider in colliders)
                {
                    var bounds = collider.bounds;
                    bounds.Expand(.8f);
                    Assert.That(bounds.Contains(point.transform.position + Vector3.up), Is.False, point.Id);
                }
            Assert.That(district.GetComponentsInChildren<TextMesh>(), Is.Empty);
        }

        [Test]
        public void Bdd15_19_WartimeDetailsPreserveRoutesAndRemoveWorldSigns()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity", OpenSceneMode.Single);
            var bindings = Object.FindObjectOfType<CombatSceneBindings>();
            Assert.That(bindings.ValidateBindings(), Is.Empty);
            Assert.That(bindings.GeometryRoot.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith("Sign_")), Is.False);
            var details = bindings.GeometryRoot.Find("VisualPolish_Wartime");
            Assert.That(details, Is.Not.Null);
            Assert.That(details.GetComponentsInChildren<Collider>(), Is.Empty);
            foreach (var name in new[] { "BrokenMasonry", "ScorchedPlaster", "BrokenWindowFrame", "EarthBank" })
                Assert.That(details.GetComponentsInChildren<Transform>().Any(t => t.name == name), Is.True, name);
        }

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
