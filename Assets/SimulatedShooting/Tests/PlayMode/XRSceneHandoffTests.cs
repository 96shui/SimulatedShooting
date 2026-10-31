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
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using Unity.XR.CoreUtils;
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

        // BDD02: a retained empty/removed-device filter must not freeze native tracking.
        [UnityTest] public IEnumerator Screen02_MenuRecoversEmptyAndRemovedDeviceFilters()
        {
            var camera=GameMain.Instance.GetComponent<MainMenuXRModeController>().VrCamera;
            var asset=camera.GetComponentInParent<InputActionManager>().actionAssets.First();
            asset.Disable();
            asset.devices=new UnityEngine.InputSystem.InputDevice[0];
            yield return null;yield return null;
            Assert.That(asset.devices.HasValue,Is.False,"Empty devices blocks every native head/hand binding");
            Assert.That(camera.GetComponent<TrackedPoseDriver>().positionInput.action.controls.Count,Is.GreaterThan(0));
            var removed=InputSystem.AddDevice<XRHMD>();
            asset.Disable();asset.devices=new UnityEngine.InputSystem.InputDevice[]{removed};
            InputSystem.RemoveDevice(removed);
            yield return null;yield return null;
            Assert.That(asset.devices.HasValue,Is.False,"Removed test devices must not remain an exclusive filter");
            asset.Disable();asset.devices=new UnityEngine.InputSystem.InputDevice[]{head,left,right};
            yield return null;yield return null;
            Assert.That(asset.devices.HasValue,Is.True,"A live-device test filter must be preserved");
            Assert.That(asset.devices.Value.Count,Is.EqualTo(3));
            yield return CheckTrackedHead(camera);
        }

        // BDD 02 "主菜单双摇杆各司其职".
        [UnityTest] public IEnumerator Bdd34_MenuHandsHideUntilTrackedAndRecoverWithoutDisablingUi()
        {
            var camera=GameMain.Instance.GetComponent<MainMenuXRModeController>().VrCamera;
            var rig=camera.GetComponentInParent<XROrigin>().gameObject;
            var hands=rig.GetComponentsInChildren<VRControllerHandVisual>();
            var rightHand=hands.Single(h=>h.SourceHandSide==VirtualHandSide.Right);
            var leftHand=hands.Single(h=>h.SourceHandSide==VirtualHandSide.Left);
            bool Hidden(VRControllerHandVisual hand)=>hand.ModelRoot.GetComponentsInChildren<Renderer>(true).All(r=>r.forceRenderingOff);
            InputSystem.QueueDeltaStateEvent(head.centerEyePosition,new Vector3(0,1.4f,0));
            InputSystem.QueueDeltaStateEvent(head.isTracked,(byte)1);InputSystem.QueueDeltaStateEvent(head.trackingState,3);
            InputSystem.QueueDeltaStateEvent(right.isTracked,(byte)0);InputSystem.QueueDeltaStateEvent(right.trackingState,0);
            InputSystem.QueueDeltaStateEvent(left.isTracked,(byte)0);InputSystem.QueueDeltaStateEvent(left.trackingState,0);
            yield return null;yield return null;yield return null;
            Assert.That(Hidden(rightHand)&&Hidden(leftHand),Is.True,"Untracked arm meshes must not fill the menu camera");
            Assert.That(rig.GetComponentsInChildren<NearFarInteractor>().Any(r=>r.isActiveAndEnabled&&r.enableUIInteraction),Is.True);
            CaptureMenuOcclusionEvidence(camera,"main-menu-after-untracked");
            var rightPosition=new Vector3(.3f,1f,.6f);var leftPosition=new Vector3(-.3f,1f,.6f);
            QueueController(right,rightPosition);QueueController(left,leftPosition);
            yield return null;yield return null;yield return null;
            Assert.That(Hidden(rightHand)||Hidden(leftHand),Is.False,"Tracked hands away from the eye must become visible");
            Assert.That(rightHand.ModelRoot.GetComponentsInChildren<Renderer>().All(r=>!r.forceRenderingOff),Is.True);
            var skin=rightHand.ModelRoot.GetComponentsInChildren<SkinnedMeshRenderer>().First();
            var modelPosition=rightHand.ModelRoot.position;
            rightHand.ModelRoot.position+=camera.transform.position-skin.bounds.center;
            yield return null;yield return null;
            Assert.That(skin.forceRenderingOff,Is.True,"A forearm enclosing the eye must hide even when its tracked hand is farther away");
            rightHand.ModelRoot.position=modelPosition;
            yield return null;yield return null;
            QueueController(right,head.centerEyePosition.ReadValue());
            yield return null;yield return null;yield return null;
            Assert.That(Hidden(rightHand),Is.True,"Even a tracked hand must not render inside the head");
            Assert.That(Hidden(leftHand),Is.False);
            QueueController(right,rightPosition);yield return new WaitForSeconds(.65f);
            Assert.That(Hidden(rightHand),Is.False,"A periodic model rescan must preserve the original visibility state");
            InputSystem.QueueDeltaStateEvent(right.isTracked,(byte)0);InputSystem.QueueDeltaStateEvent(right.trackingState,0);
            yield return null;yield return null;yield return null;
            Assert.That(Hidden(rightHand),Is.True,"Tracking loss must hide the last stale arm pose");
            QueueController(right,rightPosition);yield return null;yield return null;yield return null;
            Assert.That(Hidden(rightHand),Is.False);
            CaptureMenuOcclusionEvidence(camera,"main-menu-after-tracked");
        }

        [UnityTest] public IEnumerator Bdd34_MenuVisibilityProtectionDoesNotHideTrainingHands()
        {
            yield return SceneManager.LoadSceneAsync("ZeroingRangeScene");
            Object.FindObjectOfType<ZeroingRangeXRModeController>().SetVrModeForTests(true);
            InputSystem.QueueDeltaStateEvent(right.isTracked,(byte)0);InputSystem.QueueDeltaStateEvent(right.trackingState,0);
            InputSystem.QueueDeltaStateEvent(left.isTracked,(byte)0);InputSystem.QueueDeltaStateEvent(left.trackingState,0);
            yield return null;yield return null;
            var hands=Object.FindObjectsOfType<VRControllerHandVisual>();
            Assert.That(hands.Length,Is.EqualTo(2));
            Assert.That(hands.SelectMany(h=>h.ModelRoot.GetComponentsInChildren<Renderer>()).All(r=>!r.forceRenderingOff),Is.True);
        }

        static void CaptureMenuOcclusionEvidence(Camera camera,string name)
        {
#if UNITY_EDITOR
            const string folder="docs/codex-reports/evidence/main-menu-hand-occlusion-20261023";
            System.IO.Directory.CreateDirectory(folder);
            var texture=new Texture2D(1280,800,TextureFormat.RGB24,false);
            var render=RenderTexture.GetTemporary(1280,800,24);
            var previous=camera.targetTexture;var active=RenderTexture.active;var aspect=camera.aspect;
            try
            {
                camera.targetTexture=render;camera.aspect=1.6f;camera.Render();RenderTexture.active=render;
                texture.ReadPixels(new Rect(0,0,1280,800),0,0);texture.Apply();
                System.IO.File.WriteAllBytes(folder+"/"+name+".png",texture.EncodeToPNG());
            }
            finally{camera.targetTexture=previous;camera.aspect=aspect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(render);Object.Destroy(texture);}
#endif
        }

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

        [UnityTest] public IEnumerator Screen02_MainMenuEyeHeightVisibleRaysAndTrackedTriggerRecover()
        {
            var camera = GameMain.Instance.GetComponent<MainMenuXRModeController>().VrCamera;
            var origin = camera.GetComponentInParent<XROrigin>();
            Assert.That(origin.RequestedTrackingOriginMode, Is.EqualTo(XROrigin.TrackingOriginMode.Floor));
            Assert.That(origin.CameraYOffset, Is.EqualTo(1.65f).Within(.001f), "Device-origin fallback must have a standing eye offset");
            var deviceOrigin = origin.CurrentTrackingOriginMode == TrackingOriginModeFlags.Device;
            InputSystem.QueueDeltaStateEvent(head.centerEyePosition, new Vector3(0, deviceOrigin ? 0f : 1.65f, 0));
            InputSystem.QueueDeltaStateEvent(head.centerEyeRotation, Quaternion.identity);
            InputSystem.QueueDeltaStateEvent(head.isTracked, (byte)1);
            InputSystem.QueueDeltaStateEvent(head.trackingState, 3);
            yield return new WaitForSeconds(.3f);
            Assert.That(origin.transform.position.y, Is.InRange(-.05f, .05f), "The rig starts at the floor instead of falling from a raised root");
            Assert.That(camera.transform.position.y-origin.transform.position.y, Is.InRange(1.5f,1.8f));
            if(origin.CurrentTrackingOriginMode == TrackingOriginModeFlags.Floor)
                Assert.That(origin.CameraFloorOffsetObject.transform.localPosition.y, Is.Zero, "Floor tracking must not add a second eye height");
            var rig=origin.gameObject;
            var rightPose=rig.GetComponentsInChildren<TrackedPoseDriver>(true).Single(t=>t.name=="Right Controller");
            QueueController(right,rightPose.transform.parent.InverseTransformPoint(camera.transform.position-Vector3.up*.2f),
                Quaternion.Inverse(rightPose.transform.parent.rotation)*Quaternion.LookRotation(Vector3.up));
            yield return new WaitForSeconds(.3f);
            var curve=rightPose.GetComponentInChildren<CurveVisualController>(true);
            Assert.That(curve.isActiveAndEnabled,Is.True);
            Assert.That(curve.lineRenderer.enabled,Is.True,"A controller aiming away from the menu must still show a usable UI ray");
            var manager=rig.GetComponent<InputActionManager>();
            manager.DisableInput();
            yield return null;yield return null;
            AssertActions(rig);
            var ui=Object.FindObjectOfType<MainMenuUI>();
            var button=ui.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Button_MainMenu_OpenZeroing");
            yield return ClickWithController(button,camera);
            Assert.That(GameMain.Instance.Services.Router.Current,Is.EqualTo(ScreenId.ZeroingBriefing),"The real tracked trigger must route the main-menu button");
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
            Assert.That(runtime.PlayerCamera.GetComponent<AudioListener>()?.isActiveAndEnabled,Is.True,
                "The active mode-three XR camera must own the audio listener.");
            var shotClip = runtime.GetComponent<CombatSceneBindings>().RifleShotClip;
            Assert.That(shotClip, Is.Not.Null);
            Assert.That(shotClip.loadState, Is.Not.EqualTo(AudioDataLoadState.Unloaded),
                "Combat rifle audio must be loaded before the first shot.");
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
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var handRender=typeof(VRControllerHandVisual).GetMethod("UpdateBeforeRender",flags);
            // Coroutine resumes before LateUpdate: sample the baseline at the same
            // real render phase as the comparison, not a one-frame-old visual pose.
            foreach(var hand in hands)handRender.Invoke(hand,null);
            var gripOffsets=hands.Select(h=>h.GripAnchor.InverseTransformPoint(h.transform.position)).ToArray();
            // Simulate an origin change after Dynamic update and run Unity's real render callbacks.
            rig.transform.position+=rig.transform.forward*.03f;
            // Invoke the production XRI/hand callbacks without re-entering native input updates in the test runner.
            var managerRender=typeof(UnityEngine.XR.Interaction.Toolkit.XRInteractionManager).GetMethod("OnBeforeRender",flags);
            managerRender.Invoke(rifle.interactionManager,null);
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
            // Account for the rack-pickup rotation/attachment offset: aim the actual
            // rifle forward, rather than keeping its former sideways rack orientation.
            var controllerRotation=aimRotation*Quaternion.Inverse(heldRotation);
            var controllerPosition=rearTarget-controllerRotation*heldOffset;
            var frontTarget=rearTarget+aimRotation*gripDelta;
            QueueController(right,rightPose.transform.parent.InverseTransformPoint(controllerPosition),Quaternion.Inverse(rightPose.transform.parent.rotation)*controllerRotation);
            QueueController(left,leftPose.transform.parent.InverseTransformPoint(frontTarget),Quaternion.Inverse(leftPose.transform.parent.rotation)*aimRotation);
            yield return new WaitForSeconds(.15f);
            Assert.That(rifle.RearHandSelected&&rifle.FrontHandSelected,Is.True,"Raising to aim must keep both grips");
            foreach(var hand in hands)
            {
                var renderer=hand.ModelRoot.GetComponentsInChildren<Renderer>().First(r=>r.enabled&&!r.forceRenderingOff&&r.gameObject.activeInHierarchy);
                // Approved forearms extend below the frame. The palm must be visible,
                // and the real skinned mesh must intersect the HMD frustum.
                var point=heldCamera.WorldToViewportPoint(hand.transform.position);
                Assert.That(GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(heldCamera),renderer.bounds),Is.True);
                Assert.That(point.z,Is.GreaterThan(heldCamera.nearClipPlane),"Hand must be in front of the HMD near plane");
                Assert.That(point.x,Is.InRange(0f,1f),"Human hand must be in the HMD horizontal view");
                Assert.That(point.y,Is.InRange(0f,1f),"Human hand must be in the HMD vertical view");
            }
            PresentationEvidence.Capture(heldCamera,"VR-human-hands-"+rifle.gameObject.scene.name,"Logs/range-assist-evidence");
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
                // Do not implicitly convert an InputDevice[] null to a populated
                // nullable ReadOnlyArray: that creates an empty exclusive filter.
                if(entry.Value==null) entry.Key.devices=null;
                else entry.Key.devices=entry.Value;
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
