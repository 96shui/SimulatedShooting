using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimulatedShooting.Scene;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using VRShooting.Common;
using VRShooting.Application.Combat;
using VRShooting.Contracts;
using VRShooting.Unity.UI;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Tests.PlayMode
{
    // BDD35: production UI, shared rifle, actual XRI release and physics, no headset required.
    public sealed class Round6PresentationTests
    {
        const string Evidence="docs/codex-reports/evidence/round6-2026-11-14";
        GameObject root;
        [UnityTearDown] public IEnumerator Cleanup(){if(root!=null)Object.Destroy(root);yield return null;}

        [Test] public void Bdd35_AllThreeHudLabelsAreAvailableWithRealFalseCapabilityAndFourSpares()
        {
            root=new GameObject("Round6UI",typeof(RectTransform));root.SetActive(false);
            var ui=root.AddComponent<P3CombatUIRoot>();ui.BuildIfNeeded();
            Assert.That(root.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.name.EndsWith("_Corner")).All(t=>t.text=="拐角射击：可用"),Is.True);
            var hud=new HudDto{Player=new PlayerStatusDto{Health=10,MaxHealth=10,IsAlive=true,CornerShootingAvailable=false},Ammo=CombatConfigDto.Default.InitialAmmo};
            ui.TrenchHudView.Apply(new TrenchSessionDto(),hud);
            ui.UrbanStreetView.Apply(new UrbanSessionDto(),hud);ui.UrbanBuildingView.Apply(new UrbanSessionDto(),hud);
            foreach(var prefix in new[]{"Trench","UrbanStreet","UrbanBuilding"})
            {
                Assert.That(root.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Hud_"+prefix+"_Corner").text,Is.EqualTo("拐角射击：可用"));
                var ammo=root.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Hud_"+prefix+"_Ammo").text;
                Assert.That(ammo,Does.Contain("30 / 120"));Assert.That(ammo,Does.Contain("备用弹匣 4"));
            }
            Assert.That(ui.TrenchHudView.GetComponent<P3CombatHudView>().LastHud.Player.CornerShootingAvailable,Is.False);
        }

        [UnityTest] public IEnumerator Bdd35_MapPagesReopenAtCurrentHorizontalEyeLevel()
        {
            root=new GameObject("Round6UI",typeof(RectTransform));
            var ui=root.AddComponent<P3CombatUIRoot>();ui.BuildIfNeeded();
            var camera=new GameObject("Round6Eye").AddComponent<Camera>();camera.transform.SetParent(root.transform.parent);
            ui.TrenchMapView.Apply(new[]{CombatMapCatalog.Trench},P3ContractIds.TrenchMap);
            ui.UrbanMapView.Apply(new[]{CombatMapCatalog.Urban},P3ContractIds.UrbanMap);
            try
            {
                var adapter=root.GetComponent<TrainingUICanvasAdapter>();adapter.SetVrModeForTests(true,camera);
                foreach(var screen in new[]{ScreenId.TrenchMapSelection,ScreenId.UrbanMapSelection,ScreenId.TrenchMapSelection})
                {
                    camera.transform.SetPositionAndRotation(new Vector3(3,.65f,-2),Quaternion.Euler(-55,(int)screen*17%360,0));
                    ui.Show(screen);yield return null;
                    AssertEyePlacement();
                    camera.transform.SetPositionAndRotation(new Vector3(-2,1.8f,4),Quaternion.Euler(60,125,0));
                    ui.Show(screen);yield return null;AssertEyePlacement();
                    // Keep arbitrary-height/pitch assertions above, then use an unobstructed
                    // street view to show the complete actual world-space map page.
                    camera.transform.SetPositionAndRotation(new Vector3(24,1.65f,52),Quaternion.identity);
                    ui.Show(screen);yield return null;AssertEyePlacement();
                    PresentationEvidence.Capture(camera,screen==ScreenId.TrenchMapSelection?"trench-map-front":"urban-map-front",Evidence,false,1920,1080);
                }
                void AssertEyePlacement()
                {
                    var forward=Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized;
                    Assert.That(Vector3.Distance(root.transform.position,camera.transform.position+forward*1.3f),Is.LessThan(.001f));
                    Assert.That(Vector3.Angle(root.transform.forward,forward),Is.LessThan(.01f));
                }
            }
            finally{Object.Destroy(camera.gameObject);}
        }

        [UnityTest] public IEnumerator Bdd35_SharedRifleDropsWithoutInheritedVelocityAndCanReturnToRack()
        {
            yield return SceneManager.LoadSceneAsync("CombatScene");
            root=new GameObject("Round6DropRig");
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(root.transform);floor.transform.position=new Vector3(0,-.1f,0);floor.transform.localScale=new Vector3(10,.2f,10);
            var rifle=Object.Instantiate(Object.FindObjectOfType<CombatSceneBindings>().TrainingRiflePrefab,root.transform);
            rifle.transform.SetPositionAndRotation(new Vector3(0,1.3f,0),Quaternion.identity);
            var grab=rifle.GetComponent<TrainingRifleGrabInteractable>();grab.CaptureRackPose();grab.ReturnDroppedWeaponToRack=false;
            var body=rifle.GetComponent<Rigidbody>();
            var manager=root.AddComponent<XRInteractionManager>();grab.interactionManager=manager;
            var hand=new GameObject("Round6Rear");hand.transform.SetParent(root.transform);hand.AddComponent<SphereCollider>().isTrigger=true;
            var rear=hand.AddComponent<XRDirectInteractor>();rear.interactionManager=manager;rear.handedness=InteractorHandedness.Right;
            rear.selectInput.inputSourceMode=XRInputButtonReader.InputSourceMode.ManualValue;rear.selectInput.manualValue=1f;rear.selectInput.manualPerformed=true;
            hand.transform.SetPositionAndRotation(grab.RearAttach.position,grab.RearAttach.rotation);
            manager.SelectEnter((IXRSelectInteractor)rear,(IXRSelectInteractable)grab);yield return null;
            for(var i=0;i<5;i++){hand.transform.position+=Vector3.right*.1f;yield return null;}
            var released=rifle.transform.position;
            Assert.That(grab.RearHandSelected,Is.True);
            rear.selectInput.manualPerformed=false;rear.selectInput.manualValue=0;
            manager.SelectExit((IXRSelectInteractor)rear,(IXRSelectInteractable)grab);
            yield return new WaitForFixedUpdate();
            Assert.That(body.isKinematic,Is.False);Assert.That(body.useGravity,Is.True);
            Assert.That(new Vector2(body.velocity.x,body.velocity.z).magnitude,Is.LessThan(.03f));
            Assert.That(body.angularVelocity.magnitude,Is.LessThan(.03f));
            yield return new WaitForSeconds(2);
            Assert.That(rifle.transform.position.y,Is.GreaterThan(-.05f));
            Assert.That(Vector3.ProjectOnPlane(rifle.transform.position-released,Vector3.up).magnitude,Is.LessThan(.35f));
            Assert.That(body.velocity.magnitude,Is.LessThan(.05f));
            grab.ReturnToRack();Assert.That(body.isKinematic,Is.True);
            Assert.That(Vector3.Distance(rifle.transform.position,new Vector3(0,1.3f,0)),Is.LessThan(.001f));
        }

        [UnityTest] public IEnumerator Bdd35_LeftShoulderShiftsWholeRifleLeftAndRestoresRightTracking()
        {
            yield return SceneManager.LoadSceneAsync("CombatScene");
            root=new GameObject("Round6ShoulderRig");
            var rifle=Object.Instantiate(Object.FindObjectOfType<CombatSceneBindings>().TrainingRiflePrefab,root.transform);
            rifle.transform.SetPositionAndRotation(new Vector3(0,1.4f,.4f),Quaternion.identity);
            var grab=rifle.GetComponent<TrainingRifleGrabInteractable>();grab.CaptureRackPose();
            var binding=rifle.GetComponent<WeaponPrefabBinding>();
            var before=rifle.transform.position;
            var aim=binding.AimLinePoint.forward;var muzzle=binding.MuzzlePoint.forward;
            var manager=root.AddComponent<XRInteractionManager>();grab.interactionManager=manager;
            var controller=new GameObject("Left Controller");controller.transform.SetParent(root.transform);
            controller.AddComponent<SphereCollider>().isTrigger=true;
            var left=controller.AddComponent<XRDirectInteractor>();left.interactionManager=manager;left.handedness=InteractorHandedness.Left;
            left.selectInput.inputSourceMode=XRInputButtonReader.InputSourceMode.ManualValue;left.selectInput.manualValue=1f;left.selectInput.manualPerformed=true;
            controller.transform.SetPositionAndRotation(grab.RearAttach.position,grab.RearAttach.rotation);
            grab.SetShoulder(ShoulderSide.Left);
            manager.SelectEnter((IXRSelectInteractor)left,(IXRSelectInteractable)grab);
            for(var i=0;i<4;i++)yield return null;
            Assert.That(Vector3.Distance(rifle.transform.position,before-Vector3.right*.16f),Is.LessThan(.003f));
            Assert.That(Vector3.Angle(binding.AimLinePoint.forward,aim),Is.LessThan(.01f));
            Assert.That(Vector3.Angle(binding.MuzzlePoint.forward,muzzle),Is.LessThan(.01f));
            Assert.That(rifle.transform.localScale,Is.EqualTo(Vector3.one));
            foreach(var side in new[]{"Left","Right"})
            {
                var hand=Object.Instantiate(Resources.Load<GameObject>("Combat/Hands/VirtualHand_"+side),root.transform).GetComponent<VRControllerHandVisual>();
                hand.BindWeapon(grab);
            }
            VRHandPresentation.ApplyShoulder(grab,ShoulderSide.Left);
            foreach(var hand in root.GetComponentsInChildren<VRControllerHandVisual>())hand.SetGripForTests(true);
            var camera=new GameObject("Round6ShoulderEye").AddComponent<Camera>();camera.transform.SetParent(root.transform);
            camera.transform.SetPositionAndRotation(new Vector3(0,1.65f,-.45f),Quaternion.Euler(12,0,0));camera.nearClipPlane=.02f;
            yield return null;
            PresentationEvidence.Capture(camera,"left-shoulder-forward",Evidence,false,1920,1080);
            Assert.That(grab.RearHandSelected,Is.True);
            left.selectInput.manualPerformed=false;left.selectInput.manualValue=0;
            manager.SelectExit((IXRSelectInteractor)left,(IXRSelectInteractable)grab);
            grab.SetShoulder(ShoulderSide.Right);
            var rightNode=new GameObject("Right");rightNode.transform.SetParent(root.transform);
            var right=rightNode.AddComponent<XRDirectInteractor>();right.handedness=InteractorHandedness.Right;
            Assert.That(grab.GetAttachTransform((IXRInteractor)right),Is.SameAs(binding.RearHandGrip));
            grab.SetShoulder(ShoulderSide.Left);grab.SetShoulder(ShoulderSide.Right);
            Assert.That(grab.GetAttachTransform((IXRInteractor)right),Is.SameAs(binding.RearHandGrip));
        }
    }
}
