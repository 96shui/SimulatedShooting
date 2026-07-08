using VRShooting.Common;

namespace VRShooting.Application.Events
{
    public readonly struct ZeroingShotRecordedEvent
    {
        public string SessionId { get; init; }
        public ZeroingShotDto Shot { get; init; }
    }

    public readonly struct ZeroingRoundCompletedEvent
    {
        public string SessionId { get; init; }
        public ZeroingRoundAnalysisDto Analysis { get; init; }
    }

    public readonly struct ZeroingAdjustmentAppliedEvent
    {
        public string SessionId { get; init; }
        public ZeroingRoundAnalysisDto Analysis { get; init; }
    }

    public readonly struct ZeroingRoundStartedEvent
    {
        public string SessionId { get; init; }
        public ZeroingSessionDto Session { get; init; }
    }
}
