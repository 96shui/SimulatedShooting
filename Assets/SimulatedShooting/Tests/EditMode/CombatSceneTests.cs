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
    }
}
