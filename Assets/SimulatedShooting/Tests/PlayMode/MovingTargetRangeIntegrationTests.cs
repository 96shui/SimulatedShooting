using System.Collections;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRShooting.Application.Weapons;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.Input;
using VRShooting.Unity.Bootstrap;
using VRShooting.Unity.UI;

namespace SimulatedShooting.Tests.PlayMode
{
    /// <summary>
    /// P2 non-VR integration gate: real composition, pickup-gated countdown, visual route and result UI.
    /// </summary>
    public sealed class MovingTargetRangeIntegrationTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (GameMain.Instance != null)
            {
                Object.Destroy(GameMain.Instance.gameObject);
                yield return null;
            }

            yield return SceneManager.LoadSceneAsync("MovingTargetRangeScene", LoadSceneMode.Single);
            new GameObject("GameMain_P2Integration").AddComponent<GameMain>();
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (GameMain.Instance != null)
            {
                Object.Destroy(GameMain.Instance.gameObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator P2_RealTrigger_QuickTapFiresTwo_ForbiddenHoldDoesNotResume()
        {
            // BDD 09: quick two-shot burst, held fire, endpoint stop and release/repress gate.
            var services = GameMain.Instance.Services;
            var controller = Object.FindObjectOfType<FirstPersonTrainingWeaponController>(true);
            var input = new ManualXRTrainingInput();
            controller.ConfigureServices(services, input);
            var started = services.MovingTarget.StartSession(
                new MovingTargetSettingsDto { SpeedMetersPerSecond = 4f }, RandomSeed.Fixed(209));
            var sessionId = started.Data.SessionId;
            services.Presentation.ConfirmStart(sessionId);
            controller.InitializeForTests();
            input.Press(XRTrainingInputButton.RightGrip);
            input.Press(XRTrainingInputButton.LeftGrip);
            controller.SendMessage("Update");
            input.AdvanceFrame();
            services.MovingTargetProgress.Tick(sessionId, 3f);

            input.SetRightTriggerValue(1f);
            input.SetRightTriggerValue(0f);
            controller.SendMessage("Update");
            input.AdvanceFrame();
            yield return new WaitForSeconds(0.25f);
            Assert.That(services.MovingTarget.GetSession(sessionId).Data.ShotsFired, Is.EqualTo(2));
            Assert.That(controller.FeedbackController.ValidShotFeedbackCount, Is.EqualTo(2));

            input.Press(XRTrainingInputButton.Trigger);
            controller.SendMessage("Update");
            input.AdvanceFrame();
            yield return new WaitForSeconds(0.22f);
            var moving = services.MovingTarget.GetSession(sessionId).Data;
            Assert.That(moving.ShotsFired, Is.GreaterThanOrEqualTo(4));
            services.MovingTargetProgress.Tick(sessionId, (1f - moving.LegProgress01) * 10f + 0.001f);
            controller.SendMessage("Update");
            var countAtHold = services.MovingTarget.GetSession(sessionId).Data.ShotsFired;
            Assert.That(services.AutomaticFire.GetState(sessionId).Data.StopReason,
                Is.EqualTo(WeaponFireStopReason.ShootingBecameForbidden));
            services.MovingTargetProgress.Tick(sessionId, 2f);
            yield return new WaitForSeconds(0.2f);
            Assert.That(services.MovingTarget.GetSession(sessionId).Data.ShotsFired, Is.EqualTo(countAtHold));
        }

        [UnityTest]
        public IEnumerator P2_RetryRequiresFreshPickup_AndReenabledControllerReceivesInput()
        {
            // BDD 00: pickup order; BDD 11: retry clears the previous session.
            var services = GameMain.Instance.Services;
            var controller = Object.FindObjectOfType<FirstPersonTrainingWeaponController>(true);
            var input = new ManualXRTrainingInput();
            controller.ConfigureServices(services, input);
            var first = services.MovingTarget.StartSession(
                new MovingTargetSettingsDto { SpeedMetersPerSecond = 4f }, RandomSeed.Fixed(206));
            Assert.That(services.Presentation.ConfirmStart(first.Data.SessionId).Success, Is.True);
            Assert.That(controller.InitializeForTests(), Is.True);
            controller.enabled = false;
            controller.enabled = true;
            input.Press(XRTrainingInputButton.RightGrip);
            input.Press(XRTrainingInputButton.LeftGrip);
            controller.SendMessage("Update");
            input.AdvanceFrame();
            Assert.That(controller.CurrentHoldState, Is.EqualTo(WeaponHoldState.TwoHandHeld));
            services.MovingTargetProgress.Tick(first.Data.SessionId, 3f);
            Assert.That(controller.FireCurrentStateForTests(), Is.True);
            var feedbackCount = controller.FeedbackController.ValidShotFeedbackCount;
            var targetFeedback = Object.FindObjectOfType<MovingTargetImpactFeedback>(true);
            Assert.That(targetFeedback.TryConsume(new MovingTargetHitInput(
                "previous-session-shot", null, Vector3.zero, Vector3.forward, true)), Is.True);

            services.MovingTargetProgress.Tick(first.Data.SessionId, 100f);
            Assert.That(services.Presentation.Retry(first.Data.SessionId).Success, Is.True);
            var second = services.MovingTarget.StartSession(
                new MovingTargetSettingsDto { SpeedMetersPerSecond = 4f }, RandomSeed.Fixed(207));
            Assert.That(services.Presentation.ConfirmStart(second.Data.SessionId).Success, Is.True);
            Assert.That(controller.InitializeForTests(), Is.True);
            Assert.That(controller.CurrentHoldState, Is.EqualTo(WeaponHoldState.OnRack));
            Assert.That(controller.CurrentMagazine, Is.EqualTo(10));
            Assert.That(controller.LastShotWasValid, Is.False);
            Assert.That(targetFeedback.ConsumedShotCount, Is.Zero);
            Assert.That(services.Presentation.Get(second.Data.SessionId).Data.Phase,
                Is.EqualTo(TrainingPresentationPhase.AwaitingWeaponPickup));

            input.Release(XRTrainingInputButton.RightGrip);
            input.Release(XRTrainingInputButton.LeftGrip);
            controller.SendMessage("Update");
            input.AdvanceFrame();
            input.Press(XRTrainingInputButton.RightGrip);
            input.Press(XRTrainingInputButton.LeftGrip);
            controller.SendMessage("Update");
            input.AdvanceFrame();
            services.MovingTargetProgress.Tick(second.Data.SessionId, 3f);
            Assert.That(controller.FireCurrentStateForTests(), Is.True);
            Assert.That(controller.FeedbackController.ValidShotFeedbackCount, Is.EqualTo(feedbackCount + 1),
                "Shot sequence 1 in a new session must not be suppressed as a duplicate.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator P2_PhysicalImpactUsesCollisionPoint_EnvironmentDoesNotScore()
        {
            // BDD 09: hit feedback; task006: environment impacts must not score as targets.
            var services = GameMain.Instance.Services;
            var controller = Object.FindObjectOfType<FirstPersonTrainingWeaponController>(true);
            var input = new ManualXRTrainingInput();
            controller.ConfigureServices(services, input);
            var started = services.MovingTarget.StartSession(
                new MovingTargetSettingsDto { SpeedMetersPerSecond = 4f }, RandomSeed.Fixed(208));
            services.Presentation.ConfirmStart(started.Data.SessionId);
            controller.InitializeForTests();
            input.Press(XRTrainingInputButton.RightGrip);
            input.Press(XRTrainingInputButton.LeftGrip);
            controller.SendMessage("Update");
            input.AdvanceFrame();
            controller.SendMessage("Update");
            services.MovingTargetProgress.Tick(started.Data.SessionId, 3f);
            var binding = controller.GrabInteractable.GetComponent<VRShooting.Unity.Weapons.WeaponPrefabBinding>();
            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "P2_TestEnvironmentImpact";
            obstacle.transform.position = binding.MuzzlePoint.position + controller.CurrentAimDirection * 4f;
            Physics.SyncTransforms();
            Assert.That(Physics.Raycast(binding.MuzzlePoint.position, controller.CurrentAimDirection,
                out var collision, 5f), Is.True);
            Assert.That(collision.collider.gameObject, Is.EqualTo(obstacle));
            var before = controller.FeedbackController.ImpactFeedbackCount;
            Assert.That(controller.FireCurrentStateForTests(), Is.True);
            Assert.That(controller.LastShotHit, Is.False);
            yield return new WaitForSeconds(0.15f);
            Assert.That(controller.FeedbackController.ImpactFeedbackCount, Is.EqualTo(before + 1));
            var impact = GameObject.Find("ImpactFeedback_ZeroingTarget_001");
            Assert.That(impact, Is.Not.Null);
            Assert.That(Vector3.Distance(impact.transform.position, collision.point), Is.LessThan(0.02f));
            Assert.That(services.MovingTarget.GetSession(started.Data.SessionId).Data.HitCount, Is.Zero);
            Object.Destroy(obstacle);
        }

        [UnityTest]
        public IEnumerator P2_RealComposition_RunsPickupCountdownVisualResultAndRetry()
        {
            var runtime = Object.FindObjectOfType<MovingTargetRangeRuntimeAdapter>(true);
            var ui = Object.FindObjectOfType<MovingTargetRangeUI>(true);
            var services = GameMain.Instance.Services;

            Assert.That(runtime, Is.Not.Null);
            Assert.That(runtime.IsInitialized, Is.True);
            Assert.That(ui, Is.Not.Null);
            Assert.That(ui.LargePanelRoot.transform.parent, Is.EqualTo(runtime.RangeBindings.LargeUiAnchor));
            Assert.That(ui.MinimalHudRoot.transform.parent, Is.EqualTo(runtime.RangeBindings.MinimalHudAnchor));

            var started = services.MovingTarget.StartSession(
                new MovingTargetSettingsDto { SpeedMetersPerSecond = 4f },
                RandomSeed.Fixed(205));
            Assert.That(started.Success, Is.True, started.Message);
            var sessionId = started.Data.SessionId;
            Assert.That(services.Presentation.ConfirmStart(sessionId).Success, Is.True);

            var beforePickupPosition = runtime.VisualDriver.RouteBinding.TargetRoot.position;
            var delayed = services.MovingTargetProgress.Tick(sessionId, 10f);
            Assert.That(delayed.Data.Phase, Is.EqualTo(TargetMovePhase.WaitingCountdown));
            Assert.That(delayed.Data.CountdownSecondsRemaining, Is.EqualTo(3f).Within(0.001f));
            Assert.That(runtime.VisualDriver.RouteBinding.TargetRoot.position, Is.EqualTo(beforePickupPosition));
            Assert.That(ui.LargePanelRoot.activeSelf, Is.True);

            var pickup = services.WeaponControl.SetGripState(new WeaponGripStateInputDto
            {
                SessionId = sessionId,
                HoldState = WeaponHoldState.RearHandHeld,
                RearHandTracked = true,
                FrontHandTracked = false,
                Stability01 = 0.8f
            });
            Assert.That(pickup.Success, Is.True, pickup.Message);
            Assert.That(ui.LargePanelRoot.activeSelf, Is.False);
            Assert.That(ui.MinimalHudRoot.activeSelf, Is.True);

            var countdown = services.MovingTargetProgress.Tick(sessionId, 3f);
            Assert.That(countdown.Data.Phase, Is.EqualTo(TargetMovePhase.MovingRightToLeft));
            var moving = services.MovingTargetProgress.Tick(sessionId, 1f);
            Assert.That(moving.Data.RouteProgress01, Is.GreaterThan(0f));
            Assert.That(runtime.VisualDriver.CurrentState.Direction,
                Is.EqualTo(MovingTargetTravelDirection.RightToLeft));
            Assert.That(runtime.VisualDriver.RouteBinding.TargetRoot.position,
                Is.Not.EqualTo(beforePickupPosition));

            var completed = services.MovingTargetProgress.Tick(sessionId, 100f);
            Assert.That(completed.Data.Phase, Is.EqualTo(TargetMovePhase.Completed));
            Assert.That(runtime.VisualDriver.CurrentState.RouteProgress01, Is.EqualTo(0f).Within(0.001f));
            Assert.That(ui.LargePanelRoot.activeSelf, Is.True);
            Assert.That(ui.MinimalHudRoot.activeSelf, Is.False);

            var retry = services.Presentation.Retry(sessionId);
            Assert.That(retry.Success, Is.True, retry.Message);
            Assert.That(retry.Data.ActiveScreen, Is.EqualTo(ScreenId.MovingTargetSettings));
            Assert.That(ui.LargePanelRoot.activeSelf, Is.True);
            yield return null;
        }
    }
}
