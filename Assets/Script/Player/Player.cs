using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] Transform headTransform;

    void Awake()
    {
        if (headTransform != null)
        {
            return;
        }

        var camera = GetComponentInChildren<Camera>();
        if (camera != null)
        {
            headTransform = camera.transform;
        }
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        var input = Vector2.zero;
        if (keyboard.wKey.isPressed)
        {
            input.y += 1f;
        }

        if (keyboard.sKey.isPressed)
        {
            input.y -= 1f;
        }

        if (keyboard.dKey.isPressed)
        {
            input.x += 1f;
        }

        if (keyboard.aKey.isPressed)
        {
            input.x -= 1f;
        }

        if (input.sqrMagnitude < 0.01f)
        {
            return;
        }

        input.Normalize();

        var forward = headTransform != null ? headTransform.forward : transform.forward;
        var right = headTransform != null ? headTransform.right : transform.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        var moveDirection = forward * input.y + right * input.x;
        transform.position += moveDirection * (moveSpeed * Time.deltaTime);
    }
}
