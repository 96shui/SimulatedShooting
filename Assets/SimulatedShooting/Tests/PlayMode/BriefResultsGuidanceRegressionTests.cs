using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimulatedShooting.Scene;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Contracts;
using VRShooting.Input;
using VRShooting.Unity;
using VRShooting.Unity.Bootstrap;
using VRShooting.Unity.UI;

namespace SimulatedShooting.Tests.PlayMode
{
    // BDD04/05/07/11/29, P1 task012/017, P2 task005, P3 task004/019.
    public sealed class BriefResultsGuidanceRegressionTests
    {
        const string Evidence = "docs/codex-reports/evidence/mode1-brief-results-labels-2026-11-12";

        [UnitySetUp] public IEnumerator Setup()
        {
            if (GameMain.Instance != null) Object.Destroy(GameMain.Instance.gameObject);
            yield return null;
            yield return SceneManager.LoadSceneAsync("MainScene");
            yield return null;
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (GameMain.Instance != null) Object.Destroy(GameMain.Instance.gameObject);
            yield return null;
        }

        static GameObject Ui(string id) => Object.FindObjectsOfType<UITestId>(true).First(t => t.Id == id).gameObject;
        static void Click(string id) => Ui(id).GetComponent<Button>().onClick.Invoke();

        [UnityTest] public IEnumerator Bdd04_BriefingUsesThePhysicalTargetTexture()
        {
            Click("Button_MainMenu_OpenZeroing");
            yield return null;
            var plot = Ui("Screen_ZeroingBriefing").GetComponentsInChildren<TacticalTargetPlot>(true).Single();
            Assert.That(Ui("Text_ZeroingBriefing_TargetSize").GetComponent<TMP_Text>().text, Is.EqualTo("50cm x 50cm"));
            var texture = plot.mainTexture;
            Assert.That(texture, Is.SameAs(Resources.Load<Texture2D>("UI/Tactical/ChestTargetReference")));
            yield return SceneManager.LoadSceneAsync("ZeroingRangeScene");
            yield return null;
            var faces = Object.FindObjectsOfType<Renderer>(true).Where(r => r.name == "ChestTarget_ReferenceFace").ToArray();
            Assert.That(faces, Is.Not.Empty);
            foreach (var face in faces) Assert.That(face.sharedMaterial.mainTexture, Is.SameAs(texture));
        }

        IEnumerator LoadZeroing()
        {
            yield return SceneManager.LoadSceneAsync("ZeroingRangeScene");
            Object.FindObjectOfType<ZeroingRangeXRModeController>().SetVrModeForTests(false);
            var bootstrap=Object.FindObjectOfType<ZeroingRangeSessionBootstrap>();
            if(bootstrap==null) bootstrap=GameObject.Find("ZeroingRange").AddComponent<ZeroingRangeSessionBootstrap>();
            var services=bootstrap.EnsureServices();
            Assert.That(services.Presentation.Enter(TrainingMode.Zeroing100m).Success, Is.True);
            Assert.That(services.Presentation.ConfirmStart("").Success, Is.True);
            yield return null;
            yield return null;
            var controller = Object.FindObjectOfType<FirstPersonTrainingWeaponController>();
            controller.ConfigureServices(GameMain.Instance.Services, new ManualXRTrainingInput());
            Assert.That(controller.InitializeForTests(), Is.True);
        }

        static TargetImpactSurface[] LeaveResiduals()
        {
            var surfaces = GameObject.Find("ZeroingRange").GetComponentsInChildren<TargetImpactSurface>(true);
            Assert.That(surfaces.Length, Is.GreaterThan(1), "Exercise the entire real target row, not only the bound scoring face.");
            foreach (var surface in surfaces)
                Assert.That(surface.TryRecordWorldPoint(surface.TargetCenter.position, out _), Is.True);
            var child = new GameObject("InactiveResidualTarget");
            child.transform.SetParent(surfaces.Last().transform, false);
            var markers = new GameObject("ImpactMarkers").transform;
            markers.SetParent(child.transform, false);
            var inactive = child.AddComponent<TargetImpactSurface>();
            inactive.Configure(null, surfaces.Last().TargetCenter, markers, null);
            inactive.TryRecordWorldPoint(inactive.TargetCenter.position, out _);
            child.SetActive(false);
            return surfaces.Concat(new[] { inactive }).ToArray();
        }

        static void AssertCleared(TargetImpactSurface[] surfaces)
        {
            foreach (var surface in surfaces)
            {
                Assert.That(surface.Impacts.Count, Is.Zero, surface.name);
                Assert.That(surface.ImpactMarkerRoot.childCount, Is.Zero, surface.name + " actual marker objects");
            }
        }

