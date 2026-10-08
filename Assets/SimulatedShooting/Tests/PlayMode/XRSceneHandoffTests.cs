using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.UI;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Unity.Bootstrap;
using VRShooting.Unity.UI;

namespace SimulatedShooting.Tests.PlayMode
{
    // BDD00 real head/hand tracking; BDD18 map selection; BDD14/21 scene exit/re-entry.
    public sealed class XRSceneHandoffTests
    {
        System.Func<ICombatSceneLoader> previousFactory;
        XRHMD head;
        XRController left, right;
        readonly System.Collections.Generic.List<UnityEngine.InputSystem.InputDevice> suspendedDevices=new System.Collections.Generic.List<UnityEngine.InputSystem.InputDevice>();
        readonly System.Collections.Generic.Dictionary<InputActionAsset,UnityEngine.InputSystem.InputDevice[]> deviceFilters=new System.Collections.Generic.Dictionary<InputActionAsset,UnityEngine.InputSystem.InputDevice[]>();

        [UnitySetUp] public IEnumerator Setup()
        {
            previousFactory=ApplicationServices.CombatSceneLoaderFactory;
            if(GameMain.Instance!=null)
            {
                GameMain.Instance.GetComponent<MainMenuXRModeController>().enabled=false;
                Object.Destroy(GameMain.Instance.gameObject);
            }
            foreach(var manager in Object.FindObjectsOfType<InputActionManager>(true))manager.gameObject.SetActive(false);
            InputSystem.DisableAllEnabledActions();
            yield return null;
            ApplicationServices.CombatSceneLoaderFactory=()=>new UnityCombatSceneLoader(()=>true);
            foreach(var device in InputSystem.devices.Where(d=>d.enabled&&(d is XRHMD||d is XRController)).ToArray())
            {suspendedDevices.Add(device);InputSystem.DisableDevice(device);}
            InputSystem.RegisterLayout(@"{""name"":""SceneHandoffController"",""extend"":""XRController"",""controls"":[
                {""name"":""gripPressed"",""layout"":""Button"",""format"":""FLT"",""sizeInBits"":32,""usages"":[""GripButton""]},
                {""name"":""grip"",""layout"":""Axis""},
                {""name"":""triggerPressed"",""layout"":""Button"",""format"":""FLT"",""sizeInBits"":32,""usages"":[""TriggerButton""]},
                {""name"":""trigger"",""layout"":""Axis""}]}");
            head=InputSystem.AddDevice<XRHMD>();
            left=(XRController)InputSystem.AddDevice("SceneHandoffController");
            right=(XRController)InputSystem.AddDevice("SceneHandoffController");
            InputSystem.SetDeviceUsage(left,UnityEngine.InputSystem.CommonUsages.LeftHand);
            InputSystem.SetDeviceUsage(right,UnityEngine.InputSystem.CommonUsages.RightHand);
            yield return SceneManager.LoadSceneAsync("MainScene");
            foreach(var asset in Object.FindObjectsOfType<InputActionManager>(true).SelectMany(m=>m.actionAssets).Distinct())
            {
                deviceFilters[asset]=asset.devices.HasValue?asset.devices.Value.ToArray():null;
                var enabledActions=asset.Select(a=>a).Where(a=>a.enabled).ToArray();
                asset.Disable();
                asset.devices=new UnityEngine.InputSystem.InputDevice[]{head,left,right};
                foreach(var action in enabledActions)action.Enable();
            }
            GameMain.Instance.GetComponent<MainMenuXRModeController>().SetVrModeForTests(true);
            // Keep real pose drivers enabled: this suite sends device state, not Transform overrides.
            foreach(var pose in Object.FindObjectsOfType<TrackedPoseDriver>(true))pose.enabled=true;
            yield return null;
        }

