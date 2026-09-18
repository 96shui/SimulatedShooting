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
        public void Bdd23_Task014_BothFactionsHaveSkinnedModelsAndDistinctUniforms()
        {
            Material enemyUniform = null;
            foreach (var role in new[] { "Enemy", "Teammate" })
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimulatedShooting/Prefabs/Combat/Actor_" + role + ".prefab");
                var view = root.GetComponent<CombatActorView>();
                var model = view.VisualRoot.Find("AmericanSoldier");
                Assert.That(model, Is.Not.Null);
                var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
                Assert.That(skin.bones.Length, Is.GreaterThan(40));
                Assert.That(skin.sharedMaterials.All(m => m != null && m.shader.name == "Universal Render Pipeline/Lit"), Is.True);
                var uniform = skin.sharedMaterials[2];
                Assert.That(uniform.GetTexture("_BaseMap"), Is.Not.Null);
                if (role == "Enemy") enemyUniform = uniform;
                else Assert.That(uniform.GetColor("_BaseColor"), Is.Not.EqualTo(enemyUniform.GetColor("_BaseColor")));
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
