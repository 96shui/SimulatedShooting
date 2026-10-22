using VRShooting.Common;

namespace VRShooting.Input
{
    /// <summary>Optional role selection for physical trigger adapters; grip fields remain physical left/right.</summary>
    public interface IShoulderAwareTrainingInput
    {
        void SetShoulder(ShoulderSide shoulder);
    }
}
