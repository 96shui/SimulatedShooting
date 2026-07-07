namespace VRShooting.Common
{
    public readonly struct WeaponControlStateDto
    {
        public string SessionId { get; init; }
        public string WeaponId { get; init; }
        public int CurrentMagazine { get; init; }
        public int ReserveAmmo { get; init; }
        public bool CanShoot { get; init; }
        public ShoulderSide ShoulderSide { get; init; }
        public WeaponAimMode AimMode { get; init; }
        public bool TwoHandGripActive { get; init; }
        public float Stability01 { get; init; }
    }
}
