using NUnit.Framework;
using SimulatedShooting.Editor;
using SimulatedShooting.Scene;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VRShooting.Common;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Tests.EditMode
{
    public sealed class VRControllerHandVisualEditModeTests
    {
        private GameObject root;

        [Test]
        public void Bdd29_LeftShoulderMirrorsEveryArmBoneAndRenderedVertexAndSwitchesBackExactly()
        {
            root = new GameObject("MirroredArmsTest");
            root.transform.SetPositionAndRotation(new Vector3(2, 1, 3), Quaternion.Euler(5, 40, 9));
            var weapon = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/SimulatedShooting/Prefabs/Weapons/Weapon_training-rifle_Blockout.prefab"), root.transform);
            var binding = weapon.GetComponent<WeaponPrefabBinding>();
            var grab = weapon.GetComponent<TrainingRifleGrabInteractable>();
            grab.Configure(binding);
            var leftController = new GameObject("Left Controller").transform;
            var rightController = new GameObject("Right Controller").transform;
            leftController.SetParent(root.transform, false); rightController.SetParent(root.transform, false);
            leftController.localPosition = new Vector3(-.2f, .3f, .5f);
            rightController.localPosition = new Vector3(.2f, .3f, .2f);
            var left = Object.Instantiate(Resources.Load<GameObject>("Combat/Hands/VirtualHand_Left"), leftController)
                .GetComponent<VRControllerHandVisual>();
            var right = Object.Instantiate(Resources.Load<GameObject>("Combat/Hands/VirtualHand_Right"), rightController)
                .GetComponent<VRControllerHandVisual>();
            left.BindWeapon(grab); right.BindWeapon(grab);
            left.BeginGripPosePreview(); right.BeginGripPosePreview();
            Assert.That(Vector3.Distance(right.transform.position, binding.RearHandGrip.TransformPoint(new Vector3(.049f, -.010f, -.032f))), Is.LessThan(.00001f));
            Assert.That(Quaternion.Angle(right.transform.rotation, binding.RearHandGrip.rotation * Quaternion.Euler(-30, -15, -90)), Is.LessThan(.01f));
            Assert.That(Vector3.Distance(left.transform.position, binding.FrontHandGrip.TransformPoint(new Vector3(-.0478f, -.0516f, -.0082f))), Is.LessThan(.00001f));
            Assert.That(Quaternion.Angle(left.transform.rotation, binding.FrontHandGrip.rotation * Quaternion.Euler(-20, 65, 180)), Is.LessThan(.01f));
            var leftOriginal = CaptureGeometry(left, weapon.transform);
            var rightOriginal = CaptureGeometry(right, weapon.transform);
            var leftPose = leftController.localToWorldMatrix;
            var rightPose = rightController.localToWorldMatrix;
            Assert.That(leftOriginal.Length, Is.GreaterThan(1000), "Validate the real skinned forearm/hand, not an empty pose proxy.");
            grab.SetShoulder(ShoulderSide.Left);
            left.RefreshGripPosePreview(); right.RefreshGripPosePreview();
            Assert.That(right.HandSide, Is.EqualTo(VirtualHandSide.Left));
            Assert.That(left.HandSide, Is.EqualTo(VirtualHandSide.Right));
            Assert.That(right.transform.parent, Is.SameAs(leftController));
            Assert.That(left.transform.parent, Is.SameAs(rightController));
            AssertMirrored(rightOriginal, CaptureGeometry(right, weapon.transform), true);
            AssertMirrored(leftOriginal, CaptureGeometry(left, weapon.transform), true);
            Assert.That(leftController.localToWorldMatrix, Is.EqualTo(leftPose));
            Assert.That(rightController.localToWorldMatrix, Is.EqualTo(rightPose));
            grab.SetShoulder(ShoulderSide.Right);
            left.RefreshGripPosePreview(); right.RefreshGripPosePreview();
            AssertMirrored(rightOriginal, CaptureGeometry(right, weapon.transform), false);
            AssertMirrored(leftOriginal, CaptureGeometry(left, weapon.transform), false);
        }

        [Test]
        public void Bdd29_LeftHandCanPickUpTheRearGripInLeftShoulderMode()
        {
            root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/SimulatedShooting/Prefabs/Weapons/Weapon_training-rifle_Blockout.prefab"));
            var grab = root.GetComponent<TrainingRifleGrabInteractable>();
            grab.Configure(root.GetComponent<WeaponPrefabBinding>());
            grab.SetShoulder(ShoulderSide.Left);
            Assert.That(grab.IsHandAllowed(InteractorHandedness.Left, grab.RearAttach.position), Is.True);
            Assert.That(grab.IsHandAllowed(InteractorHandedness.Right, grab.RearAttach.position), Is.False);
            Assert.That(grab.IsHandAllowed(InteractorHandedness.Left, grab.RearAttach.position + Vector3.one), Is.False);
        }

        [Test]
        public void Bdd29_LeftShoulderAimFollowsTheMirroredRenderedBarrelWithYawAndRoll()
        {
            root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/SimulatedShooting/Prefabs/Weapons/Weapon_training-rifle_Blockout.prefab"));
            root.transform.rotation = Quaternion.Euler(5, 40, 9);
            var binding = root.GetComponent<WeaponPrefabBinding>();
            binding.RecoilRoot.localRotation = Quaternion.Euler(-7, 11, 5);
            var controller = root.AddComponent<FirstPersonTrainingWeaponController>();
            controller.ConfigureForScene(null, binding, null, null);
            var original = root.transform.InverseTransformDirection(controller.CurrentAimDirection);
            Assert.That(Mathf.Abs(original.x), Is.GreaterThan(.1f), "Exercise nonzero yaw, not a trivial straight-ahead ray.");
            root.GetComponent<TrainingRifleGrabInteractable>().SetShoulder(ShoulderSide.Left);
            var expected = new Vector3(-original.x, original.y, original.z);
            var actual = root.transform.InverseTransformDirection(controller.CurrentAimDirection);
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(.00001f), "Physical shot direction must reflect with the rendered barrel, including recoil yaw.");
        }

        static Vector3[] CaptureGeometry(VRControllerHandVisual hand, Transform frame)
        {
            var points = new List<Vector3>();
            foreach (var bone in hand.ModelRoot.GetComponentsInChildren<Transform>(true))
            {
                points.Add(frame.InverseTransformPoint(bone.position));
                foreach (var axis in new[] { Vector3.right, Vector3.up, Vector3.forward })
                {
                    // A reflected anatomical frame reverses its local X basis to stay right-handed.
                    var canonical = hand.HandSide != hand.SourceHandSide && axis == Vector3.right ? -axis : axis;
                    points.Add(frame.InverseTransformVector(bone.localToWorldMatrix.MultiplyVector(canonical)));
                }
            }
            foreach (var renderer in hand.ModelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = new Mesh();
                try
                {
                    renderer.BakeMesh(mesh, false);
                    TestContext.WriteLine($"{hand.SourceHandSide}: {hand.ModelRoot.GetComponentsInChildren<Transform>(true).Length} transforms, " +
                        $"{renderer.bones.Length} skin bones, {renderer.bones.Count(b => b != null && !b.IsChildOf(hand.ModelRoot))} external bones, " +
                        $"{mesh.vertexCount} vertices, scale={renderer.transform.lossyScale}");
                    var scale = renderer.transform.lossyScale;
                    Assert.That(scale.x * scale.y * scale.z, Is.GreaterThan(0), "The skinned rig must retain a right-handed world transform.");
                    points.AddRange(mesh.vertices.Select(v => frame.InverseTransformPoint(renderer.transform.TransformPoint(v))));
                }
                finally { Object.DestroyImmediate(mesh); }
            }
            return points.ToArray();
        }

        static void AssertMirrored(Vector3[] original, Vector3[] actual, bool mirrored)
        {
            Assert.That(actual.Length, Is.EqualTo(original.Length));
            for (var i = 0; i < original.Length; i++)
            {
                var expected = original[i];
                if (mirrored) expected.x = -expected.x;
                Assert.That(Vector3.Distance(actual[i], expected), Is.LessThan(.0001f), "Bone/basis/vertex " + i);
            }
        }

        [TearDown]
        public void TearDown()
        {
            VRHandPosePreviewManager.StopAll();
            if (root != null)
                Object.DestroyImmediate(root);
        }

        [Test]
        public void Task013_GripPoseCanBePreviewedAndRestoredWithoutPlayModeOrXRInput()
        {
            root = new GameObject("HandPoseEditModeTest");
            var controller = new GameObject("Controller").transform;
            controller.SetParent(root.transform, false);
            var hand = new GameObject("VirtualHand").transform;
            hand.SetParent(controller, false);
            hand.localPosition = new Vector3(0.1f, 0.2f, 0.3f);
            hand.localRotation = Quaternion.Euler(4f, 5f, 6f);

            var model = new GameObject("Model").transform;
            model.SetParent(hand, false);
            var index = new GameObject("R_IndexProximal").transform;
            index.SetParent(model, false);
            new GameObject("R_IndexIntermediate").transform.SetParent(index, false);
            var openFingerRotation = index.localRotation;

            var grip = new GameObject("GripAnchor").transform;
            grip.SetParent(root.transform, false);
            grip.SetPositionAndRotation(new Vector3(2f, 3f, 4f), Quaternion.Euler(10f, 20f, 30f));
            var offset = new Vector3(0.01f, 0.02f, 0.03f);
            var rotationOffset = new Vector3(-90f, 0f, -5f);

            var visual = hand.gameObject.AddComponent<VRControllerHandVisual>();
            visual.Configure(VirtualHandSide.Right, model, null, grip, offset, rotationOffset);
            var openPosition = hand.localPosition;
            var openRotation = hand.localRotation;

            visual.BeginGripPosePreview();

            Assert.That(visual.GripPosePreviewActive, Is.True);
            Assert.That(Vector3.Distance(hand.position, grip.TransformPoint(offset)), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(
                hand.rotation,
                grip.rotation * Quaternion.Euler(rotationOffset)), Is.LessThan(0.01f));
            Assert.That(Quaternion.Angle(openFingerRotation, index.localRotation), Is.InRange(65f, 80f));

            visual.EndGripPosePreview();

            Assert.That(visual.GripPosePreviewActive, Is.False);
            Assert.That(Vector3.Distance(hand.localPosition, openPosition), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(hand.localRotation, openRotation), Is.LessThan(0.01f));
            Assert.That(Quaternion.Angle(index.localRotation, openFingerRotation), Is.LessThan(0.01f));
        }

        [Test]
        public void Task013_StaticPreviewStaysVisibleWhenXRControllerHierarchyIsDisabled()
        {
            root = new GameObject("InactiveXRControllerPreviewTest");
            var controller = new GameObject("DisabledXRController").transform;
            controller.SetParent(root.transform, false);
            var hand = new GameObject("VirtualHand").transform;
            hand.SetParent(controller, false);
            var model = new GameObject("Model").transform;
            model.SetParent(hand, false);
            model.gameObject.AddComponent<MeshRenderer>();
            model.gameObject.AddComponent<MeshFilter>();

            var grip = new GameObject("GripAnchor").transform;
            grip.SetParent(root.transform, false);
            grip.SetPositionAndRotation(new Vector3(2f, 3f, 4f), Quaternion.Euler(10f, 20f, 30f));

            var visual = hand.gameObject.AddComponent<VRControllerHandVisual>();
            visual.Configure(
                VirtualHandSide.Right,
                model,
                null,
                grip,
                new Vector3(0.01f, 0.02f, 0.03f),
                new Vector3(-90f, 0f, -5f));
            controller.gameObject.SetActive(false);

            VRHandPosePreviewManager.BeginPreview(visual);
            var preview = VRHandPosePreviewManager.GetPreviewVisual(visual);

            Assert.That(VRHandPosePreviewManager.IsPreviewing(visual), Is.True);
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.ModelRoot.gameObject.activeInHierarchy, Is.True);
            Assert.That(Vector3.Distance(preview.transform.position, grip.TransformPoint(new Vector3(0.01f, 0.02f, 0.03f))),
                Is.LessThan(0.0001f));

            VRHandPosePreviewManager.EndPreview(visual);

            Assert.That(VRHandPosePreviewManager.IsPreviewing(visual), Is.False);
        }
    }
}
