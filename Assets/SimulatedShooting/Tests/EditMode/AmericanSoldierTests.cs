using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using SimulatedShooting.Scene;

namespace SimulatedShooting.Tests.EditMode
{
    public class AmericanSoldierTests
    {
        [Test]
        public void Bdd23_Task014_D3EnemyAndD4TeammateHaveDistinctSkinnedModels()
        {
            foreach (var role in new[] { "Enemy", "Teammate" })
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimulatedShooting/Prefabs/Combat/Actor_" + role + ".prefab");
                var view = root.GetComponent<CombatActorView>();
                var model = view.VisualRoot.Find(role == "Enemy" ? "DetailedCharacterD3" : "DetailedCharacterD4");
                Assert.That(model, Is.Not.Null);
                var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>();
                var skin = skins[0];
                Assert.That(skin.bones.Length, Is.GreaterThan(40));
                Assert.That(skins.SelectMany(s => s.sharedMaterials).All(m => m != null && m.shader.name == "Universal Render Pipeline/Lit"), Is.True);
                Assert.That(skins.Any(s => s.sharedMaterials.Any(m => m.GetTexture("_BaseMap") != null)), Is.True);
                if (role == "Enemy")
                {
                    Assert.That(skins.Any(s => s.sharedMaterials.Any(m => m.name == "D3_Mask")), Is.True);
                    Assert.That(skins.Single(s => s.name == "D3_Baloons").enabled, Is.False);
                    Assert.That(skins.Any(s => s.name == "D3_Suit"), Is.True);
                    Assert.That(skins.First(s => s.name == "D3_Suit").sharedMaterial.GetTexture("_BaseMap").name, Does.Contain("D3_set2"));
                    Assert.That(model.GetComponentsInChildren<Transform>(true).Any(t => t.name == "D3_Flamethrower"), Is.False);
                    var muzzleCarrier = model.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Model_QBZ191_Enemy");
                    Assert.That(muzzleCarrier.GetComponentsInChildren<Renderer>(true).Any(r => r.enabled), Is.True,
                        "BDD23 task019 restores the training rifle; back cylinders and flamethrower remain hidden");
                }
                else
                {
                    Assert.That(skins.Any(s => s.name == "D4_Helmet"), Is.True);
                    Assert.That(skins.Any(s => s.name == "D4_Vest_V2"), Is.True);
                    Assert.That(skins.First(s => s.name == "D4_Suit").sharedMaterial.GetTexture("_BaseMap").name, Does.Contain("blue"));
                }
                var animator = model.GetComponent<Animator>();
                Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
                Assert.That(animator.applyRootMotion, Is.False);
                foreach (var name in new[] { "Idle", "Walk", "Run", "Shot", "Hit", "Death" })
                    Assert.That(animator.runtimeAnimatorController.animationClips.Any(c => c.name == name && c.length > .1f), Is.True, name);
                Assert.That(view.Muzzle.IsChildOf(view.VisualRoot), Is.True);
                Assert.That(view.HitCollider, Is.Not.Null);
                Assert.That(model.GetComponentsInChildren<Transform>().Any(t => t.name == "FactionArmband"), Is.True);
            }
        }
    }
}
