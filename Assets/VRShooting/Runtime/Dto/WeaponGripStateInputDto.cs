namespace VRShooting.Common
{
    public readonly struct WeaponGripStateInputDto
    {
        public string SessionId { get; init; }
        public WeaponHoldState HoldState { get; init; }
        public bool RearHandTracked { get; init; }
        public bool FrontHandTracked { get; init; }
        public float Stability01 { get; init; }
    }
}