        [UnityTest] public IEnumerator Bdd07_ActualRetryButtonClearsEntireTargetRowIncludingInactiveFaces()
        {
            yield return LoadZeroing();
            var services = GameMain.Instance.Services;
            var id = services.TrainingSessions.Current.SessionId;
            services.WeaponControl.SetGripState(new WeaponGripStateInputDto { SessionId=id,
                HoldState=WeaponHoldState.TwoHandHeld, RearHandTracked=true, FrontHandTracked=true, Stability01=.95f });
            var bias = services.Zeroing.GetSession(id).Data.FixedImpactOffsetCm;
            for (var i=0;i<4;i++) Assert.That(services.Zeroing.RecordShot(id,new ShotInputDto {
                AimDirection=new Vector3(-bias.x,-bias.y,100), WeaponStability=.95f, FireTime=i }).Success, Is.True);
            yield return null;
            Click("Button_ZeroingImpactAnalysis_NextRound");
            yield return null;
            Assert.That(services.Router.Current, Is.EqualTo(ScreenId.ZeroingFinalRating));
            var surfaces = LeaveResiduals();
            Click("Button_ZeroingFinalRating_Retry");
            Assert.That(services.TrainingSessions.HasActiveSession, Is.False);
            Assert.That(surfaces.SelectMany(s=>s.ImpactMarkerRoot.GetComponentsInChildren<Renderer>())
                .Any(r=>r.enabled && r.gameObject.activeInHierarchy), Is.False, "No old red marker may be visible in the retry frame.");
            yield return null;
            yield return null;
            AssertCleared(surfaces);
        }

        [UnityTest] public IEnumerator Bdd05_ModeSelectionReentryClearsAllFacesAndPreservesNewSessionHits()
        {
            yield return LoadZeroing();
            var services = GameMain.Instance.Services;
            var surfaces = LeaveResiduals();
            services.Router.Open(ScreenId.ZeroingImpactAnalysis, new NavigationArgs { Mode=TrainingMode.Zeroing100m, SessionId=services.TrainingSessions.Current.SessionId });
            Click("Button_ZeroingImpactAnalysis_BackToMainMenu");
            yield return null;
            // The retained-scene path also exercises inactive targets before any scene unloading can hide the bug.
            if (surfaces.All(s=>s!=null)) AssertCleared(surfaces);
            Click("Button_MainMenu_OpenZeroing");
            yield return null;
            Click("Button_ZeroingBriefing_Start");
            yield return null;
            yield return null;
            var current = GameObject.Find("ZeroingRange").GetComponentsInChildren<TargetImpactSurface>(true);
            AssertCleared(current);
            current[0].TryRecordWorldPoint(current[0].TargetCenter.position,out _);
            yield return null; yield return null;
            Assert.That(current[0].ImpactMarkerRoot.childCount, Is.EqualTo(1), "Normal same-session hits must survive updates.");
        }

        [UnityTest] public IEnumerator Bdd11_ResultsAppearAtCurrentEyeHeightAndForwardAfterTurning()
        {
            yield return SceneManager.LoadSceneAsync("MovingTargetRangeScene");
            var mode = Object.FindObjectOfType<ZeroingRangeXRModeController>();
            mode.SetVrModeForTests(false);
            yield return null; yield return null;
            var controller = Object.FindObjectOfType<FirstPersonTrainingWeaponController>();
            controller.enabled = false;
            var services=GameMain.Instance.Services;
            var session=services.MovingTarget.StartSession(new MovingTargetSettingsDto { SpeedMetersPerSecond=4 },RandomSeed.Fixed(1112));
            var id=session.Data.SessionId;
            services.Presentation.ConfirmStart(id);
            services.WeaponControl.SetGripState(new WeaponGripStateInputDto { SessionId=id,
                HoldState=WeaponHoldState.TwoHandHeld, RearHandTracked=true, FrontHandTracked=true, Stability01=.95f });
            var camera=mode.NoVrCamera;
            camera.transform.position+=new Vector3(.23f,.17f,-.11f);
            camera.transform.rotation=Quaternion.Euler(8,73,0);
            services.MovingTargetProgress.Tick(id,100);
            yield return null;
            var ui=Object.FindObjectOfType<MovingTargetRangeUI>();
            Assert.That(Ui("Screen_MovingTargetResults").activeInHierarchy, Is.True);
            var panel=(RectTransform)ui.LargePanelRoot.transform;
            var delta=panel.TransformPoint(panel.rect.center)-camera.transform.position;
            var forward=Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized;
            Assert.That(Vector3.Dot(delta,forward), Is.InRange(1f,1.2f));
            Assert.That(Mathf.Abs(Vector3.Dot(delta,Vector3.Cross(Vector3.up,forward))), Is.LessThan(.02f));
            Assert.That(Mathf.Abs(delta.y), Is.LessThan(.02f));
            Assert.That(Vector3.Dot(panel.forward,forward), Is.GreaterThan(.99f));
            PresentationEvidence.Capture(camera,"02-mode2-results-forward",Evidence,true);
            var originalFov=camera.fieldOfView;
            var originalRotation=camera.transform.rotation;
            try
            {
                camera.fieldOfView=85f;
                camera.transform.rotation=Quaternion.LookRotation(forward,Vector3.up);
                PresentationEvidence.Capture(camera,"02-mode2-results-forward-context",Evidence,true);
            }
            finally { camera.fieldOfView=originalFov; camera.transform.rotation=originalRotation; }
            TestContext.WriteLine("Eye="+camera.transform.position+"; panel="+panel.position+"; delta="+delta+"; forward distance="+Vector3.Dot(delta,forward));
        }

