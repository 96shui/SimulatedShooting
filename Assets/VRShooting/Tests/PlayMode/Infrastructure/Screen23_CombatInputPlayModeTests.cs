using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VRShooting.Application;
using VRShooting.Application.Combat;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.Input;
using VRShooting.P3.TestSupport;
using VRShooting.Unity.Combat;

namespace VRShooting.Tests.PlayMode
{
    // BDD23 dual-grip/reload/lifecycle/posture/cover; BDD15/19/21 service-side status.
    public sealed class Screen23_CombatInputPlayModeTests
    {
        const string Session = "input-combat";
        static CombatInputFrameDto Frame(bool tracking = true, bool vr = false, PlayerPosture? posture = null) => new CombatInputFrameDto
        {
            HeadTracked = tracking, RearHandTracked = tracking, FrontHandTracked = tracking,
            RearGripInRange = true, FrontGripInRange = true, IsRealVr = vr,
            MuzzlePosition = Vector3.zero, AimDirection = Vector3.forward,
            PlayerPosition = Vector3.zero, PlayerForward = Vector3.forward, RequestedPosture = posture
        };
        static CombatCoreService Start(FakeCombatClock clock)
        {
            var core = new CombatCoreService(clock);
            Assert.That(core.Start(Session, TrainingMode.Urban, new[] { new CombatEntityVisualDto
            { EntityId = "enemy", Role = CombatEntityRole.Enemy, Position = new Vector3(0,0,10), Forward = Vector3.back } }).Success);
            return core;
        }
        static void Step(CombatInputController controller, ManualXRTrainingInput input, FakeCombatClock clock,
            CombatInputFrameDto? frame = null, double seconds = .016)
        {
            clock.Advance(seconds); var result = controller.Step(frame ?? Frame(), (float)seconds);
            Assert.That(result.Success, Is.True, result.Message); input.AdvanceFrame();
        }
        [UnityTest] public IEnumerator ManualInput_PickupTwoHandsSingleShot_ReloadShoulderPostureAndCombat()
        {
            var clock = new FakeCombatClock(); using var core = Start(clock);
            var input = new ManualXRTrainingInput(); var ray = new RayFake(); var move = new MoveFake();
            using var controller = new CombatInputController(Session, TrainingMode.Urban, core, input, clock, ray, move);
            input.Press(XRTrainingInputButton.Trigger); Step(controller,input,clock);
            Assert.That(core.GetSnapshot(Session).Data.Ammo.CurrentMagazine, Is.EqualTo(30));
            input.Release(XRTrainingInputButton.Trigger); input.Press(XRTrainingInputButton.RightGrip); Step(controller,input,clock);
            Assert.That(core.GetSnapshot(Session).Data.Weapon.HoldState, Is.EqualTo(WeaponHoldState.RearHandHeld));
            input.Press(XRTrainingInputButton.LeftGrip); input.Press(XRTrainingInputButton.Trigger); Step(controller,input,clock);
            Step(controller,input,clock, seconds: .5); Assert.That(core.GetSnapshot(Session).Data.Ammo.CurrentMagazine, Is.EqualTo(29));
            input.Release(XRTrainingInputButton.Trigger); input.Press(XRTrainingInputButton.Reload); Step(controller,input,clock);
            Assert.That(core.GetSnapshot(Session).Data.Ammo.IsReloading); Step(controller,input,clock, seconds: 2);
            Assert.That(core.GetSnapshot(Session).Data.Ammo.ReserveAmmo, Is.EqualTo(119));
            input.Press(XRTrainingInputButton.SwitchShoulder); input.SetAxes(Vector2.up, Vector2.right);
            Step(controller,input,clock,Frame(posture: PlayerPosture.Crouching));
            Assert.That(core.GetSnapshot(Session).Data.Player.Shoulder, Is.EqualTo(ShoulderSide.Left));
            Assert.That(move.Last.LocalVelocity.z, Is.EqualTo(.9f)); Assert.That(move.Last.SimulatedEyeHeight, Is.EqualTo(1.1f));
            Assert.That(move.Last.SnapTurnDegrees, Is.EqualTo(30)); Step(controller,input,clock,Frame(vr:true));
            Assert.That(move.Last.SnapTurnDegrees, Is.Zero); Assert.That(move.Last.SimulatedEyeHeight.HasValue, Is.False);
            core.Submit(new CombatInputDto { SessionId = Session, Tick = clock.Tick, EventId = "see-player", Kind = CombatInputKind.Perception,
                EntityId = "enemy", TargetId = Session + ".player", Position = new Vector3(0,0,10), Direction = Vector3.back, Flag = true });
            core.Advance(Session); Step(controller,input,clock,seconds:1);
            Assert.That(core.GetSnapshot(Session).Data.Player.Health, Is.EqualTo(90));
            ray.Target = "enemy"; input.Press(XRTrainingInputButton.Trigger); Step(controller,input,clock);
            Assert.That(core.GetSnapshot(Session).Data.Visual.Entities.Single(e => e.EntityId == "enemy").CorpseVisible);
            var health = core.GetSnapshot(Session).Data.Player.Health; Step(controller,input,clock,seconds:5);
            Assert.That(core.GetSnapshot(Session).Data.Player.Health, Is.EqualTo(health));
            yield return null;
        }
        [UnityTest] public IEnumerator TrackingAndPause_BlockHeldTrigger_AndStopMovement()
        {
            var clock = new FakeCombatClock(); using var core = Start(clock);
            var input = new ManualXRTrainingInput(); var move = new MoveFake();
            using var controller = new CombatInputController(Session, TrainingMode.Trench, core, input, clock, new RayFake(), move);
            input.Press(XRTrainingInputButton.RightGrip); input.Press(XRTrainingInputButton.LeftGrip);
            input.Press(XRTrainingInputButton.Trigger); input.SetMoveAxis(Vector2.up); Step(controller,input,clock);
            Assert.That(core.GetSnapshot(Session).Data.Ammo.CurrentMagazine, Is.EqualTo(29));
            Step(controller,input,clock,Frame(tracking:false)); Assert.That(move.Last.LocalVelocity, Is.EqualTo(Vector3.zero));
            Step(controller,input,clock); Assert.That(core.GetSnapshot(Session).Data.Ammo.CurrentMagazine, Is.EqualTo(29));
            core.SetState(Session,SessionState.Paused); Step(controller,input,clock); Assert.That(move.Last.LocalVelocity, Is.EqualTo(Vector3.zero));
            core.SetState(Session,SessionState.Running); Step(controller,input,clock); Assert.That(core.GetSnapshot(Session).Data.Ammo.CurrentMagazine, Is.EqualTo(29));
            input.Release(XRTrainingInputButton.Trigger); input.Release(XRTrainingInputButton.RightGrip); input.Release(XRTrainingInputButton.LeftGrip); Step(controller,input,clock);
            input.Press(XRTrainingInputButton.RightGrip); input.Press(XRTrainingInputButton.LeftGrip); input.Press(XRTrainingInputButton.Trigger); Step(controller,input,clock);
            Assert.That(core.GetSnapshot(Session).Data.Ammo.CurrentMagazine, Is.EqualTo(28));
            yield return null;
        }
        [UnityTest] public IEnumerator PhysicsFirstHit_BlocksWallAndEmbeddedMuzzle()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                // Isolated layers; no loaded scene objects may affect this test.
                wall.layer = 30; target.layer = 29;
                wall.transform.position = new Vector3(0,50,2); target.transform.position = new Vector3(0,50,4);
                target.AddComponent<CombatEntityBinding>().EntityId = "enemy";
                Physics.SyncTransforms();
                var query = new CombatPhysicsShotQuery((1 << 30) | (1 << 29), 1 << 30);
                var blocked = query.Cast(new Vector3(0,50,0), Vector3.forward);
                Assert.That(blocked.Hit); Assert.That(blocked.EntityId, Is.Null.Or.Empty);
                var embedded = query.Cast(wall.transform.position, Vector3.forward);
                Assert.That(embedded.Hit); Assert.That(embedded.EntityId, Is.Null.Or.Empty);
                wall.transform.position += Vector3.right * 5; Physics.SyncTransforms();
                Assert.That(query.Cast(new Vector3(0,50,0), Vector3.forward).EntityId, Is.EqualTo("enemy"));
                yield return null;
            }
            finally { Object.DestroyImmediate(wall); Object.DestroyImmediate(target); }
        }
        [UnityTest] public IEnumerator Locomotion_P1P2StayFixed_AndVrDoesNotMoveEyeHeight()
        {
            foreach (var mode in new[] { TrainingMode.Zeroing100m, TrainingMode.MovingTarget })
            {
                var policy = CombatLocomotionPolicy.Get(mode,true);
                Assert.That(policy.AllowContinuousMove || policy.AllowArtificialTurn || policy.AllowTeleport, Is.False);
                var intent = CombatLocomotionPolicy.Intent(mode,true,PlayerPosture.Prone,Vector2.one,30,false,.1f,CombatConfigDto.Default);
                Assert.That(intent.LocalVelocity, Is.EqualTo(Vector3.zero)); Assert.That(intent.SnapTurnDegrees, Is.Zero);
            }
            var root = new GameObject("CombatLocomotionProbe");
            try
            {
                var body = root.AddComponent<CharacterController>();
                var eye = new GameObject("SimulatedEye").transform; eye.SetParent(root.transform); eye.localPosition = Vector3.up * 1.73f;
                var port = new CombatCharacterLocomotion(body,eye);
                Assert.That(port.Apply(CombatLocomotionPolicy.Intent(TrainingMode.Trench,true,PlayerPosture.Prone,Vector2.zero,0,true,.1f,CombatConfigDto.Default)).Success);
                Assert.That(eye.localPosition.y, Is.EqualTo(1.73f));
                port.Apply(CombatLocomotionPolicy.Intent(TrainingMode.Trench,true,PlayerPosture.Prone,Vector2.zero,0,false,.1f,CombatConfigDto.Default));
                Assert.That(eye.localPosition.y, Is.EqualTo(.65f));
                yield return null;
            }
            finally { Object.DestroyImmediate(root); }
        }
        sealed class RayFake : ICombatShotQuery
        {
            public string Target;
            public CombatRayHitDto Cast(Vector3 origin, Vector3 direction) => new CombatRayHitDto
            { Hit = Target != null, EntityId = Target, Point = origin + direction * 10 };
        }
        sealed class MoveFake : ICombatLocomotionPort
        {
            public CombatLocomotionIntentDto Last;
            public ServiceResult<Unit> Apply(CombatLocomotionIntentDto intent) { Last = intent; return ServiceResult<Unit>.Ok(Unit.Value); }
        }
    }
}