        // BDD 02 "主菜单双摇杆各司其职".
        [UnityTest] public IEnumerator Screen02_MainMenuUsesLeftMoveAndRightSnapTurnOnly()
        {
            var rig=GameMain.Instance.GetComponent<MainMenuXRModeController>().VrCamera.transform.root;
            Assert.That(rig.GetComponentsInChildren<VRControllerHandVisual>().Count(h=>h.HasRenderableHand),Is.EqualTo(2),"Main menu also uses human hands");
            var move=rig.GetComponentInChildren<ContinuousMoveProvider>(true);
            var snap=rig.GetComponentInChildren<SnapTurnProvider>(true);
            var smooth=rig.GetComponentInChildren<ContinuousTurnProvider>(true);
            Assert.That(move,Is.Not.Null);
            Assert.That(snap,Is.Not.Null);
            Assert.That(smooth,Is.Not.Null);
            Assert.That(move.isActiveAndEnabled,Is.True);
            Assert.That(move.leftHandMoveInput.inputSourceMode,Is.EqualTo(XRInputValueReader.InputSourceMode.InputActionReference));
            Assert.That(move.rightHandMoveInput.inputSourceMode,Is.EqualTo(XRInputValueReader.InputSourceMode.Unused));
            Assert.That(snap.isActiveAndEnabled,Is.True);
            Assert.That(snap.leftHandTurnInput.inputSourceMode,Is.EqualTo(XRInputValueReader.InputSourceMode.Unused));
            Assert.That(snap.rightHandTurnInput.inputSourceMode,Is.EqualTo(XRInputValueReader.InputSourceMode.InputActionReference));
            Assert.That(smooth.enabled,Is.False);
            yield return null;
        }

        [UnityTest] public IEnumerator Screen00_RangeAutomaticallyChoosesRunningDisplay()
        {
            foreach(var scene in new[]{"MovingTargetRangeScene","ZeroingRangeScene"})
            {
                yield return SceneManager.LoadSceneAsync(scene);
                yield return null;
                var displays=new System.Collections.Generic.List<XRDisplaySubsystem>();
                SubsystemManager.GetInstances(displays);
                Assert.That(Object.FindObjectOfType<ZeroingRangeXRModeController>().IsVrMode,
                    Is.EqualTo(displays.Any(d=>d.running)),"Default scene entry must detect the headset without a forced test mode: "+scene);
            }
        }

        [UnityTest] public IEnumerator Screen18_21_VrActionsAndTrackedPosesSurviveUrbanLoadAndReturn()
        {
            var app=GameMain.Instance.Services.Combat;
            for(int round=0;round<2;round++)
            {
                app.OpenMode(TrainingMode.Urban);
                var load=app.SelectMapAsync("urban-a",VRShooting.Contracts.RandomSeed.Fixed(71));
                while(!load.IsCompleted)yield return null;
                Assert.That(load.Result.Success,Is.True,load.Result.Message);
                var runtime=Object.FindObjectOfType<CombatSceneRuntime>();
                runtime.ManualStepping=true;
                Assert.That(runtime.IsVr,Is.True);
                Assert.That(Object.FindObjectsOfType<AudioListener>().Count(l=>l.isActiveAndEnabled),Is.EqualTo(1));
                AssertActions(runtime.PlayerRoot.gameObject);
                yield return CheckTrackedHead(runtime.PlayerCamera);
                yield return GrabWithControllers(runtime.PlayerRoot.gameObject,runtime.GetComponentsInChildren<TrainingRifleGrabInteractable>().Single());
                app.ReturnToMainMenu();
                // Wait until the old scene really unloads: its OnDisable must not switch off MainScene input.
                while(SceneManager.GetSceneByName("CombatScene").isLoaded)yield return null;
                yield return null;
                var camera=GameMain.Instance.GetComponent<MainMenuXRModeController>().VrCamera;
                AssertActions(camera.GetComponentInParent<InputActionManager>().gameObject);
                yield return CheckTrackedHead(camera);
            }
        }