        [UnityTest] public IEnumerator Bdd29_Mode1HeldRifleHasNoGuidanceAndKeepsHud() => RangeGuidance("ZeroingRangeScene");
        [UnityTest] public IEnumerator Bdd29_Mode2HeldRifleHasNoGuidanceAndKeepsHud() => RangeGuidance("MovingTargetRangeScene");
        [UnityTest] public IEnumerator Bdd29_Mode3HeldRifleHasNoGuidanceAndKeepsHud() => CombatGuidance(TrainingMode.Trench);
        [UnityTest] public IEnumerator Bdd29_Mode4HeldRifleHasNoGuidanceAndKeepsHud() => CombatGuidance(TrainingMode.Urban);

#if UNITY_EDITOR
        [UnityTest] public IEnumerator Bdd29_StandaloneCalloutCannotShowLinesLabelsOrEndpointDecorations()
        {
            var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRTemplateAssets/Prefabs/Affordance/Affordance Callout.prefab");
            var root=Object.Instantiate(prefab);
            try
            {
                var callout=root.GetComponents<MonoBehaviour>().Single(c=>c.GetType().FullName=="Unity.VRTemplate.Callout");
                var fields=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
                Assert.That(callout.GetType().GetField("m_LazyTooltip",fields).GetValue(callout), Is.Not.Null);
                Assert.That(callout.GetType().GetField("m_Curve",fields).GetValue(callout), Is.Not.Null);
                Assert.That(root.transform.Find("Link Start"), Is.Not.Null,"Keep endpoint nodes and references.");
                root.SetActive(true); // Even an external activation must not restore the guidance.
                callout.GetType().GetMethod("GazeHoverStart").Invoke(callout,null);
                yield return new WaitForSeconds(1.1f);
                AssertCalloutHidden(callout);
                Assert.That(root.activeSelf, Is.False);
            }
            finally { Object.Destroy(root); }
        }
#endif

        IEnumerator RangeGuidance(string scene)
        {
            yield return SceneManager.LoadSceneAsync(scene);
            var mode=Object.FindObjectOfType<ZeroingRangeXRModeController>(); mode.SetVrModeForTests(false);
            yield return null; yield return null;
            var services=GameMain.Instance.Services;
            if (scene=="MovingTargetRangeScene") services.MovingTarget.StartSession(new MovingTargetSettingsDto { SpeedMetersPerSecond=4 },RandomSeed.Fixed(112));
            else
            {
                var bootstrap=Object.FindObjectOfType<ZeroingRangeSessionBootstrap>();
                if(bootstrap==null) bootstrap=GameObject.Find("ZeroingRange").AddComponent<ZeroingRangeSessionBootstrap>();
                Assert.That(services.Presentation.Enter(TrainingMode.Zeroing100m).Success, Is.True);
                Assert.That(services.Presentation.ConfirmStart("").Success, Is.True);
            }
            if(scene=="MovingTargetRangeScene") services.Presentation.ConfirmStart(services.TrainingSessions.Current.SessionId);
            var controller=Object.FindObjectOfType<FirstPersonTrainingWeaponController>();
            controller.ConfigureServices(services,new ManualXRTrainingInput()); controller.InitializeForTests();
            typeof(FirstPersonTrainingWeaponController).GetMethod("ForceTwoHandGripForTests",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)
                .Invoke(controller,null);
            Assert.That(controller.CurrentHoldState, Is.EqualTo(WeaponHoldState.TwoHandHeld));
            yield return Guidance(controller.GrabInteractable);
            var id=scene=="ZeroingRangeScene"?"Text_ZeroingHud_Prompt":"Hud_MovingTarget_NoFirePrompt";
            var prompt=Ui(id).GetComponent<TMP_Text>();
            Assert.That(prompt.gameObject.activeInHierarchy, Is.True);
            Assert.That(prompt.text, Is.Not.Empty);
            if(scene=="ZeroingRangeScene") PresentationEvidence.Capture(mode.NoVrCamera,"03-mode1-held-rifle-no-labels",Evidence,true);
        }

