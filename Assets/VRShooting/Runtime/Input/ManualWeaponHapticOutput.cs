using VRShooting.Common;

namespace VRShooting.Input
{
    public sealed class ManualWeaponHapticOutput : IWeaponHapticOutput
    {
        public int ImpulseCount { get; private set; }
        public bool LastFrontHandHeld { get; private set; }
        public WeaponRecoilImpulseDto LastImpulse { get; private set; }

        public void SendShotImpulse(WeaponRecoilImpulseDto impulse, bool frontHandHeld)
        {
            ImpulseCount++;
            LastFrontHandHeld = frontHandHeld;
            LastImpulse = impulse;
        }
    }
}
