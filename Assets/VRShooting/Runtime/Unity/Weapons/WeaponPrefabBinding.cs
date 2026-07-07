using UnityEngine;

namespace VRShooting.Unity.Weapons
{
    [DisallowMultipleComponent]
    public sealed class WeaponPrefabBinding : MonoBehaviour
    {
        [SerializeField] private string weaponId = "training-rifle";
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform recoilRoot;
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private Transform aimLinePoint;
        [SerializeField] private Transform rearHandGrip;
        [SerializeField] private Transform frontHandGrip;
        [SerializeField] private Transform magazinePoint;
        [SerializeField] private Transform leftShoulderPoint;
        [SerializeField] private Transform rightShoulderPoint;

        public string WeaponId => weaponId;
        public Transform VisualRoot => visualRoot;
        public Transform RecoilRoot => recoilRoot != null ? recoilRoot : transform;
        public Transform MuzzlePoint => muzzlePoint;
        public Transform AimLinePoint => aimLinePoint != null ? aimLinePoint : muzzlePoint;
        public Transform RearHandGrip => rearHandGrip;
        public Transform FrontHandGrip => frontHandGrip;
        public Transform MagazinePoint => magazinePoint;
        public Transform LeftShoulderPoint => leftShoulderPoint;
        public Transform RightShoulderPoint => rightShoulderPoint;

        public bool HasRequiredBinding =>
            muzzlePoint != null &&
            AimLinePoint != null &&
            rearHandGrip != null &&
            frontHandGrip != null &&
            leftShoulderPoint != null &&
            rightShoulderPoint != null;

        public void Configure(
            string id,
            Transform visual,
            Transform recoil,
            Transform muzzle,
            Transform aimLine,
            Transform rearGrip,
            Transform frontGrip,
            Transform magazine,
            Transform leftShoulder,
            Transform rightShoulder)
        {
            weaponId = id;
            visualRoot = visual;
            recoilRoot = recoil;
            muzzlePoint = muzzle;
            aimLinePoint = aimLine;
            rearHandGrip = rearGrip;
            frontHandGrip = frontGrip;
            magazinePoint = magazine;
            leftShoulderPoint = leftShoulder;
            rightShoulderPoint = rightShoulder;
        }
    }
}
