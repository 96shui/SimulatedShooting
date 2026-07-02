using VRShooting.Common;

namespace VRShooting.Input
{
    public readonly struct XRTrainingInputDispatchContext
    {
        public ScreenId? SourceScreen { get; init; }

        public UIEventId? ConfirmUIEvent { get; init; }

        public NavigationArgs ConfirmNavigationArgs { get; init; }
    }
}

