using VRShooting.Common;

namespace VRShooting.Application.Events
{
    public readonly struct MovingTargetStateChangedEvent
    {
        public MovingTargetSessionDto Session { get; init; }
    }

    public readonly struct MovingTargetShotRecordedEvent
    {
        public string SessionId { get; init; }
        public MovingTargetShotRecordDto Shot { get; init; }
    }

    public readonly struct MovingTargetCountdownElapsedEvent
    {
        public string SessionId { get; init; }
        public MovingTargetSessionDto Session { get; init; }
    }

    public readonly struct MovingTargetSessionCompletedEvent
    {
        public string SessionId { get; init; }
        public MovingTargetResultDto Result { get; init; }
    }

    public readonly struct MovingTargetFireSequenceCompletedEvent
    {
        public string SessionId { get; init; }
        public FireSequenceRecordDto Sequence { get; init; }
    }
}
