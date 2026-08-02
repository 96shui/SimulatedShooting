using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimulatedShooting.Scene
{
    public enum VirtualHandSide
    {
        Left,
        Right
    }

    [DisallowMultipleComponent]
    public sealed class VRControllerHandVisual : MonoBehaviour
    {
        [SerializeField] private VirtualHandSide handSide;
        [SerializeField] private Transform modelRoot;
        [SerializeField] private TrainingRifleGrabInteractable grabInteractable;
        [SerializeField] private Transform gripAnchor;
        [SerializeField] private Vector3 gripPositionOffset;
        [SerializeField] private Vector3 gripRotationOffset;
        [SerializeField] private float poseBlendSpeed = 18f;
        [SerializeField] private bool useCustomFingerPose;
        [SerializeField] private HandGripPose customFingerPose = new HandGripPose();

        readonly List<FingerBonePose> fingerBones = new List<FingerBonePose>();
        Vector3 openLocalPosition;
        Quaternion openLocalRotation;
        float gripPose01;
        bool configured;
        bool overrideGripForTests;
        bool testGripState;
        bool gripPosePreviewActive;

        public VirtualHandSide HandSide => handSide;
        public Transform ModelRoot => modelRoot;
        public bool IsGripping => ResolveGripState();
        public float GripPose01 => gripPose01;
        public bool HasRenderableHand =>
            modelRoot != null && modelRoot.GetComponentsInChildren<Renderer>(true).Length > 0;
        public int FingerBoneCount => fingerBones.Count;
        public bool GripPosePreviewActive => gripPosePreviewActive;
        public bool UsesCustomFingerPose => useCustomFingerPose;
        public Transform GripAnchor => gripAnchor;

        public void Configure(
            VirtualHandSide side,
            Transform handModel,
            TrainingRifleGrabInteractable grab,
            Transform targetGrip,
            Vector3 positionOffset,
            Vector3 rotationOffset)
        {
            handSide = side;
            modelRoot = handModel;
            grabInteractable = grab;
            gripAnchor = targetGrip;
            gripPositionOffset = positionOffset;
            gripRotationOffset = rotationOffset;
            openLocalPosition = transform.localPosition;
            openLocalRotation = transform.localRotation;
            configured = true;
            PrepareModelForManualPose();
            CacheFingerBones();
            ApplyFingerPose(0f);
        }

        public void SetGripForTests(bool gripping)
        {
            overrideGripForTests = true;
            testGripState = gripping;
            UpdateVisualPose(1f);
        }

        public void ClearTestOverride()
        {
            overrideGripForTests = false;
        }

        public void EnableCustomFingerPoseFromDefaults()
        {
            customFingerPose = HandGripPose.CreateDefault(handSide);
            useCustomFingerPose = true;
            RefreshFingerPoseOffsets();
        }

        public void ResetCustomFingerPoseToDefaults()
        {
            customFingerPose = HandGripPose.CreateDefault(handSide);
            RefreshFingerPoseOffsets();
            RefreshGripPosePreview();
        }

        public void BeginGripPosePreview()
        {
            if (gripPosePreviewActive)
            {
                RefreshGripPosePreview();
                return;
            }

            openLocalPosition = transform.localPosition;
            openLocalRotation = transform.localRotation;
            configured = true;
            PrepareModelForManualPose();
            CacheFingerBones();
            gripPosePreviewActive = true;
            gripPose01 = 1f;
            AlignToGripAnchor();
            ApplyFingerPose(1f);
        }

        public void RefreshGripPosePreview()
        {
            if (!gripPosePreviewActive)
            {
                return;
            }

            RefreshFingerPoseOffsets();
            AlignToGripAnchor();
            gripPose01 = 1f;
            ApplyFingerPose(1f);
        }

        public void EndGripPosePreview()
        {
            if (!gripPosePreviewActive)
            {
                return;
            }

            ApplyFingerPose(0f);
            transform.localPosition = openLocalPosition;
            transform.localRotation = openLocalRotation;
            gripPose01 = 0f;
            gripPosePreviewActive = false;
        }

        void Awake()
        {
            if (!configured)
            {
                openLocalPosition = transform.localPosition;
                openLocalRotation = transform.localRotation;
                configured = true;
                PrepareModelForManualPose();
                CacheFingerBones();
            }
        }

        void LateUpdate()
        {
            UpdateVisualPose(Time.deltaTime);
        }

        void UpdateVisualPose(float deltaTime)
        {
            var gripping = ResolveGripState();
            var targetPose = gripping ? 1f : 0f;
            gripPose01 = Mathf.MoveTowards(
                gripPose01,
                targetPose,
                Mathf.Max(0.01f, poseBlendSpeed) * Mathf.Max(0f, deltaTime));

            if (gripping && gripAnchor != null)
            {
                AlignToGripAnchor();
            }
            else
            {
                transform.localPosition = openLocalPosition;
                transform.localRotation = openLocalRotation;
            }

            ApplyFingerPose(gripPose01);
        }

        void AlignToGripAnchor()
        {
            if (gripAnchor == null)
            {
                return;
            }

            transform.SetPositionAndRotation(
                gripAnchor.TransformPoint(gripPositionOffset),
                gripAnchor.rotation * Quaternion.Euler(gripRotationOffset));
        }

        bool ResolveGripState()
        {
            if (overrideGripForTests)
            {
                return testGripState;
            }

            if (grabInteractable == null)
            {
                return false;
            }

            return handSide == VirtualHandSide.Right
                ? grabInteractable.RearHandSelected
                : grabInteractable.FrontHandSelected;
        }

        void CacheFingerBones()
        {
            fingerBones.Clear();
            if (modelRoot == null)
            {
                return;
            }

            var transforms = modelRoot.GetComponentsInChildren<Transform>(true);
            foreach (var joint in transforms)
            {
                if (!TryResolveRotationOffset(joint.name, out var rotationOffset) || joint.childCount == 0)
                {
                    continue;
                }

                fingerBones.Add(new FingerBonePose(
                    joint,
                    joint.localRotation,
                    rotationOffset));
            }
        }

        void RefreshFingerPoseOffsets()
        {
            if (fingerBones.Count == 0)
            {
                CacheFingerBones();
                return;
            }

            for (var index = 0; index < fingerBones.Count; index++)
            {
                var bone = fingerBones[index];
                if (bone.Transform != null &&
                    TryResolveRotationOffset(bone.Transform.name, out var rotationOffset))
                {
                    fingerBones[index] = new FingerBonePose(
                        bone.Transform,
                        bone.OpenRotation,
                        rotationOffset);
                }
            }
        }

        void PrepareModelForManualPose()
        {
            if (modelRoot == null)
            {
                return;
            }

            foreach (var animator in modelRoot.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
            }

            foreach (var renderer in modelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                renderer.updateWhenOffscreen = true;
            }
        }

        bool TryResolveRotationOffset(string jointName, out Vector3 rotationOffset)
        {
            if (useCustomFingerPose && customFingerPose != null)
            {
                return customFingerPose.TryResolve(jointName, out rotationOffset);
            }

            rotationOffset = Vector3.zero;
            var curlDegrees = 0f;
            if (jointName.IndexOf("Thumb", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var thumbOnRearHand = handSide == VirtualHandSide.Right;
                if (jointName.EndsWith("Metacarpal", StringComparison.OrdinalIgnoreCase))
                    curlDegrees = thumbOnRearHand ? 26f : 8f;
                else if (jointName.EndsWith("Proximal", StringComparison.OrdinalIgnoreCase))
                    curlDegrees = thumbOnRearHand ? 42f : 16f;
                else if (jointName.EndsWith("Distal", StringComparison.OrdinalIgnoreCase))
                    curlDegrees = thumbOnRearHand ? 28f : 8f;
                rotationOffset = new Vector3(curlDegrees, 0f, 0f);
                return curlDegrees > 0f;
            }

            var isIndex = jointName.IndexOf("Index", StringComparison.OrdinalIgnoreCase) >= 0;
            var isFinger =
                isIndex ||
                jointName.IndexOf("Middle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                jointName.IndexOf("Ring", StringComparison.OrdinalIgnoreCase) >= 0 ||
                jointName.IndexOf("Little", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!isFinger)
            {
                return false;
            }

            var rearHand = handSide == VirtualHandSide.Right;
            if (isIndex && rearHand)
            {
                // Turn the index finger toward the trigger at the knuckle, then keep
                // the remaining joints nearly straight instead of making a fist.
                if (jointName.EndsWith("Proximal", StringComparison.OrdinalIgnoreCase))
                    curlDegrees = 72f;
                else if (jointName.EndsWith("Intermediate", StringComparison.OrdinalIgnoreCase))
                    curlDegrees = 12f;
                else if (jointName.EndsWith("Distal", StringComparison.OrdinalIgnoreCase))
                    curlDegrees = 6f;
                rotationOffset = new Vector3(curlDegrees, 0f, 0f);
                return curlDegrees > 0f;
            }

            var isMiddle = jointName.IndexOf("Middle", StringComparison.OrdinalIgnoreCase) >= 0;
            var isRing = jointName.IndexOf("Ring", StringComparison.OrdinalIgnoreCase) >= 0;
            var curlBias = isIndex ? 0f : isMiddle ? 2f : isRing ? 4f : 6f;
            var isMetacarpal = jointName.EndsWith("Metacarpal", StringComparison.OrdinalIgnoreCase);
            if (jointName.EndsWith("Proximal", StringComparison.OrdinalIgnoreCase))
                curlDegrees = rearHand ? 58f + curlBias : 22f + curlBias * 2f;
            else if (jointName.EndsWith("Intermediate", StringComparison.OrdinalIgnoreCase))
                curlDegrees = rearHand ? 66f + curlBias : 30f + curlBias * 2f;
            else if (jointName.EndsWith("Distal", StringComparison.OrdinalIgnoreCase))
                curlDegrees = rearHand ? 30f + curlBias : 10f + curlBias;

            rotationOffset = new Vector3(curlDegrees, 0f, 0f);
            return isMetacarpal || curlDegrees > 0f;
        }

        readonly struct FingerBonePose
        {
            public readonly Transform Transform;
            public readonly Quaternion OpenRotation;
            public readonly Vector3 RotationOffset;

            public FingerBonePose(
                Transform transform,
                Quaternion openRotation,
                Vector3 rotationOffset)
            {
                Transform = transform;
                OpenRotation = openRotation;
                RotationOffset = rotationOffset;
            }
        }

        void ApplyFingerPose(float amount)
        {
            for (var index = 0; index < fingerBones.Count; index++)
            {
                var bone = fingerBones[index];
                if (bone.Transform == null)
                {
                    continue;
                }

                bone.Transform.localRotation =
                    bone.OpenRotation * Quaternion.SlerpUnclamped(
                        Quaternion.identity,
                        Quaternion.Euler(bone.RotationOffset),
                        amount);
            }
        }

        [Serializable]
        public sealed class FingerGripPose
        {
            [InspectorName("掌骨 Metacarpal")]
            [SerializeField] private Vector3 metacarpal;
            [InspectorName("近端 Proximal")]
            [SerializeField] private Vector3 proximal;
            [InspectorName("中段 Intermediate")]
            [SerializeField] private Vector3 intermediate;
            [InspectorName("远端 Distal")]
            [SerializeField] private Vector3 distal;

            public FingerGripPose()
            {
            }

            public FingerGripPose(
                Vector3 metacarpal,
                Vector3 proximal,
                Vector3 intermediate,
                Vector3 distal)
            {
                this.metacarpal = metacarpal;
                this.proximal = proximal;
                this.intermediate = intermediate;
                this.distal = distal;
            }

            public bool TryResolve(string jointName, out Vector3 rotationOffset)
            {
                rotationOffset = Vector3.zero;
                if (jointName.EndsWith("Metacarpal", StringComparison.OrdinalIgnoreCase))
                    rotationOffset = metacarpal;
                else if (jointName.EndsWith("Proximal", StringComparison.OrdinalIgnoreCase))
                    rotationOffset = proximal;
                else if (jointName.EndsWith("Intermediate", StringComparison.OrdinalIgnoreCase))
                    rotationOffset = intermediate;
                else if (jointName.EndsWith("Distal", StringComparison.OrdinalIgnoreCase))
                    rotationOffset = distal;
                else
                    return false;

                return true;
            }
        }

        [Serializable]
        public sealed class HandGripPose
        {
            [InspectorName("拇指 Thumb")]
            [SerializeField] private FingerGripPose thumb = new FingerGripPose();
            [InspectorName("食指 Index")]
            [SerializeField] private FingerGripPose index = new FingerGripPose();
            [InspectorName("中指 Middle")]
            [SerializeField] private FingerGripPose middle = new FingerGripPose();
            [InspectorName("无名指 Ring")]
            [SerializeField] private FingerGripPose ring = new FingerGripPose();
            [InspectorName("小指 Little")]
            [SerializeField] private FingerGripPose little = new FingerGripPose();

            public bool TryResolve(string jointName, out Vector3 rotationOffset)
            {
                if (jointName.IndexOf("Thumb", StringComparison.OrdinalIgnoreCase) >= 0)
                    return thumb.TryResolve(jointName, out rotationOffset);
                if (jointName.IndexOf("Index", StringComparison.OrdinalIgnoreCase) >= 0)
                    return index.TryResolve(jointName, out rotationOffset);
                if (jointName.IndexOf("Middle", StringComparison.OrdinalIgnoreCase) >= 0)
                    return middle.TryResolve(jointName, out rotationOffset);
                if (jointName.IndexOf("Ring", StringComparison.OrdinalIgnoreCase) >= 0)
                    return ring.TryResolve(jointName, out rotationOffset);
                if (jointName.IndexOf("Little", StringComparison.OrdinalIgnoreCase) >= 0)
                    return little.TryResolve(jointName, out rotationOffset);

                rotationOffset = Vector3.zero;
                return false;
            }

            public static HandGripPose CreateDefault(VirtualHandSide side)
            {
                var rearHand = side == VirtualHandSide.Right;
                return new HandGripPose
                {
                    thumb = Pose(rearHand ? 26f : 8f, rearHand ? 42f : 16f, 0f, rearHand ? 28f : 8f),
                    index = Pose(0f, rearHand ? 72f : 22f, rearHand ? 12f : 30f, rearHand ? 6f : 10f),
                    middle = Pose(0f, rearHand ? 60f : 26f, rearHand ? 68f : 34f, rearHand ? 32f : 12f),
                    ring = Pose(0f, rearHand ? 62f : 30f, rearHand ? 70f : 38f, rearHand ? 34f : 14f),
                    little = Pose(0f, rearHand ? 64f : 34f, rearHand ? 72f : 42f, rearHand ? 36f : 16f)
                };
            }

            static FingerGripPose Pose(
                float metacarpal,
                float proximal,
                float intermediate,
                float distal)
            {
                return new FingerGripPose(
                    new Vector3(metacarpal, 0f, 0f),
                    new Vector3(proximal, 0f, 0f),
                    new Vector3(intermediate, 0f, 0f),
                    new Vector3(distal, 0f, 0f));
            }
        }
    }
}
