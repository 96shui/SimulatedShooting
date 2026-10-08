using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SimulatedShooting.Scene;

namespace SimulatedShooting.Tests.PlayMode
{
    public sealed class RecordedBloodMistTests
    {
        // BDD23: death disables physics before feedback, but blood remains on the enemy.
        [Test]
        public void DisabledEnemyColliderKeepsBloodOnEnemyAndNotAtMuzzle()
        {
            var root = new GameObject("DeadEnemy");
            try
            {
                root.transform.position = new Vector3(3, 0, 20);
                var actor = root.AddComponent<CombatActorView>();
                var capsule = root.AddComponent<CapsuleCollider>();
                capsule.center = new Vector3(0, .9f, 0);
                capsule.height = 1.8f;
                capsule.radius = .3f;
                actor.HitCollider = capsule;
                capsule.enabled = false;
                var origin = new Vector3(0, 1.2f, 0);
                var direction = (root.transform.position + Vector3.up * 1.2f - origin).normalized;
                var point = CombatBloodImpactLocator.Resolve(actor, new Ray(origin, direction), out _);
                Assert.That(Vector3.Distance(point, root.transform.position + Vector3.up * 1.2f), Is.LessThan(.5f));
                Assert.That(Vector3.Distance(point, origin), Is.GreaterThan(19));
                var miss = CombatBloodImpactLocator.Resolve(actor, new Ray(origin, Vector3.left), out _);
                Assert.That(Vector3.Distance(miss, root.transform.position + Vector3.up * 1.2f), Is.LessThan(.5f));
            }
            finally { Object.DestroyImmediate(root); }
        }
        // BDD23 2026-10-07: recorded hit VFX must not obstruct shots and must expire.
        [UnityTest]
        public IEnumerator RecordedHitBindsAtlasWithoutCollisionAndExpires()
        {
            var root = new GameObject("RecordedHit");
            try
            {
                Assert.IsTrue(root.AddComponent<RecordedBloodMistVfx>().Initialize(1));
                var renderer = root.GetComponentInChildren<MeshRenderer>();
                Assert.IsNotNull(renderer.sharedMaterial.mainTexture);
                var collider = root.GetComponentInChildren<Collider>();
                Assert.IsTrue(collider == null || !collider.enabled);
                yield return new WaitForSeconds(1f);
                Assert.IsTrue(root == null, "Recorded blood mist must not accumulate in the scene.");
            }
            finally { if (root != null) Object.Destroy(root); }
        }
    }
}
