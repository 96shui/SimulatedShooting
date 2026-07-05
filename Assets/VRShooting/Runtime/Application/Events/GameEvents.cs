using System;
using VRShooting.Common;

namespace VRShooting.Application.Events
{
    public readonly struct ScreenChangedEvent
    {
        public ScreenId PreviousScreen { get; init; }
        public ScreenId CurrentScreen { get; init; }
        public NavigationArgs Args { get; init; }
    }

    public readonly struct SessionStartedEvent
    {
        public TrainingSessionDto Session { get; init; }
    }

    public readonly struct SessionEndedEvent
    {
        public TrainingSessionDto Session { get; init; }
        public TrainingResultDto Result { get; init; }
        public SessionEndReason Reason { get; init; }
    }
}
