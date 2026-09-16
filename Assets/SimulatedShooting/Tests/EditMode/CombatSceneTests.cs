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
        public void Bdd19_ParkedScootersStayOutsideSearchableBuildings()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity", OpenSceneMode.Single);
            var geometry = Object.FindObjectOfType<CombatSceneBindings>().GeometryRoot;
            var floors = geometry.Find("Town_PerimeterBuildings").Cast<Transform>()
                .Select(house => house.Find("Interior/Floor").GetComponent<Renderer>().bounds).ToArray();
            var scooters = geometry.GetComponentsInChildren<BoxCollider>()
                .Where(collider => collider.name == "ParkedScooter").ToArray();
            Assert.That(scooters, Has.Length.EqualTo(12));
            foreach (var scooter in scooters)
                foreach (var floor in floors)
                {
                    var bounds = scooter.bounds;
                    var overlapX = Mathf.Min(bounds.max.x, floor.max.x) - Mathf.Max(bounds.min.x, floor.min.x);
                    var overlapZ = Mathf.Min(bounds.max.z, floor.max.z) - Mathf.Max(bounds.min.z, floor.min.z);
                    Assert.That(overlapX <= .1f || overlapZ <= .1f, Is.True,
                        "Parked scooter intersects a searchable building: " + scooter.transform.position);
                }
        }

        [Test]
        public void Bdd19_VisibleUrbanWalkwaysDoNotShareCoplanarTopSurfaces()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity", OpenSceneMode.Single);
            var geometry = Object.FindObjectOfType<CombatSceneBindings>().GeometryRoot;
            var surfaces = geometry.Cast<Transform>().Select(t => t.GetComponent<MeshRenderer>())
                .Where(r => r != null && r.enabled && r.transform.localScale.y <= .6f &&
                    r.transform.localScale.x >= 1 && r.transform.localScale.z >= 1).ToArray();
            for (int i = 0; i < surfaces.Length; i++)
                for (int j = i + 1; j < surfaces.Length; j++)
                {
                    var a = surfaces[i].bounds;
                    var b = surfaces[j].bounds;
                    if (Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x) <= .1f ||
                        Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z) <= .1f) continue;
                    Assert.That(Mathf.Abs(a.max.y - b.max.y), Is.GreaterThan(.01f),
                        surfaces[i].name + " and " + surfaces[j].name + " can flicker when the camera moves");
                }
        }

        [Test]
        public void Bdd19_IndoorDummiesUsePhotographicPeopleAndFacadesHaveSurfaceMaps()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity", OpenSceneMode.Single);
            var bindings = Object.FindObjectOfType<CombatSceneBindings>();
            foreach (Transform house in bindings.GeometryRoot.Find("Town_PerimeterBuildings"))
            {
                var floorBounds = house.Find("Interior/Floor").GetComponent<Renderer>().bounds;
                var foundationBounds = house.Find("Foundation").GetComponent<Renderer>().bounds;
                Assert.That(floorBounds.max.y - foundationBounds.max.y, Is.GreaterThanOrEqualTo(.025f), house.name + " has coplanar floor surfaces");
                var dummy = house.Find("Interior/TrainingDummy/PhotographicPerson");
                Assert.That(dummy, Is.Not.Null, house.name);
                var renderers = dummy.GetComponentsInChildren<MeshRenderer>();
                Assert.That(renderers, Is.Not.Empty);
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                    Assert.That(renderer.sharedMaterial.GetTexture("_BumpMap"), Is.Not.Null);
                    Assert.That(UnityEditor.AssetDatabase.GetAssetPath(renderer.sharedMaterial.mainTexture), Does.Contain("Renderpeople"));
                }
                Assert.That(bounds.size.y, Is.InRange(1.7f, 1.85f));
                Assert.That(dummy.GetComponentsInChildren<CombatActorView>(), Is.Empty);
            }
            foreach (var name in new[] { "IvoryTiles", "MintTiles" })
            {
                var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/SimulatedShooting/Art/Combat/TaiwanStreet/" + name + ".mat");
                Assert.That(mat.GetTexture("_BumpMap"), Is.Not.Null);
                Assert.That(mat.GetTexture("_MetallicGlossMap"), Is.Not.Null);
            }
        }

        [Test]
        public void Bdd15_19_TaiwanStreetAndTrenchArtRetainReadableSignsAndRoutes()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity", OpenSceneMode.Single);
            var bindings = Object.FindObjectOfType<CombatSceneBindings>();
            var art = bindings.GeometryRoot.Find("TaiwanStreetArt");
            Assert.That(art, Is.Not.Null);
            var signs = art.GetComponentsInChildren<TMPro.TextMeshPro>();
            Assert.That(signs.Count(s => s.text.Contains("早餐") || s.text.Contains("機車行") || s.text.Contains("便當")), Is.GreaterThanOrEqualTo(3));
            foreach (var sign in signs)
            {
                Assert.That(sign.font, Is.Not.Null);
                Assert.That(sign.font.HasCharacters(sign.text), Is.True, sign.text);
            }
            var parts = art.GetComponentsInChildren<Transform>();
            Assert.That(parts.Count(t => t.name == "ParkedScooter"), Is.EqualTo(12));
            Assert.That(parts.Count(t => t.name == "RoofWaterTank"), Is.GreaterThanOrEqualTo(12));
            Assert.That(parts.Count(t => t.name == "AirConditioner"), Is.GreaterThanOrEqualTo(15));
            Assert.That(parts.Count(t => t.name == "ShallowDrainWater"), Is.EqualTo(bindings.GeometryRoot.GetComponentsInChildren<Transform>().Count(t => t.name == "TrenchFloor")));
            Assert.That(art.GetComponentsInChildren<MeshRenderer>().Length, Is.LessThan(300), "Art meshes must be batched by area/material.");
            foreach (Transform house in bindings.GeometryRoot.Find("Town_PerimeterBuildings"))
                Assert.That(house.Find("Interior/TrainingDummy"), Is.Not.Null, house.name);
            Assert.That(bindings.GeometryRoot.Find("WalkwaySafety"), Is.Not.Null);
        }

        [Test]
        public void Bdd19_StreetScootersUseImportedModelAndTaiwanFont()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity", OpenSceneMode.Single);
            var bindings = Object.FindObjectOfType<CombatSceneBindings>();
            var scooters = bindings.GeometryRoot.GetComponentsInChildren<Transform>().Where(t => t.name == "ParkedScooter").ToArray();
            Assert.That(scooters.Length, Is.EqualTo(12));
            foreach (var scooter in scooters)
            {
                var meshes = scooter.GetComponentsInChildren<MeshFilter>();
                Assert.That(meshes, Is.Not.Empty);
                Assert.That(meshes.All(m => UnityEditor.AssetDatabase.GetAssetPath(m.sharedMesh).EndsWith("ScopiaScooter/scooter2.obj")), Is.True);
                var collider = scooter.GetComponent<BoxCollider>();
                Assert.That(collider.size.z, Is.InRange(1.6f, 2.1f));
                Assert.That(collider.size.x, Is.InRange(.5f, .9f));
                Assert.That(scooter.Find("RearFairing"), Is.Null);
            }
            Assert.That(Object.FindObjectOfType<CombatSceneFixture>().InspectionFont.name, Is.EqualTo("NotoSansTC"));
            Assert.That(bindings.GeometryRoot.GetComponentsInChildren<TMPro.TextMeshPro>().Any(t => t.text.Contains("MINSHENG")), Is.False);
        }

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
        public void Bdd15_19_20_EnemyPrefabAndIndoorDeploymentUseTacticalModel()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimulatedShooting/Prefabs/Combat/Actor_Enemy.prefab");
            var view = prefab.GetComponent<CombatActorView>();
            var soldier = view.VisualRoot.Find("CC0_TacticalSWAT");
            Assert.That(soldier, Is.Not.Null);
            Assert.That(soldier.GetComponentsInChildren<Renderer>(true), Is.Not.Empty);

            EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity", OpenSceneMode.Single);
            var bindings = Object.FindObjectOfType<CombatSceneBindings>();
            var buildingSpawns = bindings.Points.Where(p => p.Kind == CombatPointKind.EnemySpawn && p.Id.StartsWith("building-spawn-")).ToArray();
            Assert.That(buildingSpawns, Has.Length.EqualTo(6));
            Assert.That(buildingSpawns.All(p => !string.IsNullOrEmpty(p.RoomId)), Is.True);
            Assert.That(buildingSpawns.Count(p => p.RoomId == "room-31"), Is.EqualTo(2));
            Assert.That(bindings.GeometryRoot.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("PermanentRoomPassage_Floor_")), Is.EqualTo(2));
            Assert.That(bindings.Doors.All(d => d.InitiallyOpen), Is.True);
            Assert.That(bindings.GeometryRoot.Find("WalkwaySafety").GetComponentsInChildren<Renderer>(true)
                .Where(r => r.name == "StairGuard" || r.name == "PlatformGuard").All(r => !r.enabled), Is.True);
            Assert.That(bindings.GeometryRoot.GetComponentsInChildren<Transform>(true).Count(t => t.name == "ParkedScooter"), Is.EqualTo(12));
        }

        [Test]
        public void Bdd19_RoundEarthBackdropIsReplacedWithRealTaipeiBuildings()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity", OpenSceneMode.Single);
            var bindings = Object.FindObjectOfType<CombatSceneBindings>();
            var environment = bindings.transform.Find("EnvironmentBackdrop");
            Assert.That(environment.GetComponentsInChildren<Transform>(true).Any(t => t.name == "DistantEarth"), Is.False);
            var backdrop = environment.Find("TaipeiGovBuildingBackdrop");
            Assert.That(backdrop, Is.Not.Null);
            Assert.That(backdrop.childCount, Is.EqualTo(14));
            Assert.That(backdrop.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(backdrop.GetComponentsInChildren<MeshFilter>(true).All(m =>
                UnityEditor.AssetDatabase.GetAssetPath(m.sharedMesh).Contains("TaipeiGovBuildings") &&
                UnityEditor.AssetDatabase.GetAssetPath(m.sharedMesh).EndsWith("building.obj")), Is.True);
        }

        [Test]
        public void Bdd19_TaiwanStreetUsesDistinctShopSignsAndVerticalBladeSigns()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity", OpenSceneMode.Single);
            var labels = Object.FindObjectsOfType<TMPro.TextMeshPro>(true).Select(label => label.text).ToArray();
            Assert.That(labels.Count(text => text == "新生五金行"), Is.EqualTo(1));
            Assert.That(labels, Does.Contain("廣慶百貨行"));
            Assert.That(labels.Count(text => text.Contains("\n")), Is.GreaterThanOrEqualTo(7));
            Assert.That(Object.FindObjectsOfType<MeshRenderer>(true).Count(renderer =>
                renderer.sharedMaterial != null && renderer.sharedMaterial.name == "TaiwanRibbonGlass"), Is.GreaterThan(0));
        }
    }
}
