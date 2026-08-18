using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;

namespace VRShooting.Input
{
    public sealed class InputSystemXRTrainingInput : IXRTrainingInput
    {
        int lastXrFrame = -1;
        bool xrTriggerHeld;
        bool xrTriggerPressed;
        bool xrTriggerReleased;

        public bool ConfirmPressed =>
            WasPressedThisFrame(Keyboard.current?.enterKey) ||
            WasPressedThisFrame(Keyboard.current?.spaceKey) ||
            WasPressedThisFrame(Gamepad.current?.buttonSouth);

        public bool BackPressed =>
            WasPressedThisFrame(Keyboard.current?.escapeKey) ||
            WasPressedThisFrame(Gamepad.current?.buttonEast);

        public bool TriggerPressed =>
            ReadXrTriggerPressed() ||
            WasPressedThisFrame(Mouse.current?.leftButton) ||
            WasPressedThisFrame(Keyboard.current?.fKey) ||
            WasPressedThisFrame(Gamepad.current?.rightTrigger);

        public bool TriggerHeld =>
            ReadXrTriggerHeld() ||
            IsPressed(Mouse.current?.leftButton) ||
            IsPressed(Keyboard.current?.fKey) ||
            IsPressed(Gamepad.current?.rightTrigger);

        public bool TriggerReleased =>
            ReadXrTriggerReleased() ||
            WasReleasedThisFrame(Mouse.current?.leftButton) ||
            WasReleasedThisFrame(Keyboard.current?.fKey) ||
            WasReleasedThisFrame(Gamepad.current?.rightTrigger);

        public float RightTriggerValue => Mathf.Max(
            ReadXrAxis(XRController.rightHand, "trigger"),
            Gamepad.current != null ? Gamepad.current.rightTrigger.ReadValue() : 0f);

        public bool RightGripPressed =>
            WasPressedThisFrame(ReadXrButton(XRController.rightHand, "gripPressed")) ||
            WasPressedThisFrame(Keyboard.current?.eKey);

        public bool RightGripHeld =>
            IsPressed(ReadXrButton(XRController.rightHand, "gripPressed")) ||
            IsPressed(Keyboard.current?.eKey);

        public bool RightGripReleased =>
            WasReleasedThisFrame(ReadXrButton(XRController.rightHand, "gripPressed")) ||
            WasReleasedThisFrame(Keyboard.current?.eKey);

        public bool LeftGripPressed =>
            WasPressedThisFrame(ReadXrButton(XRController.leftHand, "gripPressed")) ||
            WasPressedThisFrame(Keyboard.current?.gKey);

        public bool LeftGripHeld =>
            IsPressed(ReadXrButton(XRController.leftHand, "gripPressed")) ||
            IsPressed(Keyboard.current?.gKey);

        public bool LeftGripReleased =>
            WasReleasedThisFrame(ReadXrButton(XRController.leftHand, "gripPressed")) ||
            WasReleasedThisFrame(Keyboard.current?.gKey);

        public bool ReloadPressed =>
            WasPressedThisFrame(Keyboard.current?.rKey) ||
            WasPressedThisFrame(Gamepad.current?.buttonWest);

        public bool SwitchShoulderPressed =>
            WasPressedThisFrame(Keyboard.current?.qKey) ||
            WasPressedThisFrame(Gamepad.current?.rightShoulder);

        public bool AimPressed =>
            WasPressedThisFrame(Mouse.current?.rightButton) ||
            WasPressedThisFrame(Keyboard.current?.leftShiftKey) ||
            WasPressedThisFrame(Gamepad.current?.leftTrigger);

        public bool AimHeld =>
            IsPressed(Mouse.current?.rightButton) ||
            IsPressed(Keyboard.current?.leftShiftKey) ||
            IsPressed(Gamepad.current?.leftTrigger);

        public bool CommandMenuHeld =>
            IsPressed(Keyboard.current?.tabKey) ||
            IsPressed(Gamepad.current?.leftShoulder);

