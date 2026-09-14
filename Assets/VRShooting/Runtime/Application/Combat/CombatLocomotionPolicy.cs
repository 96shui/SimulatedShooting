using UnityEngine;
using VRShooting.Common;

namespace VRShooting.Application.Combat
{
    public static class CombatLocomotionPolicy
    {
        public static TrainingLocomotionPolicyDto Get(TrainingMode mode, bool active) => new TrainingLocomotionPolicyDto
        {
            Mode = mode, Posture = IsCombat(mode) ? TrainingPostureMode.CombatFree : TrainingPostureMode.ProneFixed,
            AllowContinuousMove = IsCombat(mode) && active, AllowArtificialTurn = IsCombat(mode) && active,
            AllowTeleport = false, AllowRoomScaleHeadAndHandTracking = true
        };
        public static CombatLocomotionIntentDto Intent(TrainingMode mode, bool active, PlayerPosture posture,
            Vector2 move, float snap, bool realVr, float deltaSeconds, CombatConfigDto config)
        {
            var policy = Get(mode, active);
            var height = posture == PlayerPosture.Standing ? config.StandingEyeHeight : posture == PlayerPosture.Crouching ? config.CrouchingEyeHeight : config.ProneEyeHeight;
            var speed = posture == PlayerPosture.Standing ? config.StandingSpeed : posture == PlayerPosture.Crouching ? config.CrouchingSpeed : config.ProneSpeed;
            var axis = Vector2.ClampMagnitude(move, 1);
            return new CombatLocomotionIntentDto
            {
                LocalVelocity = policy.AllowContinuousMove ? new Vector3(axis.x, 0, axis.y) * speed : Vector3.zero,
                SnapTurnDegrees = policy.AllowArtificialTurn ? snap : 0, Posture = posture,
                SimulatedEyeHeight = realVr ? (float?)null : height,
                BodyHeight = height + .15f, DeltaSeconds = deltaSeconds
            };
        }
        static bool IsCombat(TrainingMode mode) => mode == TrainingMode.Trench || mode == TrainingMode.Urban;
    }
}
