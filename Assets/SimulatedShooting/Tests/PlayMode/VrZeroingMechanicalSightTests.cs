using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using VRShooting.Application;
using VRShooting.Application.Events;
using VRShooting.Common;
using VRShooting.Input;
using VRShooting.Unity.Bootstrap;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Tests.PlayMode
{
    public sealed class VrZeroingMechanicalSightTests
    {
        // Independent landmarks from visible mesh. No AimLinePoint is used to aim the hands.
        static readonly Vector3 Rear=new Vector3(-.0057315f,.0573025f,-.118108f);
        static readonly Vector3 Front=new Vector3(-.005689f,.050240f,.228503f);
        readonly List<ZeroingShotDto> shots=new List<ZeroingShotDto>();
        GameObject tracking;
        FirstPersonTrainingWeaponController controller;
        WeaponPrefabBinding weapon;
        TargetImpactSurface target;
        TrainingRifleGrabInteractable grab;
        XRInteractionManager manager;
        XRDirectInteractor right,left;
        Transform head;
        ApplicationServices services;
        ManualXRTrainingInput input;
        System.IDisposable subscription;

        [UnitySetUp] public IEnumerator Setup()
        {
            if(GameMain.Instance!=null)Object.Destroy(GameMain.Instance.gameObject);
            yield return null;yield return SceneManager.LoadSceneAsync("ZeroingRangeScene");
            var mode=Object.FindObjectOfType<ZeroingRangeXRModeController>();mode.SetVrModeForTests(true);
            controller=Object.FindObjectOfType<FirstPersonTrainingWeaponController>();controller.enabled=false;
            var bootstrap=Object.FindObjectOfType<ZeroingRangeSessionBootstrap>();
            if(bootstrap==null)bootstrap=GameObject.Find("ZeroingRange").AddComponent<ZeroingRangeSessionBootstrap>();
            services=bootstrap.EnsureServices();
            Assert.That(services.Presentation.Enter(TrainingMode.Zeroing100m).Success,Is.True);
            Assert.That(services.Presentation.ConfirmStart("").Success,Is.True);
            weapon=Object.FindObjectOfType<WeaponPrefabBinding>();grab=weapon.GetComponent<TrainingRifleGrabInteractable>();
            target=Object.FindObjectsOfType<SceneTestId>().Single(t=>t.Id=="ZeroingRange.Target.Face").GetComponent<TargetImpactSurface>();
            manager=mode.XrOrigin.GetComponentInChildren<XRInteractionManager>(true);
            tracking=new GameObject("BDD33_VR_TrackingSubstitute");
            head=new GameObject("TrackedHead").transform;head.SetParent(tracking.transform);head.position=new Vector3(0,1.5f,0);
            var camera=head.gameObject.AddComponent<Camera>();camera.enabled=false;camera.nearClipPlane=.005f;
            XRDirectInteractor Hand(string name,InteractorHandedness side)
            {
                var node=new GameObject(name);node.transform.SetParent(tracking.transform);
                var collider=node.AddComponent<SphereCollider>();collider.radius=.02f;collider.isTrigger=true;
                var body=node.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
                var hand=node.AddComponent<XRDirectInteractor>();hand.interactionManager=manager;hand.handedness=side;
                hand.selectInput.inputSourceMode=XRInputButtonReader.InputSourceMode.ManualValue;
                return hand;
            }
            right=Hand("TrackedRight",InteractorHandedness.Right);left=Hand("TrackedLeft",InteractorHandedness.Left);
            input=new ManualXRTrainingInput();controller.ConfigureVrPoseSources(head,right.transform,left.transform);
            controller.ConfigureServices(services,input);Assert.That(controller.InitializeForTests(),Is.True);
            right.transform.SetPositionAndRotation(grab.RearAttach.position,grab.RearAttach.rotation);
            yield return null;manager.SelectEnter((IXRSelectInteractor)right,(IXRSelectInteractable)grab);
            yield return null;left.transform.SetPositionAndRotation(grab.FrontAttach.position,grab.FrontAttach.rotation);
            manager.SelectEnter((IXRSelectInteractor)left,(IXRSelectInteractable)grab);yield return null;
            Assert.That(grab.HoldState,Is.EqualTo(WeaponHoldState.TwoHandHeld));
            Assert.That(controller.UsingVrPoseSources,Is.True);
            subscription=services.EventBus.Subscribe<ZeroingShotRecordedEvent>(e=>shots.Add(e.Shot));shots.Clear();
        }

        IEnumerator AimVisibleSightsWithTrackedHands()
        {
            for(var step=0;step<8;step++)
            {
                var rear=weapon.transform.TransformPoint(Rear);
                var visible=weapon.transform.TransformDirection((Front-Rear).normalized);
                var desired=(target.TargetCenter.position-rear).normalized;
                var delta=Quaternion.FromToRotation(visible,desired);
                foreach(var hand in new[]{right,left})
                    hand.transform.SetPositionAndRotation(rear+delta*(hand.transform.position-rear),delta*hand.transform.rotation);
                yield return null;
            }
            var ray=new Ray(weapon.transform.TransformPoint(Rear),weapon.transform.TransformDirection((Front-Rear).normalized));
            Assert.That(target.TryProjectRay(ray,out var point,out _),Is.True);
            Assert.That(point.magnitude,Is.LessThan(.2f),"The tracked hands must align the visible sights with the centre");
        }

        WeaponFireInputDto Snapshot()=>(WeaponFireInputDto)typeof(FirstPersonTrainingWeaponController)
            .GetMethod("BuildFireSnapshot",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,null);

        [UnityTest] public IEnumerator Bdd33_TrackedTwoHandVisibleSightAndTriggerCanZeroNextRound()
        {
            var id=controller.SessionId;
            for(var round=1;round<=2;round++)
            {
                for(var index=0;index<3;index++)
                {
                    yield return AimVisibleSightsWithTrackedHands();
                    if(round==1&&index==0)
                    {
                        var rear=weapon.transform.TransformPoint(Rear);
                        var optical=weapon.transform.TransformDirection((Front-Rear).normalized);
                        // Substitute the player's eye behind the real aperture, never move the gun.
                        head.SetPositionAndRotation(rear-optical*.10f,Quaternion.LookRotation(optical,weapon.transform.up));
                        Capture(head.GetComponent<Camera>(),"vr-substitute-mechanical-sight");
                    }
                    var snapshot=Snapshot();
                    Assert.That(snapshot.TargetPlaneRayValid,Is.True);
                    Assert.That(snapshot.TargetPlanePointCm.magnitude,Is.LessThan(1f),"Real VR sights and sampled gun line must meet within one centimetre");
                    TestContext.WriteLine("Visible-sight aim / target-plane cm="+snapshot.TargetPlanePointCm);
                    var eyePosition=head.position;var eyeRotation=head.rotation;var eyeFov=head.GetComponent<Camera>().fieldOfView;
                    var count=shots.Count;input.Press(XRTrainingInputButton.Trigger);controller.SendMessage("Update");input.AdvanceFrame();
                    Assert.That(shots.Count,Is.EqualTo(count+1));
                    Assert.That(head.position,Is.EqualTo(eyePosition));Assert.That(head.rotation,Is.EqualTo(eyeRotation));
                    Assert.That(head.GetComponent<Camera>().fieldOfView,Is.EqualTo(eyeFov));
                    input.Release(XRTrainingInputButton.Trigger);controller.SendMessage("Update");input.AdvanceFrame();
                    controller.SendMessage("UpdateRecoil",.1f);yield return null;
                }
                var analysis=services.Zeroing.CompleteRound(id).Data;
                if(round==1)
                {
                    Assert.That(analysis.PassedTenRing,Is.False);
                    Assert.That(services.Zeroing.ApplyAdjustment(id,1).Success,Is.True);
                    Assert.That(services.Presentation.ContinueNextRound(id).Success,Is.True);
                }
                else
                {
                    Assert.That(analysis.PassedTenRing,Is.True);
                    Assert.That(analysis.AverageOffsetCm.magnitude,Is.LessThan(1f));
                    TestContext.WriteLine("VR tracked-hands round 2 mean cm="+analysis.AverageOffsetCm);
                }
            }
            Assert.That(shots.Count,Is.EqualTo(6));
            Assert.That(target.Impacts.Count,Is.EqualTo(6));
            Assert.That(target.Impacts.Skip(3).All(p=>p.InsideTenRing),Is.True);
            head.SetPositionAndRotation(target.TargetCenter.position-target.TargetCenter.forward*1.1f,
                Quaternion.LookRotation(target.TargetCenter.forward,target.TargetCenter.up));
            Capture(head.GetComponent<Camera>(),"vr-round2-target-inspection");
        }

        static void Capture(Camera camera,string name)
        {
#if UNITY_EDITOR
            const string folder="docs/codex-reports/evidence/vr-mechanical-sight-20261023";
            System.IO.Directory.CreateDirectory(folder);
            var texture=new Texture2D(1600,1000,TextureFormat.RGB24,false);
            var render=RenderTexture.GetTemporary(1600,1000,24);
            var previous=camera.targetTexture;var active=RenderTexture.active;var aspect=camera.aspect;
            try
            {
                camera.targetTexture=render;camera.aspect=1.6f;camera.Render();RenderTexture.active=render;
                texture.ReadPixels(new Rect(0,0,1600,1000),0,0);texture.Apply();
                System.IO.File.WriteAllBytes(folder+"/"+name+".png",texture.EncodeToPNG());
            }
            finally{camera.targetTexture=previous;camera.aspect=aspect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(render);Object.Destroy(texture);}
#endif
        }

        [UnityTest] public IEnumerator Bdd33_HeadMotionDoesNotChangeGunLineAndSupportReleaseBlocksFire()
        {
            yield return AimVisibleSightsWithTrackedHands();
            var before=Snapshot();head.SetPositionAndRotation(head.position+new Vector3(.03f,.02f,0),Quaternion.Euler(4,12,0));
            controller.SendMessage("Update");
            var after=Snapshot();
            Assert.That(Vector3.Distance(before.AimDirection,after.AimDirection),Is.LessThan(.00001f));
            Assert.That(head.rotation.eulerAngles.y,Is.EqualTo(12).Within(.001f));
            manager.SelectExit((IXRSelectInteractor)left,(IXRSelectInteractable)grab);yield return null;
            var count=shots.Count;input.Press(XRTrainingInputButton.Trigger);controller.SendMessage("Update");
            Assert.That(shots.Count,Is.EqualTo(count));
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            subscription?.Dispose();subscription=null;
            if(manager!=null&&grab!=null&&right!=null&&grab.RearHandSelected)
                manager.SelectExit((IXRSelectInteractor)right,(IXRSelectInteractable)grab);
            if(tracking!=null)Object.Destroy(tracking);
            yield return null;
        }
    }
}
