using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VRShooting.Common;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Scene
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class TrainingRifleGrabInteractable : XRGrabInteractable
    {
        [SerializeField] private WeaponPrefabBinding weaponBinding;
        [SerializeField] private GameObject pickupPrompt;
        [SerializeField] private float rearGrabRadius = 0.10f;
        [SerializeField] private float frontGrabRadius = 0.12f;
        [SerializeField] private float returnDistance = 3.5f;
        [SerializeField] private float returnBelowRackMeters = 1.4f;

        Vector3 rackPosition;
        Quaternion rackRotation;
        bool rackPoseCaptured;
        bool isOnRack = true;

        public event Action<WeaponHoldState, bool, bool> HoldStateChanged;

        public WeaponHoldState HoldState => ResolveHoldState();
        public bool RearHandSelected => HasSelectingHand(InteractorHandedness.Right);
        public bool FrontHandSelected => HasSelectingHand(InteractorHandedness.Left);
        public Transform RearAttach => weaponBinding != null ? weaponBinding.RearHandGrip : attachTransform;
        public Transform FrontAttach => weaponBinding != null ? weaponBinding.FrontHandGrip : secondaryAttachTransform;
        public float RearGrabRadius => rearGrabRadius;
        public float FrontGrabRadius => frontGrabRadius;
        public bool PickupPromptVisible => pickupPrompt != null && pickupPrompt.activeSelf;

        protected override void Awake()
        {
            base.Awake();
            weaponBinding ??= GetComponent<WeaponPrefabBinding>();
            selectMode = InteractableSelectMode.Multiple;
            movementType = MovementType.VelocityTracking;
            throwOnDetach = true;
            useDynamicAttach = false;
            SyncAttachTransforms();
            SetRackPhysics(true);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            CaptureRackPose();
            SetPromptVisible(false);
        }

        void Update()
        {
            if (isSelected || isOnRack || !rackPoseCaptured)
            {
                return;
            }

            var tooFar = Vector3.Distance(transform.position, rackPosition) > returnDistance;
            var tooLow = transform.position.y < rackPosition.y - returnBelowRackMeters;
            if (tooFar || tooLow)
            {
                ReturnToRack();
            }
        }

        public void Configure(WeaponPrefabBinding binding, GameObject prompt = null)
        {
            weaponBinding = binding;
            pickupPrompt = prompt;
            SyncAttachTransforms();
        }

        public void CaptureRackPose()
        {
            rackPosition = transform.position;
            rackRotation = transform.rotation;
            rackPoseCaptured = true;
            isOnRack = true;
            SetRackPhysics(true);
            PublishHoldState();
        }

        public void ReturnToRack()
        {
            if (!rackPoseCaptured || isSelected)
            {
                return;
            }

            transform.SetPositionAndRotation(rackPosition, rackRotation);
            if (TryGetComponent<Rigidbody>(out var body))
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            isOnRack = true;
            SetRackPhysics(true);
            SetPromptVisible(false);
            PublishHoldState();
        }

        public override bool IsHoverableBy(IXRHoverInteractor interactor)
        {
            if (interactor is IXRSelectInteractor selecting && interactorsSelecting.Contains(selecting))
            {
                return base.IsHoverableBy(interactor);
            }

            return base.IsHoverableBy(interactor) && IsHandAllowed(interactor.handedness, interactor.transform.position);
        }

        public override bool IsSelectableBy(IXRSelectInteractor interactor)
        {
            if (interactorsSelecting.Contains(interactor))
            {
                return base.IsSelectableBy(interactor);
            }

            return base.IsSelectableBy(interactor) && IsHandAllowed(interactor.handedness, interactor.transform.position);
        }

        public override Transform GetAttachTransform(IXRInteractor interactor)
        {
            if (interactor != null && interactor.handedness == InteractorHandedness.Left && FrontAttach != null)
            {
                return FrontAttach;
            }

            if (interactor != null && interactor.handedness == InteractorHandedness.Right && RearAttach != null)
            {
                return RearAttach;
            }

            return base.GetAttachTransform(interactor);
        }

        public bool IsHandAllowed(InteractorHandedness handedness, Vector3 handPosition)
        {
            switch (handedness)
            {
                case InteractorHandedness.Right:
                    return !RearHandSelected && IsWithinRadius(handPosition, RearAttach, rearGrabRadius);
                case InteractorHandedness.Left:
                    return RearHandSelected && !FrontHandSelected &&
                           IsWithinRadius(handPosition, FrontAttach, frontGrabRadius);
                default:
                    return false;
            }
        }

        protected override void OnHoverEntered(HoverEnterEventArgs args)
        {
            base.OnHoverEntered(args);
            if (args.interactorObject.handedness == InteractorHandedness.Right && !RearHandSelected)
            {
                SetPromptVisible(true);
            }
        }

        protected override void OnHoverExited(HoverExitEventArgs args)
        {
            base.OnHoverExited(args);
            if (!RearHandSelected)
            {
                SetPromptVisible(false);
            }
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            isOnRack = false;
            SetPromptVisible(false);
            PublishHoldState();
        }

        protected override void OnSelectEntering(SelectEnterEventArgs args)
        {
            SetRackPhysics(false);
            base.OnSelectEntering(args);
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            var releasedRearHand = args.interactorObject.handedness == InteractorHandedness.Right;
            base.OnSelectExited(args);

            if (releasedRearHand && interactionManager != null)
            {
                for (var index = interactorsSelecting.Count - 1; index >= 0; index--)
                {
                    interactionManager.SelectCancel(interactorsSelecting[index], this);
                }
            }

            PublishHoldState();
        }

        protected override void OnSelectExiting(SelectExitEventArgs args)
        {
            base.OnSelectExiting(args);
            if (interactorsSelecting.Count == 0 && TryGetComponent<Rigidbody>(out var body))
            {
                body.isKinematic = false;
                body.useGravity = true;
            }
        }

        void SyncAttachTransforms()
        {
            if (weaponBinding == null)
            {
                return;
            }

            attachTransform = weaponBinding.RearHandGrip;
            secondaryAttachTransform = weaponBinding.FrontHandGrip;
        }

        WeaponHoldState ResolveHoldState()
        {
            if (RearHandSelected && FrontHandSelected)
            {
                return WeaponHoldState.TwoHandHeld;
            }

            if (RearHandSelected)
            {
                return WeaponHoldState.RearHandHeld;
            }

            return isOnRack ? WeaponHoldState.OnRack : WeaponHoldState.Dropped;
        }

        bool HasSelectingHand(InteractorHandedness handedness)
        {
            for (var index = 0; index < interactorsSelecting.Count; index++)
            {
                if (interactorsSelecting[index].handedness == handedness)
                {
                    return true;
                }
            }

            return false;
        }

        void PublishHoldState()
        {
            HoldStateChanged?.Invoke(ResolveHoldState(), RearHandSelected, FrontHandSelected);
        }

        void SetPromptVisible(bool visible)
        {
            if (pickupPrompt != null)
            {
                pickupPrompt.SetActive(visible);
            }
        }

        static bool IsWithinRadius(Vector3 position, Transform anchor, float radius)
        {
            return anchor != null && Vector3.SqrMagnitude(position - anchor.position) <= radius * radius;
        }

        void SetRackPhysics(bool onRack)
        {
            if (!TryGetComponent<Rigidbody>(out var body))
            {
                return;
            }

            body.isKinematic = onRack;
            body.useGravity = !onRack;
        }
    }
}
