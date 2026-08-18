using VRShooting.Common;

namespace VRShooting.Application.Events
{
    public readonly struct WeaponChangedEvent
    {
        public WeaponDefinitionDto Weapon { get; init; }
    }

    public readonly struct WeaponStateChangedEvent
    {
        public WeaponControlStateDto State { get; init; }
    }

    public readonly struct WeaponHoldStateChangedEvent
    {
        public string SessionId { get; init; }
        public WeaponHoldState PreviousState { get; init; }
        public WeaponHoldState CurrentState { get; init; }
        public bool RearHandTracked { get; init; }
        public bool FrontHandTracked { get; init; }
    }

    public readonly struct WeaponShotResultEvent
    {
        public WeaponShotResultDto Result { get; init; }
    }

    public readonly struct WeaponFireSequenceChangedEvent
    {
        public WeaponFireSequenceStateDto State { get; init; }
    }

    public readonly struct AmmoChangedEvent
    {
        public string SessionId { get; init; }
        public AmmoDto Ammo { get; init; }
    }

    public readonly struct ReloadStartedEvent
    {
        public string SessionId { get; init; }
    }

    public readonly struct ReloadCompletedEvent
    {
        public string SessionId { get; init; }
        public AmmoDto Ammo { get; init; }
    }

    public readonly struct ShoulderChangedEvent
    {
        public string SessionId { get; init; }
        public ShoulderSide ShoulderSide { get; init; }
    }

    public readonly struct AimModeChangedEvent
    {
        public string SessionId { get; init; }
        public WeaponAimMode AimMode { get; init; }
    }
}
