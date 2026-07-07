using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRShooting.Application.Events;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application.Weapons
{
    public sealed class WeaponControlService : IWeaponControlService, IWeaponService, IAmmoService
    {
        public const string TrainingRifleId = "training-rifle";

        readonly IGameEventBus eventBus;
        readonly Dictionary<string, SessionWeaponState> sessions = new Dictionary<string, SessionWeaponState>();
        readonly Dictionary<string, WeaponDefinitionDto> weapons;

        string equippedWeaponId = TrainingRifleId;
        string previewWeaponId = TrainingRifleId;

        public WeaponControlService(IGameEventBus eventBus = null)
        {
            this.eventBus = eventBus;
            weapons = CreateDefaultWeapons().ToDictionary(weapon => weapon.WeaponId, weapon => weapon);
        }

        public ServiceResult<IReadOnlyList<WeaponDefinitionDto>> GetWeapons()
        {
            return ServiceResult<IReadOnlyList<WeaponDefinitionDto>>.Ok(weapons.Values.ToArray());
        }

        public ServiceResult<WeaponDefinitionDto> GetWeapon(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId) || !weapons.TryGetValue(weaponId, out var weapon))
            {
                return ServiceResult<WeaponDefinitionDto>.Fail(ErrorCode.NotFound, "weapon not found");
            }

            return ServiceResult<WeaponDefinitionDto>.Ok(weapon);
        }

        public ServiceResult<WeaponDefinitionDto> GetEquippedWeapon()
        {
            return GetWeapon(equippedWeaponId);
        }

        public ServiceResult<WeaponDefinitionDto> SelectPreview(string weaponId)
        {
            var result = GetWeapon(weaponId);
            if (!result.Success)
            {
                return result;
            }

            previewWeaponId = weaponId;
            return result;
        }

        public ServiceResult<WeaponDefinitionDto> Equip(string weaponId, TrainingMode? mode)
        {
            var result = GetWeapon(weaponId);
            if (!result.Success)
            {
                return result;
            }

            if (mode.HasValue && !result.Data.ApplicableModes.Contains(mode.Value))
            {
                return ServiceResult<WeaponDefinitionDto>.Fail(ErrorCode.InvalidInput, "weapon is not applicable to mode");
            }

            equippedWeaponId = weaponId;
            previewWeaponId = weaponId;
            eventBus?.Publish(new WeaponChangedEvent { Weapon = result.Data });
            return result;
        }

        public ServiceResult<WeaponControlStateDto> StartSession(string sessionId, string weaponId, TrainingMode mode)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return ServiceResult<WeaponControlStateDto>.Fail(ErrorCode.InvalidInput, "session id is required");
            }

            var resolvedWeaponId = string.IsNullOrWhiteSpace(weaponId) ? TrainingRifleId : weaponId;
            var weaponResult = GetWeapon(resolvedWeaponId);
            if (!weaponResult.Success)
            {
                return ServiceResult<WeaponControlStateDto>.Fail(weaponResult.ErrorCode, weaponResult.Message);
            }

            if (!weaponResult.Data.ApplicableModes.Contains(mode))
            {
                return ServiceResult<WeaponControlStateDto>.Fail(ErrorCode.InvalidInput, "weapon is not applicable to mode");
            }

            var state = new SessionWeaponState
            {
                SessionId = sessionId,
                Weapon = weaponResult.Data,
                CurrentMagazine = weaponResult.Data.MagazineCapacity,
                ReserveAmmo = weaponResult.Data.MaxReserveAmmo,
                ShoulderSide = ShoulderSide.Right,
                AimMode = WeaponAimMode.HipFire,
                Stability01 = 1f,
                TwoHandGripActive = false
            };
            sessions[sessionId] = state;

            var dto = ToStateDto(state);
            eventBus?.Publish(new WeaponStateChangedEvent { State = dto });
            eventBus?.Publish(new AmmoChangedEvent { SessionId = sessionId, Ammo = ToAmmoDto(state) });
            return ServiceResult<WeaponControlStateDto>.Ok(dto);
        }

        public ServiceResult<WeaponControlStateDto> GetState(string sessionId)
        {
            if (!TryGetSession(sessionId, out var state, out var failure))
            {
                return ServiceResult<WeaponControlStateDto>.Fail(failure.ErrorCode, failure.Message);
            }

            return ServiceResult<WeaponControlStateDto>.Ok(ToStateDto(state));
        }

        public ServiceResult<WeaponShotResultDto> Fire(WeaponFireInputDto input)
        {
            if (!TryGetSession(input.SessionId, out var state, out var failure))
            {
                return ServiceResult<WeaponShotResultDto>.Fail(failure.ErrorCode, failure.Message);
            }

            state.AimMode = input.AimMode;
            state.ShoulderSide = input.ShoulderSide;
            state.TwoHandGripActive = input.TwoHandGripActive;
            state.Stability01 = Mathf.Clamp01(input.Stability01);

            var aimDirection = input.AimDirection;
            if (!IsFinite(input.MuzzlePosition) || !IsFinite(aimDirection) || aimDirection.sqrMagnitude <= 0.0001f)
            {
                var invalidResult = BuildShotResult(input, state, false, ErrorCode.InvalidInput, "invalid muzzle or aim direction");
                eventBus?.Publish(new WeaponShotResultEvent { Result = invalidResult });
                return ServiceResult<WeaponShotResultDto>.Fail(ErrorCode.InvalidInput, invalidResult.Message, invalidResult);
            }

            if (!CanShoot(state))
            {
                var blockedResult = BuildShotResult(input, state, false, ErrorCode.InvalidState, "weapon cannot shoot");
                eventBus?.Publish(new WeaponShotResultEvent { Result = blockedResult });
                eventBus?.Publish(new WeaponStateChangedEvent { State = ToStateDto(state) });
                return ServiceResult<WeaponShotResultDto>.Fail(ErrorCode.InvalidState, blockedResult.Message, blockedResult);
            }

            state.CurrentMagazine--;
            var result = BuildShotResult(input, state, true, ErrorCode.None, string.Empty);
            eventBus?.Publish(new AmmoChangedEvent { SessionId = state.SessionId, Ammo = ToAmmoDto(state) });
            eventBus?.Publish(new WeaponStateChangedEvent { State = ToStateDto(state) });
            eventBus?.Publish(new WeaponShotResultEvent { Result = result });
            return ServiceResult<WeaponShotResultDto>.Ok(result);
        }

        public ServiceResult<WeaponControlStateDto> Reload(string sessionId)
        {
            var startResult = StartReload(sessionId);
            if (!startResult.Success)
            {
                return ServiceResult<WeaponControlStateDto>.Fail(startResult.ErrorCode, startResult.Message);
            }

            var completeResult = CompleteReload(sessionId);
            if (!completeResult.Success)
            {
                return ServiceResult<WeaponControlStateDto>.Fail(completeResult.ErrorCode, completeResult.Message);
            }

            return GetState(sessionId);
        }

        public ServiceResult<WeaponControlStateDto> SetShoulder(string sessionId, ShoulderSide shoulderSide)
        {
            if (!TryGetSession(sessionId, out var state, out var failure))
            {
                return ServiceResult<WeaponControlStateDto>.Fail(failure.ErrorCode, failure.Message);
            }

            if (state.ShoulderSide == shoulderSide)
            {
                return ServiceResult<WeaponControlStateDto>.Ok(ToStateDto(state));
            }

            state.ShoulderSide = shoulderSide;
            var dto = ToStateDto(state);
            eventBus?.Publish(new ShoulderChangedEvent { SessionId = sessionId, ShoulderSide = shoulderSide });
            eventBus?.Publish(new WeaponStateChangedEvent { State = dto });
            return ServiceResult<WeaponControlStateDto>.Ok(dto);
        }

        public ServiceResult<WeaponControlStateDto> ToggleShoulder(string sessionId)
        {
            if (!TryGetSession(sessionId, out var state, out var failure))
            {
                return ServiceResult<WeaponControlStateDto>.Fail(failure.ErrorCode, failure.Message);
            }

            var next = state.ShoulderSide == ShoulderSide.Right ? ShoulderSide.Left : ShoulderSide.Right;
            return SetShoulder(sessionId, next);
        }

        public ServiceResult<WeaponControlStateDto> SetAimMode(string sessionId, WeaponAimMode aimMode)
        {
            if (!TryGetSession(sessionId, out var state, out var failure))
            {
                return ServiceResult<WeaponControlStateDto>.Fail(failure.ErrorCode, failure.Message);
            }

            if (state.AimMode == aimMode)
            {
                return ServiceResult<WeaponControlStateDto>.Ok(ToStateDto(state));
            }

            state.AimMode = aimMode;
            var dto = ToStateDto(state);
            eventBus?.Publish(new AimModeChangedEvent { SessionId = sessionId, AimMode = aimMode });
            eventBus?.Publish(new WeaponStateChangedEvent { State = dto });
            return ServiceResult<WeaponControlStateDto>.Ok(dto);
        }

        public ServiceResult<WeaponControlStateDto> SetGripState(string sessionId, bool twoHandGripActive, float stability01)
        {
            if (!TryGetSession(sessionId, out var state, out var failure))
            {
                return ServiceResult<WeaponControlStateDto>.Fail(failure.ErrorCode, failure.Message);
            }

            var nextStability = Mathf.Clamp01(stability01);
            if (state.TwoHandGripActive == twoHandGripActive && Mathf.Approximately(state.Stability01, nextStability))
            {
                return ServiceResult<WeaponControlStateDto>.Ok(ToStateDto(state));
            }

            state.TwoHandGripActive = twoHandGripActive;
            state.Stability01 = nextStability;
            var dto = ToStateDto(state);
            eventBus?.Publish(new WeaponStateChangedEvent { State = dto });
            return ServiceResult<WeaponControlStateDto>.Ok(dto);
        }

        public ServiceResult<AmmoDto> GetAmmo(string sessionId)
        {
            if (!TryGetSession(sessionId, out var state, out var failure))
            {
                return ServiceResult<AmmoDto>.Fail(failure.ErrorCode, failure.Message);
            }

            return ServiceResult<AmmoDto>.Ok(ToAmmoDto(state));
        }

        public ServiceResult<AmmoDto> ConsumeAmmo(string sessionId, int amount)
        {
            if (!TryGetSession(sessionId, out var state, out var failure))
            {
                return ServiceResult<AmmoDto>.Fail(failure.ErrorCode, failure.Message);
            }

            if (amount <= 0)
            {
                return ServiceResult<AmmoDto>.Fail(ErrorCode.InvalidInput, "amount must be positive");
            }

            if (state.CurrentMagazine < amount || state.IsReloading)
            {
                return ServiceResult<AmmoDto>.Fail(ErrorCode.InvalidState, "not enough ammo");
            }

            state.CurrentMagazine -= amount;
            var ammo = ToAmmoDto(state);
            eventBus?.Publish(new AmmoChangedEvent { SessionId = sessionId, Ammo = ammo });
            eventBus?.Publish(new WeaponStateChangedEvent { State = ToStateDto(state) });
            return ServiceResult<AmmoDto>.Ok(ammo);
        }

        public ServiceResult<AmmoDto> StartReload(string sessionId)
        {
            if (!TryGetSession(sessionId, out var state, out var failure))
            {
                return ServiceResult<AmmoDto>.Fail(failure.ErrorCode, failure.Message);
            }

            if (state.IsReloading)
            {
                return ServiceResult<AmmoDto>.Fail(ErrorCode.Busy, "reload already started");
            }

            if (state.CurrentMagazine >= state.Weapon.MagazineCapacity)
            {
                return ServiceResult<AmmoDto>.Ok(ToAmmoDto(state));
            }

            if (state.ReserveAmmo <= 0)
            {
                return ServiceResult<AmmoDto>.Fail(ErrorCode.InvalidState, "no reserve ammo");
            }

            state.IsReloading = true;
            var ammo = ToAmmoDto(state);
            eventBus?.Publish(new ReloadStartedEvent { SessionId = sessionId });
            eventBus?.Publish(new AmmoChangedEvent { SessionId = sessionId, Ammo = ammo });
            eventBus?.Publish(new WeaponStateChangedEvent { State = ToStateDto(state) });
            return ServiceResult<AmmoDto>.Ok(ammo);
        }

        public ServiceResult<AmmoDto> CompleteReload(string sessionId)
        {
            if (!TryGetSession(sessionId, out var state, out var failure))
            {
                return ServiceResult<AmmoDto>.Fail(failure.ErrorCode, failure.Message);
            }

            if (!state.IsReloading)
            {
                return ServiceResult<AmmoDto>.Ok(ToAmmoDto(state));
            }

            var needed = state.Weapon.MagazineCapacity - state.CurrentMagazine;
            var loaded = Mathf.Min(needed, state.ReserveAmmo);
            state.CurrentMagazine += loaded;
            state.ReserveAmmo -= loaded;
            state.IsReloading = false;

            var ammo = ToAmmoDto(state);
            eventBus?.Publish(new ReloadCompletedEvent { SessionId = sessionId, Ammo = ammo });
            eventBus?.Publish(new AmmoChangedEvent { SessionId = sessionId, Ammo = ammo });
            eventBus?.Publish(new WeaponStateChangedEvent { State = ToStateDto(state) });
            return ServiceResult<AmmoDto>.Ok(ammo);
        }

        static IEnumerable<WeaponDefinitionDto> CreateDefaultWeapons()
        {
            yield return new WeaponDefinitionDto
            {
                WeaponId = TrainingRifleId,
                DisplayName = "P1 100m training rifle",
                Type = WeaponType.AssaultRifle,
                MagazineCapacity = 3,
                MaxReserveAmmo = 6,
                Recoil = RecoilLevel.Medium,
                ApplicableModes = new[] { TrainingMode.Zeroing100m }
            };
            yield return new WeaponDefinitionDto
            {
                WeaponId = "w_191",
                DisplayName = "19-1 automatic rifle",
                Type = WeaponType.AssaultRifle,
                MagazineCapacity = 30,
                MaxReserveAmmo = 120,
                Recoil = RecoilLevel.Medium,
                ApplicableModes = new[] { TrainingMode.Trench, TrainingMode.Urban }
            };
            yield return new WeaponDefinitionDto
            {
                WeaponId = "w_951",
                DisplayName = "95-1 automatic rifle",
                Type = WeaponType.AssaultRifle,
                MagazineCapacity = 30,
                MaxReserveAmmo = 120,
                Recoil = RecoilLevel.Medium,
                ApplicableModes = new[] { TrainingMode.Trench, TrainingMode.Urban }
            };
            yield return new WeaponDefinitionDto
            {
                WeaponId = "w_qbs09",
                DisplayName = "QBS-09 shotgun",
                Type = WeaponType.Shotgun,
                MagazineCapacity = 8,
                MaxReserveAmmo = 40,
                Recoil = RecoilLevel.High,
                ApplicableModes = new[] { TrainingMode.Trench, TrainingMode.Urban }
            };
            yield return new WeaponDefinitionDto
            {
                WeaponId = "w_qjb201",
                DisplayName = "QJB-201 light machine gun",
                Type = WeaponType.LightMachineGun,
                MagazineCapacity = 75,
                MaxReserveAmmo = 150,
                Recoil = RecoilLevel.High,
                ApplicableModes = new[] { TrainingMode.Trench, TrainingMode.Urban }
            };
        }

        bool TryGetSession(string sessionId, out SessionWeaponState state, out ServiceFailure failure)
        {
            if (string.IsNullOrWhiteSpace(sessionId) || !sessions.TryGetValue(sessionId, out state))
            {
                state = null;
                failure = new ServiceFailure(ErrorCode.InvalidState, "weapon session is not initialized");
                return false;
            }

            failure = default;
            return true;
        }

        static bool CanShoot(SessionWeaponState state)
        {
            return state.CurrentMagazine > 0 && !state.IsReloading;
        }

        static WeaponShotResultDto BuildShotResult(
            WeaponFireInputDto input,
            SessionWeaponState state,
            bool isValidShot,
            ErrorCode errorCode,
            string message)
        {
            var direction = input.AimDirection.sqrMagnitude > 0.0001f ? input.AimDirection.normalized : Vector3.zero;
            return new WeaponShotResultDto
            {
                SessionId = state.SessionId,
                WeaponId = state.Weapon.WeaponId,
                IsValidShot = isValidShot,
                CurrentMagazine = state.CurrentMagazine,
                ReserveAmmo = state.ReserveAmmo,
                MuzzlePosition = input.MuzzlePosition,
                AimDirection = direction,
                Hit = isValidShot && input.Hit,
                HitPoint = input.HitPoint,
                HitObjectId = input.HitObjectId ?? string.Empty,
                AimMode = state.AimMode,
                ShoulderSide = state.ShoulderSide,
                ErrorCode = errorCode,
                Message = message ?? string.Empty
            };
        }

        static WeaponControlStateDto ToStateDto(SessionWeaponState state)
        {
            return new WeaponControlStateDto
            {
                SessionId = state.SessionId,
                WeaponId = state.Weapon.WeaponId,
                CurrentMagazine = state.CurrentMagazine,
                ReserveAmmo = state.ReserveAmmo,
                CanShoot = CanShoot(state),
                ShoulderSide = state.ShoulderSide,
                AimMode = state.AimMode,
                TwoHandGripActive = state.TwoHandGripActive,
                Stability01 = state.Stability01
            };
        }

        static AmmoDto ToAmmoDto(SessionWeaponState state)
        {
            return new AmmoDto
            {
                CurrentMagazine = state.CurrentMagazine,
                ReserveAmmo = state.ReserveAmmo,
                MagazineCapacity = state.Weapon.MagazineCapacity,
                IsReloading = state.IsReloading
            };
        }

        static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        sealed class SessionWeaponState
        {
            public string SessionId;
            public WeaponDefinitionDto Weapon;
            public int CurrentMagazine;
            public int ReserveAmmo;
            public bool IsReloading;
            public ShoulderSide ShoulderSide;
            public WeaponAimMode AimMode;
            public bool TwoHandGripActive;
            public float Stability01;
        }

        readonly struct ServiceFailure
        {
            public ServiceFailure(ErrorCode errorCode, string message)
            {
                ErrorCode = errorCode;
                Message = message;
            }

            public ErrorCode ErrorCode { get; }
            public string Message { get; }
        }
    }
}
