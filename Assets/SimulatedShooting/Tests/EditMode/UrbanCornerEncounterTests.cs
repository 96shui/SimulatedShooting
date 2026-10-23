using System.Linq;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace SimulatedShooting.Tests.EditMode
{
    public sealed class UrbanCornerEncounterTests
    {
        // BDD31 / P3 scene task009: six existing candidates, navigable corners, hidden from entry.
        [Test] public void Bdd31_BuildingCandidatesAreConcealedNavigableCorners()
        {
            var setup=EditorSceneManager.GetSceneManagerSetup();
            var nav=default(NavMeshDataInstance);
            try
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity",OpenSceneMode.Single);
                var binding=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CombatSceneBindings>(true)).Single();
                nav=NavMesh.AddNavMeshData(binding.NavigationData);Physics.SyncTransforms();
                var points=binding.Points.Where(p=>p.Id.StartsWith("building-spawn-")).ToArray();
                Assert.That(points.Length,Is.EqualTo(6));
                foreach(var point in points)
                {
                    var pos=point.transform.position;
                    Assert.That(NavMesh.SamplePosition(pos,out var hit,.15f,NavMesh.AllAreas),Is.True,point.Id);
                    Assert.That(Mathf.Abs(hit.position.y-pos.y),Is.LessThan(.15f),point.Id);
                    Assert.That(Physics.CheckCapsule(pos+Vector3.up*.4f,pos+Vector3.up*1.4f,.25f,~(1<<2),QueryTriggerInteraction.Ignore),Is.False,"Enemy embedded in geometry: "+point.Id);
                    var start=binding.UrbanEntry.position+Vector3.up*1.4f;
                    var target=pos+Vector3.up*1.1f;
                    Assert.That(Physics.Linecast(start,target,~(1<<2),QueryTriggerInteraction.Ignore),Is.True,"Exposed from street entry: "+point.Id);
                }
            }
            finally{if(nav.valid)nav.Remove();EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}
