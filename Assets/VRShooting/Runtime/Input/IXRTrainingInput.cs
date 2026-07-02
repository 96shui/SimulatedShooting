using UnityEngine;

namespace VRShooting.Input
{
    public interface IXRTrainingInput
    {
        bool ConfirmPressed { get; }

        bool BackPressed { get; }

        bool TriggerPressed { get; }

        bool ReloadPressed { get; }

        bool SwitchShoulderPressed { get; }

        bool CommandMenuHeld { get; }

        Vector2 TurnAxis { get; }

        Vector2 MoveAxis { get; }
    }
}

