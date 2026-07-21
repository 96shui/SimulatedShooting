using UnityEngine;

namespace VRShooting.Input
{
    public interface IWeaponPoseInput
    {
        bool HeadTracked { get; }
        bool RearHandTracked { get; }
        bool FrontHandTracked { get; }
        Pose HeadPose { get; }
        Pose RearHandPose { get; }
        Pose FrontHandPose { get; }
    }
}
