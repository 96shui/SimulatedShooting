using System;
using System.Collections.Generic;
using UnityEngine;
using VRShooting.Application.Events;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application.Weapons
{
    /// <summary>
    /// 用显式 Tick 调度 P1 单发与 P2 两发起射/长按连射。不读取 XR、Scene 或帧计数。
    /// </summary>
    public sealed class WeaponAutomaticFireService : IWeaponAutomaticFireService
    {
        readonly IWeaponControlService weapons;
        readonly IAmmoService ammo;
        readonly IGameEventBus eventBus;
        readonly Dictionary<string, SessionFireState> sessions = new Dictionary<string, SessionFireState>();

        public WeaponAutomaticFireService(
            IWeaponControlService weapons,
            IAmmoService ammo,
            IGameEventBus eventBus = null)
        {
            this.weapons = weapons ?? throw new ArgumentNullException(nameof(weapons));
            this.ammo = ammo ?? throw new ArgumentNullException(nameof(ammo));
            this.eventBus = eventBus;
        }

        public event Action<WeaponFireSequenceStateDto> FireSequenceChanged;

        public ServiceResult<WeaponFireSequenceStateDto> StartSession(
            string sessionId,
            WeaponFireMode fireMode,
            WeaponAutoFireConfigDto config)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return ServiceResult<WeaponFireSequenceStateDto>.Fail(
                    ErrorCode.InvalidInput,
                    "session id is required",
                    WeaponFireSequenceStateDto.Empty);
            }

            if (!IsFinitePositive(config.ShotIntervalSeconds))
            {
                return ServiceResult<WeaponFireSequenceStateDto>.Fail(
                    ErrorCode.InvalidInput,
                    "shot interval must be a finite positive value",
                    WeaponFireSequenceStateDto.Empty);
            }

            if (fireMode == WeaponFireMode.InitialTwoThenAutomatic && config.InitialShotCount != 2)
            {
                return ServiceResult<WeaponFireSequenceStateDto>.Fail(
                    ErrorCode.InvalidInput,
                    "P2 initial shot count must be 2",
                    WeaponFireSequenceStateDto.Empty);
            }

            if (sessions.TryGetValue(sessionId, out var existing))
            {
                CancelInternal(existing, WeaponFireStopReason.TrainingCompleted, false);
            }

            var state = new SessionFireState
            {
                SessionId = sessionId,
                FireMode = fireMode,
                Config = config,
                Phase = WeaponFireSequencePhase.Idle,
                TriggerArmed = true,
                SequenceId = string.Empty
            };
            sessions[sessionId] = state;
            return ServiceResult<WeaponFireSequenceStateDto>.Ok(ToDto(state));
        }

        public ServiceResult<WeaponFireSequenceStateDto> GetState(string sessionId)
        {
            if (!TryGetState(sessionId, out var state, out var failure))
            {
                return failure;
            }

            return ServiceResult<WeaponFireSequenceStateDto>.Ok(ToDto(state));
        }

        public ServiceResult<WeaponFireSequenceStateDto> UpdateTrigger(WeaponTriggerStateInputDto input)
        {
            if (!TryGetState(input.SessionId, out var state, out var failure))
            {
                return failure;
            }

            if (input.Pressed)
            {
                state.PendingPressed = true;
            }

            if (input.Released)
            {
                state.PendingReleased = true;
                state.TriggerHeld = false;
            }
            else
            {
                state.TriggerHeld = input.Held || input.Pressed;
            }

            return ServiceResult<WeaponFireSequenceStateDto>.Ok(ToDto(state));
        }

        public ServiceResult<WeaponFireSequenceStateDto> Tick(
            string sessionId,
            float deltaTime,
            WeaponFireInputDto latestFireSnapshot)
        {
            if (!TryGetState(sessionId, out var state, out var failure))
            {
                return failure;
            }

            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
            {
                return ServiceResult<WeaponFireSequenceStateDto>.Fail(
                    ErrorCode.InvalidInput,
                    "deltaTime must be a finite non-negative value",
                    ToDto(state));
            }

            if (state.InTick)
            {
                return ServiceResult<WeaponFireSequenceStateDto>.Ok(ToDto(state));
            }

            state.InTick = true;
            try
            {
                HandleTriggerEdges(state, latestFireSnapshot);
                state.Elapsed += deltaTime;
                FireDueShots(state, latestFireSnapshot);
                return ServiceResult<WeaponFireSequenceStateDto>.Ok(ToDto(state));
            }
            finally
            {
                state.InTick = false;
            }
        }

        public ServiceResult<WeaponFireSequenceStateDto> Cancel(string sessionId, WeaponFireStopReason reason)
        {
            if (!TryGetState(sessionId, out var state, out var failure))
            {
                return failure;
            }

            CancelInternal(state, reason, true);
            return ServiceResult<WeaponFireSequenceStateDto>.Ok(ToDto(state));
        }

        internal void ReleaseSession(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId) || !sessions.TryGetValue(sessionId, out var state))
            {
                return;
            }

            CancelInternal(state, WeaponFireStopReason.WeaponBecameInvalid, false);
            sessions.Remove(sessionId);
        }

        void HandleTriggerEdges(SessionFireState state, WeaponFireInputDto snapshot)
        {
            if (state.PendingReleased)
            {
                state.PendingReleased = false;
                state.TriggerHeld = false;
                if (IsActiveSequence(state))
                {
                    state.ReleasedDuringSequence = true;
                    if (state.Phase == WeaponFireSequencePhase.ContinuousFire)
                    {
                        StopSequence(state, WeaponFireStopReason.TriggerReleased);
                    }
                }

                state.TriggerArmed = true;
            }

            if (!state.PendingPressed)
            {
                return;
            }

            state.PendingPressed = false;
            if (!state.TriggerArmed || IsActiveSequence(state))
            {
                return;
            }

            if (state.FireMode == WeaponFireMode.SingleShot)
            {
                StartSingleShot(state, snapshot);
                return;
            }

            StartInitialTwo(state);
        }

        void StartSingleShot(SessionFireState state, WeaponFireInputDto snapshot)
        {
            state.SequenceCounter++;
            state.SequenceId = "seq-" + state.SequenceCounter;
            state.Phase = WeaponFireSequencePhase.Idle;
            state.ShotsFired = 0;
            state.StopReason = null;
            state.ReleasedDuringSequence = false;
            state.TriggerArmed = false;
            FireShot(state, snapshot);
            state.TriggerArmed = !state.TriggerHeld;
        }

        void StartInitialTwo(SessionFireState state)
        {
            var ammoState = ammo.GetAmmo(state.SessionId);
            if (!ammoState.Success || ammoState.Data.CurrentMagazine < 2)
            {
                return;
            }

            state.SequenceCounter++;
            state.SequenceId = "seq-" + state.SequenceCounter;
            var reservationId = state.SequenceId;
            var reserved = ammo.ReserveAmmo(state.SessionId, 2, reservationId);
            if (!reserved.Success)
            {
                state.SequenceId = string.Empty;
                state.SequenceCounter--;
                return;
            }

            state.ReservationId = reservationId;
            state.Phase = WeaponFireSequencePhase.InitialTwoShots;
            state.ShotsFired = 0;
            state.RemainingInitialShots = 2;
            state.StopReason = null;
            state.ReleasedDuringSequence = false;
            state.NextShotAt = state.Elapsed;
            state.TriggerArmed = false;
            PublishSequence(state);
        }

        void FireDueShots(SessionFireState state, WeaponFireInputDto snapshot)
        {
            var guard = 0;
            while (IsActiveSequence(state) && state.Elapsed + 0.00001f >= state.NextShotAt && guard++ < 64)
            {
                if (state.Phase == WeaponFireSequencePhase.ContinuousFire)
                {
                    if (!state.TriggerHeld || state.ReleasedDuringSequence)
                    {
                        StopSequence(state, WeaponFireStopReason.TriggerReleased);
                        break;
                    }

                    var ammoState = ammo.GetAmmo(state.SessionId);
                    if (!ammoState.Success || ammoState.Data.CurrentMagazine <= 0)
                    {
                        StopSequence(state, WeaponFireStopReason.AmmoDepleted);
                        break;
                    }
                }

                if (!FireShot(state, snapshot))
                {
                    StopSequence(state, WeaponFireStopReason.WeaponBecameInvalid);
                    break;
                }

                state.NextShotAt += state.Config.ShotIntervalSeconds;

                if (state.Phase == WeaponFireSequencePhase.InitialTwoShots && state.ShotsFired >= 2)
                {
                    if (state.TriggerHeld && !state.ReleasedDuringSequence)
                    {
                        state.Phase = WeaponFireSequencePhase.ContinuousFire;
                        PublishSequence(state);
                    }
                    else
                    {
                        StopSequence(state, WeaponFireStopReason.TriggerReleased);
                        break;
                    }
                }

                if (state.Phase == WeaponFireSequencePhase.ContinuousFire)
                {
                    var after = ammo.GetAmmo(state.SessionId);
                    if (!after.Success || after.Data.CurrentMagazine <= 0)
                    {
                        StopSequence(state, WeaponFireStopReason.AmmoDepleted);
                        break;
                    }
                }
            }
        }

        bool FireShot(SessionFireState state, WeaponFireInputDto snapshot)
        {
            var input = CopySnapshot(snapshot, state.SessionId);
            state.ShotsFired++;
            var result = weapons.Fire(input);
            if (!result.Success || !result.Data.IsValidShot)
            {
                state.ShotsFired--;
                return false;
            }

            if (state.Phase == WeaponFireSequencePhase.InitialTwoShots)
            {
                state.RemainingInitialShots = Math.Max(0, state.RemainingInitialShots - 1);
            }

            return true;
        }

        void StopSequence(SessionFireState state, WeaponFireStopReason reason)
        {
            CancelInternal(state, reason, true);
        }

        void CancelInternal(SessionFireState state, WeaponFireStopReason reason, bool publish)
        {
            var hadActiveSequence = IsActiveSequence(state) || !string.IsNullOrEmpty(state.ReservationId);
            ReleaseReservation(state);

            if (!hadActiveSequence && state.Phase != WeaponFireSequencePhase.InitialTwoShots
                && state.Phase != WeaponFireSequencePhase.ContinuousFire)
            {
                if (reason == WeaponFireStopReason.ShootingBecameForbidden
                    || reason == WeaponFireStopReason.WeaponBecameInvalid
                    || reason == WeaponFireStopReason.TrainingCompleted)
                {
                    if (state.TriggerHeld)
                    {
                        state.TriggerArmed = false;
                    }
                }

                return;
            }

            state.Phase = WeaponFireSequencePhase.Stopped;
            state.StopReason = reason;
            state.RemainingInitialShots = 0;
            state.ReleasedDuringSequence = false;

            if (reason == WeaponFireStopReason.ShootingBecameForbidden
                || reason == WeaponFireStopReason.WeaponBecameInvalid
                || reason == WeaponFireStopReason.TrainingCompleted
                || state.TriggerHeld)
            {
                state.TriggerArmed = false;
            }
            else
            {
                state.TriggerArmed = true;
            }

            if (publish)
            {
                PublishSequence(state);
            }

            state.SequenceId = state.SequenceId ?? string.Empty;
        }

        void ReleaseReservation(SessionFireState state)
        {
            if (string.IsNullOrEmpty(state.ReservationId))
            {
                return;
            }

            ammo.ReleaseAmmoReservation(state.SessionId, state.ReservationId);
            state.ReservationId = string.Empty;
        }

        void PublishSequence(SessionFireState state)
        {
            var dto = ToDto(state);
            eventBus?.Publish(new WeaponFireSequenceChangedEvent { State = dto });
            FireSequenceChanged?.Invoke(dto);
        }

        bool TryGetState(
            string sessionId,
            out SessionFireState state,
            out ServiceResult<WeaponFireSequenceStateDto> failure)
        {
            if (string.IsNullOrWhiteSpace(sessionId) || !sessions.TryGetValue(sessionId, out state))
            {
                state = null;
                failure = ServiceResult<WeaponFireSequenceStateDto>.Fail(
                    ErrorCode.InvalidState,
                    "weapon auto-fire session is not initialized",
                    WeaponFireSequenceStateDto.Empty);
                return false;
            }

            failure = default;
            return true;
        }

        static bool IsActiveSequence(SessionFireState state)
        {
            return state.Phase == WeaponFireSequencePhase.InitialTwoShots
                   || state.Phase == WeaponFireSequencePhase.ContinuousFire;
        }

        static WeaponFireSequenceStateDto ToDto(SessionFireState state)
        {
            return new WeaponFireSequenceStateDto
            {
                SessionId = state.SessionId,
                SequenceId = state.SequenceId ?? string.Empty,
                FireMode = state.FireMode,
                Phase = state.Phase,
                ShotsFired = state.ShotsFired,
                TriggerHeld = state.TriggerHeld,
                TriggerArmedForNewSequence = state.TriggerArmed,
                StopReason = state.StopReason
            };
        }

        static WeaponFireInputDto CopySnapshot(WeaponFireInputDto snapshot, string sessionId)
        {
            return new WeaponFireInputDto
            {
                SessionId = sessionId,
                MuzzlePosition = snapshot.MuzzlePosition,
                RawAimDirection = snapshot.RawAimDirection,
                AimDirection = snapshot.AimDirection,
                WeaponPosition = snapshot.WeaponPosition,
                AimMotionOffsetCm = snapshot.AimMotionOffsetCm,
                Stability01 = snapshot.Stability01,
                TwoHandGripActive = snapshot.TwoHandGripActive,
                AimMode = snapshot.AimMode,
                ShoulderSide = snapshot.ShoulderSide,
                Hit = snapshot.Hit,
                HitPoint = snapshot.HitPoint,
                HitObjectId = snapshot.HitObjectId
            };
        }

        static bool IsFinitePositive(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
        }

        sealed class SessionFireState
        {
            public string SessionId;
            public WeaponFireMode FireMode;
            public WeaponAutoFireConfigDto Config;
            public WeaponFireSequencePhase Phase;
            public string SequenceId;
            public int SequenceCounter;
            public int ShotsFired;
            public int RemainingInitialShots;
            public bool TriggerHeld;
            public bool TriggerArmed;
            public bool PendingPressed;
            public bool PendingReleased;
            public bool ReleasedDuringSequence;
            public bool InTick;
            public float Elapsed;
            public float NextShotAt;
            public string ReservationId;
            public WeaponFireStopReason? StopReason;
        }
    }
}