        IEnumerator CombatGuidance(TrainingMode mode)
        {
            var app=GameMain.Instance.Services.Combat;
            app.OpenMode(mode);
            var load=app.SelectMapAsync(mode==TrainingMode.Trench?"trench-a":"urban-a",RandomSeed.Fixed(112));
            while(!load.IsCompleted) yield return null;
            Assert.That(load.Result.Success, Is.True,load.Result.Message);
            var runtime=Object.FindObjectOfType<CombatSceneRuntime>(); runtime.ManualStepping=true;
            var input=new ManualXRTrainingInput(); runtime.InputOverride=input;
            runtime.FrameOverride=new CombatInputFrameDto { HeadTracked=true,RearHandTracked=true,FrontHandTracked=true,
                RearGripInRange=true,FrontGripInRange=true,PlayerPosition=runtime.PlayerRoot.position,
                PlayerForward=runtime.PlayerRoot.forward,MuzzlePosition=runtime.PlayerCamera.transform.position,AimDirection=Vector3.up };
            if(mode==TrainingMode.Trench)
            {
                Assert.That(app.Start().Success, Is.True);
                for(var i=0;i<1200&&!app.Mission.HasCombatStarted;i++) { runtime.Step(.1f); if(i%20==0) yield return null; }
                Assert.That(app.Mission.HasCombatStarted, Is.True);
            }
            runtime.Step(.02f);
            input.Press(XRTrainingInputButton.RightGrip); input.Press(XRTrainingInputButton.LeftGrip);
            runtime.Step(.02f);
            Assert.That(app.Mission.Core.GetSnapshot(app.Mission.SessionId).Data.Weapon.TwoHandGripActive, Is.True);
            yield return Guidance(runtime.ReloadPresentation.GetComponent<TrainingRifleGrabInteractable>());
            var texts=GameMain.Instance.GetComponent<P3LiveUIController>().View.GetComponentsInChildren<TMP_Text>(true);
            var prompts=texts.Where(t=>t.gameObject.activeInHierarchy && t.name.EndsWith("_Prompt")).ToArray();
            Assert.That(prompts, Is.Not.Empty,"HUD prompt panel remains visible.");
            Assert.That(prompts.Any(t=>t.text.Contains("交互")), Is.True,"Keep the existing P3 interaction prompt text.");
            PresentationEvidence.Capture(runtime.PlayerCamera,"03-"+mode+"-held-rifle-no-labels",Evidence,true);
        }

        static IEnumerator Guidance(TrainingRifleGrabInteractable grab)
        {
            // Hover attempts must not re-enable the retained rifle prompt or any of its decorations.
            typeof(TrainingRifleGrabInteractable).GetMethod("SetPromptVisible",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)
                .Invoke(grab,new object[]{true});
            Assert.That(grab.PickupPromptVisible, Is.False);
            var prompt=grab.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Prompt_training-rifle_Pickup");
            Assert.That(prompt.gameObject.activeInHierarchy, Is.False);
            var callouts=Object.FindObjectsOfType<MonoBehaviour>(true).Where(c=>c.GetType().FullName=="Unity.VRTemplate.Callout").ToArray();
            Assert.That(callouts, Is.Not.Empty,"The real mode rig must include the audited Callout assemblies.");
            foreach(var callout in callouts) callout.GetType().GetMethod("GazeHoverStart").Invoke(callout,null);
            yield return new WaitForSeconds(1.1f);
            foreach(var callout in callouts) AssertCalloutHidden(callout);
        }

        static void AssertCalloutHidden(MonoBehaviour callout)
        {
            var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            var tooltip=(Transform)callout.GetType().GetField("m_LazyTooltip",flags).GetValue(callout);
            var curve=(GameObject)callout.GetType().GetField("m_Curve",flags).GetValue(callout);
            // Rig grouping Callouts can omit either optional reference; the standalone prefab test requires both.
            if(tooltip!=null) Assert.That(tooltip.gameObject.activeInHierarchy, Is.False,"Detached tooltip must remain hidden too.");
            if(curve!=null) Assert.That(curve.activeInHierarchy, Is.False,"Entire blue line must remain hidden.");
            Assert.That(callout.GetComponentsInChildren<Renderer>(true).Any(r=>r.enabled&&r.gameObject.activeInHierarchy), Is.False,callout.name);
            Assert.That(callout.GetComponentsInChildren<Canvas>(true).Any(c=>c.enabled&&c.gameObject.activeInHierarchy), Is.False,callout.name);
        }
    }
}
