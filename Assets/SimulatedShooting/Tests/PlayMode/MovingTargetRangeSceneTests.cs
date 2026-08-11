using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimulatedShooting.Scene;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SimulatedShooting.Tests.PlayMode
{
    public sealed class MovingTargetRangeSceneTests
    {
        [UnitySetUp]
        public IEnumerator LoadMovingTargetRangeScene()
        {
            yield return SceneManager.LoadSceneAsync("MovingTargetRangeScene", LoadSceneMode.Single);
            Physics.SyncTransforms();
        }

        [Test]
        public void Bdd08_StartTraining_SceneContainsAllTask005StableIds()
        {
            var requiredIds = new[]
            {
                "MovingTargetRange.Root",
                "MovingTargetRange.ShootingPosition",
                "MovingTargetRange.PlayerSpawn",
                "MovingTargetRange.Origin.VR",
                "MovingTargetRange.Camera.NoVR",
                "MovingTargetRange.Hud.Anchor",
                "MovingTargetRange.Route.Root",
                "MovingTargetRange.Route.RightEndpoint",
                "MovingTargetRange.Route.LeftEndpoint",
                "MovingTargetRange.Target",
                "MovingTargetRange.Target.HitSurface",
                "MovingTargetRange.Target.Binding"
            };

            foreach (var id in requiredIds)
                Assert.That(Find(id), Is.Not.Null, $"Missing task005 test ID: {id}");
        }

        [Test]
        public void Bdd09_DayHud_UsesOneHundredMetreRangeAndFortyMetreRoute()
        {
            var shooting = Find("MovingTargetRange.ShootingPosition").transform.position;
            var route = Find("MovingTargetRange.Route.Root").transform.position;
            var right = Find("MovingTargetRange.Route.RightEndpoint").transform.position;
            var left = Find("MovingTargetRange.Route.LeftEndpoint").transform.position;

            var horizontalRange = Vector2.Distance(new Vector2(shooting.x, shooting.z), new Vector2(route.x, route.z));
            Assert.That(horizontalRange, Is.EqualTo(100f).Within(0.10f));
            Assert.That(Vector3.Distance(right, left), Is.EqualTo(40f).Within(0.05f));
            Assert.That(Vector3.Distance((right + left) * 0.5f, route), Is.LessThan(0.001f));
        }

        [Test]
        public void Bdd09_WaitingCountdown_TargetStartsAndRemainsAtRightEndpoint()
        {
            var binding = Find("MovingTargetRange.Target.Binding").GetComponent<MovingTargetRouteBinding>();

            Assert.That(binding.NormalizedProgress, Is.Zero.Within(0.001f));
            Assert.That(Vector3.Distance(binding.TargetRoot.position, binding.RightEndpoint.position),
                Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator Bdd09_WaitingCountdown_RealFrameTimeCannotMoveTarget()
        {
            var binding = Find("MovingTargetRange.Target.Binding").GetComponent<MovingTargetRouteBinding>();
            var initialPosition = binding.TargetRoot.position;

            yield return new WaitForSeconds(0.12f);

            Assert.That(Vector3.Distance(binding.TargetRoot.position, initialPosition), Is.LessThan(0.001f));
            Assert.That(binding.NormalizedProgress, Is.Zero.Within(0.001f));
        }

        [Test]
        public void Bdd09_RoutePhases_OnlyExplicitAuthoritativeProgressMovesTarget()
        {
            var binding = Find("MovingTargetRange.Target.Binding").GetComponent<MovingTargetRouteBinding>();

            binding.ApplyNormalizedProgress(0.25f);
            Assert.That(Vector3.Distance(binding.TargetRoot.position,
                Vector3.Lerp(binding.RightEndpoint.position, binding.LeftEndpoint.position, 0.25f)),
                Is.LessThan(0.001f));
            binding.ApplyNormalizedProgress(1f);
            Assert.That(Vector3.Distance(binding.TargetRoot.position, binding.LeftEndpoint.position),
                Is.LessThan(0.001f));
            binding.ApplyNormalizedProgress(0.5f);
            Assert.That(Vector3.Distance(binding.TargetRoot.position,
                (binding.RightEndpoint.position + binding.LeftEndpoint.position) * 0.5f),
                Is.LessThan(0.001f));
            binding.ApplyNormalizedProgress(0f);
            Assert.That(Vector3.Distance(binding.TargetRoot.position, binding.RightEndpoint.position),
                Is.LessThan(0.001f));
        }

        [Test]
        public void Bdd09_DayHud_TestRaysHitStableSurfaceAtCentreAndBothEndpoints()
        {
            var binding = Find("MovingTargetRange.Target.Binding").GetComponent<MovingTargetRouteBinding>();
            var shooting = Find("MovingTargetRange.ShootingPosition").transform.position;

            foreach (var progress in new[] { 0f, 0.5f, 1f })
            {
                binding.ApplyNormalizedProgress(progress);
                Physics.SyncTransforms();
                var direction = (binding.TargetCenter.position - shooting).normalized;
                Assert.That(Physics.Raycast(shooting, direction, out var hit, 110f), Is.True,
                    $"No hit at route progress {progress}");
                Assert.That(hit.collider, Is.EqualTo(binding.HitSurface));
                Assert.That(hit.collider.GetComponent<SceneTestId>().Id,
                    Is.EqualTo("MovingTargetRange.Target.HitSurface"));
            }
        }

        [Test]
        public void Bdd10_NightHud_ReusesRouteAndProvidesDayNightOpticHooks()
        {
            Assert.That(Find("MovingTargetRange.Lighting.Day"), Is.Not.Null);
            Assert.That(Find("MovingTargetRange.Lighting.Night"), Is.Not.Null);
            Assert.That(Find("MovingTargetRange.Optic.LowLight"), Is.Not.Null);
            Assert.That(Find("MovingTargetRange.Route.LeftEndpoint"), Is.Not.Null);
            Assert.That(Find("MovingTargetRange.Route.RightEndpoint"), Is.Not.Null);
        }

        [Test]
        public void Bdd09_SideProfileTarget_HasUnifiedHitSurfaceAndFeedbackHooks()
        {
            var target = Find("MovingTargetRange.Target");
            var binding = Find("MovingTargetRange.Target.Binding").GetComponent<MovingTargetRouteBinding>();

            Assert.That(target.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThanOrEqualTo(6));
            Assert.That(target.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(1));
            Assert.That(binding.HitSurface, Is.Not.Null);
            Assert.That(binding.TargetCenter, Is.Not.Null);
            Assert.That(binding.ImpactFeedbackRoot, Is.Not.Null);
            Assert.That(binding.TargetRoot.gameObject.isStatic, Is.False);
        }

        [Test]
        public void Bdd09_NoVrMode_UsesFloorOriginAndOnePlayerView()
        {
            var originObject = Find("MovingTargetRange.Origin.VR");
            var origin = originObject.GetComponent<XROrigin>();
            var noVrCamera = Find("MovingTargetRange.Camera.NoVR").GetComponent<Camera>();

            Assert.That(origin, Is.Not.Null);
            Assert.That(origin.RequestedTrackingOriginMode, Is.EqualTo(XROrigin.TrackingOriginMode.Floor));
            Assert.That(origin.CameraYOffset, Is.Zero.Within(0.001f));
            Assert.That(noVrCamera.isActiveAndEnabled, Is.True);
            Assert.That(Object.FindObjectsOfType<Camera>().Count(camera => camera.isActiveAndEnabled), Is.EqualTo(1));
            Assert.That(Object.FindObjectsOfType<AudioListener>().Count(listener => listener.isActiveAndEnabled),
                Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Bdd09_VrMode_KeepsExactlyOneCameraAndAudioListener()
        {
            var originObject = Find("MovingTargetRange.Origin.VR");
            var noVrCamera = Find("MovingTargetRange.Camera.NoVR").GetComponent<Camera>();
            var controller = Object.FindObjectOfType<ZeroingRangeXRModeController>(true);

            controller.SetVrModeForTests(true);
            yield return null;

            Assert.That(noVrCamera.isActiveAndEnabled, Is.False);
            Assert.That(originObject.GetComponentsInChildren<Camera>(true)
                .Count(camera => camera.isActiveAndEnabled), Is.EqualTo(1));
            Assert.That(Object.FindObjectsOfType<AudioListener>().Count(listener => listener.isActiveAndEnabled),
                Is.EqualTo(1));
        }

        static GameObject Find(string id)
        {
            return SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<SceneTestId>(true))
                .FirstOrDefault(testId => testId.Id == id)?.gameObject;
        }
    }
}
