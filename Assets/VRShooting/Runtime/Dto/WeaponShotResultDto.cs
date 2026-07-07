using UnityEngine;
using VRShooting.Contracts;

namespace VRShooting.Common
{
    public readonly struct WeaponShotResultDto
    {
        public string SessionId { get; init; }
        public string WeaponId { get; init; }
        public bool IsValidShot { get; init; }
        public int CurrentMagazine { get; init; }
        public int ReserveAmmo { get; init; }
        public Vector3 MuzzlePosition { get; init; }
        public Vector3 AimDirection { get; init; }
        public bool Hit { get; init; }
        public Vector3 HitPoint { get; init; }
        public string HitObjectId { get; init; }
        public WeaponAimMode AimMode { get; init; }
        public ShoulderSide ShoulderSide { get; init; }
        public ErrorCode ErrorCode { get; init; }
        public string Message { get; init; }
    }
}
