using System.Collections;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.TestTools;

namespace SimulatedShooting.Tests.PlayMode
{
    public sealed class GrenadePlayModeTests
    {
        // BDD27: the imported M67 asset must be available through the production Resources path.
        [UnityTest]
        public IEnumerator ModeThreeGrenadeResourceContainsRenderableModel()
        {
            yield return null;
            var model=Resources.Load<GameObject>("Combat/m67_low");
            Assert.That(model,Is.Not.Null,"M67 model was not imported into the runtime Resources path");
            Assert.That(model.GetComponentsInChildren<Renderer>(true).Length,Is.GreaterThan(0));
            var instance=Object.Instantiate(model);
            var projectile=instance.AddComponent<CombatGrenadeProjectile>();
            Assert.That(projectile,Is.Not.Null);
            Object.Destroy(instance);
        }
    }
}
