using System;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Input
{
    public sealed class XRTrainingInputCommandDispatcher
    {
        readonly IXRTrainingInput input;
        readonly IGameEventBus eventBus;
        readonly IUIRouter router;

        public XRTrainingInputCommandDispatcher(
            IXRTrainingInput input,
            IGameEventBus eventBus,
            IUIRouter router = null)
        {
            this.input = input ?? throw new ArgumentNullException(nameof(input));
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            this.router = router;
        }

        public ServiceResult<int> ProcessFrame(XRTrainingInputDispatchContext context = default)
        {
            var sourceScreen = ResolveSourceScreen(context);
            var commandCount = 0;

            if (input.ConfirmPressed)
            {
                Publish(XRTrainingInputCommandType.Confirm, sourceScreen);
                commandCount++;

                if (router != null && context.ConfirmUIEvent.HasValue)
                {
                    var result = router.HandleUIEvent(
                        context.ConfirmUIEvent.Value,
                        sourceScreen,
                        context.ConfirmNavigationArgs);
                    if (!result.Success)
                    {
                        return ServiceResult<int>.Fail(result.ErrorCode, result.Message, commandCount);
                    }
                }
            }

            if (input.BackPressed)
            {
                Publish(XRTrainingInputCommandType.Back, sourceScreen);
                commandCount++;

                if (router != null)
                {
                    var result = router.HandleUIEvent(UIEventId.Common_Back, sourceScreen);
                    if (!result.Success)
                    {
                        return ServiceResult<int>.Fail(result.ErrorCode, result.Message, commandCount);
                    }
                }
            }

            if (input.TriggerPressed)
            {
                Publish(XRTrainingInputCommandType.Trigger, sourceScreen);
                commandCount++;
            }

            if (input.ReloadPressed)
            {
                Publish(XRTrainingInputCommandType.Reload, sourceScreen);
                commandCount++;
            }

            if (input.SwitchShoulderPressed)
            {
                Publish(XRTrainingInputCommandType.SwitchShoulder, sourceScreen);
                commandCount++;
            }

            if (input.AimPressed)
            {
                Publish(XRTrainingInputCommandType.AimPressed, sourceScreen);
                commandCount++;
            }

            if (input.AimHeld)
            {
                Publish(XRTrainingInputCommandType.AimHeld, sourceScreen);
                commandCount++;
            }

            if (input.CommandMenuHeld)
            {
                Publish(XRTrainingInputCommandType.CommandMenuHeld, sourceScreen);
                commandCount++;
            }

            commandCount += PublishGripEdges(sourceScreen);

            return ServiceResult<int>.Ok(commandCount);
        }

        int PublishGripEdges(ScreenId sourceScreen)
        {
            var commandCount = 0;
            if (input.RightGripPressed)
            {
                Publish(XRTrainingInputCommandType.RightGripPressed, sourceScreen);
                commandCount++;
            }

            if (input.RightGripReleased)
            {
                Publish(XRTrainingInputCommandType.RightGripReleased, sourceScreen);
                commandCount++;
            }

            if (input.LeftGripPressed)
            {
                Publish(XRTrainingInputCommandType.LeftGripPressed, sourceScreen);
                commandCount++;
            }

            if (input.LeftGripReleased)
            {
                Publish(XRTrainingInputCommandType.LeftGripReleased, sourceScreen);
                commandCount++;
            }

            return commandCount;
        }

        ScreenId ResolveSourceScreen(XRTrainingInputDispatchContext context)
        {
            if (context.SourceScreen.HasValue)
            {
                return context.SourceScreen.Value;
            }

            return router != null ? router.Current : ScreenId.MainMenu;
        }

        void Publish(XRTrainingInputCommandType commandType, ScreenId sourceScreen)
        {
            eventBus.Publish(new XRTrainingInputCommandEvent
            {
                CommandType = commandType,
                SourceScreen = sourceScreen,
                MoveAxis = input.MoveAxis,
                TurnAxis = input.TurnAxis,
                AimHeld = input.AimHeld,
                CommandMenuHeld = input.CommandMenuHeld,
                RightTriggerValue = input.RightTriggerValue,
                RightGripHeld = input.RightGripHeld,
                LeftGripHeld = input.LeftGripHeld
            });
        }
    }
}

