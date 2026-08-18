using UnityEngine;

namespace VRShooting.Input
{
    public interface IXRTrainingInput
    {
        bool ConfirmPressed { get; }

        bool BackPressed { get; }

        bool TriggerPressed { get; }

        bool TriggerHeld { get; }

        bool TriggerReleased { get; }

        float RightTriggerValue { get; }

        bool RightGripPressed { get; }

        bool RightGripHeld { get; }

        bool RightGripReleased { get; }

        bool LeftGripPressed { get; }

        bool LeftGripHeld { get; }

        bool LeftGripReleased { get; }

        bool ReloadPressed { get; }

        bool SwitchShoulderPressed { get; }

        bool AimPressed { get; }

        bool AimHeld { get; }

        bool CommandMenuHeld { get; }

        Vector2 TurnAxis { get; }

        Vector2 MoveAxis { get; }
    }
}

