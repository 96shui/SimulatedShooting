using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimulatedShooting.Scene;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRShooting.Unity.Weapons;

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
        public void Task003_006_SceneContainsStableProneRangeBindingsAndRouteIds()
        {
            var requiredIds = new[]
            {
                "MovingTargetRange.Root",
                "MovingTargetRange.FiringStation.Root",
                "MovingTargetRange.FiringStation.PlayerRoot",
                "MovingTargetRange.FiringStation.ProneHeadReference",
                "MovingTargetRange.FiringStation.AimForward",
                "MovingTargetRange.FiringStation.LargeUiAnchor",
                "MovingTargetRange.FiringStation.MinimalHudAnchor",
                "MovingTargetRange.FiringStation.WeaponRackAnchor",
                "MovingTargetRange.Origin.VR",
                "MovingTargetRange.Camera.NoVR",
                "MovingTargetRange.Route.Root",
                "MovingTargetRange.Route.RightEndpoint",
                "MovingTargetRange.Route.LeftEndpoint",
                "MovingTargetRange.Target",
                "MovingTargetRange.Target.HitSurface",
                "MovingTargetRange.Target.Binding"
            };

            foreach (var id in requiredIds)
                Assert.That(Find(id), Is.Not.Null, $"Missing task003/task006 test ID: {id}");
        }

        [Test]
        public void Task003_006_TrainingRifleCarriesTheCompleteP1InteractionAndFeedbackSetup()
        {
            var controller = Object.FindObjectOfType<FirstPersonTrainingWeaponController>(true);
            var binding = Object.FindObjectOfType<WeaponPrefabBinding>(true);

            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.name, Is.EqualTo("WeaponPlayerRoot"));
            Assert.That(binding, Is.Not.Null);
            Assert.That(binding.name, Is.EqualTo("Weapon_training-rifle_Blockout"));
            Assert.That(binding.HasRequiredBinding, Is.True);
            Assert.That(binding.GetComponent<TrainingRifleGrabInteractable>(), Is.Not.Null);
            Assert.That(controller.HasRequiredWeaponBinding, Is.True);
            Assert.That(controller.HasVrPoseSources, Is.True);
            Assert.That(controller.FeedbackController, Is.Not.Null);
            Assert.That(controller.FeedbackController.HasRequiredAudio, Is.True);
            Assert.That(controller.FeedbackController.HasProjectileVisualPrefab, Is.True);
            Assert.That(controller.transform.Find("TracerRoot_training-rifle"), Is.Not.Null);
            Assert.That(Object.FindObjectsOfType<Transform>(true)
                .Any(item => item.name == "DirectInteractor_Right"), Is.True);
            Assert.That(Object.FindObjectsOfType<Transform>(true)
                .Any(item => item.name == "DirectInteractor_Left"), Is.True);
        }

        [Test]
        public void Task003_ProneBindingsAreCompleteUniqueAndIndependent()
        {
            var bindings = Find("MovingTargetRange.FiringStation.Root")
                .GetComponent<TrainingRangeSceneBindings>();

            Assert.That(bindings, Is.Not.Null);
            Assert.That(bindings.ValidateBindings(out var error), Is.True, error);
            Assert.That(bindings.AllAnchors.Distinct().Count(), Is.EqualTo(bindings.AllAnchors.Count));
            Assert.That(bindings.LargeUiAnchor.parent, Is.Not.EqualTo(bindings.MinimalHudAnchor));
            Assert.That(bindings.MinimalHudAnchor.parent, Is.Not.EqualTo(bindings.WeaponRackAnchor));
            Assert.That(bindings.WeaponRackAnchor.parent, Is.Not.EqualTo(bindings.LargeUiAnchor));
        }

        [Test]
        public void Task003_StartAreaUsesTheExactZeroingRangeForegroundCopy()
        {
            var copy = Find("MovingTargetRange.Visual.ZeroingStartAreaCopy");
            Assert.That(copy, Is.Not.Null);
            Assert.That(Object.FindObjectsOfType<Transform>(true)
                .Any(item => item.name == "FiringStationVisual"), Is.False,
                "The approximate P2-only foreground must be removed.");

            foreach (var name in new[]
                     {
                         "FiringPad", "FiringLine", "ShootingBench", "RangeGate", "SafetyBoundary",
                         "WeaponCrate_Left", "WeaponReference_Blockout", "DistancePost_0m_-6.2",
                         "DistancePost_0m_6.2", "DistanceBoard_0m_-6.2", "DistanceBoard_0m_6.2",
                         "DistanceLabel_0m_Left", "DistanceLabel_0m_Right"
                     })
            {
                Assert.That(copy.GetComponentsInChildren<Transform>(true).Any(item => item.name == name), Is.True,
                    $"Missing ZeroingRange start-area copy: {name}");
            }

            Assert.That(copy.GetComponentsInChildren<Transform>(true)
                .Count(item => item.name.StartsWith("Sandbag_")), Is.EqualTo(18));
            var gate = copy.GetComponentsInChildren<Transform>(true).Single(item => item.name == "RangeGate");
            Assert.That(gate.position, Is.EqualTo(new Vector3(0f, 0f, 4.5f)));
            var bench = copy.GetComponentsInChildren<Transform>(true).Single(item => item.name == "ShootingBench");
            Assert.That(bench.position, Is.EqualTo(new Vector3(0f, 0.72f, 2.8f)));
        }

        [Test]
        public void Task003_NoVrPreviewCameraRetainsTheTunedRangeComposition()
        {
            var camera = Find("MovingTargetRange.Camera.NoVR").transform;
            Assert.That(camera.localPosition, Is.EqualTo(new Vector3(0f, 1.5f, 0f)));
            Assert.That(Quaternion.Angle(camera.localRotation, Quaternion.identity), Is.LessThan(0.01f));
        }

        [Test]
        public void Task003_ArtificialMoveTeleportAndTurnDoNotMovePlayerRoot()
        {
            var guard = Find("MovingTargetRange.FiringStation.Root")
                .GetComponent<FixedProneLocomotionGuard>();
            var root = guard.PlayerRootAnchor;
            var position = root.position;
            var rotation = root.rotation;

            Assert.That(guard.ArtificialLocomotionDisabled, Is.True);
            Assert.That(guard.TryApplyArtificialMotionForTests(new Vector3(2f, 0f, 3f), 45f), Is.False);
            Assert.That(root.position, Is.EqualTo(position));
            Assert.That(Quaternion.Angle(root.rotation, rotation), Is.LessThan(0.001f));
            Assert.That(Object.FindObjectsOfType<PlayerFootstepAudio>(true), Is.Empty);
        }

        [Test]
        public void Task003_TrackedHeadReferenceCanChangeWithoutMovingPlayerRoot()
        {
            var bindings = Find("MovingTargetRange.FiringStation.Root")
                .GetComponent<TrainingRangeSceneBindings>();
            var playerPosition = bindings.PlayerRootAnchor.position;
            var initialHeadPosition = bindings.ProneHeadReference.localPosition;

            bindings.ProneHeadReference.localPosition += new Vector3(0.05f, 0.02f, 0.03f);

            Assert.That(bindings.ProneHeadReference.localPosition, Is.Not.EqualTo(initialHeadPosition));
            Assert.That(bindings.PlayerRootAnchor.position, Is.EqualTo(playerPosition));
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
        public void Task018_ReferenceRange_HasThreeDimensionalRangeAndWallSlogans()
        {
            Assert.That(Find("MovingTargetRange.Visual.ReferenceBackdrop"), Is.Null,
                "The reference photograph must not be used as a panoramic scene plane.");
            Assert.That(RenderSettings.skybox, Is.Not.Null);
            var camera = Find("MovingTargetRange.Camera.NoVR").GetComponent<Camera>();
            var expected = new[]
            {
                ("MovingTargetRange.Visual.Slogan.ListenToParty", "Slogan_听党指挥"),
                ("MovingTargetRange.Visual.Slogan.WinBattles", "Slogan_能打胜仗"),
                ("MovingTargetRange.Visual.Slogan.ExcellentConduct", "Slogan_作风优良")
            };
            foreach (var definition in expected)
            {
                var slogan = Find(definition.Item1);
                Assert.That(slogan, Is.Not.Null);
                Assert.That(slogan.name, Is.EqualTo(definition.Item2));
                var characterRenderers = slogan.GetComponentsInChildren<Renderer>(true);
                Assert.That(characterRenderers.Length, Is.EqualTo(4));
                Assert.That(characterRenderers.All(renderer => renderer.sharedMaterial.mainTexture != null), Is.True);
                Assert.That(characterRenderers.All(renderer => renderer.sharedMaterial.color.r >
                    renderer.sharedMaterial.color.g * 4f), Is.True);
                Assert.That(characterRenderers.All(renderer => FacesCamera(renderer, camera)), Is.True,
                    "Wall slogan mesh faces must point toward the player without mirroring the glyphs.");
                Assert.That(characterRenderers.All(renderer => renderer.bounds.size.y >= 5.3f), Is.True);
                Assert.That(characterRenderers.All(renderer => renderer.bounds.min.y >= 22.7f), Is.True,
                    "Every wall slogan character must sit fully above the mountain ridge.");
                var viewport = camera.WorldToViewportPoint(slogan.transform.position);
                Assert.That(viewport.z, Is.GreaterThan(0f));
                Assert.That(viewport.y, Is.InRange(0.52f, 0.75f));
            }
        }

        [Test]
        public void Task018_ReferenceRange_HasCentredDataWinsSloganBelowWinBattles()
        {
            var dataWins = Find("MovingTargetRange.Visual.Slogan.DataWins");
            var winBattles = Find("MovingTargetRange.Visual.Slogan.WinBattles");
            var camera = Find("MovingTargetRange.Camera.NoVR").GetComponent<Camera>();

            Assert.That(dataWins, Is.Not.Null);
            Assert.That(dataWins.name, Is.EqualTo("Slogan_数据致胜"));
            Assert.That(dataWins.transform.position.x, Is.Zero.Within(0.001f));
            Assert.That(dataWins.transform.position.y, Is.LessThan(winBattles.transform.position.y - 6f));
            Assert.That(dataWins.GetComponentsInChildren<Renderer>(true).Length, Is.EqualTo(4));
            Assert.That(dataWins.GetComponentsInChildren<Renderer>(true).All(renderer =>
                FacesCamera(renderer, camera)), Is.True);
        }

        [Test]
        public void Task018_ReferenceRange_DataWinsCharactersAlignWithNumberCharacter()
        {
            var dataWins = Find("MovingTargetRange.Visual.Slogan.DataWins");
            var characterRenderers = dataWins.GetComponentsInChildren<Renderer>(true);
            var numberCharacter = characterRenderers.Single(renderer => renderer.name == "Character_数");

            Assert.That(characterRenderers.Length, Is.EqualTo(4));
            Assert.That(characterRenderers.All(renderer =>
                Mathf.Approximately(renderer.transform.localPosition.y, numberCharacter.transform.localPosition.y) &&
                Mathf.Approximately(renderer.transform.localPosition.z, numberCharacter.transform.localPosition.z)),
                Is.True, "数据致胜四个字必须与“数”字保持同一水平面和高度。");
            Assert.That(characterRenderers.All(renderer =>
                Mathf.Approximately(renderer.bounds.size.y, numberCharacter.bounds.size.y)),
                Is.True, "数据致胜四个字必须与“数”字保持相同字高。");
        }

        [Test]
        public void Task018_ReferenceRange_UsesThreeDimensionalRangeAndSparseGroundDetail()
        {
            var berm = Find("MovingTargetRange.Visual.Berm3D");
            var sandyGround = Find("MovingTargetRange.Visual.SandyGround");
            var ridgeWall = Find("MovingTargetRange.Visual.RidgeWall");
            var targetBays = Find("MovingTargetRange.Visual.TargetBayPanels");
            var sparseGrass = Find("MovingTargetRange.Visual.SparseGrass");

            Assert.That(sandyGround, Is.Not.Null);
            Assert.That(sandyGround.GetComponent<MeshCollider>(), Is.Not.Null);
            Assert.That(berm, Is.Not.Null);
            Assert.That(berm.GetComponentInChildren<MeshCollider>(), Is.Not.Null);
            Assert.That(berm.GetComponentInChildren<Renderer>().bounds.size.x, Is.GreaterThanOrEqualTo(300f));
            Assert.That(ridgeWall, Is.Not.Null);
            Assert.That(ridgeWall.GetComponentsInChildren<Renderer>(true)
                .Max(renderer => renderer.bounds.max.x) - ridgeWall.GetComponentsInChildren<Renderer>(true)
                .Min(renderer => renderer.bounds.min.x), Is.GreaterThanOrEqualTo(260f));
            Assert.That(targetBays.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThanOrEqualTo(24));
            Assert.That(targetBays.GetComponentsInChildren<Renderer>(true)
                .Max(renderer => renderer.bounds.max.x) - targetBays.GetComponentsInChildren<Renderer>(true)
                .Min(renderer => renderer.bounds.min.x), Is.GreaterThanOrEqualTo(175f));
            Assert.That(sparseGrass.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThanOrEqualTo(96));
        }

        [Test]
        public void Task018_ReferenceRange_CopiesP1DistanceMarkersFromTwentyFiveToOneHundredMetres()
        {
            foreach (var distance in new[] { 25, 50, 75, 100 })
            {
                foreach (var side in new[] { "Left", "Right" })
                {
                    var label = Find($"MovingTargetRange.Environment.DistanceLabel.{distance}m.{side}");
                    Assert.That(label, Is.Not.Null, $"Missing {distance}m {side} distance label");
                    var text = label.GetComponent<TextMesh>();
                    Assert.That(text.text, Is.EqualTo($"{distance} m"));
                    Assert.That(text.anchor, Is.EqualTo(TextAnchor.MiddleCenter));
                    Assert.That(text.fontStyle, Is.EqualTo(FontStyle.Bold));
                    Assert.That(text.color, Is.EqualTo(Color.white));
                }
            }
        }

        [Test]
        public void Task018_ReferenceRange_HasFiveSpacedP1FixedTargetsAroundCentre()
        {
            var targetIds = new[]
            {
                "MovingTargetRange.FixedTarget.Left.Far",
                "MovingTargetRange.FixedTarget.Left.Near",
                "MovingTargetRange.FixedTarget.Center",
                "MovingTargetRange.FixedTarget.Right.Near",
                "MovingTargetRange.FixedTarget.Right.Far"
            };
            var targets = targetIds.Select(Find).ToArray();

            Assert.That(targets, Has.All.Not.Null);
            Assert.That(targets[2].transform.position.x, Is.Zero.Within(0.001f));
            for (var index = 1; index < targets.Length; index++)
                Assert.That(targets[index].transform.position.x - targets[index - 1].transform.position.x,
                    Is.EqualTo(3.5f).Within(0.001f));

            foreach (var target in targets)
            {
                var surface = target.GetComponentInChildren<TargetImpactSurface>(true);
                var face = surface.GetComponent<Renderer>();
                var tenRing = target.transform.Find("TenRing_10cm").GetComponent<Renderer>();
                Assert.That(face.bounds.size.x, Is.EqualTo(0.5f).Within(0.001f));
                Assert.That(face.bounds.size.y, Is.EqualTo(0.5f).Within(0.001f));
                Assert.That(tenRing.bounds.size.x, Is.EqualTo(0.1f).Within(0.001f));
                Assert.That(tenRing.bounds.size.y, Is.EqualTo(0.1f).Within(0.001f));
            }
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
        public void Task006_SceneContainsOnlyFixedDayLighting()
        {
            Assert.That(Find("MovingTargetRange.Lighting.Day"), Is.Not.Null);
            Assert.That(Find("MovingTargetRange.Lighting.Night"), Is.Null);
            Assert.That(Find("MovingTargetRange.Optic.LowLight"), Is.Null);
            Assert.That(Find("MovingTargetRange.Route.LeftEndpoint"), Is.Not.Null);
            Assert.That(Find("MovingTargetRange.Route.RightEndpoint"), Is.Not.Null);
        }

        [Test]
        public void Task006_VisualDriverAcceptsAuthoritativeStateWithoutInferringRules()
        {
            var routeObject = Find("MovingTargetRange.Target.Binding");
            var binding = routeObject.GetComponent<MovingTargetRouteBinding>();
            var driver = routeObject.GetComponent<MovingTargetVisualDriver>();
            var state = new MovingTargetVisualState(
                0.5f,
                MovingTargetTravelDirection.RightToLeft,
                endpointHold: true,
                canShoot: false,
                speedMetresPerSecond: 4f);

            driver.Apply(state);

            Assert.That(binding.NormalizedProgress, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(driver.CurrentState.Direction, Is.EqualTo(MovingTargetTravelDirection.RightToLeft));
            Assert.That(driver.CurrentState.EndpointHold, Is.True);
            Assert.That(driver.CurrentState.CanShoot, Is.False);
            Assert.That(Vector3.Distance(binding.TargetRoot.position,
                (binding.RightEndpoint.position + binding.LeftEndpoint.position) * 0.5f), Is.LessThan(0.001f));
        }

        [Test]
        public void Task006_FakeTimelineCoversSpeedsHoldsReverseFinishAndRetry()
        {
            var timeline = Find("MovingTargetRange.Target.Binding").GetComponent<MovingTargetFakeTimeline>();

            Assert.That(timeline.FrameCount, Is.EqualTo(8));
            CollectionAssert.AreEquivalent(new[] { 3f, 4f, 5f },
                Enumerable.Range(0, timeline.FrameCount)
                    .Select(index => timeline.GetFrame(index).SpeedMetresPerSecond)
                    .Where(speed => speed > 0f)
                    .Distinct()
                    .ToArray());
            Assert.That(timeline.GetFrame(4).EndpointHold, Is.True);
            Assert.That(timeline.GetFrame(5).Direction, Is.EqualTo(MovingTargetTravelDirection.LeftToRight));
            Assert.That(timeline.GetFrame(6).RouteProgress01, Is.Zero);
            Assert.That(timeline.GetFrame(7).RouteProgress01, Is.Zero);
        }

        [Test]
        public void Task006_TargetAndEnvironmentLayersAndMasksAreSeparated()
        {
            var binding = Find("MovingTargetRange.Target.Binding").GetComponent<MovingTargetRouteBinding>();
            var adapter = binding.HitSurface.GetComponent<MovingTargetHitAdapter>();
            var environment = Find("MovingTargetRange.Environment.Root");
            var groundCollider = Find("MovingTargetRange.Visual.SandyGround").GetComponent<Collider>();

            Assert.That(binding.HitSurface.gameObject.layer, Is.Not.EqualTo(environment.layer));
            Assert.That(ContainsLayer(adapter.TargetLayerMask, binding.HitSurface.gameObject.layer), Is.True);
            Assert.That(ContainsLayer(adapter.EnvironmentLayerMask, environment.layer), Is.True);
            Assert.That(ContainsLayer(adapter.TargetLayerMask, environment.layer), Is.False);
            Assert.That(adapter.IsTargetCollider(binding.HitSurface), Is.True);
            Assert.That(adapter.IsTargetCollider(groundCollider), Is.False,
                "Environment ray hits must be recorded as misses instead of target hits.");
        }

        [Test]
        public void Task006_ImpactFeedbackConsumesEachConfirmedShotIdOnlyOnce()
        {
            var binding = Find("MovingTargetRange.Target.Binding").GetComponent<MovingTargetRouteBinding>();
            var adapter = binding.HitSurface.GetComponent<MovingTargetHitAdapter>();
            var feedback = binding.ImpactFeedbackRoot.GetComponent<MovingTargetImpactFeedback>();
            var point = binding.TargetCenter.position;

            Assert.That(adapter.TryReportConfirmedHit(
                "shot-001", binding.HitSurface, point, -binding.TargetCenter.forward, out _), Is.True);
            Assert.That(adapter.TryReportConfirmedHit(
                "shot-001", binding.HitSurface, point, -binding.TargetCenter.forward, out _), Is.True);
            Assert.That(feedback.ConsumedShotCount, Is.EqualTo(1));
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

        static bool ContainsLayer(LayerMask mask, int layer)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        static bool FacesCamera(Renderer renderer, Camera camera)
        {
            var worldFaceNormal = renderer.transform.TransformDirection(Vector3.back).normalized;
            return Vector3.Dot(worldFaceNormal, camera.transform.forward.normalized) < -0.99f;
        }
    }
}
