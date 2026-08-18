namespace VRShooting.Input
{
    /// <summary>
    /// Trigger 模拟量迟滞阈值。XR、无 VR 替身和自动测试共用。
    /// </summary>
    public static class WeaponTriggerHysteresis
    {
        public const float PressThreshold = 0.75f;
        public const float ReleaseThreshold = 0.25f;

        public static bool CrossedPress(bool currentlyHeld, float value01)
        {
            return !currentlyHeld && value01 >= PressThreshold;
        }

        public static bool CrossedRelease(bool currentlyHeld, float value01)
        {
            return currentlyHeld && value01 <= ReleaseThreshold;
        }
    }
}
