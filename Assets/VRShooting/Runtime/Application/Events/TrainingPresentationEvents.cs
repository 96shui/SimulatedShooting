using VRShooting.Common;

namespace VRShooting.Application.Events
{
    public readonly struct TrainingPresentationChangedEvent
    {
        public TrainingPresentationDto Presentation { get; init; }
    }

    public readonly struct TrainingWeaponPickupEvent
    {
        public string SessionId { get; init; }
        public string WeaponId { get; init; }
        public WeaponHoldState PreviousState { get; init; }
        public WeaponHoldState CurrentState { get; init; }
    }

    public readonly struct MovingTargetCountdownRequestedEvent
    {
        public string SessionId { get; init; }
        public TrainingPresentationDto Presentation { get; init; }
        public float CountdownSeconds { get; init; }
    }
}
