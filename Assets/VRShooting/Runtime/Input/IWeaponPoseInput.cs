using UnityEngine;

namespace VRShooting.Input
{
    public interface IWeaponPoseInput
    {
        bool RearHandActive { get; }
        bool FrontHandActive { get; }
        Pose HeadPose { get; }
        Pose RearHandPose { get; }
        Pose FrontHandPose { get; }
    }
}
