using System;
using UnityEngine;
using VRShooting.Application;
using VRShooting.Application.Combat;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Input
{
    /// <summary>Session-bound controller; callers collect world facts then Step once per clock Tick.</summary>
    public sealed class CombatInputController : IDisposable
    {
        readonly string session;
        readonly TrainingMode mode;
        readonly ICombatCoreService core;
        readonly IXRTrainingInput input;
        readonly ICombatClock clock;
        readonly ICombatShotQuery shots;
        readonly ICombatLocomotionPort locomotion;
        readonly CombatConfigDto config;
        bool rearHeld, frontHeld, triggerWasHeld, turnLatched, disposed;
        long lastFrame = -1;

        public CombatInputController(string session, TrainingMode mode, ICombatCoreService core, IXRTrainingInput input,
            ICombatClock clock, ICombatShotQuery shots, ICombatLocomotionPort locomotion, CombatConfigDto? config = null)
        {
            if (string.IsNullOrWhiteSpace(session)) throw new ArgumentException("Session required", nameof(session));
            if (mode != TrainingMode.Trench && mode != TrainingMode.Urban) throw new ArgumentException("P3 mode required", nameof(mode));
            this.session = session; this.mode = mode;
            this.core = core ?? throw new ArgumentNullException(nameof(core));
            this.input = input ?? throw new ArgumentNullException(nameof(input));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.shots = shots ?? throw new ArgumentNullException(nameof(shots));
            this.locomotion = locomotion ?? throw new ArgumentNullException(nameof(locomotion));
            this.config = config ?? CombatConfigDto.Default;
            if (!this.config.Validate().Success) throw new ArgumentException("Invalid combat configuration", nameof(config));
        }
        public ServiceResult<Unit> Step(CombatInputFrameDto frame, float deltaSeconds)
        {
            if (disposed) return Fail(ErrorCode.InvalidState);
            if (clock.Tick <= lastFrame) return Fail(ErrorCode.InvalidState);
            if (!Finite(deltaSeconds) || deltaSeconds < 0 || !Finite(input.MoveAxis.x) || !Finite(input.MoveAxis.y) ||
                !Finite(input.TurnAxis.x) || !Finite(frame.MuzzlePosition) || !Direction(frame.AimDirection) ||
                !Finite(frame.PlayerPosition) || !Direction(frame.PlayerForward) ||
                (frame.RequestedPosture.HasValue && (frame.RequestedPosture < PlayerPosture.Standing || frame.RequestedPosture > PlayerPosture.Prone)))
                return Fail(ErrorCode.InvalidInput);
            var query = core.GetSnapshot(session); if (!query.Success) return Fail(query.ErrorCode);
            lastFrame = clock.Tick;
            var pressed = input.TriggerHeld && !triggerWasHeld;
            triggerWasHeld = input.TriggerHeld;
            bool tracking = frame.HeadTracked && frame.RearHandTracked && frame.FrontHandTracked;
            var result = core.SetTracking(session, tracking); if (!result.Success) return result;
            bool active = tracking && query.Data.State == SessionState.Running && query.Data.Player.IsAlive;
            if (!active)
            {
                rearHeld = frontHeld = false; turnLatched = Math.Abs(input.TurnAxis.x) > .2f;
                var stopped = locomotion.Apply(CombatLocomotionPolicy.Intent(mode, false, query.Data.Player.Posture,
                    Vector2.zero, 0, frame.IsRealVr, deltaSeconds, config));
                if (!stopped.Success) return stopped;
                return core.Advance(session);
            }
            if (!input.RightGripHeld) rearHeld = false;
            if (input.RightGripPressed && frame.RearGripInRange) rearHeld = true;
            if (!input.LeftGripHeld || !rearHeld) frontHeld = false;
            if (rearHeld && input.LeftGripPressed && frame.FrontGripInRange) frontHeld = true;
            result = core.SetGrip(new WeaponGripStateInputDto { SessionId = session,
                HoldState = frontHeld ? WeaponHoldState.TwoHandHeld : rearHeld ? WeaponHoldState.RearHandHeld : WeaponHoldState.Dropped,
                RearHandTracked = rearHeld, FrontHandTracked = frontHeld, Stability01 = frontHeld ? 1 : .3f });
            if (!result.Success) return result;
            if (frame.RequestedPosture.HasValue) { result = core.SetPosture(session, frame.RequestedPosture.Value); if (!result.Success) return result; }
            if (input.SwitchShoulderPressed) { result = core.ToggleShoulder(session); if (!result.Success) return result; }
            var player = core.GetSnapshot(session).Data.Player;
            float snap = 0;
            if (Math.Abs(input.TurnAxis.x) < .2f) turnLatched = false;
            else if (!turnLatched && Math.Abs(input.TurnAxis.x) >= .7f) { snap = Math.Sign(input.TurnAxis.x) * config.SnapTurnDegrees; turnLatched = true; }
            result = locomotion.Apply(CombatLocomotionPolicy.Intent(mode, true, player.Posture, input.MoveAxis, snap, frame.IsRealVr, deltaSeconds, config));
            if (!result.Success) return result;
            result = core.Submit(new CombatInputDto { SessionId = session, EventId = "input.pose-" + clock.Tick,
                Tick = clock.Tick, Kind = CombatInputKind.PlayerPose, EntityId = session + ".player",
                Position = frame.PlayerPosition, Direction = frame.PlayerForward });
            if (!result.Success) return result;
            if (input.ReloadPressed) core.Reload(session);
            if (pressed)
            {
                var shot = core.Fire(new WeaponFireInputDto { SessionId = session, MuzzlePosition = frame.MuzzlePosition,
                    AimDirection = frame.AimDirection, RawAimDirection = frame.AimDirection,
                    AimMode = input.AimHeld ? WeaponAimMode.AimDownSights : WeaponAimMode.HipFire });
                if (shot.Success)
                {
                    var hit = shots.Cast(shot.Data.Shot.MuzzlePosition, shot.Data.Shot.AimDirection);
                    if (hit.Hit && !string.IsNullOrEmpty(hit.EntityId))
                    {
                        result = core.Submit(new CombatInputDto { SessionId = session, EventId = "input.hit-" + shot.Data.ShotId,
                            Tick = clock.Tick, Kind = CombatInputKind.Hit, EntityId = session + ".player", TargetId = hit.EntityId,
                            ShotId = shot.Data.ShotId, Position = hit.Point, Flag = true, Value = 1 });
                        if (!result.Success) return result;
                    }
                }
            }
            return core.Advance(session);
        }
        static bool Finite(float x) => !float.IsNaN(x) && !float.IsInfinity(x);
        static bool Finite(Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z);
        static bool Direction(Vector3 v) => Finite(v) && Finite(v.sqrMagnitude) && v.sqrMagnitude > 1e-8;
        static ServiceResult<Unit> Fail(ErrorCode code) => ServiceResult<Unit>.Fail(code, "Invalid combat input: " + code);
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            locomotion.Apply(new CombatLocomotionIntentDto());
            core.SetState(session, SessionState.Cancelled);
        }
    }
}
