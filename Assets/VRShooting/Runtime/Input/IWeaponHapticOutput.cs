using VRShooting.Common;

namespace VRShooting.Input
{
    public interface IWeaponHapticOutput
    {
        void SendShotImpulse(WeaponRecoilImpulseDto impulse, bool frontHandHeld);
    }
}
