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
        bool commandMenuHeld;

        bool confirmPressed;
        bool backPressed;
        bool triggerPressed;
        bool reloadPressed;
        bool switchShoulderPressed;

        public bool ConfirmPressed => confirmPressed;

        public bool BackPressed => backPressed;

        public bool TriggerPressed => triggerPressed;

        public bool ReloadPressed => reloadPressed;

        public bool SwitchShoulderPressed => switchShoulderPressed;

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
                    SetEdgeState(ref triggerHeld, ref triggerPressed, isHeld);
                    break;
                case XRTrainingInputButton.Reload:
                    SetEdgeState(ref reloadHeld, ref reloadPressed, isHeld);
                    break;
                case XRTrainingInputButton.SwitchShoulder:
                    SetEdgeState(ref switchShoulderHeld, ref switchShoulderPressed, isHeld);
                    break;
                case XRTrainingInputButton.CommandMenu:
                    commandMenuHeld = isHeld;
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

        public void AdvanceFrame()
        {
            confirmPressed = false;
            backPressed = false;
            triggerPressed = false;
            reloadPressed = false;
            switchShoulderPressed = false;
        }

        public void Clear()
        {
            confirmHeld = false;
            backHeld = false;
            triggerHeld = false;
            reloadHeld = false;
            switchShoulderHeld = false;
            commandMenuHeld = false;
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
    }
}

