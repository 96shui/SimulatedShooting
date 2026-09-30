using System.Linq;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using VRShooting.Common;

namespace SimulatedShooting.Tests.EditMode
{
    public sealed class BunkersTrenchSceneTests
    {
        const string ScenePath = "Assets/Scenes/BunkersOriginalMode3CombatScene.unity";
        const string ModelFolder = "Assets/TheTalesFactory/Bunkers & Trenches Pack/";

        // BDD 12 "模式 03 使用新战壕场景"; BDD 14 "新战壕入口与敌人候选点";
        // BDD 15 "小地图与新战壕对应"; P3 scene tasks 003/006.
        [Test]
        public void Bdd12_14_15_OriginalDemoTerrainHasWalkableSpawnsAndOwnMap()
        {
            Assert.That(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ScenePath), Is.True);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            NavMeshDataInstance navigation = default;
            try
            {
                var binding = scene.GetRootGameObjects()
                    .SelectMany(r => r.GetComponentsInChildren<CombatSceneBindings>(true)).Single();
                Assert.That(binding.ValidateBindings(), Is.Empty);
                Assert.That(binding.GeometryRoot.GetComponentsInChildren<MeshFilter>(true)
                    .Count(f => f.sharedMesh != null && AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith(ModelFolder)),
                    Is.GreaterThanOrEqualTo(8));
                var terrain = binding.GeometryRoot.GetComponentsInChildren<Terrain>(true).Single();
                Assert.That(terrain.GetComponent<TerrainCollider>(), Is.Not.Null);
                Assert.That(terrain.terrainData.size.x, Is.GreaterThan(500));
                var map = binding.Maps.Single(m => m.Id == "trench-a");
                Assert.That(AssetDatabase.GetAssetPath(map.Plan), Does.Contain("OriginalDemoMap"));
                var points = binding.Points.Where(p => p.RegionId == "trench-a").ToArray();
                var enemies = points.Where(p => p.Kind == CombatPointKind.EnemySpawn).ToArray();
                Assert.That(enemies.Length, Is.InRange(5, 8));
                Assert.That(points.Count(p => p.Kind == CombatPointKind.SearchNode), Is.GreaterThanOrEqualTo(5));
                foreach (var point in points)
                {
                    var uv = map.WorldToMap(point.transform.position);
                    Assert.That(uv.x, Is.InRange(0f, 1f), point.Id);
                    Assert.That(uv.y, Is.InRange(0f, 1f), point.Id);
                }
                Assert.That(binding.TrenchEntry.position.y,
                    Is.EqualTo(terrain.SampleHeight(binding.TrenchEntry.position) + terrain.transform.position.y).Within(.25f));
                navigation = NavMesh.AddNavMeshData(binding.NavigationData);
                var definition = CombatSceneDefinitionBuilder.Build(binding, TrainingMode.Trench);
                var valid = definition.Validate();
                Assert.That(valid.Success, Is.True, valid.Message);
                var start = binding.TrenchEntry.position;
                Assert.That(NavMesh.SamplePosition(start, out var startHit, .8f, NavMesh.AllAreas), Is.True);
                foreach (var target in binding.TeammateSpawns.Concat(points.Where(p => p.RequiresNavigation).Select(p => p.transform)))
                {
                    Assert.That(NavMesh.SamplePosition(target.position, out var hit, .8f, NavMesh.AllAreas), Is.True, target.name);
                    var path = new NavMeshPath();
                    Assert.That(NavMesh.CalculatePath(startHit.position, hit.position, NavMesh.AllAreas, path), Is.True, target.name);
                    Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete), target.name);
                }
            }
            finally
            {
                if (navigation.valid) navigation.Remove();
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void Bdd12_ModeThreeAndUrbanUseDifferentPhysicalScenes()
        {
            Assert.That(UnityCombatSceneLoader.ScenePathFor(TrainingMode.Trench), Is.EqualTo(ScenePath));
            Assert.That(UnityCombatSceneLoader.ScenePathFor(TrainingMode.Urban), Is.EqualTo("Assets/Scenes/CombatScene.unity"));
        }
    }
}
