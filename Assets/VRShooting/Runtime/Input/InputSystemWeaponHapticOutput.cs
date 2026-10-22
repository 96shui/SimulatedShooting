using UnityEngine.InputSystem.XR;
using VRShooting.Common;

namespace VRShooting.Input
{
    public sealed class InputSystemWeaponHapticOutput : IWeaponHapticOutput
    {
        public ShoulderSide Shoulder { get; set; } = ShoulderSide.Right;
        public void SendShotImpulse(WeaponRecoilImpulseDto impulse, bool frontHandHeld)
        {
            var rear = Shoulder == ShoulderSide.Left ? XRController.leftHand : XRController.rightHand;
            var front = Shoulder == ShoulderSide.Left ? XRController.rightHand : XRController.leftHand;
            if (rear is XRControllerWithRumble rightHand)
            {
                rightHand.SendImpulse(impulse.RearHandHapticAmplitude01, impulse.HapticDurationSeconds);
            }

            if (frontHandHeld && front is XRControllerWithRumble leftHand)
            {
                leftHand.SendImpulse(impulse.FrontHandHapticAmplitude01, impulse.HapticDurationSeconds);
            }
        }
    }
}