        [UnityTest] public IEnumerator Bdd23_Task019_TrenchReconThenTrackedHandsGrabStableRifle()
        {
            var app=GameMain.Instance.Services.Combat;
            app.OpenMode(TrainingMode.Trench);
            var load=app.SelectMapAsync("trench-a",VRShooting.Contracts.RandomSeed.Fixed(71));
            while(!load.IsCompleted)yield return null;
            Assert.That(load.Result.Success,Is.True,load.Result.Message);
            var runtime=Object.FindObjectOfType<CombatSceneRuntime>();runtime.ManualStepping=true;
            Assert.That(runtime.IsVr,Is.True);
            AssertActions(runtime.PlayerRoot.gameObject);
            yield return CheckTrackedHead(runtime.PlayerCamera);
            Assert.That(app.Start().Success,Is.True);
            runtime.InputOverride=new VRShooting.Input.ManualXRTrainingInput();
            runtime.FrameOverride=new CombatInputFrameDto {HeadTracked=true,RearHandTracked=true,FrontHandTracked=true,
                PlayerPosition=runtime.PlayerRoot.position,PlayerForward=runtime.PlayerRoot.forward};
            for(int frame=0;frame<1200&&!app.Mission.HasCombatStarted;frame++)
            {
                runtime.Step(.1f);
                Assert.That(runtime.LastFrameResult.Success,Is.True,app.Mission.Opening?.Phase+": "+runtime.LastFrameResult.Message);
                if(frame%20==0)yield return null;
            }
            Assert.That(app.Mission.HasCombatStarted,Is.True);
            runtime.Step(.02f);runtime.InputOverride=null;runtime.FrameOverride=null;
            yield return GrabWithControllers(runtime.PlayerRoot.gameObject,runtime.GetComponentsInChildren<TrainingRifleGrabInteractable>().Single());
            Assert.That(Object.FindObjectsOfType<AudioListener>().Count(l=>l.isActiveAndEnabled),Is.EqualTo(1));
            app.ReturnToMainMenu();
            while(SceneManager.GetSceneByPath(UnityCombatSceneLoader.TrenchScenePath).isLoaded)yield return null;
            AssertActions(GameMain.Instance.GetComponent<MainMenuXRModeController>().VrCamera.GetComponentInParent<InputActionManager>().gameObject);
        }

        [UnityTest] public IEnumerator Screen00_MovingRangeHeadAndBothHandsReceiveDevicePoses()
        {
            yield return SceneManager.LoadSceneAsync("MovingTargetRangeScene");
            var mode=Object.FindObjectOfType<ZeroingRangeXRModeController>();
            mode.SetVrModeForTests(true);
            yield return null;
            AssertActions(mode.XrOrigin);
            yield return CheckTrackedHead(mode.XrOrigin.GetComponentInChildren<Camera>());
            var p=new Vector3(.3f,1.1f,.5f);
            QueueController(right,p);QueueController(left,new Vector3(-.3f,1.1f,.5f));
            yield return null;yield return null;
            foreach(var name in new[]{"Left Controller","Right Controller"})
            {
                var pose=mode.XrOrigin.GetComponentsInChildren<TrackedPoseDriver>(true).Single(t=>t.name==name);
                Assert.That(pose.isActiveAndEnabled,Is.True,name+" must become tracked");
                Assert.That(pose.transform.localPosition.y,Is.EqualTo(1.1f).Within(.01f));
                Assert.That(pose.GetComponentsInChildren<VRControllerHandVisual>().Any(h=>h.HasRenderableHand),Is.True,name+" hand must be visible");
            }
            foreach(var direct in mode.XrOrigin.GetComponentsInChildren<XRDirectInteractor>(true))
            {
                Assert.That(direct.selectInput.inputActionValue.bindings.Count,Is.GreaterThan(0),"Grip Value must have its own analog binding");
                Assert.That(direct.selectInput.inputActionValue.bindings.Any(b=>b.path.EndsWith("/grip")),Is.True);
            }
            var rifle=Object.FindObjectOfType<TrainingRifleGrabInteractable>();
            yield return GrabWithControllers(mode.XrOrigin,rifle);
        }

