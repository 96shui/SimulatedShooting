using System;
using System.Collections.Generic;
using UnityEngine;
using VRShooting.Common;

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
        Transform presentationGrip;
        float presentationCurl01;
        bool mirrored;
        bool controllerParentCaptured;
        Transform homeControllerParent;
        Transform trackedControllerParent;
        Vector3 openLocalScale = Vector3.one;
        readonly List<ModelNodePose> sourceModelPose = new List<ModelNodePose>();
        readonly List<MirroredSkin> mirroredSkins = new List<MirroredSkin>();

        public VirtualHandSide HandSide => mirrored
            ? (handSide == VirtualHandSide.Right ? VirtualHandSide.Left : VirtualHandSide.Right) : handSide;
        public VirtualHandSide SourceHandSide => handSide;
        public TrainingRifleGrabInteractable Weapon => grabInteractable;
        public Transform HomeControllerParent { get { CaptureControllerParent(); return homeControllerParent; } }
        public Transform TrackedControllerParent => trackedControllerParent;
        public Transform ModelRoot => modelRoot;
        public bool IsGripping => ResolveGripState();
        public float GripPose01 => gripPose01;
        public bool HasRenderableHand =>
            modelRoot != null && modelRoot.GetComponentsInChildren<Renderer>(true).Length > 0;
        public int FingerBoneCount
        {
            get
            {
                EnsureConfigured();
                return fingerBones.Count;
            }
        }
        public bool GripPosePreviewActive => gripPosePreviewActive;
        public bool UsesCustomFingerPose => useCustomFingerPose;
        public Transform GripAnchor => gripAnchor;
        public bool HasPresentationGrip => presentationGrip != null;
        public float PresentationCurl01 => presentationCurl01;

        // Only the rendered hand follows this anchor; tracked controllers/interactors stay untouched.
        public void SetPresentationGrip(Transform anchor, float curl01 = 0)
        {
            EnsureConfigured();
            presentationGrip = anchor;
            presentationCurl01 = anchor != null ? Mathf.Clamp01(curl01) : 0;
            if (anchor == null && trackedControllerParent != null && transform.parent != trackedControllerParent)
                transform.SetParent(trackedControllerParent, false);
            UpdateVisualPose(1f);
        }

        public void SetShoulderPresentation(ShoulderSide side, Transform controller)
        {
            EnsureConfigured();
            var nextMirror = side == ShoulderSide.Left;
            if (mirrored != nextMirror) ApplyModelMirror(nextMirror);
            mirrored = nextMirror;
            trackedControllerParent = controller;
            if (presentationGrip == null && controller != null && transform.parent != controller)
                transform.SetParent(controller, false);
            if (gripPosePreviewActive) RefreshGripPosePreview();
            else UpdateVisualPose(0f);
        }

        void CaptureControllerParent()
        {
            if (controllerParentCaptured) return;
            controllerParentCaptured = true;
            homeControllerParent = trackedControllerParent = transform.parent;
            openLocalScale = transform.localScale;
        }

        static Quaternion MirrorRotation(Quaternion rotation)
            => new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w);

        void ApplyModelMirror(bool value)
        {
            if (modelRoot == null) return;
            if (sourceModelPose.Count == 0)
            {
                foreach (var node in modelRoot.GetComponentsInChildren<Transform>(true))
                    sourceModelPose.Add(new ModelNodePose(node));
                var reflection = Matrix4x4.Scale(new Vector3(-1, 1, 1));
                foreach (var renderer in modelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var original = renderer.sharedMesh;
                    if (original == null) continue;
                    var mesh = Instantiate(original);
                    mesh.name = original.name + "_MirroredShoulder";
                    mesh.hideFlags = HideFlags.DontSave;
                    var vertices = mesh.vertices;
                    for (var i = 0; i < vertices.Length; i++) vertices[i].x = -vertices[i].x;
                    mesh.vertices = vertices;
                    var normals = mesh.normals;
                    for (var i = 0; i < normals.Length; i++) normals[i].x = -normals[i].x;
                    mesh.normals = normals;
                    var tangents = mesh.tangents;
                    for (var i = 0; i < tangents.Length; i++) { tangents[i].x = -tangents[i].x; tangents[i].w = -tangents[i].w; }
                    mesh.tangents = tangents;
                    for (var submesh = 0; submesh < mesh.subMeshCount; submesh++)
                    {
                        var triangles = mesh.GetTriangles(submesh);
                        for (var i = 0; i < triangles.Length; i += 3)
                        { var swap = triangles[i]; triangles[i] = triangles[i + 2]; triangles[i + 2] = swap; }
                        mesh.SetTriangles(triangles, submesh);
                    }
                    var bindposes = mesh.bindposes;
                    for (var i = 0; i < bindposes.Length; i++) bindposes[i] = reflection * bindposes[i] * reflection;
                    mesh.bindposes = bindposes;
                    mesh.RecalculateBounds();
                    mirroredSkins.Add(new MirroredSkin(renderer, original, mesh));
                }
            }
            foreach (var pose in sourceModelPose)
            {
                var position = pose.Position;
                if (value) position.x = -position.x;
                pose.Node.localPosition = position;
                pose.Node.localRotation = value ? MirrorRotation(pose.Rotation) : pose.Rotation;
            }
            foreach (var skin in mirroredSkins)
            {
                skin.Renderer.sharedMesh = value ? skin.Mirrored : skin.Original;
                var bounds = skin.Bounds;
                if (value) { var center = bounds.center; center.x = -center.x; bounds.center = center; }
                skin.Renderer.localBounds = bounds;
            }
        }

        void OnDestroy()
        {
            foreach (var skin in mirroredSkins)
                if (skin.Mirrored != null)
                {
                    if (Application.isPlaying) Destroy(skin.Mirrored);
                    else DestroyImmediate(skin.Mirrored);
                }
        }

        readonly struct ModelNodePose
        {
            public readonly Transform Node;
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public ModelNodePose(Transform node) { Node = node; Position = node.localPosition; Rotation = node.localRotation; }
        }

        readonly struct MirroredSkin
        {
            public readonly SkinnedMeshRenderer Renderer;
            public readonly Mesh Original, Mirrored;
            public readonly Bounds Bounds;
            public MirroredSkin(SkinnedMeshRenderer renderer, Mesh original, Mesh mirror)
            { Renderer = renderer; Original = original; Mirrored = mirror; Bounds = renderer.localBounds; }
        }

        public void BindWeapon(TrainingRifleGrabInteractable grab)
        {
            grabInteractable = grab;
            gripAnchor = grab == null ? null : handSide == VirtualHandSide.Right ? grab.RearAttach : grab.FrontAttach;
        }

        void OnEnable() { Application.onBeforeRender += UpdateBeforeRender; }
        void OnDisable() { Application.onBeforeRender -= UpdateBeforeRender; }
        [BeforeRenderOrder(UnityEngine.XR.Interaction.Toolkit.XRInteractionUpdateOrder.k_BeforeRenderOrder + 1)]
        void UpdateBeforeRender()
        {
            if (Application.isPlaying && isActiveAndEnabled) UpdateVisualPose(0);
        }

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
            controllerParentCaptured = false;
            CaptureControllerParent();
            PrepareModelForManualPose();
            CacheFingerBones();
            ApplyFingerPose(0f);
        }

        public void SetGripForTests(bool gripping)
        {
            EnsureConfigured();
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
            ApplyOpenPose();
            gripPose01 = 0f;
            gripPosePreviewActive = false;
        }

        void Awake()
        {
            EnsureConfigured();
        }

        void EnsureConfigured()
        {
            CaptureControllerParent();
            if (configured)
            {
                return;
            }

            openLocalPosition = transform.localPosition;
            openLocalRotation = transform.localRotation;
            configured = true;
            PrepareModelForManualPose();
            CacheFingerBones();
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

            if (gripping && (presentationGrip != null || gripAnchor != null))
            {
                AlignToGripAnchor();
            }
            else
            {
                ApplyOpenPose();
            }

            ApplyFingerPose(gripPose01);
        }

        void AlignToGripAnchor()
        {
            var anchor = presentationGrip != null ? presentationGrip : gripAnchor;
            if (anchor == null)
            {
                return;
            }

            var world = anchor.localToWorldMatrix * Matrix4x4.TRS(
                gripPositionOffset, Quaternion.Euler(gripRotationOffset), openLocalScale);
            // Reflect the mesh/bindposes and rig locally instead of leaving a negative
            // world scale on a SkinnedMeshRenderer (Unity's baked skinning distorts it).
            if (mirrored) world *= Matrix4x4.Scale(new Vector3(-1, 1, 1));
            var local = transform.parent != null ? transform.parent.worldToLocalMatrix * world : world;
            var x = (Vector3)local.GetColumn(0);
            var y = (Vector3)local.GetColumn(1);
            var z = (Vector3)local.GetColumn(2);
            transform.localPosition = local.GetColumn(3);
            transform.localRotation = Quaternion.LookRotation(z, y);
            var reflected = Vector3.Dot(Vector3.Cross(x, y), z) < 0;
            transform.localScale = new Vector3(x.magnitude * (reflected ? -1 : 1), y.magnitude, z.magnitude);
        }

        void ApplyOpenPose()
        {
            transform.localPosition = mirrored
                ? new Vector3(-openLocalPosition.x, openLocalPosition.y, openLocalPosition.z) : openLocalPosition;
            transform.localRotation = mirrored
                ? new Quaternion(openLocalRotation.x, -openLocalRotation.y, -openLocalRotation.z, openLocalRotation.w) : openLocalRotation;
            var scale = openLocalScale;
            transform.localScale = scale;
        }

        bool ResolveGripState()
        {
            if (presentationGrip != null) return true;
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
                    curlDegrees = thumbOnRearHand ? 42f : 12f;
                else if (jointName.EndsWith("Distal", StringComparison.OrdinalIgnoreCase))
                    curlDegrees = thumbOnRearHand ? 28f : 5f;
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
                curlDegrees = rearHand ? 58f + curlBias : 16f + curlBias;
            else if (jointName.EndsWith("Intermediate", StringComparison.OrdinalIgnoreCase))
                curlDegrees = rearHand ? 66f + curlBias : 20f + curlBias;
            else if (jointName.EndsWith("Distal", StringComparison.OrdinalIgnoreCase))
                curlDegrees = rearHand ? 30f + curlBias : 6f + curlBias * 0.5f;

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

                var offset = bone.RotationOffset;
                if (presentationCurl01 > 0)
                {
                    var name = bone.Transform.name;
                    var thumb = name.IndexOf("Thumb", StringComparison.OrdinalIgnoreCase) >= 0;
                    var curl = name.EndsWith("Proximal", StringComparison.OrdinalIgnoreCase) ? (thumb ? 42f : 60f)
                        : name.EndsWith("Intermediate", StringComparison.OrdinalIgnoreCase) ? 70f
                        : name.EndsWith("Distal", StringComparison.OrdinalIgnoreCase) ? 30f
                        : thumb ? 26f : 0;
                    offset.x = Mathf.Lerp(offset.x, Mathf.Max(offset.x, curl), presentationCurl01);
                }
                var rotation = bone.OpenRotation * Quaternion.SlerpUnclamped(
                        Quaternion.identity,
                        Quaternion.Euler(offset),
                        amount);
                bone.Transform.localRotation = mirrored ? MirrorRotation(rotation) : rotation;
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
                    thumb = Pose(rearHand ? 26f : 8f, rearHand ? 42f : 12f, 0f, rearHand ? 28f : 5f),
                    index = Pose(0f, rearHand ? 72f : 16f, rearHand ? 12f : 20f, rearHand ? 6f : 6f),
                    middle = Pose(0f, rearHand ? 60f : 18f, rearHand ? 68f : 22f, rearHand ? 32f : 7f),
                    ring = Pose(0f, rearHand ? 62f : 20f, rearHand ? 70f : 24f, rearHand ? 34f : 8f),
                    little = Pose(0f, rearHand ? 64f : 22f, rearHand ? 72f : 26f, rearHand ? 36f : 9f)
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
