using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SimulatedShooting.Scene
{
    // Local, replaceable inspection input. Production locomotion is supplied by the gameplay track.
    [RequireComponent(typeof(CharacterController))]
    public sealed class CombatSceneWalker : MonoBehaviour
    {
        public Camera View;
        public bool InputEnabled = true;
        public bool ObservationMode { get; private set; }
        float pitch;
        float fall;

        public void SetObservationMode(bool enabled)
        {
            ObservationMode = enabled;
            fall = 0;
            var controller = GetComponent<CharacterController>();
            if (enabled)
            {
                controller.enabled = false;
                return;
            }

            if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out var hit, 100, ~0, QueryTriggerInteraction.Ignore))
                transform.position = hit.point + Vector3.up * .05f;
            controller.enabled = true;
        }

        public void JumpToObservationPoint(Vector3 position, Vector3 lookAt)
        {
            SetObservationMode(true);
            transform.position = position;
            var direction = (lookAt - View.transform.position).normalized;
            transform.rotation = Quaternion.Euler(0, Quaternion.LookRotation(direction).eulerAngles.y, 0);
            pitch = -Mathf.Asin(direction.y) * Mathf.Rad2Deg;
            View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }

        public void Move(Vector2 axes, Vector2 look)
        {
            transform.Rotate(0, look.x, 0);
            pitch = Mathf.Clamp(pitch - look.y, -80, 80);
            View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            var controller = GetComponent<CharacterController>();
            fall = controller.isGrounded ? -2 : fall - 20 * Time.deltaTime;
            controller.Move(((transform.forward * axes.y + transform.right * axes.x) * 3.5f + Vector3.up * fall) * Time.deltaTime);
        }

        void Update()
        {
            if (!InputEnabled || !View.enabled) return;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; var m = Mouse.current;
            if (k == null || m == null) return;
            if (k.oKey.wasPressedThisFrame) SetObservationMode(!ObservationMode);
            var axes = new Vector2((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0), (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
            var look = m.rightButton.isPressed ? m.delta.ReadValue() * 0.12f : Vector2.zero;
            if (!ObservationMode)
            {
                Move(Vector2.ClampMagnitude(axes, 1), look);
                return;
            }

            transform.Rotate(0, look.x, 0);
            pitch = Mathf.Clamp(pitch - look.y, -80, 80);
            View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            var vertical = (k.spaceKey.isPressed ? 1 : 0) - (k.leftCtrlKey.isPressed ? 1 : 0);
            var speed = k.leftShiftKey.isPressed ? 30 : 12;
            var movement = View.transform.forward * axes.y + View.transform.right * axes.x + Vector3.up * vertical;
            transform.position += Vector3.ClampMagnitude(movement, 1) * speed * Time.deltaTime;
#endif
        }
    }
}
