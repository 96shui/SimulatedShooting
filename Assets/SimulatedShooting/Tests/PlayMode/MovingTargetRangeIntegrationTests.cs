using System.Collections;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRShooting.Application.Weapons;
using VRShooting.Common;
using VRShooting.Contracts;
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
