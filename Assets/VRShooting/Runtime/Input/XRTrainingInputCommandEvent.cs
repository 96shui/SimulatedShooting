using UnityEngine;
using VRShooting.Common;

namespace VRShooting.Input
{
    public readonly struct XRTrainingInputCommandEvent
    {
        public XRTrainingInputCommandType CommandType { get; init; }

        public ScreenId SourceScreen { get; init; }

        public Vector2 MoveAxis { get; init; }

        public Vector2 TurnAxis { get; init; }

        public bool AimHeld { get; init; }

        public bool CommandMenuHeld { get; init; }

        public float RightTriggerValue { get; init; }

        public bool RightGripHeld { get; init; }

        public bool LeftGripHeld { get; init; }
    }
}

