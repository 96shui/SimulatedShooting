using System.Collections;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VRShooting.Application.Combat;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.Input;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Tests.PlayMode
{
    // BDD31 / P3 task009, task010: production scene and rifle, replace only tracking/input.
    public sealed class UrbanPickupRegressionTests
    {
        ICombatSceneLease lease;
        CombatMission mission;
        [UnityTest] public IEnumerator Bdd31_DesktopRifleStaysVisibleAndReachableBeforePickup()=>Verify(false);
        [UnityTest] public IEnumerator Bdd31_VrRifleStaysVisibleAndReachableAfterHeadCalibration()=>Verify(true);
        IEnumerator Verify(bool vr)
        {
            yield return SceneManager.LoadSceneAsync("MainScene");yield return null;
            var load=new UnityCombatSceneLoader(()=>vr).LoadAsync(TrainingMode.Urban,CancellationToken.None);
            while(!load.IsCompleted)yield return null;
            Assert.That(load.Result.Success,Is.True,load.Result.Message);lease=load.Result.Data;
            var runtime=Object.FindObjectOfType<CombatSceneRuntime>();runtime.ManualStepping=true;
            mission=new CombatMission(lease);
            Assert.That(mission.Start(RandomSeed.Fixed(31)).Success,Is.True);
            Assert.That(lease.Activate(mission.Core,mission.World,mission.State,mission.Hud,mission.Squad,mission.Grenades,mission,mission.SessionId).Success,Is.True);
            runtime.InputOverride=new ManualXRTrainingInput();
            var eye=runtime.PlayerCamera.transform;
            var weapon=runtime.ReloadPresentation.GetComponent<WeaponPrefabBinding>();
            foreach(var yaw in new[]{0f,90f,180f})
            {
                eye.localRotation=Quaternion.Euler(0,yaw,0);
                eye.localPosition+=Vector3.up*.1f; // HMD height calibration after scene activation.
                runtime.FrameOverride=new CombatInputFrameDto{HeadTracked=true,RearHandTracked=true,FrontHandTracked=true,
                    IsRealVr=vr,PlayerPosition=runtime.PlayerRoot.position,PlayerForward=runtime.PlayerRoot.forward,
                    MuzzlePosition=eye.position,AimDirection=eye.forward};
                runtime.Step(.02f);
                var expected=eye.position+Vector3.ProjectOnPlane(eye.forward,Vector3.up).normalized*.45f-Vector3.up*.28f;
                Assert.That(Vector3.Distance(weapon.RearHandGrip.position,expected),Is.LessThan(.01f));
                Assert.That(Vector3.Distance(eye.position,weapon.RearHandGrip.position),Is.LessThan(.65f));
                Assert.That(weapon.GetComponentsInChildren<Renderer>().Any(r=>r.enabled&&r.gameObject.activeInHierarchy),Is.True);
                var planes=GeometryUtility.CalculateFrustumPlanes(runtime.PlayerCamera);
                Assert.That(weapon.GetComponentsInChildren<Renderer>().Any(r=>r.enabled&&GeometryUtility.TestPlanesAABB(planes,r.bounds)),Is.True,"Rifle must be visible inside the actual player camera frustum");
                Assert.That(weapon.GetComponent<TrainingRifleGrabInteractable>().HoldState,Is.EqualTo(WeaponHoldState.OnRack));
                if(yaw==0)Capture(runtime.PlayerCamera,vr?"urban-vr-pickup":"urban-desktop-pickup");
            }
            if(!vr)
            {
                var candidate=mission.Urban.GetEnemyAssignments(mission.SessionId).Data.First(a=>a.RoomId.Length>0);
                var actor=runtime.Actors[candidate.EntityId];
                var center=actor.HitCollider.bounds.center;
                var inward=Vector3.ProjectOnPlane(new Vector3(31,center.y,76)-center,Vector3.up).normalized;
                eye.SetPositionAndRotation(center+inward*2,Quaternion.LookRotation(-inward));
                Capture(runtime.PlayerCamera,"urban-building-corner");
            }
        }
        static void Capture(Camera camera,string name)
        {
            const string folder="docs/codex-reports/evidence/range-restore-urban-20261023";
            System.IO.Directory.CreateDirectory(folder);
            var target=RenderTexture.GetTemporary(1280,720,24);
            var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
            var previous=camera.targetTexture;var active=RenderTexture.active;
            try
            {
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
                System.IO.File.WriteAllBytes(folder+"/"+name+".png",texture.EncodeToPNG());
            }
            finally{camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);Object.Destroy(texture);}
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            lease?.Deactivate();mission?.Dispose();lease?.Dispose();lease=null;mission=null;
            yield return null;yield return null;
        }
    }
}