        public Vector2 TurnAxis
        {
            get
            {
                var gamepadValue = Gamepad.current != null ? Gamepad.current.rightStick.ReadValue() : Vector2.zero;
                if (gamepadValue.sqrMagnitude > 0f)
                {
                    return gamepadValue;
                }

                var mouseValue = ReadMouseTurnAxis();
                if (mouseValue.sqrMagnitude > 0f)
                {
                    return mouseValue;
                }

                return ReadKeyboardTurnAxis();
            }
        }

        public Vector2 MoveAxis
        {
            get
            {
                var gamepadValue = Gamepad.current != null ? Gamepad.current.leftStick.ReadValue() : Vector2.zero;
                if (gamepadValue.sqrMagnitude > 0f)
                {
                    return gamepadValue;
                }

                return ReadKeyboardMoveAxis();
            }
        }

        static bool WasPressedThisFrame(ButtonControl control)
        {
            return control != null && control.wasPressedThisFrame;
        }

        static bool IsPressed(ButtonControl control)
        {
            return control != null && control.isPressed;
        }

        static bool WasReleasedThisFrame(ButtonControl control)
        {
            return control != null && control.wasReleasedThisFrame;
        }

        bool ReadXrTriggerPressed()
        {
            UpdateXrTriggerState();
            return xrTriggerPressed;
        }

        bool ReadXrTriggerHeld()
        {
            UpdateXrTriggerState();
            return xrTriggerHeld;
        }

        bool ReadXrTriggerReleased()
        {
            UpdateXrTriggerState();
            return xrTriggerReleased;
        }

        void UpdateXrTriggerState()
        {
            if (lastXrFrame == Time.frameCount)
            {
                return;
            }

            lastXrFrame = Time.frameCount;
            xrTriggerPressed = false;
            xrTriggerReleased = false;
            var value = ReadXrAxis(XRController.rightHand, "trigger");
            if (WeaponTriggerHysteresis.CrossedPress(xrTriggerHeld, value))
            {
                xrTriggerHeld = true;
                xrTriggerPressed = true;
            }
            else if (WeaponTriggerHysteresis.CrossedRelease(xrTriggerHeld, value))
            {
                xrTriggerHeld = false;
                xrTriggerReleased = true;
            }
        }

        static ButtonControl ReadXrButton(XRController controller, string controlName)
        {
            return controller?.TryGetChildControl<ButtonControl>(controlName);
        }

        static float ReadXrAxis(XRController controller, string controlName)
        {
            var axis = controller?.TryGetChildControl<AxisControl>(controlName);
            if (axis != null)
            {
                return Mathf.Clamp01(axis.ReadValue());
            }

            var button = ReadXrButton(controller, controlName + "Pressed");
            return button != null ? button.ReadValue() : 0f;
        }

        static Vector2 ReadKeyboardMoveAxis()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            var value = Vector2.zero;
            if (keyboard.aKey.isPressed)
            {
                value.x -= 1f;
            }

            if (keyboard.dKey.isPressed)
            {
                value.x += 1f;
            }

            if (keyboard.sKey.isPressed)
            {
                value.y -= 1f;
            }

            if (keyboard.wKey.isPressed)
            {
                value.y += 1f;
            }

            return Vector2.ClampMagnitude(value, 1f);
        }

        static Vector2 ReadKeyboardTurnAxis()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            var value = Vector2.zero;
            if (keyboard.leftArrowKey.isPressed)
            {
                value.x -= 1f;
            }

            if (keyboard.rightArrowKey.isPressed)
            {
                value.x += 1f;
            }

            if (keyboard.downArrowKey.isPressed)
            {
                value.y -= 1f;
            }

            if (keyboard.upArrowKey.isPressed)
            {
                value.y += 1f;
            }

            return Vector2.ClampMagnitude(value, 1f);
        }

        static Vector2 ReadMouseTurnAxis()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return Vector2.zero;
            }

            return Vector2.ClampMagnitude(mouse.delta.ReadValue() * 0.08f, 1f);
        }
    }
}

