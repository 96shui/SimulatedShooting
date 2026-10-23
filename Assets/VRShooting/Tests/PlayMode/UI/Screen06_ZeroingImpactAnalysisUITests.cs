using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Unity;
using VRShooting.Unity.UI;

namespace VRShooting.Tests.PlayMode.UI
{
    [TestFixture]
    public class Screen06_ZeroingImpactAnalysisUITests
    {
        GameObject root;
        ApplicationServices services;
        MainMenuUI mainMenuUi;
        ZeroingRangeUI zeroingRangeUi;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // UI contract tests own their services; a previous live scene must not navigate them away.
            if(VRShooting.Unity.Bootstrap.GameMain.Instance!=null)
                Object.Destroy(VRShooting.Unity.Bootstrap.GameMain.Instance.gameObject);
            yield return null;
            services = ApplicationServices.CreateDefault();
            root = new GameObject("Test_ZeroingImpactAnalysisUI", typeof(RectTransform));
            root.SetActive(false);
            mainMenuUi = CreateUiRoot<MainMenuUI>("MainMenuUI");
            zeroingRangeUi = CreateUiRoot<ZeroingRangeUI>("ZeroingRangeUI");
            root.SetActive(true);
            mainMenuUi.Initialize(services);
            zeroingRangeUi.Initialize(services);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null)
            {
                Object.Destroy(root);
            }

