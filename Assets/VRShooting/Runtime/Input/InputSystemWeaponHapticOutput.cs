using UnityEngine.InputSystem.XR;
using VRShooting.Common;

namespace VRShooting.Input
{
    public sealed class InputSystemWeaponHapticOutput : IWeaponHapticOutput
    {
        public void SendShotImpulse(WeaponRecoilImpulseDto impulse, bool frontHandHeld)
        {
            if (XRController.rightHand is XRControllerWithRumble rightHand)
            {
                rightHand.SendImpulse(impulse.RearHandHapticAmplitude01, impulse.HapticDurationSeconds);
            }

            if (frontHandHeld && XRController.leftHand is XRControllerWithRumble leftHand)
            {
                leftHand.SendImpulse(impulse.FrontHandHapticAmplitude01, impulse.HapticDurationSeconds);
            }
        }
    }
}
