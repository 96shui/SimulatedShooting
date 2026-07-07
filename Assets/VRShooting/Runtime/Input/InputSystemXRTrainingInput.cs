using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace VRShooting.Input
{
    public sealed class InputSystemXRTrainingInput : IXRTrainingInput
    {
        public bool ConfirmPressed =>
            WasPressedThisFrame(Keyboard.current?.enterKey) ||
            WasPressedThisFrame(Keyboard.current?.spaceKey) ||
            WasPressedThisFrame(Gamepad.current?.buttonSouth);

        public bool BackPressed =>
            WasPressedThisFrame(Keyboard.current?.escapeKey) ||
            WasPressedThisFrame(Gamepad.current?.buttonEast);

        public bool TriggerPressed =>
            WasPressedThisFrame(Mouse.current?.leftButton) ||
            WasPressedThisFrame(Keyboard.current?.fKey) ||
            WasPressedThisFrame(Gamepad.current?.rightTrigger);

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

