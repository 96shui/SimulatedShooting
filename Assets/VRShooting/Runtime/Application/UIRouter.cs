using System.Collections.Generic;
using VRShooting.Application.Events;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application
{
    public sealed class UIRouter : IUIRouter
    {
        readonly IGameEventBus eventBus;
        readonly Stack<ScreenId> backStack = new Stack<ScreenId>();
        bool isTransitioning;

        public UIRouter(IGameEventBus eventBus, ScreenId initialScreen = ScreenId.MainMenu)
        {
            this.eventBus = eventBus;
            Current = initialScreen;
        }

        public ScreenId Current { get; private set; }

        public TrainingMode? SelectedMode { get; private set; }

        public bool IsTransitioning => isTransitioning;

        public ServiceResult<ScreenId> Open(ScreenId screen, NavigationArgs args = default)
        {
            if (isTransitioning)
            {
                return ServiceResult<ScreenId>.Fail(ErrorCode.Busy, "router is transitioning");
            }

            if (args.Mode.HasValue)
            {
                SelectedMode = args.Mode.Value;
            }

            if (Current == screen)
            {
                return ServiceResult<ScreenId>.Ok(Current);
            }

            isTransitioning = true;
            try
            {
                var previous = Current;
                backStack.Push(previous);
                Current = screen;

                eventBus.Publish(new ScreenChangedEvent
                {
                    PreviousScreen = previous,
                    CurrentScreen = Current,
                    Args = args
                });

                return ServiceResult<ScreenId>.Ok(Current);
            }
            finally
            {
                isTransitioning = false;
            }
        }

        public ServiceResult<ScreenId> Back()
        {
            if (isTransitioning)
            {
                return ServiceResult<ScreenId>.Fail(ErrorCode.Busy, "router is transitioning");
            }

            if (backStack.Count == 0)
            {
                return ServiceResult<ScreenId>.Fail(ErrorCode.InvalidState, "no previous screen");
            }

            isTransitioning = true;
            try
            {
                var previous = Current;
                Current = backStack.Pop();

                eventBus.Publish(new ScreenChangedEvent
                {
                    PreviousScreen = previous,
                    CurrentScreen = Current,
                    Args = default
                });

                return ServiceResult<ScreenId>.Ok(Current);
            }
            finally
            {
                isTransitioning = false;
            }
        }

        public ServiceResult<ScreenId> HandleUIEvent(UIEventId eventId, ScreenId sourceScreen, NavigationArgs args = default)
        {
            switch (eventId)
            {
                case UIEventId.MainMenu_OpenZeroing:
                    return Open(ScreenId.ZeroingBriefing, new NavigationArgs
                    {
                        Mode = TrainingMode.Zeroing100m,
                        ReturnToScreen = ScreenId.MainMenu.ToString()
                    });

                case UIEventId.MainMenu_OpenMovingTarget:
                    return Open(ScreenId.MovingTargetSettings, new NavigationArgs
                    {
                        Mode = TrainingMode.MovingTarget,
                        ReturnToScreen = ScreenId.MainMenu.ToString()
                    });

                case UIEventId.MainMenu_OpenTrench:
                    return Open(ScreenId.TrenchMapSelection, new NavigationArgs
                    {
                        Mode = TrainingMode.Trench,
                        ReturnToScreen = ScreenId.MainMenu.ToString()
                    });

                case UIEventId.MainMenu_OpenUrban:
                    return Open(ScreenId.UrbanMapSelection, new NavigationArgs
                    {
                        Mode = TrainingMode.Urban,
                        ReturnToScreen = ScreenId.MainMenu.ToString()
                    });

                case UIEventId.MainMenu_OpenArmory:
                    return Open(ScreenId.Armory, new NavigationArgs
                    {
                        ReturnToScreen = ScreenId.MainMenu.ToString()
                    });

                case UIEventId.MainMenu_OpenSettings:
                    return Open(ScreenId.Settings, new NavigationArgs
                    {
                        ReturnToScreen = ScreenId.MainMenu.ToString()
                    });

                case UIEventId.Zeroing_Start:
                    return Open(ScreenId.ZeroingHud, args);

                case UIEventId.Zeroing_ApplyAdjustment:
                    return Current == ScreenId.ZeroingImpactAnalysis
                        ? ServiceResult<ScreenId>.Ok(Current)
                        : ServiceResult<ScreenId>.Fail(ErrorCode.InvalidState, "apply adjustment is only valid on impact analysis");

                case UIEventId.Zeroing_NextRound:
                    if (args.ReturnToScreen == ScreenId.MainMenu.ToString())
                    {
                        return Open(ScreenId.MainMenu, args);
                    }

                    return Open(args.ReturnToScreen == ScreenId.ZeroingFinalRating.ToString()
                        ? ScreenId.ZeroingFinalRating
                        : ScreenId.ZeroingHud, args);

                case UIEventId.Zeroing_BackToMainMenu:
                    return Open(ScreenId.MainMenu, args);

                case UIEventId.Zeroing_Retry:
                    return Open(ScreenId.ZeroingBriefing, args);

                case UIEventId.Zeroing_BackToModeSelection:
                    return Open(ScreenId.MainMenu, args);

                case UIEventId.Common_Back:
                    return Back();

                default:
                    return ServiceResult<ScreenId>.Fail(
                        ErrorCode.InvalidInput,
                        $"unsupported ui event: {eventId} from {sourceScreen}");
            }
        }
    }
}
