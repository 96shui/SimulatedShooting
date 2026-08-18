using System;
using UnityEngine;

namespace VRShooting.Input
{
    public sealed class ManualXRTrainingInput : IXRTrainingInput
    {
        bool confirmHeld;
        bool backHeld;
        bool triggerHeld;
        bool reloadHeld;
        bool switchShoulderHeld;
        bool aimHeld;
        bool commandMenuHeld;
        bool rightGripHeld;
        bool leftGripHeld;

        bool confirmPressed;
        bool backPressed;
        bool triggerPressed;
        bool triggerReleased;
        bool reloadPressed;
        bool switchShoulderPressed;
        bool aimPressed;
        bool rightGripPressed;
        bool rightGripReleased;
        bool leftGripPressed;
        bool leftGripReleased;

        public bool ConfirmPressed => confirmPressed;

        public bool BackPressed => backPressed;

        public bool TriggerPressed => triggerPressed;

        public bool TriggerHeld => triggerHeld;

        public bool TriggerReleased => triggerReleased;

        public float RightTriggerValue { get; private set; }

        public bool RightGripPressed => rightGripPressed;

        public bool RightGripHeld => rightGripHeld;

        public bool RightGripReleased => rightGripReleased;

        public bool LeftGripPressed => leftGripPressed;

        public bool LeftGripHeld => leftGripHeld;

        public bool LeftGripReleased => leftGripReleased;

        public bool ReloadPressed => reloadPressed;

        public bool SwitchShoulderPressed => switchShoulderPressed;

        public bool AimPressed => aimPressed;

        public bool AimHeld => aimHeld;

        public bool CommandMenuHeld => commandMenuHeld;

        public Vector2 TurnAxis { get; private set; }

        public Vector2 MoveAxis { get; private set; }

        public void Press(XRTrainingInputButton button)
        {
            SetButton(button, true);
        }

        public void Release(XRTrainingInputButton button)
        {
            SetButton(button, false);
        }

        public void SetButton(XRTrainingInputButton button, bool isHeld)
        {
            switch (button)
            {
                case XRTrainingInputButton.Confirm:
                    SetEdgeState(ref confirmHeld, ref confirmPressed, isHeld);
                    break;
                case XRTrainingInputButton.Back:
                    SetEdgeState(ref backHeld, ref backPressed, isHeld);
                    break;
                case XRTrainingInputButton.Trigger:
                    SetEdgeState(ref triggerHeld, ref triggerPressed, ref triggerReleased, isHeld);
                    RightTriggerValue = isHeld ? 1f : 0f;
                    break;
                case XRTrainingInputButton.Reload:
                    SetEdgeState(ref reloadHeld, ref reloadPressed, isHeld);
                    break;
                case XRTrainingInputButton.SwitchShoulder:
                    SetEdgeState(ref switchShoulderHeld, ref switchShoulderPressed, isHeld);
                    break;
                case XRTrainingInputButton.Aim:
                    SetEdgeState(ref aimHeld, ref aimPressed, isHeld);
                    break;
                case XRTrainingInputButton.CommandMenu:
                    commandMenuHeld = isHeld;
                    break;
                case XRTrainingInputButton.RightGrip:
                    SetEdgeState(ref rightGripHeld, ref rightGripPressed, ref rightGripReleased, isHeld);
                    break;
                case XRTrainingInputButton.LeftGrip:
                    SetEdgeState(ref leftGripHeld, ref leftGripPressed, ref leftGripReleased, isHeld);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(button), button, "unsupported input button");
            }
        }

        public void SetMoveAxis(Vector2 value)
        {
            MoveAxis = value;
        }

        public void SetTurnAxis(Vector2 value)
        {
            TurnAxis = value;
        }

        public void SetAxes(Vector2 moveAxis, Vector2 turnAxis)
        {
            MoveAxis = moveAxis;
            TurnAxis = turnAxis;
        }

        public void SetRightTriggerValue(float value)
        {
            RightTriggerValue = Mathf.Clamp01(value);
            if (WeaponTriggerHysteresis.CrossedPress(triggerHeld, RightTriggerValue))
            {
                triggerHeld = true;
                triggerPressed = true;
                triggerReleased = false;
            }
            else if (WeaponTriggerHysteresis.CrossedRelease(triggerHeld, RightTriggerValue))
            {
                triggerHeld = false;
                triggerReleased = true;
            }
        }

        public void AdvanceFrame()
        {
            confirmPressed = false;
            backPressed = false;
            triggerPressed = false;
            triggerReleased = false;
            reloadPressed = false;
            switchShoulderPressed = false;
            aimPressed = false;
            rightGripPressed = false;
            rightGripReleased = false;
            leftGripPressed = false;
            leftGripReleased = false;
        }

        public void Clear()
        {
            confirmHeld = false;
            backHeld = false;
            triggerHeld = false;
            reloadHeld = false;
            switchShoulderHeld = false;
            aimHeld = false;
            commandMenuHeld = false;
            rightGripHeld = false;
            leftGripHeld = false;
            RightTriggerValue = 0f;
            AdvanceFrame();
            TurnAxis = Vector2.zero;
            MoveAxis = Vector2.zero;
        }

        static void SetEdgeState(ref bool held, ref bool pressed, bool isHeld)
        {
            if (isHeld)
            {
                if (!held)
                {
                    pressed = true;
                }

                held = true;
                return;
            }

            held = false;
            pressed = false;
        }

        static void SetEdgeState(ref bool held, ref bool pressed, ref bool released, bool isHeld)
        {
            if (isHeld)
            {
                pressed = !held;
                released = false;
                held = true;
                return;
            }

            released = held;
            held = false;
            pressed = false;
        }
    }
}

