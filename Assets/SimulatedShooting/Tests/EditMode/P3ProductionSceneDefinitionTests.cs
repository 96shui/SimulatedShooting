using System.Linq;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using VRShooting.Common;
using VRShooting.Contracts;

namespace SimulatedShooting.Tests.EditMode
{
    public sealed class P3ProductionSceneDefinitionTests
    {
        [Test]
        public void Bdd14_18_RuntimeLoaderRejectsEditModeWithoutChangingScenes()
        {
            int count = UnityEngine.SceneManagement.SceneManager.sceneCount;
            var result = new UnityCombatSceneLoader().LoadAsync(TrainingMode.Trench, System.Threading.CancellationToken.None).GetAwaiter().GetResult();
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.ResourceUnavailable));
            Assert.That(UnityEngine.SceneManagement.SceneManager.sceneCount, Is.EqualTo(count));
            Assert.That(UnityCombatSceneLoader.PreparingProductionScene, Is.False);
        }

        // BDD24 fixed-seed candidates; BDD25 three floors and independent room gates.
        [TestCase(TrainingMode.Trench)]
        [TestCase(TrainingMode.Urban)]
        public void Screen24_25_RealSceneProvidesValidProductionDefinition(TrainingMode mode)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity", OpenSceneMode.Additive);
            NavMeshDataInstance navigation=default;
            try
            {
                var binding = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CombatSceneBindings>(true)).Single();
                navigation=NavMesh.AddNavMeshData(binding.NavigationData);
                var definition = CombatSceneDefinitionBuilder.Build(binding, mode);
                var valid = definition.Validate();
                Assert.That(valid.Success, Is.True, valid.Message);
                Assert.That(definition.SpawnPoints.All(p => p.Navigable), Is.True);
                Assert.That(definition.SpawnPoints.All(p => Vector3.Distance(p.WorldPosition,p.EstimatePosition)>.1f), Is.True,"C05: estimates must not reveal actual enemy positions");
                Assert.That(binding.WeaponAnchor.GetComponentsInChildren<Collider>(true).All(c=>!c.enabled),Is.True,"Inspection rifle must not block the walking route");
                if (mode == TrainingMode.Urban)
                {
                    Assert.That(definition.Floors.Count, Is.EqualTo(3));
                    Assert.That(definition.Floors.Sum(f => f.Rooms.Count), Is.InRange(3, 5));
                }
                else Assert.That(definition.SearchNodes.Count, Is.GreaterThan(0));
            }
            finally { if(navigation.valid)navigation.Remove();EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
