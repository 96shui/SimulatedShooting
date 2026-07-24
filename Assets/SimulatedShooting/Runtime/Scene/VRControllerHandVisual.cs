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

        readonly List<FingerBonePose> fingerBones = new List<FingerBonePose>();
        Vector3 openLocalPosition;
        Quaternion openLocalRotation;
        float gripPose01;
        bool configured;
        bool overrideGripForTests;
        bool testGripState;

        public VirtualHandSide HandSide => handSide;
        public Transform ModelRoot => modelRoot;
        public bool IsGripping => ResolveGripState();
        public float GripPose01 => gripPose01;
        public bool HasRenderableHand =>
            modelRoot != null && modelRoot.GetComponentsInChildren<Renderer>(true).Length > 0;
        public int FingerBoneCount => fingerBones.Count;

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
                transform.SetPositionAndRotation(
                    gripAnchor.TransformPoint(gripPositionOffset),
                    gripAnchor.rotation * Quaternion.Euler(gripRotationOffset));
            }
            else
            {
                transform.localPosition = openLocalPosition;
                transform.localRotation = openLocalRotation;
            }

            ApplyFingerPose(gripPose01);
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
                if (!TryResolveCurlDegrees(joint.name, out var curlDegrees) || joint.childCount == 0)
                {
                    continue;
                }

                fingerBones.Add(new FingerBonePose(
                    joint,
                    joint.localRotation,
                    Vector3.right,
                    curlDegrees));
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

        bool TryResolveCurlDegrees(string jointName, out float curlDegrees)
        {
            curlDegrees = 0f;
            if (jointName.IndexOf("Thumb", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (jointName.EndsWith("Metacarpal", StringComparison.OrdinalIgnoreCase))
                    curlDegrees = 24f;
                else if (jointName.EndsWith("Proximal", StringComparison.OrdinalIgnoreCase))
                    curlDegrees = 46f;
                else if (jointName.EndsWith("Distal", StringComparison.OrdinalIgnoreCase))
                    curlDegrees = 34f;
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

            if (jointName.EndsWith("Proximal", StringComparison.OrdinalIgnoreCase))
                curlDegrees = isIndex && handSide == VirtualHandSide.Right ? 38f : 68f;
            else if (jointName.EndsWith("Intermediate", StringComparison.OrdinalIgnoreCase))
                curlDegrees = isIndex && handSide == VirtualHandSide.Right ? 54f : 82f;
            else if (jointName.EndsWith("Distal", StringComparison.OrdinalIgnoreCase))
                curlDegrees = isIndex && handSide == VirtualHandSide.Right ? 28f : 52f;

            return curlDegrees > 0f;
        }

        readonly struct FingerBonePose
        {
            public readonly Transform Transform;
            public readonly Quaternion OpenRotation;
            public readonly Vector3 CurlAxis;
            public readonly float CurlDegrees;

            public FingerBonePose(
                Transform transform,
                Quaternion openRotation,
                Vector3 curlAxis,
                float curlDegrees)
            {
                Transform = transform;
                OpenRotation = openRotation;
                CurlAxis = curlAxis;
                CurlDegrees = curlDegrees;
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
                    bone.OpenRotation * Quaternion.AngleAxis(bone.CurlDegrees * amount, bone.CurlAxis);
            }
        }
    }
}
