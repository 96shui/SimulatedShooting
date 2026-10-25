using System;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private float frontGrabRadius = 0.22f;
        [SerializeField] private float returnDistance = 3.5f;
        [SerializeField] private float returnBelowRackMeters = 1.4f;

        Vector3 rackPosition;
        Quaternion rackRotation;
        bool rackPoseCaptured;
        bool isOnRack = true;
        bool transferringHands;
        HashSet<IXRSelectInteractor> previousSelectors;
        Vector3 rightShoulderVisualScale;
        bool visualScaleCaptured;

        public ShoulderSide Shoulder { get; private set; } = ShoulderSide.Right;
        public InteractorHandedness RearHandedness => Shoulder == ShoulderSide.Left ? InteractorHandedness.Left : InteractorHandedness.Right;
        public InteractorHandedness FrontHandedness => Shoulder == ShoulderSide.Left ? InteractorHandedness.Right : InteractorHandedness.Left;

        public event Action<WeaponHoldState, bool, bool> HoldStateChanged;

        public WeaponHoldState HoldState => ResolveHoldState();
        public bool RearHandSelected => HasSelectingHand(RearHandedness);
        public bool FrontHandSelected => HasSelectingHand(FrontHandedness);
        public Transform RearAttach => weaponBinding != null ? weaponBinding.RearHandGrip : attachTransform;
        public Transform FrontAttach => weaponBinding != null ? weaponBinding.FrontHandGrip : secondaryAttachTransform;
        public float RearGrabRadius => rearGrabRadius;
        public float FrontGrabRadius => frontGrabRadius;
        public bool PickupPromptVisible => pickupPrompt != null && pickupPrompt.activeSelf;
        public bool FrontHandInteractionLocked { get; private set; }
        public bool ReturnDroppedWeaponToRack { get; set; } = true;

        // Release only the support hand; the right hand keeps the rifle and UI interactors stay active.
        public void SetFrontHandInteractionLocked(bool locked)
        {
            if (FrontHandInteractionLocked == locked) return;
            FrontHandInteractionLocked = locked;
            if (!locked || interactionManager == null) return;
            for (var i = interactorsSelecting.Count - 1; i >= 0; i--)
                if (interactorsSelecting[i].handedness == FrontHandedness)
                    interactionManager.SelectCancel(interactorsSelecting[i], this);
        }

        protected override void Awake()
        {
            base.Awake();
            weaponBinding ??= GetComponent<WeaponPrefabBinding>();
            selectMode = InteractableSelectMode.Multiple;
            // Follow tracked poses at Dynamic/BeforeRender frequency. VelocityTracking
            // chases a moving rig at fixed physics frequency and oscillates while walking.
            movementType = MovementType.Instantaneous;
            smoothPosition = false;
            smoothRotation = false;
            attachEaseInTime = 0f;
            throwOnDetach = true;
            useDynamicAttach = false;
            // A held grip must still select when the hand enters range a frame later.
            foreach (var interactor in FindObjectsOfType<XRBaseInputInteractor>(true))
                ConfigureHeldGrip(interactor);
            SyncAttachTransforms();
            SetRackPhysics(true);
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            if(updatePhase==XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender&&isSelected)
            {
                // NearFar refreshes its attachment only in Dynamic update. Walking can
                // move the origin afterwards; refresh the physical grip before rendering.
                foreach(var selecting in interactorsSelecting)
                    if(selecting is NearFarInteractor near)
                        near.interactionAttachController.ResetOffset();
            }
            base.ProcessInteractable(updatePhase);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            CaptureRackPose();
            SetPromptVisible(false);
        }

        void Update()
        {
            if (!ReturnDroppedWeaponToRack || isSelected || isOnRack || !rackPoseCaptured)
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

        public void SetShoulder(ShoulderSide shoulder)
        {
            if (Shoulder == shoulder) return;
            weaponBinding ??= GetComponent<WeaponPrefabBinding>();
            if (!visualScaleCaptured && weaponBinding != null && weaponBinding.VisualRoot != null)
            {
                rightShoulderVisualScale = weaponBinding.VisualRoot.localScale;
                visualScaleCaptured = true;
            }
            var selected = interactorsSelecting.ToArray();
            previousSelectors = new HashSet<IXRSelectInteractor>(selected);
            transferringHands = true;
            try
            {
                if (interactionManager != null)
                    foreach (var interactor in selected) interactionManager.SelectCancel(interactor, this);
                Shoulder = shoulder;
                if (visualScaleCaptured)
                {
                    var scale = rightShoulderVisualScale;
                    if (shoulder == ShoulderSide.Left) scale.x = -scale.x;
                    weaponBinding.VisualRoot.localScale = scale;
                }
                VRHandPresentation.ApplyShoulder(this, shoulder);
                // Re-enter rear first: XRI's primary selection must follow the new trigger hand.
                var rear = selected.FirstOrDefault(i => i.handedness == RearHandedness);
                var front = selected.FirstOrDefault(i => i.handedness == FrontHandedness);
                if (interactionManager != null && rear != null)
                {
                    interactionManager.SelectEnter(rear, this);
                    if (front != null && !FrontHandInteractionLocked) interactionManager.SelectEnter(front, this);
                }
            }
            finally
            {
                transferringHands = false;
                previousSelectors = null;
            }
            PublishHoldState();
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
            if (TryGetComponent<Rigidbody>(out var body) && !body.isKinematic)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            isOnRack = true;
            SetRackPhysics(true);
            SetPromptVisible(false);
            PublishHoldState();
        }

        public void ResetToRack()
        {
            // A new training session must not inherit the previous XR selection.
            if (interactionManager != null)
            {
                while (interactorsSelecting.Count > 0)
                {
                    interactionManager.SelectCancel(interactorsSelecting[interactorsSelecting.Count - 1], this);
                }
            }
            SetShoulder(ShoulderSide.Right);
            ReturnToRack();
        }

        public override bool IsHoverableBy(IXRHoverInteractor interactor)
        {
            if (FrontHandInteractionLocked && interactor.handedness == FrontHandedness) return false;
            if (interactor is IXRSelectInteractor selecting && interactorsSelecting.Contains(selecting))
            {
                return base.IsHoverableBy(interactor);
            }

            return base.IsHoverableBy(interactor) && IsHandAllowed(interactor.handedness, interactor.transform.position);
        }

        public override bool IsSelectableBy(IXRSelectInteractor interactor)
        {
            if (FrontHandInteractionLocked && interactor.handedness == FrontHandedness) return false;
            if (transferringHands && previousSelectors != null && previousSelectors.Contains(interactor))
                return base.IsSelectableBy(interactor);
            if (interactorsSelecting.Contains(interactor))
            {
                return base.IsSelectableBy(interactor);
            }

            return base.IsSelectableBy(interactor) && IsHandAllowed(interactor.handedness, interactor.transform.position);
        }

        public override Transform GetAttachTransform(IXRInteractor interactor)
        {
            if (interactor != null && interactor.handedness == FrontHandedness && FrontAttach != null)
            {
                return FrontAttach;
            }

            if (interactor != null && interactor.handedness == RearHandedness && RearAttach != null)
            {
                return RearAttach;
            }

            return base.GetAttachTransform(interactor);
        }

        public bool IsHandAllowed(InteractorHandedness handedness, Vector3 handPosition)
        {
            if (handedness == RearHandedness)
                return !RearHandSelected && IsWithinRadius(handPosition, RearAttach, rearGrabRadius);
            if (handedness == FrontHandedness)
                return !FrontHandInteractionLocked && RearHandSelected && !FrontHandSelected &&
                    IsWithinRadius(handPosition, FrontAttach, frontGrabRadius);
            return false;
        }

        protected override void OnHoverEntered(HoverEnterEventArgs args)
        {
            ConfigureHeldGrip(args.interactorObject as XRBaseInputInteractor);
            base.OnHoverEntered(args);
            if (args.interactorObject.handedness == RearHandedness && !RearHandSelected)
            {
                SetPromptVisible(true);
            }
        }

        static void ConfigureHeldGrip(XRBaseInputInteractor interactor)
        {
            if (interactor is XRDirectInteractor || interactor is NearFarInteractor)
                interactor.selectActionTrigger = XRBaseInputInteractor.InputTriggerType.State;
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
            // Only prime the first grab's cached release state. Adding the support
            // hand must not make an already-held Instantaneous rifle dynamic.
            if (!isSelected) SetRackPhysics(false);
            base.OnSelectEntering(args);
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            var releasedRearHand = args.interactorObject.handedness == RearHandedness;
            base.OnSelectExited(args);

            if (releasedRearHand && !transferringHands && interactionManager != null)
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

        protected override void SetupRigidbodyDrop(Rigidbody body)
        {
            base.SetupRigidbodyDrop(body);
            // XRI caches the pre-grab rack state; explicitly restore physical drop.
            body.isKinematic = false;
            body.useGravity = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
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
            if (transferringHands) return;
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
