namespace VRShooting.Common
{
    public readonly struct WeaponRecoilImpulseDto
    {
        public float PitchDegrees { get; init; }
        public float YawDegrees { get; init; }
        public float RollDegrees { get; init; }
        public float RearwardMeters { get; init; }
        public float UpwardMeters { get; init; }
        public float ControlMultiplier { get; init; }
        public float KickDurationSeconds { get; init; }
        public float SettleDurationSeconds { get; init; }
        public float NoVrCameraPitchDegrees { get; init; }
        public float NoVrCameraYawDegrees { get; init; }
        public float RearHandHapticAmplitude01 { get; init; }
        public float FrontHandHapticAmplitude01 { get; init; }
        public float HapticDurationSeconds { get; init; }

        public bool HasImpulse => PitchDegrees > 0f || RearwardMeters > 0f || UpwardMeters > 0f;
    }
}
