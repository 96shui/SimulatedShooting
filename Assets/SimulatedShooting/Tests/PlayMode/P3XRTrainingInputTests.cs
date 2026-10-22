using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using VRShooting.Unity.Bootstrap;

using UnityEngine.InputSystem.XR;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.Input;

namespace SimulatedShooting.Tests.PlayMode
{
    // BDD23 posture/input; interface18 physical P3 controller bindings.
    public sealed class P3XRTrainingInputTests
    {
        XRController left,right;
        [SetUp] public void Setup()
        {
            InputSystem.RegisterLayout(@"{""name"":""P3BindingTestController"",""extend"":""XRController"",""controls"":[
                {""name"":""thumbstick"",""layout"":""Stick""},
                {""name"":""primaryButton"",""layout"":""Button"",""format"":""FLT"",""sizeInBits"":32},
                {""name"":""primary2DAxisClick"",""layout"":""Button"",""format"":""FLT"",""sizeInBits"":32},
                {""name"":""secondaryButton"",""layout"":""Button"",""format"":""FLT"",""sizeInBits"":32},
                {""name"":""gripPressed"",""layout"":""Button"",""format"":""FLT"",""sizeInBits"":32},
                {""name"":""trigger"",""layout"":""Axis""}]} ");
            left=(XRController)InputSystem.AddDevice("P3BindingTestController");InputSystem.SetDeviceUsage(left,UnityEngine.InputSystem.CommonUsages.LeftHand);
            right=(XRController)InputSystem.AddDevice("P3BindingTestController");InputSystem.SetDeviceUsage(right,UnityEngine.InputSystem.CommonUsages.RightHand);
        }
        [TearDown] public void Cleanup()
        {
            if(left!=null&&left.added)InputSystem.RemoveDevice(left);
            if(right!=null&&right.added)InputSystem.RemoveDevice(right);
            InputSystem.RemoveLayout("P3BindingTestController");
        }

        [UnityTest] public IEnumerator Bdd29_RangeBSelectsLeftTriggerAndRequiresReleaseOnSwitch()
            => VerifyShoulderRoles(new InputSystemXRTrainingInput());

        [UnityTest] public IEnumerator Bdd29_CombatBSelectsLeftTriggerAndRequiresReleaseOnSwitch()
            => VerifyShoulderRoles(new P3XRTrainingInput());

        [UnityTest] public IEnumerator Bdd29_P1RealBInputRebindsGripPresentationAndFiresOnlyTheLeftTrigger()
            => VerifyRangeShoulder(TrainingMode.Zeroing100m);
        [UnityTest] public IEnumerator Bdd29_P2RealBInputRebindsGripPresentationAndFiresOnlyTheLeftTrigger()
            => VerifyRangeShoulder(TrainingMode.MovingTarget);
        [UnityTest] public IEnumerator Bdd29_TrenchRealBInputMirrorsTheProductionRifleAndSelectsOnlyTheLeftTrigger()
            => VerifyCombatShoulder(TrainingMode.Trench);
        [UnityTest] public IEnumerator Bdd29_UrbanRealBInputMirrorsTheProductionRifleAndSelectsOnlyTheLeftTrigger()
            => VerifyCombatShoulder(TrainingMode.Urban);

