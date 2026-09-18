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
using UnityEngine.XR.Interaction.Toolkit.Interactors;
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
            if(GameMain.Instance!=null)Object.Destroy(GameMain.Instance.gameObject);
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
                    yield return ClickWithController(ui.TrenchBriefingView.StartButton,runtime.PlayerCamera);
                    Assert.That(app.Snapshot.Screen,Is.EqualTo(ScreenId.TrenchHud),"Incoming rig must click the start button");
                }
                app.ReturnToMainMenu();
                while(SceneManager.GetSceneByName("CombatScene").isLoaded)yield return null;
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
            Assert.That(rays.Any(r=>r.TryGetUIModel(out var model)&&model.currentRaycast.gameObject!=null&&
                (model.currentRaycast.gameObject==button.gameObject||model.currentRaycast.gameObject.transform.IsChildOf(button.transform))),Is.True,
                "Production tracked ray must hit "+button.name+"; "+string.Join(";",rays.Select(r=>r.TryGetUIModel(out var m)?"hit="+m.currentRaycast.gameObject:"no model")));
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
            // Stop consumers before replacing device filters/removing devices held by XRI selections.
            foreach(var manager in Object.FindObjectsOfType<InputActionManager>(true))manager.gameObject.SetActive(false);
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
            if(GameMain.Instance!=null)Object.Destroy(GameMain.Instance.gameObject);
            yield return null;
            yield return SceneManager.LoadSceneAsync("MainScene");
            GameMain.Instance?.GetComponent<MainMenuXRModeController>()?.ClearForcedModeForTests();
            yield return null;
        }
    }
}
