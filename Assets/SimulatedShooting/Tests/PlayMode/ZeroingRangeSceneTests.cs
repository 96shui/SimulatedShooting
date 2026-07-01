using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SimulatedShooting.Tests.PlayMode
{
    public sealed class ZeroingRangeSceneTests
    {
        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("ZeroingRangeScene", LoadSceneMode.Single);
            Physics.SyncTransforms();
        }

        [Test]
        public void Task004_SceneContainsStableTrainingAnchorsForVrAndNoVr()
        {
            Assert.That(Find("ZeroingRange.ShootingPosition"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Target.Primary"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Origin.VR"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Camera.NoVR").GetComponent<Camera>(), Is.Not.Null);
        }

        [Test]
        public void Task004_TargetHasSpecifiedDimensionsAtOneHundredMetres()
        {
            var shootingPosition = Find("ZeroingRange.ShootingPosition").transform;
            var target = Find("ZeroingRange.Target.Primary").transform;
            var face = Find("ZeroingRange.Target.Face").GetComponent<Renderer>();
            var tenRing = Find("ZeroingRange.Target.TenRing").GetComponent<Renderer>();

            Assert.That(Vector3.Distance(shootingPosition.position, target.position), Is.EqualTo(100f).Within(0.001f));
            Assert.That(face.bounds.size.x, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(face.bounds.size.y, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(tenRing.bounds.size.x, Is.EqualTo(0.1f).Within(0.001f));
            Assert.That(tenRing.bounds.size.y, Is.EqualTo(0.1f).Within(0.001f));
        }

        [Test]
        public void Task004_TargetCanBeHitFromShootingPosition()
        {
            var origin = Find("ZeroingRange.ShootingPosition").transform.position;
            var target = Find("ZeroingRange.Target.Primary").transform;
            var direction = (target.position - origin).normalized;

            Assert.That(Physics.Raycast(origin, direction, out var hit, 101f), Is.True);
            Assert.That(hit.transform == target || hit.transform.IsChildOf(target), Is.True);
        }

        [Test]
        public void Task004_FirstPersonCompositionFramesTargetThroughRangeLane()
        {
            var camera = Find("ZeroingRange.Camera.NoVR").GetComponent<Camera>();
            var target = Find("ZeroingRange.Target.Primary").transform;
            var viewportPoint = camera.WorldToViewportPoint(target.position);

            Assert.That(viewportPoint.z, Is.GreaterThan(0f));
            Assert.That(viewportPoint.x, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(viewportPoint.y, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(Find("ZeroingRange.Environment.Lane"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Environment.Berm.Left"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Environment.Berm.Right"), Is.Not.Null);
            Assert.That(Find("ZeroingRange.Weapon.Reference"), Is.Not.Null);
        }

        private static GameObject Find(string id)
        {
            return SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<SceneTestId>(true))
                .FirstOrDefault(testId => testId.Id == id)?.gameObject;
        }
    }
}
