using System;
using System.Linq;
using UnityEngine;
using VRShooting.Common;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Scene
{
    // task020: presentation samples the service's deadline; it never changes ammunition.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class CombatRifleReloadPresentation : MonoBehaviour
    {
        WeaponPrefabBinding weapon;
        Camera eye;
        Transform magazine, handAnchor, rearHandAnchor;
        VRControllerHandVisual leftHand, rightHand;
        Renderer[] magazineRenderers;
        bool[] originalVisibility;
        Vector3 magazinePosition, pivotInMesh;
        Quaternion magazineRotation;
        Vector3 recoilPosition, rearPivot, frontInRecoil;
        CombatCoreSnapshotDto snapshot;
        Func<double> now;
        bool vr;

        public bool IsAnimating { get; private set; }
        public float Progress01 { get; private set; }
        public Transform Magazine => magazine;
        public Transform ReloadHandAnchor => handAnchor;
        public VRControllerHandVisual LeftHand => leftHand;

        public void Configure(WeaponPrefabBinding binding, Camera playerEye, GameObject rig, bool realVr, Func<double> clock)
        {
            weapon = binding; eye = playerEye; vr = realVr; now = clock;
            magazine = binding.MagazineVisual;
            if (magazine == null) throw new InvalidOperationException("Missing removable magazine on combat rifle");
            magazinePosition = magazine.localPosition; magazineRotation = magazine.localRotation;
            recoilPosition = weapon.RecoilRoot.localPosition;
            rearPivot = weapon.RecoilRoot.parent.InverseTransformPoint(weapon.RearHandGrip.position);
            frontInRecoil = weapon.RecoilRoot.InverseTransformPoint(weapon.FrontHandGrip.position);
            pivotInMesh = magazine.InverseTransformPoint(binding.MagazinePoint.position);
            magazineRenderers = magazine.GetComponentsInChildren<Renderer>(true);
            originalVisibility = magazineRenderers.Select(r => r.enabled).ToArray();
            handAnchor = new GameObject("Grip_training-rifle_ReloadHand").transform;
            handAnchor.SetParent(transform, false);
            rearHandAnchor = new GameObject("Grip_training-rifle_ReloadRearHand").transform;
            rearHandAnchor.SetParent(transform, false);
            if (vr)
                leftHand = rig.GetComponentsInChildren<VRControllerHandVisual>(true).First(h => h.HandSide == VirtualHandSide.Left);
            else
            {
                leftHand = CreateHand(VirtualHandSide.Left);
                rightHand = CreateHand(VirtualHandSide.Right);
            }
        }

        VRControllerHandVisual CreateHand(VirtualHandSide side)
        {
            var hand = Instantiate(Resources.Load<GameObject>("Combat/Hands/VirtualHand_" + side), transform, false)
                .GetComponent<VRControllerHandVisual>();
            hand.BindWeapon(GetComponent<TrainingRifleGrabInteractable>());
            foreach (var node in hand.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = 2;
            hand.gameObject.SetActive(false);
            return hand;
        }

        public void ApplySnapshot(CombatCoreSnapshotDto value)
        {
            snapshot = value;
            Sample();
        }
        void LateUpdate() => Sample();
        void OnEnable() => Application.onBeforeRender += Sample;
        void OnDisable() { Application.onBeforeRender -= Sample; Restore(); }

        // XRI moves the held rifle before this callback; keep the magazine/hand attached in both eyes.
        [BeforeRenderOrder(UnityEngine.XR.Interaction.Toolkit.XRInteractionUpdateOrder.k_BeforeRenderOrder + 2)]
        public void Sample()
        {
            if (weapon == null || magazine == null || eye == null) return;
            var holding = snapshot.Weapon.TwoHandGripActive;
            if (!vr)
            {
                leftHand.gameObject.SetActive(holding);
                rightHand.gameObject.SetActive(holding);
                rightHand.SetPresentationGrip(holding ? weapon.RearHandGrip : null);
            }
            if (!snapshot.Ammo.IsReloading || snapshot.State != SessionState.Running || !snapshot.TrackingValid)
            {
                Restore();
                if (!vr) leftHand.SetPresentationGrip(holding ? weapon.FrontHandGrip : null);
                return;
            }
            IsAnimating = true;
            Progress01 = Mathf.Clamp01((float)((now() - snapshot.ReloadStartedAt) / snapshot.ReloadDurationSeconds));
            var p = Progress01;
            var raise = p < .15f ? Ease(p, 0, .15f) : p > .90f ? 1 - Ease(p, .90f, 1) : 1;
            var lean = Quaternion.Euler(-35f * raise, -12f * raise, -20f * raise);
            weapon.RecoilRoot.localRotation = lean;
            weapon.RecoilRoot.localPosition = rearPivot + lean * (recoilPosition - rearPivot)
                + (vr ? Vector3.zero : new Vector3(-.06f, .15f, .12f) * raise);
            if (!vr)
            {
                var rear = weapon.RecoilRoot.TransformPoint(weapon.RecoilRoot.parent.InverseTransformPoint(weapon.RearHandGrip.position) - recoilPosition);
                rearHandAnchor.SetPositionAndRotation(rear, weapon.RearHandGrip.rotation * lean);
                rightHand.SetPresentationGrip(rearHandAnchor);
            }
            var seated = weapon.MagazinePoint.position;
            var pulled = seated - weapon.transform.up * .17f;
            // Position below the current viewing frustum, even while walking or looking down.
            var hidden = eye.transform.TransformPoint(new Vector3(-.20f, -.85f, .12f));
            Vector3 pivot;
            if (p < .15f) pivot = seated;
            else if (p < .30f) pivot = Vector3.Lerp(seated, pulled, Ease(p, .15f, .30f));
            else if (p < .48f) pivot = Vector3.Lerp(pulled, hidden, Ease(p, .30f, .48f));
            else if (p < .62f) pivot = hidden;
            else if (p < .82f) pivot = Vector3.Lerp(hidden, pulled, Ease(p, .62f, .82f));
            else if (p < .90f) pivot = Vector3.Lerp(pulled, seated, Ease(p, .82f, .90f));
            else pivot = seated;
            var rotation = magazine.parent.rotation * magazineRotation;
            var tilt = p >= .30f && p < .82f ? Mathf.Sin(Ease(p, .30f, .82f) * Mathf.PI) * -18f : 0;
            rotation *= Quaternion.Euler(0, 0, tilt);
            magazine.SetPositionAndRotation(pivot - rotation * pivotInMesh, rotation);
            for (var i = 0; i < magazineRenderers.Length; i++)
                magazineRenderers[i].enabled = originalVisibility[i] && !(p >= .48f && p < .62f);
            var handPosition = pivot;
            var front = weapon.RecoilRoot.TransformPoint(frontInRecoil);
            if (p < .15f) handPosition = Vector3.Lerp(front, seated, Ease(p, 0, .15f));
            if (p >= .90f) handPosition = Vector3.Lerp(seated, front, Ease(p, .90f, 1));
            handAnchor.SetPositionAndRotation(handPosition, weapon.FrontHandGrip.rotation);
            var curl = p < .25f ? Ease(p, .15f, .25f) : p > .90f ? 1 - Ease(p, .90f, 1) : 1;
            leftHand.SetPresentationGrip(handAnchor, curl);
        }

        void Restore()
        {
            if (IsAnimating && weapon != null)
            {
                weapon.RecoilRoot.localPosition = recoilPosition;
                weapon.RecoilRoot.localRotation = Quaternion.identity;
            }
            IsAnimating = false; Progress01 = 0;
            if (magazine != null) { magazine.localPosition = magazinePosition; magazine.localRotation = magazineRotation; }
            if (magazineRenderers != null)
                for (var i = 0; i < magazineRenderers.Length; i++) if (magazineRenderers[i] != null) magazineRenderers[i].enabled = originalVisibility[i];
            if (leftHand != null) leftHand.SetPresentationGrip(null);
        }
        static float Ease(float value, float start, float end) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(start, end, value));
    }
}
