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
        float pitch;
        float fall;
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
            var axes = new Vector2((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0), (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
            Move(Vector2.ClampMagnitude(axes, 1), m.rightButton.isPressed ? m.delta.ReadValue() * 0.12f : Vector2.zero);
#endif
        }
    }
}
