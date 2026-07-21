using System;
using UnityEngine;
using VRShooting.Common;

namespace VRShooting.Application.Weapons
{
    public static class WeaponRecoilRules
    {
        public static WeaponRecoilImpulseDto Compute(
            string sessionId,
            int shotSequence,
            float stability01,
            RecoilLevel recoilLevel)
        {
            var stability = Mathf.Clamp01(stability01);
            var controlMultiplier = Mathf.Clamp(Mathf.Lerp(1.45f, 0.85f, stability), 0.8f, 1.45f);
            var levelMultiplier = ResolveLevelMultiplier(recoilLevel);
            var lateralSample = ResolveDeterministicLateral(sessionId, shotSequence);
            var yawMagnitude = Mathf.Lerp(0.15f, 0.60f, Mathf.Abs(lateralSample));

            return new WeaponRecoilImpulseDto
            {
                PitchDegrees = Mathf.Clamp(3.1f * levelMultiplier * controlMultiplier, 2.5f, 4f),
                YawDegrees = Mathf.Sign(lateralSample) * yawMagnitude * levelMultiplier,
                RollDegrees = -Mathf.Sign(lateralSample) * 0.12f * levelMultiplier,
                RearwardMeters = Mathf.Clamp(0.027f * levelMultiplier * controlMultiplier, 0.02f, 0.04f),
                UpwardMeters = Mathf.Clamp(0.012f * levelMultiplier * controlMultiplier, 0.008f, 0.02f),
                ControlMultiplier = controlMultiplier,
                KickDurationSeconds = 0.055f,
                SettleDurationSeconds = Mathf.Lerp(0.34f, 0.24f, stability),
                NoVrCameraPitchDegrees = Mathf.Clamp(0.32f * controlMultiplier, 0.15f, 0.50f),
                NoVrCameraYawDegrees = Mathf.Clamp(lateralSample * 0.16f, -0.20f, 0.20f),
                RearHandHapticAmplitude01 = 0.68f,
                FrontHandHapticAmplitude01 = 0.28f,
                HapticDurationSeconds = 0.055f
            };
        }

        public static float ResolveDeterministicLateral(string sessionId, int shotSequence)
        {
            unchecked
            {
                var hash = 17;
                var value = sessionId ?? string.Empty;
                for (var index = 0; index < value.Length; index++)
                {
                    hash = hash * 31 + value[index];
                }

                hash = hash * 31 + Math.Max(1, shotSequence);
                hash ^= hash << 13;
                hash ^= hash >> 17;
                hash ^= hash << 5;
                var normalized = (hash & 0x7fffffff) / (float)int.MaxValue;
                var signed = normalized * 2f - 1f;
                if (Mathf.Abs(signed) < 0.05f)
                {
                    signed = shotSequence % 2 == 0 ? 0.05f : -0.05f;
                }

                return Mathf.Clamp(signed, -1f, 1f);
            }
        }

        static float ResolveLevelMultiplier(RecoilLevel recoilLevel)
        {
            switch (recoilLevel)
            {
                case RecoilLevel.Low:
                    return 0.88f;
                case RecoilLevel.High:
                    return 1.15f;
                default:
                    return 1f;
            }
        }
    }
}