            yield return null;
        }

        T CreateUiRoot<T>(string objectName) where T : TrainingUIRoot
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(root.transform, false);
            var ui = go.AddComponent<T>();
            typeof(TrainingUIRoot)
                .GetField("buildOnAwake", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(ui, false);
            return ui;
        }

        [UnityTest]
        public IEnumerator Bdd30_HelpShowsRoundAdviceNotTheEditedOrAppliedAmount()
        {
            yield return OpenHudAndCompleteRound();
            FindButton("Button_ZeroingImpactAnalysis_Help").onClick.Invoke();
            var help = FindText("Text_ZeroingImpactAnalysis_Suggestion").text;
            var front = FindText("Text_ZeroingImpactAnalysis_FrontSight").text;
            var rear = FindText("Text_ZeroingImpactAnalysis_RearSight").text;
            var pending = FindText("Text_ZeroingImpactAnalysis_CorrectionX").text;
            Assert.That(help, Does.Contain("本轮建议"));
            for (var i = 0; i < 4; i++)
                FindButton("Button_ZeroingImpactAnalysis_HorizontalPlus").onClick.Invoke();
            FindButton("Button_ZeroingImpactAnalysis_VerticalPlus").onClick.Invoke();
            Assert.That(FindText("Text_ZeroingImpactAnalysis_CorrectionX").text, Is.Not.EqualTo(pending));
            Assert.That(FindText("Text_ZeroingImpactAnalysis_Suggestion").text, Is.EqualTo(help));
            Assert.That(FindText("Text_ZeroingImpactAnalysis_FrontSight").text, Is.EqualTo(front));
            Assert.That(FindText("Text_ZeroingImpactAnalysis_RearSight").text, Is.EqualTo(rear));
            FindButton("Button_ZeroingImpactAnalysis_ApplyAdjustment").onClick.Invoke();
            Assert.That(FindText("Text_ZeroingImpactAnalysis_Suggestion").text, Is.EqualTo(help));
        }

        [UnityTest]
        public IEnumerator Screen06_ThreeShotsOpenImpactAnalysisAndRenderDto()
        {
            yield return OpenHudAndCompleteRound();

            Assert.AreEqual(ScreenId.ZeroingImpactAnalysis, services.Router.Current);
            Assert.IsTrue(FindById("Screen_ZeroingImpactAnalysis").activeSelf);
            Assert.IsNotNull(FindById("Placeholder_ZeroingImpactAnalysis_Target"));
            Assert.IsTrue(FindById("Image_ZeroingImpactAnalysis_Impact_1").activeSelf);
            Assert.IsTrue(FindById("Image_ZeroingImpactAnalysis_Impact_2").activeSelf);
            Assert.IsTrue(FindById("Image_ZeroingImpactAnalysis_Impact_3").activeSelf);

            AssertHelpVisible(false);
            FindButton("Button_ZeroingImpactAnalysis_Help").onClick.Invoke();
            AssertHelpVisible(true);
            Assert.That(FindText("Text_ZeroingImpactAnalysis_Suggestion").text, Does.Contain("调整1度约0.064厘米"));
            Assert.That(FindText("Text_ZeroingImpactAnalysis_FrontSight").text, Does.Contain("顺时针"));
            Assert.That(FindText("Text_ZeroingImpactAnalysis_RearSight").text, Does.Contain("觇孔"));
        }

        [UnityTest]
        public IEnumerator Screen06_HelpOwnsAllTipsAndResetsOnNextRound()
        {
            // BDD06 2026-10-09: guidance requires an explicit Help click.
            yield return OpenHudAndCompleteRound();
            AssertHelpVisible(false);
            CaptureHelp("P1-analysis-help-closed");
            FindButton("Button_ZeroingImpactAnalysis_HorizontalPlus").onClick.Invoke();
            AssertHelpVisible(false);
            var help = FindButton("Button_ZeroingImpactAnalysis_Help");
            help.onClick.Invoke();
            AssertHelpVisible(true);
            CaptureHelp("P1-analysis-help-open");
            FindButton("Button_ZeroingImpactAnalysis_VerticalPlus").onClick.Invoke();
            AssertHelpVisible(true);
            help.onClick.Invoke();
            AssertHelpVisible(false);
            help.onClick.Invoke();
            ApplyAndNext();
            yield return null;
            Fire(new Vector3(12f, 12f, 100f));
            Fire(new Vector3(12f, 12f, 100f));
            Fire(new Vector3(12f, 12f, 100f));
            yield return null;
            Assert.AreEqual(ScreenId.ZeroingImpactAnalysis, services.Router.Current);
            AssertHelpVisible(false);
        }

        void AssertHelpVisible(bool visible)
        {
            foreach (var suffix in new[] { "FrontSight", "RearSight", "Suggestion", "PreviewAverage" })
                Assert.AreEqual(visible, FindById("Text_ZeroingImpactAnalysis_" + suffix).activeSelf, suffix);
            Assert.IsTrue(FindButton("Button_ZeroingImpactAnalysis_Help").gameObject.activeSelf);
            Assert.IsTrue(FindById("Text_ZeroingImpactAnalysis_CorrectionX").activeSelf);
            Assert.IsTrue(FindById("Text_ZeroingImpactAnalysis_CorrectionY").activeSelf);
        }

        void CaptureHelp(string name)
        {
            var canvas=zeroingRangeUi.GetComponent<Canvas>();
            var previousMode=canvas.renderMode;var previousCamera=canvas.worldCamera;
            var cameraRoot=new GameObject("HelpEvidenceCamera");
            var camera=cameraRoot.AddComponent<Camera>();camera.enabled=false;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=1<<31;
            var parts=zeroingRangeUi.GetComponentsInChildren<Transform>(true);
            var layers=new int[parts.Length];
            var target=new RenderTexture(1920,1080,24);var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            var previousActive=RenderTexture.active;
            try
            {
                for(int i=0;i<parts.Length;i++){layers[i]=parts[i].gameObject.layer;parts[i].gameObject.layer=31;}
                camera.targetTexture=target;
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                Canvas.ForceUpdateCanvases();
                foreach(var text in zeroingRangeUi.GetComponentsInChildren<TMP_Text>())text.ForceMeshUpdate();
                camera.Render();RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();
                var folder=System.IO.Path.Combine(UnityEngine.Application.dataPath,"../Temp/GoalRepair/images");
                System.IO.Directory.CreateDirectory(folder);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,name+".png"),texture.EncodeToPNG());
            }
            finally
            {
                for(int i=0;i<parts.Length;i++)parts[i].gameObject.layer=layers[i];
                canvas.renderMode=previousMode;canvas.worldCamera=previousCamera;RenderTexture.active=previousActive;
                target.Release();Object.Destroy(target);Object.Destroy(texture);Object.Destroy(cameraRoot);
            }
        }

        [UnityTest]
        public IEnumerator Screen06_ApplyAdjustmentUpdatesStateAndNextRoundReturnsToHud()
        {
            yield return OpenHudAndCompleteRound();

            var apply = FindButton("Button_ZeroingImpactAnalysis_ApplyAdjustment");
            apply.onClick.Invoke();
            yield return null;

            Assert.That(FindText("Text_ZeroingImpactAnalysis_AppliedState").text, Does.Contain("已应用"));
            Assert.IsFalse(apply.interactable);

            FindButton("Button_ZeroingImpactAnalysis_NextRound").onClick.Invoke();
            yield return null;

            Assert.AreEqual(ScreenId.ZeroingHud, services.Router.Current);
            Assert.That(FindText("Text_ZeroingHud_Round").text, Does.Contain("2/3"));
            Assert.That(FindText("Text_ZeroingHud_Ammo").text, Does.Contain("3/3"));
        }

        [UnityTest]
        public IEnumerator Screen06_ManualAxisButtonsPreviewAndCommitCorrection()
        {
            // BDD 06: horizontal and vertical controls move the preview independently.
            yield return OpenHudAndCompleteRound();
            var sessionId = services.TrainingSessions.Current.SessionId;
            var before = services.Zeroing.CompleteRound(sessionId).Data;

            var horizontal = FindButton("Button_ZeroingImpactAnalysis_HorizontalPlus");
            var vertical = FindButton("Button_ZeroingImpactAnalysis_VerticalMinus");
            horizontal.onClick.Invoke();
            vertical.onClick.Invoke();
            yield return null;

            var preview = services.Zeroing.CompleteRound(sessionId).Data;
            Assert.AreEqual(before.AverageOffsetCm, preview.AverageOffsetCm);
            Assert.AreEqual(before.ProposedCorrectionCm + new Vector2(2f, -.064f), preview.ProposedCorrectionCm);
            Assert.That(FindText("Text_ZeroingImpactAnalysis_PreviewAverage").text, Does.Contain("预览"));
            var averageMarker = FindById("Image_ZeroingImpactAnalysis_AverageCenter");
            var previewMarker = FindById("Image_ZeroingImpactAnalysis_AdjustedCenter");
            Assert.IsTrue(averageMarker.activeSelf);
            Assert.IsTrue(previewMarker.activeSelf);
            Assert.AreNotEqual(averageMarker.GetComponent<RectTransform>().offsetMin,
                previewMarker.GetComponent<RectTransform>().offsetMin);

            FindButton("Button_ZeroingImpactAnalysis_ApplyAdjustment").onClick.Invoke();
            yield return null;
            Assert.IsFalse(horizontal.interactable);
            Assert.IsFalse(vertical.interactable);
            Assert.IsTrue(services.Zeroing.CompleteRound(sessionId).Data.AdjustmentApplied);
        }

        [UnityTest]
        public IEnumerator Screen06_OneDegreeMovesBlueMarkerVerticallyWithoutChangingHorizontal()
        {
            // BDD06 2026-10-07: preserve real scale even for subpixel movement.
            yield return OpenHudAndCompleteRound();
            var marker = FindById("Image_ZeroingImpactAnalysis_AdjustedCenter").GetComponent<RectTransform>();
            var before = marker.anchoredPosition;
            var sessionId = services.TrainingSessions.Current.SessionId;
            var analysis = services.Zeroing.CompleteRound(sessionId).Data;
            FindButton("Button_ZeroingImpactAnalysis_VerticalPlus").onClick.Invoke();
            yield return null;
            var after = services.Zeroing.CompleteRound(sessionId).Data;
            Assert.That(after.PreviewAverageOffsetCm.y - analysis.PreviewAverageOffsetCm.y,
                Is.EqualTo(.064f).Within(.0001f));
            Assert.That(marker.anchoredPosition.x, Is.EqualTo(before.x).Within(.0001f));
            var expected = TacticalTargetPlot.MapImpactPointCm(new Rect(76.5f, 62.5f, 255f, 255f), after.PreviewAverageOffsetCm);
            Assert.That(Vector2.Distance(marker.anchoredPosition, expected), Is.LessThan(.001f));
            Assert.That(marker.anchoredPosition.y, Is.GreaterThan(before.y));
            FindButton("Button_ZeroingImpactAnalysis_VerticalMinus").onClick.Invoke();
            Assert.That(Vector2.Distance(marker.anchoredPosition, before), Is.LessThan(.001f));
        }
        [UnityTest]
        public IEnumerator Screen06_BackToMainMenuButtonReturnsToMainMenu()
        {
            yield return OpenHudAndCompleteRound();

            FindButton("Button_ZeroingImpactAnalysis_BackToMainMenu").onClick.Invoke();
            yield return null;

            Assert.AreEqual(ScreenId.MainMenu, services.Router.Current);
            Assert.IsTrue(FindById("Screen_MainMenu").activeSelf);
        }

        [UnityTest]
        public IEnumerator Bdd06_VerticalLongHoldRepeatsOneDegreeAtConstantSpeedAndReleaseDoesNotAddClick()
        {
            yield return OpenHudAndCompleteRound();
            var id = services.TrainingSessions.Current.SessionId;
            var before = services.Zeroing.CompleteRound(id).Data.PreviewAverageOffsetCm;
            var button = FindButton("Button_ZeroingImpactAnalysis_VerticalPlus");
            var repeating = button.GetComponent<RepeatingAdjustmentButton>();
            var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            repeating.OnPointerDown(pointer);
            yield return new WaitForSecondsRealtime(2.25f);
            repeating.OnPointerUp(pointer);
            var held = services.Zeroing.CompleteRound(id).Data.PreviewAverageOffsetCm;
            Assert.That(held.y - before.y, Is.GreaterThan(.064f));
            Assert.That(held.y - before.y, Is.LessThan(3f),
                "Long hold must keep the original one-degree step, without switching to five or fifteen degrees.");
            Assert.That(held.x, Is.EqualTo(before.x));
            button.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(services.Zeroing.CompleteRound(id).Data.PreviewAverageOffsetCm, Is.EqualTo(held),
                "Pointer release must not add a step or keep repeating.");
        }

        [UnityTest]
        public IEnumerator Bdd06_LargeSuggestedFrontSightAngleRemainsAdjustableInUi()
        {
            yield return OpenHudAndCompleteRoundAt(new Vector3(-8f, 80f, 100f));
            var id = services.TrainingSessions.Current.SessionId;
            var before = services.Zeroing.CompleteRound(id).Data;
            Assert.That(before.FrontSightDegreesToAdjust, Is.GreaterThan(781f));
            var button = FindButton("Button_ZeroingImpactAnalysis_VerticalMinus");
            for (var i = 0; i < 20; i++)
            {
                Assert.That(button.interactable, Is.True);
                button.onClick.Invoke();
            }
            var after = services.Zeroing.CompleteRound(id).Data;
            Assert.That(after.ProposedCorrectionCm.y,
                Is.EqualTo(before.ProposedCorrectionCm.y - 20f * .064f).Within(.002f));
            var degrees = Mathf.Abs(after.ProposedCorrectionCm.y / ZeroingRules.CmPerFrontSightDegree)
                .ToString("0", System.Globalization.CultureInfo.InvariantCulture);
            Assert.That(FindText("Text_ZeroingImpactAnalysis_CorrectionY").text, Does.Contain(degrees));
            Assert.That(button.interactable, Is.True);
            FindButton("Button_ZeroingImpactAnalysis_ApplyAdjustment").onClick.Invoke();
            Assert.That(button.interactable, Is.False, "Only committing the adjustment should lock this round.");
        }

        [UnityTest]
        public IEnumerator Screen06_ThirdRoundPrimaryActionOpensFinalRating()
        {
            yield return OpenHudAndCompleteRound();
            ApplyAndNext();
            yield return null;

            Fire(new Vector3(12f, 12f, 100f));
            Fire(new Vector3(12f, 12f, 100f));
            Fire(new Vector3(12f, 12f, 100f));
            yield return null;
            ApplyAndNext();
            yield return null;

            Fire(new Vector3(12f, -8f, 100f));
            Fire(new Vector3(12f, -8f, 100f));
            Fire(new Vector3(12f, -8f, 100f));
            yield return null;

            Assert.AreEqual(ScreenId.ZeroingFinalRating, services.Router.Current);
            Assert.IsFalse(FindById("Screen_ZeroingImpactAnalysis").activeSelf);
            Assert.IsTrue(FindById("Screen_ZeroingFinalRating").activeSelf);
        }

        IEnumerator OpenHudAndCompleteRound()
        {
            yield return OpenHudAndCompleteRoundAt(new Vector3(-8f, 12f, 100f));
        }

        [UnityTest]
        public IEnumerator Bdd06_Oct07_HorizontalFractionalMean_PreservesFractionalClicks()
        {
            yield return OpenHudAndCompleteRoundAt(new Vector3(-3.1f, 12f, 100f));
            var sessionId = services.TrainingSessions.Current.SessionId;
            var initial = services.Zeroing.CompleteRound(sessionId).Data;
            var clicks = initial.ProposedCorrectionCm.x / ZeroingRules.CmPerRearSightClick;
            Assert.That(clicks, Is.EqualTo(-initial.AverageOffsetCm.x / 2f).Within(.001f));
            Assert.That(FindText("Text_ZeroingImpactAnalysis_CorrectionX").text, Does.Contain("."));
            for (var i = 0; i < 12; i++)
            {
                FindButton("Button_ZeroingImpactAnalysis_HorizontalMinus").onClick.Invoke();
                yield return null;
                var current = services.Zeroing.CompleteRound(sessionId).Data;
                Assert.That(current.ProposedCorrectionCm.x, Is.EqualTo(initial.ProposedCorrectionCm.x - (i + 1) * 2f));
                Assert.That(FindText("Text_ZeroingImpactAnalysis_CorrectionX").text, Does.Contain("."));
                Assert.That(current.ProposedCorrectionCm.y, Is.EqualTo(initial.ProposedCorrectionCm.y));
            }
        }

        IEnumerator OpenHudAndCompleteRoundAt(Vector3 point)
        {
            FindButton("Button_MainMenu_OpenZeroing").onClick.Invoke();
            yield return null;
            FindButton("Button_ZeroingBriefing_Start").onClick.Invoke();
            yield return null;

            Fire(point);
            Fire(point);
            Fire(point);
            yield return null;
        }

        void Fire(Vector3 hitPoint)
        {
            var sessionId = services.TrainingSessions.Current.SessionId;
            Assert.IsTrue(services.WeaponControl.SetGripState(new WeaponGripStateInputDto
            {
                SessionId = sessionId,
                HoldState = WeaponHoldState.TwoHandHeld,
                RearHandTracked = true,
                FrontHandTracked = true,
                Stability01 = 0.95f
            }).Success);
            var result = services.WeaponControl.Fire(new WeaponFireInputDto
            {
                SessionId = sessionId,
                MuzzlePosition = Vector3.zero,
                RawAimDirection = Vector3.forward,
                AimDirection = Vector3.forward,
                WeaponPosition = Vector3.zero,
                Stability01 = 0.95f,
                TwoHandGripActive = true,
                AimMode = WeaponAimMode.AimDownSights,
                ShoulderSide = ShoulderSide.Right,
                Hit = true,
                HitPoint = hitPoint,
                HitObjectId = "Target_100m"
            });

            Assert.IsTrue(result.Success, result.Message);
        }

        void ApplyAndNext()
        {
            FindButton("Button_ZeroingImpactAnalysis_ApplyAdjustment").onClick.Invoke();
            FindButton("Button_ZeroingImpactAnalysis_NextRound").onClick.Invoke();
        }

        Button FindButton(string id)
        {
            var go = FindById(id);
            Assert.IsNotNull(go, id);
            var button = go.GetComponent<Button>();
            Assert.IsNotNull(button, id);
            return button;
        }

        TextMeshProUGUI FindText(string id)
        {
            var go = FindById(id);
            Assert.IsNotNull(go, id);
            var text = go.GetComponent<TextMeshProUGUI>();
            Assert.IsNotNull(text, id);
            return text;
        }

        GameObject FindById(string id)
        {
            var allIds = root.GetComponentsInChildren<UITestId>(true);
            for (var i = 0; i < allIds.Length; i++)
            {
                if (allIds[i].Id == id)
                {
                    return allIds[i].gameObject;
                }
            }

            return null;
        }
    }
}