        IEnumerator GrabWithControllers(GameObject rig,TrainingRifleGrabInteractable rifle)
        {
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<ButtonControl>("gripPressed"),0f);
            InputSystem.QueueDeltaStateEvent(left.GetChildControl<ButtonControl>("gripPressed"),0f);
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<AxisControl>("grip"),0f);
            InputSystem.QueueDeltaStateEvent(left.GetChildControl<AxisControl>("grip"),0f);
            yield return null;
            var rightPose=rig.GetComponentsInChildren<TrackedPoseDriver>(true).Single(t=>t.name=="Right Controller");
            foreach(var side in new[]{"Left Controller","Right Controller"})
            {
                var tracked=rig.GetComponentsInChildren<TrackedPoseDriver>(true).Single(t=>t.name==side);
                var hand=tracked.GetComponentInChildren<VRControllerHandVisual>();
                Assert.That(hand!=null&&hand.HasRenderableHand,Is.True,side+" must show a human hand");
                Assert.That(tracked.GetComponentsInChildren<Renderer>().Where(r=>r is MeshRenderer||r is SkinnedMeshRenderer)
                    .Where(r=>r.GetComponentInParent<VRControllerHandVisual>()==null&&r.GetComponentInParent<TrainingRifleGrabInteractable>()==null)
                    .All(r=>!r.enabled),Is.True,"Hardware controller meshes must stay hidden");
            }
            // Device position reaches the production direct interactor; do not call SelectEnter manually.
            QueueController(right,rightPose.transform.parent.InverseTransformPoint(rifle.RearAttach.position));
            yield return new WaitForSeconds(.15f);
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<ButtonControl>("gripPressed"),1f);
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<AxisControl>("grip"),1f);
            yield return new WaitForSeconds(.15f);
            Assert.That(rifle.RearHandSelected,Is.True,"Physical right grip must pick up the rifle; "+string.Join(";",
                rig.GetComponentsInChildren<XRBaseInputInteractor>(true).Where(i=>i.handedness==InteractorHandedness.Right)
                    .Select(i=>i.name+" active="+i.isActiveAndEnabled+" grip="+i.selectInput.ReadIsPerformed()+
                        " distance="+Vector3.Distance(i.transform.position,rifle.RearAttach.position)+" hover="+i.hasHover)));
            yield return new WaitForSeconds(.5f);
            var leftPose=rig.GetComponentsInChildren<TrackedPoseDriver>(true).Single(t=>t.name=="Left Controller");
            QueueController(left,leftPose.transform.parent.InverseTransformPoint(rifle.FrontAttach.position));
            yield return new WaitForSeconds(.15f);
            InputSystem.QueueDeltaStateEvent(left.GetChildControl<ButtonControl>("gripPressed"),1f);
            InputSystem.QueueDeltaStateEvent(left.GetChildControl<AxisControl>("grip"),1f);
            yield return new WaitForSeconds(.15f);
            Assert.That(rifle.FrontHandSelected,Is.True,"Physical left grip must form a two-hand hold; distance="+
                Vector3.Distance(leftPose.transform.position,rifle.FrontAttach.position)+" selected="+string.Join(";",rifle.interactorsSelecting.Select(i=>i.transform.name)));
            // BDD23 task019: stable device poses while the tracking origin translates.
            var heldOffset=rightPose.transform.InverseTransformPoint(rifle.RearAttach.position);
            var heldRotation=Quaternion.Inverse(rightPose.transform.rotation)*rifle.transform.rotation;
            for(int frame=0;frame<45;frame++)
            {
                rig.transform.position+=rig.transform.forward*.015f;
                yield return null;
                Assert.That(Vector3.Distance(rightPose.transform.InverseTransformPoint(rifle.RearAttach.position),heldOffset),Is.LessThan(.005f),"Walking must not make the gun chase the hand");
                Assert.That(Quaternion.Angle(Quaternion.Inverse(rightPose.transform.rotation)*rifle.transform.rotation,heldRotation),Is.LessThan(.5f));
            }
            var hands=rig.GetComponentsInChildren<VRControllerHandVisual>();
            var gripOffsets=hands.Select(h=>h.GripAnchor.InverseTransformPoint(h.transform.position)).ToArray();
            // Simulate an origin change after Dynamic update and run Unity's real render callbacks.
            rig.transform.position+=rig.transform.forward*.03f;
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            // Invoke the production XRI/hand callbacks without re-entering native input updates in the test runner.
            var managerRender=typeof(UnityEngine.XR.Interaction.Toolkit.XRInteractionManager).GetMethod("OnBeforeRender",flags);
            managerRender.Invoke(rifle.interactionManager,null);
            var handRender=typeof(VRControllerHandVisual).GetMethod("UpdateBeforeRender",flags);
            var order=(BeforeRenderOrderAttribute)System.Attribute.GetCustomAttribute(handRender,typeof(BeforeRenderOrderAttribute));
            Assert.That(order.order,Is.GreaterThan(UnityEngine.XR.Interaction.Toolkit.XRInteractionUpdateOrder.k_BeforeRenderOrder));
            foreach(var hand in hands)handRender.Invoke(hand,null);
            Assert.That(Vector3.Distance(rightPose.transform.InverseTransformPoint(rifle.RearAttach.position),heldOffset),Is.LessThan(.005f),"BeforeRender must update the held gun");
            for(int i=0;i<hands.Length;i++)
                Assert.That(Vector3.Distance(hands[i].GripAnchor.InverseTransformPoint(hands[i].transform.position),gripOffsets[i]),Is.LessThan(.001f),"Hand visuals must follow the latest gun pose before rendering");
            var heldCamera=rig.GetComponentInChildren<Camera>();
            // Lift the real tracked hands into the HMD aiming corridor for first-person evidence.
            var aimRotation=heldCamera.transform.rotation;
            var rearTarget=heldCamera.transform.position+heldCamera.transform.forward*.45f-heldCamera.transform.up*.18f+heldCamera.transform.right*.1f;
            var gripDelta=rifle.transform.InverseTransformDirection(rifle.FrontAttach.position-rifle.RearAttach.position);
            var frontTarget=rearTarget+aimRotation*heldOffset+(aimRotation*heldRotation)*gripDelta;
            QueueController(right,rightPose.transform.parent.InverseTransformPoint(rearTarget),Quaternion.Inverse(rightPose.transform.parent.rotation)*aimRotation);
            QueueController(left,leftPose.transform.parent.InverseTransformPoint(frontTarget),Quaternion.Inverse(leftPose.transform.parent.rotation)*aimRotation);
            yield return new WaitForSeconds(.15f);
            Assert.That(rifle.RearHandSelected&&rifle.FrontHandSelected,Is.True,"Raising to aim must keep both grips");
            foreach(var hand in hands)
            {
                var renderer=hand.ModelRoot.GetComponentsInChildren<Renderer>().First(r=>r.enabled&&!r.forceRenderingOff&&r.gameObject.activeInHierarchy);
                var point=heldCamera.WorldToViewportPoint(renderer.bounds.center);
                Assert.That(point.z,Is.GreaterThan(heldCamera.nearClipPlane),"Hand must be in front of the HMD near plane");
                Assert.That(point.x,Is.InRange(0f,1f),"Human hand must be in the HMD horizontal view");
                Assert.That(point.y,Is.InRange(0f,1f),"Human hand must be in the HMD vertical view");
            }
            PresentationEvidence.Capture(heldCamera,"VR-human-hands-"+rifle.gameObject.scene.name);
        }

        [UnityTest] public IEnumerator Screen14_18_TrackedTriggerClicksMapsAndBriefingAcrossRigHandoff()
        {
            var app=GameMain.Instance.Services.Combat;
            var ui=GameMain.Instance.GetComponent<P3LiveUIController>().View;
            foreach(var mode in new[]{TrainingMode.Trench,TrainingMode.Urban})
            {
                app.OpenMode(mode);
                var camera=GameMain.Instance.GetComponent<MainMenuXRModeController>().VrCamera;
                ui.GetComponent<TrainingUICanvasAdapter>().SetMode(true,camera);
                yield return CheckTrackedHead(camera);
                yield return ClickWithController(mode==TrainingMode.Trench?ui.TrenchMapView.SelectButton:ui.UrbanMapView.SelectButton,camera);
                float deadline=Time.realtimeSinceStartup+30;
                while(app.Snapshot.Busy&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(app.Snapshot.Error,Is.EqualTo(VRShooting.Contracts.ErrorCode.None));
                var runtime=Object.FindObjectOfType<CombatSceneRuntime>();
                Assert.That(runtime,Is.Not.Null,"XR trigger must actually load the selected map");
                runtime.ManualStepping=true;
                AssertActions(runtime.PlayerRoot.gameObject);
                if(mode==TrainingMode.Trench)
                {
                    yield return CheckTrackedHead(runtime.PlayerCamera);
                    yield return ClickWithController(ui.TrenchBriefingView.StartButton,runtime.PlayerCamera);
                    Assert.That(app.Snapshot.Screen,Is.EqualTo(ScreenId.TrenchDroneRecon),"Incoming rig must click start without bypassing recon");
                }
                app.ReturnToMainMenu();
                while(SceneManager.GetSceneByPath(UnityCombatSceneLoader.ScenePathFor(mode)).isLoaded)yield return null;
                yield return null;
            }
        }

        IEnumerator ClickWithController(Button button,Camera camera)
        {
            var adapter=button.GetComponentInParent<TrainingUICanvasAdapter>();
            adapter.ForcePlacementForTests();
            Canvas.ForceUpdateCanvases();
            var rig=camera.GetComponentInParent<InputActionManager>();
            var pose=rig.GetComponentsInChildren<TrackedPoseDriver>(true).Single(t=>t.name=="Right Controller");
            var center=((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center);
            var origin=camera.transform.position+camera.transform.right*.15f-camera.transform.up*.1f;
            QueueController(right,pose.transform.parent.InverseTransformPoint(origin),
                Quaternion.Inverse(pose.transform.parent.rotation)*Quaternion.LookRotation(center-origin));
            yield return new WaitForSeconds(.3f);
            var rays=rig.GetComponentsInChildren<NearFarInteractor>().Where(r=>r.handedness==UnityEngine.XR.Interaction.Toolkit.Interactors.InteractorHandedness.Right).ToArray();
            // Production ray stabilization settles gradually after an instantaneous device pose change.
            var settleDeadline=Time.realtimeSinceStartup+2f;
            while(Time.realtimeSinceStartup<settleDeadline&&!rays.Any(r=>r.TryGetUIModel(out var value)&&value.currentRaycast.gameObject!=null&&
                (value.currentRaycast.gameObject==button.gameObject||value.currentRaycast.gameObject.transform.IsChildOf(button.transform))))
                yield return null;
            Assert.That(rays.Any(r=>r.TryGetUIModel(out var model)&&model.currentRaycast.gameObject!=null&&
                (model.currentRaycast.gameObject==button.gameObject||model.currentRaycast.gameObject.transform.IsChildOf(button.transform))),Is.True,
                "Production tracked ray must hit "+button.name+"; canvasVr="+adapter.IsVrMode+" button="+button.gameObject.activeInHierarchy+
                " camera="+camera.transform.position+" center="+center+" now="+((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center)+
                " pose="+pose.transform.position+" forward="+pose.transform.forward+"; "+string.Join(";",rays.Select(r=>
                    "rayActive="+r.isActiveAndEnabled+" far="+r.enableFarCasting+" ui="+r.enableUIInteraction+
                    " origin="+r.farInteractionCaster.effectiveCastOrigin.position+" dir="+r.farInteractionCaster.effectiveCastOrigin.forward+
                    (r.TryGetUIModel(out var m)?" hit="+m.currentRaycast.gameObject:" no model"))));
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<ButtonControl>("triggerPressed"),1f);
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<AxisControl>("trigger"),1f);
            yield return new WaitForSeconds(.1f);
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<ButtonControl>("triggerPressed"),0f);
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<AxisControl>("trigger"),0f);
            yield return new WaitForSeconds(.1f);
        }

        IEnumerator CheckTrackedHead(Camera camera)
        {
            var rotation=Quaternion.Euler(8,31,0);
            InputSystem.QueueDeltaStateEvent(head.centerEyePosition,new Vector3(0,1.4f,0));
            InputSystem.QueueDeltaStateEvent(head.centerEyeRotation,rotation);
            InputSystem.QueueDeltaStateEvent(head.isTracked,(byte)1);
            InputSystem.QueueDeltaStateEvent(head.trackingState,3);
            yield return null;yield return null;
            var driver=camera.GetComponent<TrackedPoseDriver>();
            Assert.That(Quaternion.Angle(camera.transform.localRotation,rotation),Is.LessThan(.5f),
                "HMD rotation must reach the live camera; actual="+camera.transform.localRotation+" device="+head.centerEyeRotation.ReadValue()+
                " action="+driver.rotationInput.action.ReadValue<Quaternion>()+" tracking="+driver.trackingStateInput.action.ReadValue<int>()+
                " controls="+string.Join(";",driver.rotationInput.action.controls.Select(c=>c.path+" enabled="+c.device.enabled)));
        }
        static void AssertActions(GameObject rig)
        {
            var manager=rig.GetComponentInChildren<InputActionManager>(true);
            Assert.That(manager.isActiveAndEnabled,Is.True);
            foreach(var pose in rig.GetComponentsInChildren<TrackedPoseDriver>(true))
            {
                Assert.That(pose.positionInput.action.enabled,Is.True,pose.name+" position action disabled by another rig");
                Assert.That(pose.rotationInput.action.enabled,Is.True,pose.name+" rotation action disabled by another rig");
            }
            foreach(var map in manager.actionAssets.SelectMany(a=>a.actionMaps).Where(m=>m.name.Contains("Interaction")))
                Assert.That(map.enabled,Is.True,map.name+" must read grip and UI trigger");
        }
        static void QueueController(XRController device,Vector3 position,Quaternion? rotation=null)
        {
            InputSystem.QueueDeltaStateEvent(device.devicePosition,position);
            InputSystem.QueueDeltaStateEvent(device.deviceRotation,rotation??Quaternion.identity);
            InputSystem.QueueDeltaStateEvent(device.isTracked,(byte)1);
            InputSystem.QueueDeltaStateEvent(device.trackingState,3);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            GameMain.Instance?.Services.Combat.ReturnToMainMenu();
            // Stop the menu controller before yielding: it otherwise reactivates the rig
            // while device filters are replaced, leaving readers on disposed input state.
            if(GameMain.Instance!=null)
            {
                GameMain.Instance.GetComponent<MainMenuXRModeController>().enabled=false;
                Object.Destroy(GameMain.Instance.gameObject);
            }
            // Stop consumers before replacing device filters/removing devices held by XRI selections.
            foreach(var manager in Object.FindObjectsOfType<InputActionManager>(true))manager.gameObject.SetActive(false);
            InputSystem.DisableAllEnabledActions();
            yield return null;
            foreach(var entry in deviceFilters)
            {
                entry.Key.Disable();
                entry.Key.devices=entry.Value;
            }
            deviceFilters.Clear();
            foreach(var device in new UnityEngine.InputSystem.InputDevice[]{head,left,right})
                if(device!=null&&device.added)InputSystem.RemoveDevice(device);
            InputSystem.RemoveLayout("SceneHandoffController");
            foreach(var device in suspendedDevices)if(device.added)InputSystem.EnableDevice(device);
            suspendedDevices.Clear();
            ApplicationServices.CombatSceneLoaderFactory=previousFactory;
            yield return null;
            yield return SceneManager.LoadSceneAsync("MainScene");
            GameMain.Instance?.GetComponent<MainMenuXRModeController>()?.ClearForcedModeForTests();
            yield return null;
        }
    }
}