        IEnumerator VerifyCombatShoulder(TrainingMode mode)
        {
            if (GameMain.Instance != null) Object.Destroy(GameMain.Instance.gameObject);
            yield return null;
            yield return SceneManager.LoadSceneAsync("MainScene"); yield return null;
            var app = GameMain.Instance.Services.Combat;
            Assert.That(app.OpenMode(mode).Success, Is.True);
            var load = app.SelectMapAsync(mode == TrainingMode.Trench ? "trench-a" : "urban-a", RandomSeed.Fixed(29));
            while (!load.IsCompleted) yield return null;
            Assert.That(load.Result.Success, Is.True, load.Result.Message);
            var runtime = Object.FindObjectOfType<CombatSceneRuntime>(); runtime.ManualStepping = true;
            var input = new P3XRTrainingInput(); runtime.InputOverride = input;
            var core = app.Mission.Core; var session = app.Mission.SessionId;
            runtime.FrameOverride = new CombatInputFrameDto
            {
                HeadTracked = true, RearHandTracked = true, FrontHandTracked = true, IsRealVr = true,
                RearGripInRange = true, FrontGripInRange = true, PlayerPosition = runtime.PlayerRoot.position,
                PlayerForward = runtime.PlayerRoot.forward, MuzzlePosition = runtime.PlayerCamera.transform.position, AimDirection = Vector3.up
            };
            void Step(float seconds = .02f)
            {
                input.Sample(true); runtime.Step(seconds);
                Assert.That(runtime.LastFrameResult.Success, Is.True, runtime.LastFrameResult.Message);
            }
            if (mode == TrainingMode.Trench)
            {
                Assert.That(app.Start().Success, Is.True);
                for (var i = 0; i < 1200 && !app.Mission.HasCombatStarted; i++)
                { Step(.1f); if (i % 20 == 0) yield return null; }
                Assert.That(app.Mission.HasCombatStarted, Is.True);
                Step(); // The release frame after recon is not a gameplay command.
            }
            session = app.Mission.SessionId; // Trench creates its session on Start, not map selection.
            Assert.That(core.GetSnapshot(session).Data.Player.MaxHealth, Is.EqualTo(mode == TrainingMode.Trench ? 10 : 5));
            var headPose = runtime.PlayerCamera.transform.localToWorldMatrix;
            void Button(XRController device, string control, float value)
                => InputSystem.QueueDeltaStateEvent(device.GetChildControl<ButtonControl>(control), value);
            Button(right, "gripPressed", 1); Button(left, "gripPressed", 1); yield return null; Step();
            var gripState = core.GetSnapshot(session).Data;
            Assert.That(gripState.Weapon.TwoHandGripActive, Is.True,
                "state=" + gripState.State + " health=" + gripState.Player.Health + " tracking=" + gripState.TrackingValid +
                " grip=" + gripState.Weapon.HoldState + " input=" + input.RightGripPressed + "/" + input.RightGripHeld + "," + input.LeftGripPressed + "/" + input.LeftGripHeld +
                " locked=" + runtime.CharacterActionsLocked);
            Button(right, "secondaryButton", 1); yield return null; Step();
            Assert.That(core.GetSnapshot(session).Data.Player.Shoulder, Is.EqualTo(ShoulderSide.Left));
            Assert.That(runtime.ReloadPresentation.GetComponent<TrainingRifleGrabInteractable>().Shoulder, Is.EqualTo(ShoulderSide.Left));
            Assert.That(runtime.ReloadPresentation.LeftHand.HandSide, Is.EqualTo(VirtualHandSide.Right));
            yield return null; Step();
            Assert.That(core.GetSnapshot(session).Data.Player.Shoulder, Is.EqualTo(ShoulderSide.Left));
            var magazine = core.GetSnapshot(session).Data.Ammo.CurrentMagazine;
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<AxisControl>("trigger"), 1f);
            yield return null; Step();
            Assert.That(core.GetSnapshot(session).Data.Ammo.CurrentMagazine, Is.EqualTo(magazine));
            InputSystem.QueueDeltaStateEvent(left.GetChildControl<AxisControl>("trigger"), 1f);
            yield return null; Step();
            Assert.That(core.GetSnapshot(session).Data.Ammo.CurrentMagazine, Is.EqualTo(magazine - 1));
            Button(right, "secondaryButton", 0); yield return null; Step();
            Button(right, "secondaryButton", 1); yield return null; Step();
            Assert.That(core.GetSnapshot(session).Data.Player.Shoulder, Is.EqualTo(ShoulderSide.Right));
            Assert.That(core.GetSnapshot(session).Data.Ammo.CurrentMagazine, Is.EqualTo(magazine - 1), "Switching to an already-held trigger cannot fire.");
            Assert.That(runtime.PlayerCamera.transform.localToWorldMatrix, Is.EqualTo(headPose));
            Assert.That(app.ReturnToMainMenu().Success, Is.True);
            yield return null; yield return null;
        }

        [UnityTearDown] public IEnumerator CleanupApplication()
        {
            if (GameMain.Instance != null)
            {
                GameMain.Instance.Services.Combat.ReturnToMainMenu();
                Object.Destroy(GameMain.Instance.gameObject);
            }
            yield return null;
        }

        IEnumerator VerifyRangeShoulder(TrainingMode mode)
        {
            if (GameMain.Instance != null) Object.Destroy(GameMain.Instance.gameObject);
            yield return null;
            yield return SceneManager.LoadSceneAsync(mode == TrainingMode.Zeroing100m ? "ZeroingRangeScene" : "MovingTargetRangeScene");
            Object.FindObjectOfType<ZeroingRangeXRModeController>().SetVrModeForTests(false);
            if (GameMain.Instance == null) new GameObject("GameMain_ShoulderInput").AddComponent<GameMain>();
            yield return null;
            var services = GameMain.Instance.Services;
            var controller = Object.FindObjectOfType<FirstPersonTrainingWeaponController>(true);
            controller.enabled = false;
            var rangeInput = new InputSystemXRTrainingInput();
            controller.ConfigureServices(services, rangeInput);
            string session;
            if (mode == TrainingMode.MovingTarget)
            {
                var started = services.MovingTarget.StartSession(new MovingTargetSettingsDto { SpeedMetersPerSecond = 4f }, RandomSeed.Fixed(29));
                Assert.That(started.Success, Is.True); session = started.Data.SessionId;
                Assert.That(services.Presentation.ConfirmStart(session).Success, Is.True);
            }
            else
            {
                Assert.That(services.Presentation.Enter(mode).Success, Is.True);
                Assert.That(services.Presentation.ConfirmStart("").Success, Is.True);
                session = services.TrainingSessions.Current.SessionId;
            }
            Assert.That(controller.InitializeForTests(), Is.True);
            void Step() => controller.SendMessage("Update");
            void Button(XRController device, string control, float value)
                => InputSystem.QueueDeltaStateEvent(device.GetChildControl<ButtonControl>(control), value);
            Button(right, "secondaryButton", 1); yield return null; Step();
            Assert.That(controller.CurrentShoulder, Is.EqualTo(ShoulderSide.Left));
            Assert.That(controller.GrabInteractable.Shoulder, Is.EqualTo(ShoulderSide.Left), "The actual rifle must mirror, not just the DTO.");
            yield return null; Step();
            Assert.That(controller.CurrentShoulder, Is.EqualTo(ShoulderSide.Left), "Held B cannot toggle twice.");
            Button(left, "gripPressed", 1); yield return null; Step();
            Assert.That(controller.CurrentHoldState, Is.EqualTo(WeaponHoldState.RearHandHeld));
            Button(right, "gripPressed", 1); yield return null; Step();
            Assert.That(controller.TwoHandGripActive, Is.True);
            Button(left, "gripPressed", 0); Button(right, "gripPressed", 0); yield return null; Step();
            Button(left, "gripPressed", 1); Button(right, "gripPressed", 1); yield return null; Step();
            Assert.That(controller.TwoHandGripActive, Is.True, "Simultaneous physical grip edges must form a left-shoulder hold too.");
            if (mode == TrainingMode.MovingTarget) services.MovingTargetProgress.Tick(session, 3f);
            var magazine = controller.CurrentMagazine;
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<AxisControl>("trigger"), 1f);
            yield return null; Step();
            Assert.That(controller.CurrentMagazine, Is.EqualTo(magazine), "Support-hand trigger must not fire.");
            InputSystem.QueueDeltaStateEvent(left.GetChildControl<AxisControl>("trigger"), 1f);
            yield return null; Step();
            var sequence = services.AutomaticFire.GetState(session);
            Assert.That(controller.CurrentMagazine, Is.LessThan(magazine),
                "The left trigger must reach the real weapon service. input=" + rangeInput.TriggerPressed + "/" + rangeInput.TriggerHeld +
                " canShoot=" + controller.CanShoot + " phase=" + services.Presentation.Get(session).Data.Phase +
                " shootingAllowed=" + services.Presentation.Get(session).Data.ShootingAllowed +
                " sequence=" + sequence.Data.Phase + "/armed=" + sequence.Data.TriggerArmedForNewSequence + "/shots=" + sequence.Data.ShotsFired);
            var afterShot = controller.CurrentMagazine;
            Button(right, "secondaryButton", 0); yield return null; Step();
            // P2 may finish the initial two-shot burst before switching; capture just before B.
            afterShot = controller.CurrentMagazine;
            Button(right, "secondaryButton", 1); yield return null; Step();
            Assert.That(controller.CurrentShoulder, Is.EqualTo(ShoulderSide.Right));
            Assert.That(controller.CurrentMagazine, Is.EqualTo(afterShot), "Switching to an already-held right trigger must not fire.");
            Assert.That(controller.GrabInteractable.Shoulder, Is.EqualTo(ShoulderSide.Right));
            if (GameMain.Instance != null) Object.Destroy(GameMain.Instance.gameObject);
            yield return null;
        }

        IEnumerator VerifyShoulderRoles(IXRTrainingInput input)
        {
            void Sample()
            {
                if (input is P3XRTrainingInput combat) combat.Sample(true);
                // The live range controller reads the trigger on every frame, including release.
                _ = input.TriggerHeld;
            }
            yield return null; Sample();
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<ButtonControl>("secondaryButton"), 1f);
            yield return null; Sample();
            Assert.That(input.SwitchShoulderPressed, Is.True, "Right B must switch shoulders in every mode.");
            yield return null; Sample();
            Assert.That(input.SwitchShoulderPressed, Is.False, "Holding B must not switch again.");
            var shoulderInput = (object)input as IShoulderAwareTrainingInput;
            Assert.That(shoulderInput, Is.Not.Null);
            shoulderInput.SetShoulder(ShoulderSide.Left);
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<AxisControl>("trigger"), 1f);
            yield return null; Sample();
            Assert.That(input.TriggerHeld || input.TriggerPressed, Is.False, "The support-hand trigger must not fire.");
            InputSystem.QueueDeltaStateEvent(left.GetChildControl<AxisControl>("trigger"), 1f);
            yield return null; Sample();
            Assert.That(input.TriggerPressed && input.TriggerHeld, Is.True);
            Assert.That(input.RightTriggerValue, Is.EqualTo(1f), "The legacy field carries the active trigger value.");
            shoulderInput.SetShoulder(ShoulderSide.Right);
            yield return null; Sample();
            Assert.That(input.TriggerHeld || input.TriggerPressed, Is.False, "A trigger already held while switching must be rearmed.");
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<AxisControl>("trigger"), 0f);
            yield return null; Sample();
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<AxisControl>("trigger"), 1f);
            yield return null; Sample();
            Assert.That(input.TriggerPressed && input.TriggerHeld, Is.True);
        }
        [UnityTest] public IEnumerator Screen23_VrSticksAndFaceButtonsReachP3Commands()
        {
            yield return null;
            var input=new P3XRTrainingInput();input.Sample(true);
            Assert.That(input.ReloadPressed||input.ConfirmPressed||input.SwitchShoulderPressed||input.PosturePressed,Is.False);
            InputSystem.QueueDeltaStateEvent(left.GetChildControl<StickControl>("thumbstick"),new Vector2(.3f,.8f));
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<StickControl>("thumbstick"),new Vector2(.9f,0));
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<ButtonControl>("primaryButton"),1f);
            InputSystem.QueueDeltaStateEvent(left.GetChildControl<ButtonControl>("primaryButton"),1f);
            InputSystem.QueueDeltaStateEvent(left.GetChildControl<ButtonControl>("primary2DAxisClick"),1f);
            yield return null;
            input.Sample(true);
            Assert.That(input.MoveAxis.y,Is.GreaterThan(.5f));Assert.That(input.TurnAxis.x,Is.GreaterThan(.7f));
            Assert.That(input.GrenadePressed,Is.True,"right A grenade");Assert.That(input.ReloadPressed,Is.True,"left X reload");Assert.That(input.ConfirmPressed,Is.False,"X must not also submit an interaction command in mode three");
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<ButtonControl>("secondaryButton"),1f);
            InputSystem.QueueDeltaStateEvent(left.GetChildControl<ButtonControl>("secondaryButton"),1f);
            yield return null;input.Sample(true);
            Assert.That(input.SwitchShoulderPressed,Is.True,"right secondary shoulder");Assert.That(input.PosturePressed,Is.True,"left secondary posture");
        }
        [UnityTest] public IEnumerator Screen23_GrenadeCommandRemainsAvailableWhileBothGripsAreHeld()
        {
            yield return null;
            var input=new P3XRTrainingInput();input.Sample(true);
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<ButtonControl>("gripPressed"),1f);
            InputSystem.QueueDeltaStateEvent(left.GetChildControl<ButtonControl>("gripPressed"),1f);
            yield return null;input.Sample(true);
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<ButtonControl>("primaryButton"),1f);
            yield return null;input.Sample(true);
            Assert.That(input.RightGripHeld&&input.LeftGripHeld,Is.True);
            Assert.That(input.GrenadePressed,Is.True);
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<ButtonControl>("primaryButton"),0f);
            yield return null;input.Sample(true);
            Assert.That(input.GrenadePressed,Is.False);
        }
        [UnityTest] public IEnumerator Screen23_ModeThreeXReloadUsesPressEdgesAndModeFourKeepsStickBinding()
        {
            yield return null;
            var trench=new P3XRTrainingInput();var urban=new P3XRTrainingInput { ReloadWithLeftX=false };
            trench.Sample(true);urban.Sample(true);
            InputSystem.QueueDeltaStateEvent(left.GetChildControl<ButtonControl>("primary2DAxisClick"),1f);
            yield return null;trench.Sample(true);urban.Sample(true);
            Assert.That(trench.ReloadPressed,Is.False,"left stick click must not reload mode three");
            Assert.That(urban.ReloadPressed,Is.True);
            InputSystem.QueueDeltaStateEvent(left.GetChildControl<ButtonControl>("primaryButton"),1f);
            yield return null;trench.Sample(true);urban.Sample(true);
            Assert.That(trench.ReloadPressed,Is.True);Assert.That(trench.ConfirmPressed,Is.False);
            Assert.That(urban.ReloadPressed,Is.False);Assert.That(urban.ConfirmPressed,Is.True);
            trench.Sample(true);Assert.That(trench.ReloadPressed,Is.False,"resampling a held X must not repeat");
            yield return null;trench.Sample(true);Assert.That(trench.ReloadPressed,Is.False);
            InputSystem.QueueDeltaStateEvent(left.GetChildControl<ButtonControl>("primaryButton"),0f);
            yield return null;trench.Sample(true);Assert.That(trench.ReloadPressed,Is.False);
            InputSystem.QueueDeltaStateEvent(left.GetChildControl<ButtonControl>("primaryButton"),1f);
            yield return null;trench.Sample(true);Assert.That(trench.ReloadPressed,Is.True);
            trench.Reset();trench.Sample(false);Assert.That(trench.ReloadPressed,Is.False);
        }
        [UnityTest] public IEnumerator Screen23_VrGripsRetainIndependentPressHoldReleaseEdges()
        {
            yield return null;
            var input=new P3XRTrainingInput();input.Sample(true);
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<ButtonControl>("gripPressed"),1f);
            InputSystem.QueueDeltaStateEvent(left.GetChildControl<ButtonControl>("gripPressed"),1f);
            yield return null;input.Sample(true);
            Assert.That(input.RightGripPressed&&input.LeftGripPressed,Is.True);
            input.Sample(true);Assert.That(input.RightGripHeld&&input.LeftGripHeld,Is.True);Assert.That(input.RightGripPressed,Is.False);
            InputSystem.QueueDeltaStateEvent(left.GetChildControl<ButtonControl>("gripPressed"),0f);yield return null;input.Sample(true);
            Assert.That(input.LeftGripReleased,Is.True);Assert.That(input.RightGripHeld,Is.True);
        }
    }
}
