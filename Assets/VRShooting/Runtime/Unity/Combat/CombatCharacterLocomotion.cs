using UnityEngine;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Unity.Combat
{
    /// <summary>Ground movement through CharacterController collisions. Real VR never writes the eye pose.</summary>
    public sealed class CombatCharacterLocomotion : ICombatLocomotionPort
    {
        readonly CharacterController body;
        readonly Transform simulatedEye;
        public CombatCharacterLocomotion(CharacterController body, Transform simulatedEye = null)
        { this.body = body; this.simulatedEye = simulatedEye; }
        public ServiceResult<Unit> Apply(CombatLocomotionIntentDto intent)
        {
            if (body == null || !body.enabled || !body.gameObject.activeInHierarchy)
                return ServiceResult<Unit>.Fail(ErrorCode.ResourceUnavailable, "Character controller unavailable");
            if (!Finite(intent.LocalVelocity.x) || !Finite(intent.LocalVelocity.y) || !Finite(intent.LocalVelocity.z) ||
                !Finite(intent.SnapTurnDegrees) || !Finite(intent.DeltaSeconds) || intent.DeltaSeconds < 0 ||
                !Finite(intent.BodyHeight) || (intent.SimulatedEyeHeight.HasValue && !Finite(intent.SimulatedEyeHeight.Value)))
                return ServiceResult<Unit>.Fail(ErrorCode.InvalidInput);
            if (intent.BodyHeight > 0)
            {
                body.height = Mathf.Max(body.radius * 2, intent.BodyHeight);
                body.center = new Vector3(0, body.height / 2, 0);
            }
            if (intent.SimulatedEyeHeight.HasValue && simulatedEye != null)
            { var p = simulatedEye.localPosition; p.y = intent.SimulatedEyeHeight.Value; simulatedEye.localPosition = p; }
            body.transform.Rotate(0, intent.SnapTurnDegrees, 0);
            var velocity = body.transform.TransformDirection(intent.LocalVelocity);
            // A small downward motion keeps the capsule grounded on ramps; no teleport or climb action.
            if (intent.LocalVelocity.sqrMagnitude > 0) velocity.y = -2f;
            body.Move(velocity * intent.DeltaSeconds);
            return ServiceResult<Unit>.Ok(Unit.Value);
        }
        static bool Finite(float x) => !float.IsNaN(x) && !float.IsInfinity(x);
    }
}
