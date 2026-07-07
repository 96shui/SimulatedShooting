using UnityEngine;

namespace VRShooting.Common
{
    public readonly struct WeaponFireInputDto
    {
        public string SessionId { get; init; }
        public Vector3 MuzzlePosition { get; init; }
        public Vector3 AimDirection { get; init; }
        public Vector3 WeaponPosition { get; init; }
        public float Stability01 { get; init; }
        public bool TwoHandGripActive { get; init; }
        public WeaponAimMode AimMode { get; init; }
        public ShoulderSide ShoulderSide { get; init; }
        public bool Hit { get; init; }
        public Vector3 HitPoint { get; init; }
        public string HitObjectId { get; init; }
    }
}
