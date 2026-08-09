using NUnit.Framework;
using SimulatedShooting.Editor;
using SimulatedShooting.Scene;
using UnityEngine;

namespace SimulatedShooting.Tests.EditMode
{
    public sealed class VRControllerHandVisualEditModeTests
    {
        private GameObject root;

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
