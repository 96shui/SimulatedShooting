using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using VRShooting.Application.Combat;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.Input;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Tests.PlayMode
{
    // task020 / BDD23: real rifle and human hand assets, service-owned reload, no hardware needed.
    public sealed class CombatMagazineReloadPlayModeTests
    {
        GameObject rifle, cameraObject, testRig;
        CombatCoreService core;
        CombatSceneClock clock;
        CombatRifleReloadPresentation presentation;
        Camera eye;
        Vector3 seatedPosition;

        [UnitySetUp] public IEnumerator Setup()
        {
            yield return SceneManager.LoadSceneAsync("CombatScene");
            var bindings = UnityEngine.Object.FindObjectOfType<CombatSceneBindings>();
            foreach (var fixture in bindings.GetComponentsInChildren<CombatSceneFixture>(true)) fixture.enabled = false;
            cameraObject = new GameObject("ReloadTestEye"); eye = cameraObject.AddComponent<Camera>();
            eye.nearClipPlane = .02f;
            eye.stereoTargetEye = StereoTargetEyeMask.None;
            eye.cullingMask = ~0;
            eye.transform.position = new Vector3(0, 1.65f, 0);
            rifle = UnityEngine.Object.Instantiate(bindings.TrainingRiflePrefab);
            rifle.GetComponent<TrainingRifleGrabInteractable>().enabled = false;
            rifle.GetComponent<Rigidbody>().isKinematic = true;
            rifle.transform.position = eye.transform.position + new Vector3(.13f, -.17f, .28f);
            rifle.GetComponent<Rigidbody>().position = rifle.transform.position;
            rifle.GetComponent<TrainingRifleGrabInteractable>().CaptureRackPose();
            var weapon = rifle.GetComponent<WeaponPrefabBinding>();
            Assert.That(weapon.MagazineVisual, Is.Not.Null, "Install removable QBZ191 magazine before delivery");
            seatedPosition = weapon.MagazineVisual.localPosition;
            clock = new CombatSceneClock();
            core = new CombatCoreService(clock);
            presentation = rifle.AddComponent<CombatRifleReloadPresentation>();
            presentation.Configure(weapon, eye, null, false, () => clock.Now);
            core.Changed += presentation.ApplySnapshot;
            core.Start("reload-play", TrainingMode.Trench, Array.Empty<CombatEntityVisualDto>());
            core.SetGrip(new WeaponGripStateInputDto { SessionId = "reload-play", HoldState = WeaponHoldState.TwoHandHeld,
                RearHandTracked = true, FrontHandTracked = true, Stability01 = 1 });
            yield return null;
        }

        [UnityTest] public IEnumerator HandPullsMagazineBelowViewThenSeatsBeforeAmmoCompletes()
        {
            Fire(); core.Reload("reload-play");
            Assert.That(presentation.LeftHand.HasRenderableHand, Is.True);
            Step(.50f);
            Assert.That(presentation.Magazine.localPosition.y, Is.LessThan(seatedPosition.y - .08f));
            Assert.That(presentation.LeftHand.HasPresentationGrip, Is.True);
            Assert.That(presentation.LeftHand.PresentationCurl01, Is.GreaterThan(.99f), "Fingers close around the magazine rather than retaining the flat support pose");
            Capture("01-pull");
            Step(.50f);
            Assert.That(presentation.Progress01, Is.EqualTo(.5f).Within(.001f));
            var handViewport = eye.WorldToViewportPoint(presentation.ReloadHandAnchor.position);
            Assert.That(handViewport.y, Is.LessThan(0), "Magazine and hand travel beneath the player frustum");
            Assert.That(presentation.Magazine.GetComponent<MeshRenderer>().enabled, Is.False);
            Capture("02-below-view");
            Assert.That(core.GetSnapshot("reload-play").Data.Ammo.CurrentMagazine, Is.EqualTo(29));
            Assert.That(core.GetSnapshot("reload-play").Data.Ammo.ReserveAmmo, Is.EqualTo(60));
            Step(.70f);
            Assert.That(presentation.Magazine.GetComponent<MeshRenderer>().enabled, Is.True);
            Capture("03-insert");
            Step(.29f);
            Assert.That(core.GetSnapshot("reload-play").Data.Ammo.IsReloading, Is.True);
            Step(.02f);
            Assert.That(core.GetSnapshot("reload-play").Data.Ammo.CurrentMagazine, Is.EqualTo(30));
            Assert.That(core.GetSnapshot("reload-play").Data.Ammo.ReserveMagazines, Is.EqualTo(1));
            Assert.That(presentation.IsAnimating, Is.False);
            Assert.That(presentation.Magazine.localPosition, Is.EqualTo(seatedPosition));
            Assert.That(eye.transform.position, Is.EqualTo(new Vector3(0, 1.65f, 0)));
            Capture("04-complete");
            yield return null;
        }

        [UnityTest] public IEnumerator CancelRestoresVisibleMagazineAndKeepsBothSpares()
        {
            Fire(); core.Reload("reload-play"); Step(1f);
            core.SetState("reload-play", SessionState.Paused);
            Assert.That(presentation.IsAnimating, Is.False);
            Assert.That(presentation.Magazine.localPosition, Is.EqualTo(seatedPosition));
            Assert.That(presentation.Magazine.GetComponent<MeshRenderer>().enabled, Is.True);
            Assert.That(core.GetSnapshot("reload-play").Data.Ammo.ReserveMagazines, Is.EqualTo(2));
            Assert.That(core.GetSnapshot("reload-play").Data.Ammo.CurrentMagazine, Is.EqualTo(29));
            Step(5f);
            Assert.That(core.GetSnapshot("reload-play").Data.Ammo.CurrentMagazine, Is.EqualTo(29));
            yield return null;
        }

        [UnityTest] public IEnumerator Bdd29_ReloadMagazineArmsAndFingersMirrorAtEveryCriticalPose()
        {
            Fire(); core.Reload("reload-play");
            var grab = rifle.GetComponent<TrainingRifleGrabInteractable>();
            var rightPosition = rifle.transform.position;
            var eyeLocal = eye.transform.InverseTransformPoint(rightPosition); eyeLocal.x = -eyeLocal.x;
            var leftPosition = eye.transform.TransformPoint(eyeLocal);
            foreach (var seconds in new[] { .5f, .5f, .7f })
            {
                Step(seconds);
                var rightGeometry = ReloadGeometry();
                Capture("mirror-right-" + presentation.Progress01.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
                Assert.That(core.ToggleShoulder("reload-play").Success, Is.True);
                grab.SetShoulder(ShoulderSide.Left);
                rifle.transform.position = leftPosition; presentation.Sample();
                Capture("mirror-left-" + presentation.Progress01.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
                var leftGeometry = ReloadGeometry();
                Assert.That(presentation.LeftHand.HandSide, Is.EqualTo(VirtualHandSide.Right));
                Assert.That(leftGeometry.Length, Is.EqualTo(rightGeometry.Length));
                for (var i = 0; i < rightGeometry.Length; i++)
                {
                    var expected = rightGeometry[i]; expected.x = -expected.x;
                    Assert.That(Vector3.Distance(leftGeometry[i], expected), Is.LessThan(.0001f),
                        "Reload pose " + presentation.Progress01 + ", rendered vertex " + i);
                }
                Assert.That(core.ToggleShoulder("reload-play").Success, Is.True);
                grab.SetShoulder(ShoulderSide.Right);
                rifle.transform.position = rightPosition; presentation.Sample();
                var restored = ReloadGeometry();
                for (var i = 0; i < restored.Length; i++)
                    Assert.That(Vector3.Distance(restored[i], rightGeometry[i]), Is.LessThan(.0001f));
            }
            Assert.That(eye.transform.position, Is.EqualTo(new Vector3(0, 1.65f, 0)));
            yield return null;
        }

        Vector3[] ReloadGeometry()
        {
            var points = new List<Vector3> { eye.transform.InverseTransformPoint(presentation.ReloadHandAnchor.position) };
            foreach (var filter in presentation.Magazine.GetComponentsInChildren<MeshFilter>(true))
            {
                // Production magazine meshes are GPU-only. The eight named local box
                // corners exercise its complete affine pose without changing import flags.
                var bounds = filter.sharedMesh.bounds;
                foreach (var x in new[] { bounds.min.x, bounds.max.x })
                    foreach (var y in new[] { bounds.min.y, bounds.max.y })
                        foreach (var z in new[] { bounds.min.z, bounds.max.z })
                            points.Add(eye.transform.InverseTransformPoint(filter.transform.TransformPoint(new Vector3(x, y, z))));
            }
            foreach (var hand in rifle.GetComponentsInChildren<VRControllerHandVisual>(true).OrderBy(h => h.SourceHandSide))
                foreach (var skin in hand.ModelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var mesh = new Mesh();
                    try
                    {
                        skin.BakeMesh(mesh, false);
                        foreach (var point in mesh.vertices)
                            points.Add(eye.transform.InverseTransformPoint(skin.transform.TransformPoint(point)));
                    }
                    finally { UnityEngine.Object.DestroyImmediate(mesh); }
                }
            return points.ToArray();
        }

        [UnityTest] public IEnumerator Bdd29_SwitchingShoulderDuringVrReloadRestoresTheNewSupportController()
        {
            core.Changed -= presentation.ApplySnapshot;
            foreach (var hand in rifle.GetComponentsInChildren<VRControllerHandVisual>(true)) hand.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(presentation); yield return null;
            testRig = new GameObject("ShoulderReloadRig");
            var leftController = new GameObject("Left Controller").transform;
            var rightController = new GameObject("Right Controller").transform;
            leftController.SetParent(testRig.transform, false); rightController.SetParent(testRig.transform, false);
            leftController.position = new Vector3(-.2f, 1.2f, .3f); rightController.position = new Vector3(.2f, 1.1f, .2f);
            var leftPose = leftController.localToWorldMatrix; var rightPose = rightController.localToWorldMatrix;
            var grab = rifle.GetComponent<TrainingRifleGrabInteractable>();
            foreach (var side in new[] { VirtualHandSide.Left, VirtualHandSide.Right })
            {
                var parent = side == VirtualHandSide.Left ? leftController : rightController;
                var hand = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Combat/Hands/VirtualHand_" + side), parent, false);
                hand.GetComponent<VRControllerHandVisual>().BindWeapon(grab);
            }
            presentation = rifle.AddComponent<CombatRifleReloadPresentation>();
            presentation.Configure(rifle.GetComponent<WeaponPrefabBinding>(), eye, testRig, true, () => clock.Now, true);
            core.Changed += presentation.ApplySnapshot;
            Fire(); core.Reload("reload-play"); Step(.5f);
            Assert.That(grab.FrontHandInteractionLocked, Is.True);
            core.ToggleShoulder("reload-play"); grab.SetShoulder(ShoulderSide.Left); presentation.Sample();
            Assert.That(presentation.LeftHand.HandSide, Is.EqualTo(VirtualHandSide.Right));
            Step(1.51f);
            Assert.That(presentation.IsAnimating, Is.False);
            Assert.That(presentation.LeftHand.transform.parent, Is.SameAs(rightController));
            Assert.That(grab.FrontHandInteractionLocked, Is.False);
            Assert.That(leftController.localToWorldMatrix, Is.EqualTo(leftPose));
            Assert.That(rightController.localToWorldMatrix, Is.EqualTo(rightPose));
            yield return null;
        }

        [UnityTest] public IEnumerator MovingAndRotatingEyeKeepsOffscreenMagazineBelowView()
        {
            Fire(); core.Reload("reload-play"); Step(1f);
            foreach (var pitch in new[] { -60f, 0f, 60f })
            {
                eye.transform.SetPositionAndRotation(new Vector3(3, 1.1f, -2), Quaternion.Euler(pitch, 90, 0));
                rifle.transform.SetPositionAndRotation(eye.transform.TransformPoint(new Vector3(.13f, -.17f, .28f)), eye.transform.rotation);
                presentation.Sample();
                Assert.That(eye.WorldToViewportPoint(presentation.ReloadHandAnchor.position).y, Is.LessThan(0));
            }
            yield return null;
        }

        [UnityTest] public IEnumerator VrAnimationPreservesControllersGripAnchorsAndHmd()
        {
            core.Changed -= presentation.ApplySnapshot;
            foreach (var hand in rifle.GetComponentsInChildren<VRControllerHandVisual>(true)) hand.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(presentation); yield return null;
            testRig = new GameObject("ReloadTrackedRig");
            var controller = new GameObject("Left Controller").transform;
            controller.SetParent(testRig.transform, false); controller.position = new Vector3(-.2f, 1.2f, .3f);
            var handObject = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Combat/Hands/VirtualHand_Left"), controller, false);
            handObject.GetComponent<VRControllerHandVisual>().BindWeapon(rifle.GetComponent<TrainingRifleGrabInteractable>());
            var weapon = rifle.GetComponent<WeaponPrefabBinding>();
            var rear = weapon.RearHandGrip.position; var front = weapon.FrontHandGrip.position;
            var trackedPosition = controller.position; var trackedRotation = controller.rotation;
            var headPosition = eye.transform.position; var headRotation = eye.transform.rotation;
            presentation = rifle.AddComponent<CombatRifleReloadPresentation>();
            var handLocalPosition = handObject.transform.localPosition;
            var handLocalRotation = handObject.transform.localRotation;
            presentation.Configure(weapon, eye, testRig, true, () => clock.Now, true);
            core.Changed += presentation.ApplySnapshot;
            presentation.ApplySnapshot(core.GetSnapshot("reload-play").Data);
            Fire(); core.Reload("reload-play"); Step(1f);
            Assert.That(presentation.LeftHand.HasPresentationGrip, Is.True);
            Assert.That(rifle.GetComponent<TrainingRifleGrabInteractable>().FrontHandInteractionLocked, Is.True);
            Assert.That(handObject.transform.parent, Is.EqualTo(presentation.ReloadHandAnchor), "The animated hand is detached from the tracked controller");
            Assert.That(controller.position, Is.EqualTo(trackedPosition)); Assert.That(controller.rotation, Is.EqualTo(trackedRotation));
            Assert.That(weapon.RearHandGrip.position, Is.EqualTo(rear)); Assert.That(weapon.FrontHandGrip.position, Is.EqualTo(front));
            Assert.That(eye.transform.position, Is.EqualTo(headPosition)); Assert.That(eye.transform.rotation, Is.EqualTo(headRotation));
            var animatedPosition = handObject.transform.position;
            var animatedRotation = handObject.transform.rotation;
            controller.SetPositionAndRotation(new Vector3(3, 2, -2), Quaternion.Euler(75, 45, 15));
            yield return null; presentation.Sample();
            Assert.That(Vector3.Distance(handObject.transform.position, animatedPosition), Is.LessThan(.0001f));
            Assert.That(Quaternion.Angle(handObject.transform.rotation, animatedRotation), Is.LessThan(.01f));
            core.SetTracking("reload-play", false);
            Assert.That(presentation.LeftHand.HasPresentationGrip, Is.False);
            Assert.That(presentation.LeftHand.PresentationCurl01, Is.Zero);
            Assert.That(presentation.Magazine.localPosition, Is.EqualTo(seatedPosition));
            Assert.That(rifle.GetComponent<TrainingRifleGrabInteractable>().FrontHandInteractionLocked, Is.False);
            Assert.That(handObject.transform.parent, Is.EqualTo(controller));
            Assert.That(handObject.transform.localPosition, Is.EqualTo(handLocalPosition));
            Assert.That(Quaternion.Angle(handObject.transform.localRotation, handLocalRotation), Is.LessThan(.01f));
            Assert.That(core.GetSnapshot("reload-play").Data.Ammo.ReserveMagazines, Is.EqualTo(2));
            yield return null;
        }

        [UnityTest] public IEnumerator ModeThreeCompletionAndDisableRestoreLeftControllerControl()
        {
            core.Changed -= presentation.ApplySnapshot;
            UnityEngine.Object.Destroy(presentation); yield return null;
            testRig = new GameObject("ReloadRestoreRig");
            var controller = new GameObject("Left Controller").transform; controller.SetParent(testRig.transform, false);
            var hand = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Combat/Hands/VirtualHand_Left"), controller, false);
            var grab = rifle.GetComponent<TrainingRifleGrabInteractable>();
            hand.GetComponent<VRControllerHandVisual>().BindWeapon(grab);
            presentation = rifle.AddComponent<CombatRifleReloadPresentation>();
            presentation.Configure(rifle.GetComponent<WeaponPrefabBinding>(), eye, testRig, true, () => clock.Now, true);
            core.Changed += presentation.ApplySnapshot;
            presentation.ApplySnapshot(core.GetSnapshot("reload-play").Data);
            core.Reload("reload-play");
            Assert.That(grab.FrontHandInteractionLocked, Is.False, "A full magazine must not lock the hand");
            Assert.That(hand.transform.parent, Is.EqualTo(controller));
            Fire(); core.Reload("reload-play"); Step(1f);
            Assert.That(grab.FrontHandInteractionLocked, Is.True);
            Step(1.01f);
            Assert.That(grab.FrontHandInteractionLocked, Is.False);
            Assert.That(presentation.LeftHand.HasPresentationGrip, Is.False);
            Assert.That(hand.transform.parent, Is.EqualTo(controller));
            var local = hand.transform.localPosition;
            controller.position += Vector3.right;
            Assert.That(hand.transform.localPosition, Is.EqualTo(local), "The hand once again follows its controller");
            Fire(); core.Reload("reload-play"); Step(.2f);
            presentation.enabled = false;
            Step(.3f);
            Assert.That(grab.FrontHandInteractionLocked, Is.False);
            Assert.That(hand.transform.parent, Is.EqualTo(controller));
            Assert.That(presentation.LeftHand.HasPresentationGrip, Is.False);
            yield return null;
        }

        [UnityTest] public IEnumerator ModeThreeFrontGripLockCancelsLeftSelectionAndKeepsRightHandHoldingRifle()
        {
            testRig = new GameObject("ReloadSelectRig");
            var manager = testRig.AddComponent<XRInteractionManager>();
            var grab = rifle.GetComponent<TrainingRifleGrabInteractable>(); grab.interactionManager = manager; grab.enabled = true;
            XRDirectInteractor Hand(InteractorHandedness side)
            {
                var node = new GameObject(side.ToString()); node.transform.SetParent(testRig.transform);
                node.AddComponent<SphereCollider>().isTrigger = true;
                var hand = node.AddComponent<XRDirectInteractor>(); hand.interactionManager = manager; hand.handedness = side;
                return hand;
            }
            var right = Hand(InteractorHandedness.Right); var left = Hand(InteractorHandedness.Left);
            right.transform.position = grab.RearAttach.position; left.transform.position = grab.FrontAttach.position;
            manager.SelectEnter((IXRSelectInteractor)right, (IXRSelectInteractable)grab);
            manager.SelectEnter((IXRSelectInteractor)left, (IXRSelectInteractable)grab);
            Assert.That(grab.FrontHandSelected && grab.RearHandSelected, Is.True);
            grab.SetFrontHandInteractionLocked(true);
            Assert.That(grab.FrontHandSelected, Is.False); Assert.That(grab.RearHandSelected, Is.True);
            Assert.That(grab.IsSelectableBy((IXRSelectInteractor)left), Is.False);
            Assert.That(grab.IsHoverableBy((IXRHoverInteractor)left), Is.False);
            grab.SetFrontHandInteractionLocked(false);
            Assert.That(grab.IsSelectableBy((IXRSelectInteractor)left), Is.True);
            manager.SelectEnter((IXRSelectInteractor)left, (IXRSelectInteractable)grab);
            Assert.That(grab.FrontHandSelected, Is.True);
            grab.ResetToRack();
            yield return null;
        }

        [UnityTest] public IEnumerator Bdd29_RealXriSelectionsTransferRearFirstAndLockOnlyTheNewSupportHand()
        {
            testRig = new GameObject("ShoulderSelectRig");
            var manager = testRig.AddComponent<XRInteractionManager>();
            var grab = rifle.GetComponent<TrainingRifleGrabInteractable>(); grab.interactionManager = manager; grab.enabled = true;
            XRDirectInteractor Hand(InteractorHandedness side)
            {
                var node = new GameObject(side.ToString()); node.transform.SetParent(testRig.transform);
                node.AddComponent<SphereCollider>().isTrigger = true;
                var hand = node.AddComponent<XRDirectInteractor>(); hand.interactionManager = manager; hand.handedness = side;
                return hand;
            }
            var right = Hand(InteractorHandedness.Right); var left = Hand(InteractorHandedness.Left);
            right.transform.position = grab.RearAttach.position; left.transform.position = grab.FrontAttach.position;
            manager.SelectEnter((IXRSelectInteractor)right, (IXRSelectInteractable)grab);
            manager.SelectEnter((IXRSelectInteractor)left, (IXRSelectInteractable)grab);
            var leftPose = left.transform.localToWorldMatrix; var rightPose = right.transform.localToWorldMatrix;
            grab.SetShoulder(ShoulderSide.Left);
            Assert.That(grab.RearHandSelected && grab.FrontHandSelected, Is.True);
            Assert.That(grab.interactorsSelecting[0], Is.SameAs(left));
            Assert.That(grab.GetAttachTransform((IXRInteractor)left), Is.SameAs(grab.RearAttach));
            Assert.That(grab.GetAttachTransform((IXRInteractor)right), Is.SameAs(grab.FrontAttach));
            Assert.That(left.transform.localToWorldMatrix, Is.EqualTo(leftPose));
            Assert.That(right.transform.localToWorldMatrix, Is.EqualTo(rightPose));
            grab.SetFrontHandInteractionLocked(true);
            Assert.That(grab.RearHandSelected, Is.True); Assert.That(grab.FrontHandSelected, Is.False);
            Assert.That(grab.IsSelectableBy((IXRSelectInteractor)right), Is.False);
            Assert.That(grab.IsHoverableBy((IXRHoverInteractor)right), Is.False);
            grab.SetFrontHandInteractionLocked(false);
            right.transform.position = grab.FrontAttach.position;
            manager.SelectEnter((IXRSelectInteractor)right, (IXRSelectInteractable)grab);
            grab.SetShoulder(ShoulderSide.Right);
            Assert.That(grab.interactorsSelecting[0], Is.SameAs(right));
            Assert.That(grab.RearHandSelected && grab.FrontHandSelected, Is.True);
            grab.ResetToRack();
            yield return null;
        }

        void Fire() => Assert.That(core.Fire(new WeaponFireInputDto { SessionId = "reload-play", AimDirection = Vector3.forward }).Success);
        void Step(float seconds) { clock.Step(seconds); core.Advance("reload-play"); presentation.Sample(); }
        void Capture(string name)
        {
            const string folder = "Logs/task020-x-reload-evidence";
            System.IO.Directory.CreateDirectory(folder);
            var target = RenderTexture.GetTemporary(960, 720, 24);
            var previousTarget = eye.targetTexture; var previousActive = RenderTexture.active;
            var pixels = new Texture2D(960, 720, TextureFormat.RGB24, false);
            try
            {
                eye.targetTexture = target; eye.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 960, 720), 0, 0); pixels.Apply();
                System.IO.File.WriteAllBytes(folder + "/" + name + ".png", pixels.EncodeToPNG());
                System.IO.File.WriteAllText(folder + "/" + name + ".txt", "eye=" + eye.transform.position + " rifle=" + rifle.transform.position + "\n" +
                    string.Join("\n", Array.ConvertAll(rifle.GetComponentsInChildren<Renderer>(true), r => r.name + " enabled=" + r.enabled + " active=" + r.gameObject.activeInHierarchy + " bounds=" + r.bounds)));
            }
            finally
            {
                eye.targetTexture = previousTarget; RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(target); UnityEngine.Object.Destroy(pixels);
            }
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (core != null) { core.Changed -= presentation.ApplySnapshot; core.Dispose(); }
            if (rifle != null) UnityEngine.Object.Destroy(rifle);
            if (cameraObject != null) UnityEngine.Object.Destroy(cameraObject);
            if (testRig != null) UnityEngine.Object.Destroy(testRig);
            yield return null;
        }
    }
}
