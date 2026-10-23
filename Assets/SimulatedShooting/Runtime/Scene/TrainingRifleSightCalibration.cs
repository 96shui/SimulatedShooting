using UnityEngine;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Scene
{
    /// <summary>Optical landmarks of the imported QBZ191 mesh, in RecoilRoot metres.</summary>
    public static class TrainingRifleSightCalibration
    {
        // OBJ aperture inner-ring centre and the nearest top edge of the front post.
        // Unity's OBJ importer mirrors source X. The visible sight line is not gun +Z.
        public static readonly Vector3 RearAperture = new Vector3(-.0057315f,.0573025f,-.118108f);
        public static readonly Vector3 FrontPostTip = new Vector3(-.005689f,.050240f,.228503f);

        public static void Apply(WeaponPrefabBinding binding)
        {
            var root=binding.RecoilRoot;
            var rear=root.TransformPoint(RearAperture);
            var front=root.TransformPoint(FrontPostTip);
            binding.AimLinePoint.SetPositionAndRotation(rear,Quaternion.LookRotation(front-rear,root.up));
        }
    }
}
