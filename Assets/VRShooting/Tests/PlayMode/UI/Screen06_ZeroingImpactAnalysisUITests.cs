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
        public IEnumerator Bdd06_OpeningHelpHidesAnswerButtonAndPanel()
        {
            // BDD06 2026-11-10: help and answer are mutually exclusive.
            yield return OpenHudAndCompleteRound();
            var answer = FindButton("Button_ZeroingImpactAnalysis_Answer");
            var panel = FindById("Panel_ZeroingImpactAnalysis_Answer");
            Assert.IsTrue(answer.gameObject.activeSelf);
            Assert.IsFalse(panel.activeSelf);

            FindButton("Button_ZeroingImpactAnalysis_Help").onClick.Invoke();

            Assert.IsTrue(FindText("Text_ZeroingImpactAnalysis_Suggestion").gameObject.activeSelf);
            Assert.IsFalse(answer.gameObject.activeSelf, "Opening help must hide the answer button.");
            Assert.IsFalse(panel.activeSelf);
        }

        [UnityTest]
        public IEnumerator Bdd06_InfoPanelsAreExclusiveAndKeepTheirFixedPositions()
        {
            // BDD06 2026-11-10: three states, without moving either ray target.
            yield return OpenHudAndCompleteRound();
            var help = FindButton("Button_ZeroingImpactAnalysis_Help");
            var answer = FindButton("Button_ZeroingImpactAnalysis_Answer");
            var answerPanel = FindById("Panel_ZeroingImpactAnalysis_Answer");
            AssertInfoPanelsCollapsed();
            AssertInfoPanelLayout();
            CaptureMutexEvidence("01-collapsed");

            help.onClick.Invoke();
            AssertHelpVisible(true);
            Assert.That(help.GetComponentInChildren<TextMeshProUGUI>().text, Is.EqualTo("收起帮助"));
            Assert.IsFalse(answer.gameObject.activeSelf);
            Assert.IsFalse(answerPanel.activeSelf);
            Assert.IsFalse(FindById("Text_ZeroingImpactAnalysis_Coordinates").activeSelf);
            AssertInfoPanelLayout();
            CaptureMutexEvidence("02-help-expanded");

            help.onClick.Invoke();
            AssertInfoPanelsCollapsed();
            AssertInfoPanelLayout();
            answer.onClick.Invoke();
            Assert.IsTrue(answer.gameObject.activeSelf);
            Assert.IsFalse(help.gameObject.activeSelf);
            Assert.IsTrue(answerPanel.activeSelf);
            foreach (var suffix in new[] { "FrontSight", "RearSight", "Suggestion", "PreviewAverage" })
                Assert.IsFalse(FindById("Text_ZeroingImpactAnalysis_" + suffix).activeSelf, suffix);
            Assert.IsTrue(FindById("Text_ZeroingImpactAnalysis_Coordinates").activeSelf);
            AssertInfoPanelLayout();
            CaptureMutexEvidence("03-answer-expanded");

            answer.onClick.Invoke();
            AssertInfoPanelsCollapsed();
            AssertInfoPanelLayout();
            CaptureMutexEvidence("04-collapsed-restored");
        }

        [UnityTest]
        public IEnumerator Bdd06_InfoPanelsResetWhenChangingRoundLeavingAndReentering()
        {
            yield return OpenHudAndCompleteRound();
            FindButton("Button_ZeroingImpactAnalysis_Help").onClick.Invoke();
            Assert.IsFalse(FindButton("Button_ZeroingImpactAnalysis_Answer").gameObject.activeSelf);
            ApplyAndNext();
            yield return null;
            Assert.That(services.Router.Current, Is.EqualTo(ScreenId.ZeroingHud));
            AssertInfoPanelsCollapsed();

            Fire(new Vector3(12f, 12f, 100f));
            Fire(new Vector3(12f, 12f, 100f));
            Fire(new Vector3(12f, 12f, 100f));
            Fire(new Vector3(12f, 12f, 100f));
            yield return null;
            Assert.That(services.Router.Current, Is.EqualTo(ScreenId.ZeroingImpactAnalysis));
            AssertInfoPanelsCollapsed();
            FindButton("Button_ZeroingImpactAnalysis_Answer").onClick.Invoke();
            Assert.IsFalse(FindButton("Button_ZeroingImpactAnalysis_Help").gameObject.activeSelf);
            FindButton("Button_ZeroingImpactAnalysis_BackToMainMenu").onClick.Invoke();
            yield return null;
            Assert.That(services.Router.Current, Is.EqualTo(ScreenId.MainMenu));
            AssertInfoPanelsCollapsed();

            yield return OpenHudAndCompleteRound();
            Assert.That(services.Router.Current, Is.EqualTo(ScreenId.ZeroingImpactAnalysis));
            AssertInfoPanelsCollapsed();
        }

        void AssertInfoPanelsCollapsed()
        {
            AssertHelpVisible(false);
            Assert.That(FindButton("Button_ZeroingImpactAnalysis_Help").GetComponentInChildren<TextMeshProUGUI>().text, Is.EqualTo("帮助"));
            Assert.IsTrue(FindButton("Button_ZeroingImpactAnalysis_Answer").gameObject.activeSelf);
            Assert.IsFalse(FindById("Panel_ZeroingImpactAnalysis_Answer").activeSelf);
            Assert.IsTrue(FindById("Text_ZeroingImpactAnalysis_Coordinates").activeSelf);
        }

        void AssertInfoPanelLayout()
        {
            var help = FindButton("Button_ZeroingImpactAnalysis_Help").GetComponent<RectTransform>();
            var answer = FindButton("Button_ZeroingImpactAnalysis_Answer").GetComponent<RectTransform>();
            var panel = FindById("Panel_ZeroingImpactAnalysis_Answer").GetComponent<RectTransform>();
            Assert.That(help.offsetMin, Is.EqualTo(new Vector2(28, 285)));
            Assert.That(help.offsetMax, Is.EqualTo(new Vector2(160, 342)));
            Assert.That(answer.offsetMin, Is.EqualTo(new Vector2(176, 285)));
            Assert.That(answer.offsetMax, Is.EqualTo(new Vector2(308, 342)));
            Assert.That(panel.offsetMin, Is.EqualTo(new Vector2(1450, 300)));
            Assert.That(panel.offsetMax, Is.EqualTo(new Vector2(1830, 850)));
        }

        void CaptureMutexEvidence(string name)
        {
            if (System.Environment.GetEnvironmentVariable("HELP_ANSWER_MUTEX_CAPTURE") == "1")
                CaptureHelp(name);
        }

        [UnityTest]
        public IEnumerator Bdd06_AnswerReflectsActualPendingAndAppliedCorrectionAndResetsOnNextRound()
        {
            yield return OpenHudAndCompleteRound();
            var sessionId = services.TrainingSessions.Current.SessionId;
            var answer = FindButton("Button_ZeroingImpactAnalysis_Answer");
            Assert.That(answer.GetComponentInChildren<TextMeshProUGUI>().text, Is.EqualTo("答案"));
            var panel = FindById("Panel_ZeroingImpactAnalysis_Answer");
            Assert.IsNotNull(panel);
            Assert.IsFalse(panel.activeSelf);
            var help = FindButton("Button_ZeroingImpactAnalysis_Help");
            var helpRect = help.GetComponent<RectTransform>();
            var answerRect = answer.GetComponent<RectTransform>();
            Assert.That(answerRect.sizeDelta, Is.EqualTo(helpRect.sizeDelta));
            Assert.That(answerRect.offsetMin.x, Is.GreaterThan(helpRect.offsetMax.x));
            answer.onClick.Invoke();
            Assert.IsTrue(panel.activeSelf);
            Assert.That(answer.GetComponentInChildren<TextMeshProUGUI>().text, Is.EqualTo("答案"));
            var text = FindText("Text_ZeroingImpactAnalysis_AnswerContent");
            var initial = text.text;
            Assert.That(initial, Does.Contain("当前固定偏移").And.Contain("觇孔").And.Contain("准心柱").And.Contain("通过"));
            Assert.That(initial, Does.Contain("优秀").And.Contain("良好").And.Contain("及格"));
            Assert.That(initial, Does.Contain("觇孔：0 格").And.Contain("准心柱：0°"));
            Assert.IsFalse(help.gameObject.activeSelf);
            Assert.IsFalse(FindById("Text_ZeroingImpactAnalysis_Suggestion").activeSelf);
            Assert.That(FindText("Text_ZeroingImpactAnalysis_FrontSight").text, Does.Contain("顺时针 5°"));
            Assert.That(FindText("Text_ZeroingImpactAnalysis_RearSight").text, Does.Contain("向前 1 格"));
            FindButton("Button_ZeroingImpactAnalysis_HorizontalPlus").onClick.Invoke();
            EnsureAdjustmentDoesNotPass();
            Assert.That(text.text, Is.Not.EqualTo(initial));
            var pending = services.Zeroing.CompleteRound(sessionId).Data;
            Assert.That(text.text, Does.Contain((pending.AverageOffsetCm + pending.ProposedCorrectionCm).magnitude.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture)));
            var prediction = (pending.AverageOffsetCm + pending.ProposedCorrectionCm).magnitude;
            FindButton("Button_ZeroingImpactAnalysis_ApplyAdjustment").onClick.Invoke();
            var applied = services.Zeroing.CompleteRound(sessionId).Data;
            Assert.IsTrue(panel.activeSelf);
            Assert.That(text.text, Does.Contain("已应用"));
            Assert.That((applied.AverageOffsetCm + applied.AppliedCorrectionCm).magnitude, Is.EqualTo(prediction).Within(.001f));
            Assert.That(text.text, Does.Contain(prediction.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture)));
            Assert.That(text.text, Does.Contain("当前固定偏移"));
            Assert.That(FindText("Text_ZeroingImpactAnalysis_RearSight").text, Is.Not.EqualTo("觇孔：向前 1 格"));
            FindButton("Button_ZeroingImpactAnalysis_NextRound").onClick.Invoke();
            yield return null;
            Assert.IsFalse(panel.activeSelf);
            Assert.IsTrue(help.gameObject.activeSelf);
            Assert.IsTrue(answer.gameObject.activeSelf);
            Fire(new Vector3(12f, 12f, 100f)); Fire(new Vector3(12f, 12f, 100f)); Fire(new Vector3(12f, 12f, 100f)); Fire(new Vector3(12f, 12f, 100f));
            yield return null;
            Assert.AreEqual(ScreenId.ZeroingImpactAnalysis, services.Router.Current);
            Assert.IsFalse(panel.activeSelf);
            Assert.IsTrue(help.gameObject.activeSelf);
            Assert.IsTrue(answer.gameObject.activeSelf);
            answer.onClick.Invoke();
            Assert.That(text.text, Is.Not.EqualTo(initial));
        }

        [UnityTest]
        public IEnumerator Bdd06_AnswerAndSightReadoutsFollowReversedActualAdjustment()
        {
            yield return OpenHudAndCompleteRound();
            var sessionId = services.TrainingSessions.Current.SessionId;
            var help = FindButton("Button_ZeroingImpactAnalysis_Help");
            help.onClick.Invoke();
            AssertHelpVisible(true);
            help.onClick.Invoke();
            FindButton("Button_ZeroingImpactAnalysis_Answer").onClick.Invoke();
            for (var i = 0; i < 6; i++) FindButton("Button_ZeroingImpactAnalysis_HorizontalMinus").onClick.Invoke();
            for (var i = 0; i < 42; i++) FindButton("Button_ZeroingImpactAnalysis_VerticalPlus").onClick.Invoke();
            var answer = FindText("Text_ZeroingImpactAnalysis_AnswerContent").text;
            Assert.That(answer, Does.Contain("觇孔：向后 6 格").And.Contain("准心柱：逆时针 210°"));
            var correction = services.Zeroing.CompleteRound(sessionId).Data.ProposedCorrectionCm;
            Assert.That(correction.x, Is.EqualTo(-12f).Within(.001f));
            Assert.That(correction.y, Is.EqualTo(-13.44f).Within(.001f));
            FindButton("Button_ZeroingImpactAnalysis_ApplyAdjustment").onClick.Invoke();
            var current = services.Zeroing.GetSession(sessionId).Data.CurrentAdjustment;
            Assert.That(current.RearSightClicks, Is.EqualTo(-5));
            Assert.That(current.FrontSightDegrees, Is.EqualTo(-205f));
            Assert.That(FindText("Text_ZeroingImpactAnalysis_RearSight").text, Does.Contain("向后 5 格"));
            Assert.That(FindText("Text_ZeroingImpactAnalysis_FrontSight").text, Does.Contain("逆时针 205°"));
        }

        [UnityTest]
        public IEnumerator Bdd06_ApplyingNearZeroOffsetImmediatelyEnablesViewGrade()
        {
            // Aim at the centre: the initial fixed offset guarantees three shots outside the ten ring.
            yield return OpenHudAndCompleteRoundAt(new Vector3(0f, 0f, 100f), true);
            Assert.That(services.Router.Current, Is.EqualTo(ScreenId.ZeroingImpactAnalysis));
            var sessionId = services.TrainingSessions.Current.SessionId;
            var before = services.Zeroing.CompleteRound(sessionId).Data;
            Assert.That(before.PassedTenRing, Is.False);
            Assert.That(FindText("Text_ZeroingImpactAnalysis_AppliedState").text, Does.Contain("均值").And.Contain("5"));
            var xSteps = Mathf.RoundToInt(-before.AverageOffsetCm.x / 2f);
            var ySteps = Mathf.RoundToInt(-before.AverageOffsetCm.y / .32f);
            for (var i = 0; i < Mathf.Abs(xSteps); i++)
                FindButton("Button_ZeroingImpactAnalysis_Horizontal" + (xSteps > 0 ? "Plus" : "Minus")).onClick.Invoke();
            for (var i = 0; i < Mathf.Abs(ySteps); i++)
                FindButton("Button_ZeroingImpactAnalysis_Vertical" + (ySteps > 0 ? "Minus" : "Plus")).onClick.Invoke();
            FindButton("Button_ZeroingImpactAnalysis_ApplyAdjustment").onClick.Invoke();
            yield return null;
            var after = services.Zeroing.CompleteRound(sessionId).Data;
            Assert.That(after.PassedTenRing, Is.True);
            Assert.That(after.FinalResultAvailable, Is.True);
            Assert.That(after.Shots[0].ImpactPointCm, Is.EqualTo(before.Shots[0].ImpactPointCm));
            Assert.That(FindText("Text_ZeroingImpactAnalysis_AppliedState").text, Does.Contain("通过"));
            var next = FindButton("Button_ZeroingImpactAnalysis_NextRound");
            Assert.That(next.interactable, Is.True);
            Assert.That(next.GetComponentInChildren<TextMeshProUGUI>().text, Is.EqualTo("查看评级"));
            next.onClick.Invoke();
            yield return null;
            Assert.That(services.Router.Current, Is.EqualTo(ScreenId.ZeroingFinalRating));
            Assert.That(services.Zeroing.GetFinalResult(sessionId).Data.Grade, Is.EqualTo(ResultGrade.Excellent));
        }

        [UnityTest]
        public IEnumerator Bdd06_AdjustmentControlsShowCurrentMechanicalValuesAndUnits()
        {
            yield return OpenHudAndCompleteRound();
            var id = services.TrainingSessions.Current.SessionId;
            var horizontal = FindButton("Button_ZeroingImpactAnalysis_HorizontalPlus");
            var vertical = FindButton("Button_ZeroingImpactAnalysis_VerticalPlus");
            Assert.That(horizontal.GetComponentInChildren<TextMeshProUGUI>().text, Does.Contain("1格"));
            Assert.That(vertical.GetComponentInChildren<TextMeshProUGUI>().text, Does.Contain("5°"));
            Assert.That(FindButton("Button_ZeroingImpactAnalysis_HorizontalMinus").GetComponentInChildren<TextMeshProUGUI>().text, Does.Contain("1格"));
            Assert.That(FindButton("Button_ZeroingImpactAnalysis_VerticalMinus").GetComponentInChildren<TextMeshProUGUI>().text, Does.Contain("5°"));
            var before = services.Zeroing.CompleteRound(id).Data.ProposedCorrectionCm;
            horizontal.onClick.Invoke();
            vertical.onClick.Invoke();
            var after = services.Zeroing.CompleteRound(id).Data.ProposedCorrectionCm;
            Assert.That(after.x, Is.EqualTo(before.x + 2f).Within(.001f));
            Assert.That(after.y, Is.EqualTo(before.y - .32f).Within(.001f));
            var clicks = Mathf.Abs(after.x / ZeroingRules.CmPerRearSightClick).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "格";
            var degrees = Mathf.Abs(after.y / ZeroingRules.CmPerFrontSightDegree).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "°";
            Assert.That(FindText("Text_ZeroingImpactAnalysis_CorrectionX").text, Does.Contain(clicks));
            Assert.That(FindText("Text_ZeroingImpactAnalysis_CorrectionY").text, Does.Contain(degrees));
            if (Mathf.Abs(after.x) > .001f)
                Assert.That(FindText("Text_ZeroingImpactAnalysis_CorrectionX").text, Does.Contain(after.x > 0 ? "向前" : "向后"));
            if (Mathf.Abs(after.y) > .001f)
                Assert.That(FindText("Text_ZeroingImpactAnalysis_CorrectionY").text, Does.Contain(after.y > 0 ? "顺时针" : "逆时针"));
            FindButton("Button_ZeroingImpactAnalysis_ApplyAdjustment").onClick.Invoke();
            Assert.That(horizontal.interactable, Is.False);
            Assert.That(vertical.interactable, Is.False);
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
            var coordinates = FindText("Text_ZeroingImpactAnalysis_Coordinates").text;
            Assert.That(coordinates, Does.Contain("#1").And.Contain("#2").And.Contain("#3"));
            Assert.That(coordinates, Does.Contain("均值(黄)").And.Contain("当前补偿(蓝)").And.Contain("cm"));
            var analysis = services.Zeroing.CompleteRound(services.TrainingSessions.Current.SessionId).Data;
            for (var i = 0; i < analysis.Shots.Count; i++)
            {
                var dot = FindById("Image_ZeroingImpactAnalysis_Impact_" + (i + 1)).GetComponent<RectTransform>();
                var expected = TacticalTargetPlot.MapImpactPointCm(new Rect(76.5f, 62.5f, 255f, 255f), analysis.Shots[i].ImpactPointCm);
                Assert.That(Vector2.Distance(dot.anchoredPosition, expected), Is.LessThan(.01f));
            }

            AssertHelpVisible(false);
            FindButton("Button_ZeroingImpactAnalysis_Help").onClick.Invoke();
            AssertHelpVisible(true);
            Assert.That(FindText("Text_ZeroingImpactAnalysis_Suggestion").text, Does.Contain("每次调整5度约0.32厘米"));
            Assert.That(FindText("Text_ZeroingImpactAnalysis_FrontSight").text, Does.Contain("顺时针"));
            Assert.That(FindText("Text_ZeroingImpactAnalysis_RearSight").text, Does.Contain("觇孔"));
            Assert.That(FindText("Text_ZeroingImpactAnalysis_RearSight").text, Does.Contain("格"));
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
                var evidenceRequested = System.Environment.GetEnvironmentVariable("HELP_ANSWER_MUTEX_CAPTURE") == "1"
                    && (name == "01-collapsed" || name == "02-help-expanded" || name == "03-answer-expanded" || name == "04-collapsed-restored");
                var folder=System.IO.Path.Combine(UnityEngine.Application.dataPath,
                    evidenceRequested ? "../docs/codex-reports/evidence/help-answer-mutex-2026-11-10" : "../Temp/GoalRepair/images");
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
            EnsureAdjustmentDoesNotPass();

            var apply = FindButton("Button_ZeroingImpactAnalysis_ApplyAdjustment");
            apply.onClick.Invoke();
            yield return null;

            Assert.That(FindText("Text_ZeroingImpactAnalysis_AppliedState").text, Does.Contain("已应用"));
            Assert.IsFalse(apply.interactable);

            FindButton("Button_ZeroingImpactAnalysis_NextRound").onClick.Invoke();
            yield return null;

            Assert.AreEqual(ScreenId.ZeroingHud, services.Router.Current);
            Assert.That(FindText("Text_ZeroingHud_Round").text, Does.Contain("2/3"));
            Assert.That(FindText("Text_ZeroingHud_Ammo").text, Does.Contain("4/4"));
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
            Assert.That(Vector2.Distance(before.ProposedCorrectionCm + new Vector2(2f, .32f), preview.ProposedCorrectionCm), Is.LessThan(.001f));
            Assert.That(FindText("Text_ZeroingImpactAnalysis_PreviewAverage").text, Does.Contain("当前补偿"));
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
        public IEnumerator Screen06_FiveDegreesMoveBlueMarkerVerticallyWithoutChangingHorizontal()
        {
            // BDD06: preserve the physical scale of one five-degree step.
            yield return OpenHudAndCompleteRound();
            var marker = FindById("Image_ZeroingImpactAnalysis_AdjustedCenter").GetComponent<RectTransform>();
            var before = marker.anchoredPosition;
            var sessionId = services.TrainingSessions.Current.SessionId;
            var analysis = services.Zeroing.CompleteRound(sessionId).Data;
            FindButton("Button_ZeroingImpactAnalysis_VerticalMinus").onClick.Invoke();
            yield return null;
            var after = services.Zeroing.CompleteRound(sessionId).Data;
            Assert.That(after.PreviewAverageOffsetCm.y - analysis.PreviewAverageOffsetCm.y,
                Is.EqualTo(.32f).Within(.0001f));
            Assert.That(marker.anchoredPosition.x, Is.EqualTo(before.x).Within(.0001f));
            var expected = TacticalTargetPlot.MapImpactPointCm(new Rect(76.5f, 62.5f, 255f, 255f), after.ProposedCorrectionCm);
            Assert.That(Vector2.Distance(marker.anchoredPosition, expected), Is.LessThan(.001f));
            Assert.That(marker.anchoredPosition.y, Is.GreaterThan(before.y));
            FindButton("Button_ZeroingImpactAnalysis_VerticalPlus").onClick.Invoke();
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
        public IEnumerator Bdd06_VerticalLongHoldRepeatsFiveDegreesAndReleaseDoesNotAddClick()
        {
            yield return OpenHudAndCompleteRound();
            var id = services.TrainingSessions.Current.SessionId;
            var before = services.Zeroing.CompleteRound(id).Data.PreviewAverageOffsetCm;
            var button = FindButton("Button_ZeroingImpactAnalysis_VerticalMinus");
            var repeating = button.GetComponent<RepeatingAdjustmentButton>();
            var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            repeating.OnPointerDown(pointer);
            yield return new WaitForSecondsRealtime(2.25f);
            repeating.OnPointerUp(pointer);
            var held = services.Zeroing.CompleteRound(id).Data.PreviewAverageOffsetCm;
            Assert.That(held.y - before.y, Is.GreaterThan(.32f));
            Assert.That(held.y - before.y, Is.LessThan(12f));
            Assert.That((held.y - before.y) / .32f, Is.EqualTo(Mathf.Round((held.y - before.y) / .32f)).Within(.001f));
            Assert.That(held.x, Is.EqualTo(before.x));
            button.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(services.Zeroing.CompleteRound(id).Data.PreviewAverageOffsetCm, Is.EqualTo(held),
                "Pointer release must not add a step or keep repeating.");
        }

        [UnityTest]
        public IEnumerator Bdd06_LargeSuggestedFrontSightAngleRemainsAdjustableInUi()
        {
            yield return OpenHudAndCompleteRoundAt(new Vector3(-8f, 20f, 100f));
            var id = services.TrainingSessions.Current.SessionId;
            var before = services.Zeroing.CompleteRound(id).Data;
            Assert.That(before.FrontSightDegreesToAdjust, Is.GreaterThan(250f));
            var button = FindButton("Button_ZeroingImpactAnalysis_VerticalMinus");
            for (var i = 0; i < 20; i++)
            {
                Assert.That(button.interactable, Is.True);
                button.onClick.Invoke();
            }
            var after = services.Zeroing.CompleteRound(id).Data;
            Assert.That(after.ProposedCorrectionCm.y,
                Is.EqualTo(before.ProposedCorrectionCm.y + 20f * .32f).Within(.002f));
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
            Fire(new Vector3(12f, 12f, 100f));
            yield return null;
            ApplyAndNext();
            yield return null;

            Fire(new Vector3(12f, -8f, 100f));
            Fire(new Vector3(12f, -8f, 100f));
            Fire(new Vector3(12f, -8f, 100f));
            Fire(new Vector3(12f, -8f, 100f));
            yield return null;

            Assert.AreEqual(ScreenId.ZeroingImpactAnalysis, services.Router.Current);
            FindButton("Button_ZeroingImpactAnalysis_NextRound").onClick.Invoke();
            yield return null;
            Assert.AreEqual(ScreenId.ZeroingFinalRating, services.Router.Current);
            Assert.IsFalse(FindById("Screen_ZeroingImpactAnalysis").activeSelf);
            Assert.IsTrue(FindById("Screen_ZeroingFinalRating").activeSelf);
        }

        [UnityTest]
        public IEnumerator Bdd06_FourImpactsShowIdenticalMarkersAndAverage()
        {
            yield return OpenAnalysisWithImpacts(new Vector3(5,7,100), new Vector3(13,7,100), new Vector3(9,14,100), new Vector3(22,20,100));
            var result = services.Zeroing.CompleteRound(services.TrainingSessions.Current.SessionId).Data;
            Assert.That(result.DiscardedShotIndex, Is.EqualTo(3));
            var targetRegion = new Rect(76.5f, 62.5f, 255f, 255f);
            for (var i = 1; i <= 4; i++)
            {
                var dot = FindById("Image_ZeroingImpactAnalysis_Impact_" + i);
                Assert.That(dot, Is.Not.Null);
                Assert.That(dot.activeSelf, Is.True);
                Assert.That(FindById("Text_ZeroingImpactAnalysis_Discarded_" + i), Is.Null);
                Assert.That(dot.GetComponent<Image>().color, Is.EqualTo((Color)new Color32(255, 92, 68, 255)));
                var outline = dot.GetComponent<Outline>();
                Assert.That(outline.effectColor, Is.EqualTo((Color)new Color32(255, 225, 160, 255)));
                Assert.That(outline.effectDistance, Is.EqualTo(new Vector2(2f, -2f)));
                Assert.That(dot.transform.childCount, Is.EqualTo(1));
                var missed = FindById("Text_ZeroingImpactAnalysis_Missed_" + i);
                Assert.That(missed, Is.Not.Null);
                Assert.That(missed.activeSelf, Is.False);
                Assert.That(missed.transform.parent, Is.EqualTo(dot.transform));
                var expected = TacticalTargetPlot.MapImpactPointCm(targetRegion, result.Shots[i - 1].ImpactPointCm);
                Assert.That(Vector2.Distance(dot.GetComponent<RectTransform>().anchoredPosition, expected), Is.LessThan(.01f));
            }
            var average = FindById("Image_ZeroingImpactAnalysis_AverageCenter");
            var correction = FindById("Image_ZeroingImpactAnalysis_AdjustedCenter");
            Assert.That(average.activeSelf, Is.True);
            Assert.That(correction.activeSelf, Is.True);
            Assert.That(Vector2.Distance(average.GetComponent<RectTransform>().anchoredPosition,
                TacticalTargetPlot.MapImpactPointCm(targetRegion, result.AverageOffsetCm)), Is.LessThan(.01f));
            Assert.That(Vector2.Distance(correction.GetComponent<RectTransform>().anchoredPosition,
                TacticalTargetPlot.MapImpactPointCm(targetRegion, result.ProposedCorrectionCm)), Is.LessThan(.01f));
            CaptureP1Evidence(zeroingRangeUi, "02-analysis-four-shots-no-label");
        }

        [UnityTest]
        public IEnumerator Bdd06_OffTargetShotStillAppearsInTheFourShotPlot()
        {
            yield return OpenAnalysisWithImpacts(new Vector3(8,8,100), new Vector3(9,8,100), new Vector3(8,9,100), new Vector3(70,0,100));
            var analysis = services.Zeroing.CompleteRound(services.TrainingSessions.Current.SessionId).Data;
            Assert.That(analysis.DiscardedShotIndex, Is.EqualTo(-1));
            Assert.That(analysis.CalculationSetAvailable, Is.True);
            for (var i = 1; i <= 4; i++) Assert.That(FindById("Image_ZeroingImpactAnalysis_Impact_" + i).activeSelf, Is.True);
            Assert.That(FindById("Text_ZeroingImpactAnalysis_Missed_4").activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator Bdd06_SpreadDisablesAllAdjustmentsAndHidesAnswer()
        {
            yield return OpenAnalysisWithImpacts(new Vector3(-20,0,100), new Vector3(0,0,100), new Vector3(20,0,100), new Vector3(20,0,100));
            Assert.That(FindText("Text_ZeroingImpactAnalysis_Coordinates").text, Does.Contain("弹着点过于分散，本次射击无参考价值，不可用于调整，请进行下一轮。"));
            foreach (var id in new[] { "HorizontalMinus", "HorizontalPlus", "VerticalMinus", "VerticalPlus", "ApplyAdjustment" })
                Assert.That(FindButton("Button_ZeroingImpactAnalysis_" + id).interactable, Is.False, id);
            Assert.That(FindById("Text_ZeroingImpactAnalysis_Suggestion").activeSelf, Is.False);
            Assert.That(FindById("Button_ZeroingImpactAnalysis_Answer").activeSelf, Is.False);
            Assert.That(FindButton("Button_ZeroingImpactAnalysis_NextRound").interactable, Is.True);
            CaptureP1Evidence(zeroingRangeUi, "03-analysis-spread");
        }

        [UnityTest]
        public IEnumerator Bdd06_ThirdSpreadAllowsRatingWithoutAdjustment()
        {
            var points = new[] { new Vector3(-20,0,100), new Vector3(0,0,100), new Vector3(20,0,100), new Vector3(20,0,100) };
            yield return OpenAnalysisWithImpacts(points);
            for (var round = 1; round < 3; round++)
            {
                Assert.That(services.Zeroing.CompleteRound(services.TrainingSessions.Current.SessionId).Data.SpreadRejected, Is.True);
                FindButton("Button_ZeroingImpactAnalysis_NextRound").onClick.Invoke();
                yield return null;
                foreach (var point in points) Fire(point);
                yield return null;
            }
            var button = FindButton("Button_ZeroingImpactAnalysis_NextRound");
            Assert.That(button.interactable, Is.True);
            Assert.That(button.GetComponentInChildren<TextMeshProUGUI>().text, Is.EqualTo("查看评级"));
            button.onClick.Invoke();
            yield return null;
            Assert.That(services.Router.Current, Is.EqualTo(ScreenId.ZeroingFinalRating));
            Assert.That(services.Zeroing.GetFinalResult(services.TrainingSessions.Current.SessionId).Data.Grade, Is.EqualTo(ResultGrade.Fail));
        }

        [UnityTest]
        public IEnumerator Bdd06_AboveSuggestsCounterclockwiseAndClockwiseMovesBlueUp()
        {
            yield return OpenHudAndCompleteRound();
            FindButton("Button_ZeroingImpactAnalysis_Help").onClick.Invoke();
            Assert.That(FindText("Text_ZeroingImpactAnalysis_Suggestion").text, Does.Contain("逆时针"));
            var marker = FindById("Image_ZeroingImpactAnalysis_AdjustedCenter").GetComponent<RectTransform>();
            var initial = marker.anchoredPosition;
            FindButton("Button_ZeroingImpactAnalysis_VerticalMinus").onClick.Invoke();
            FindButton("Button_ZeroingImpactAnalysis_VerticalMinus").onClick.Invoke();
            yield return null;
            Assert.That(marker.anchoredPosition.y, Is.GreaterThan(initial.y));
            Assert.That(services.Zeroing.CompleteRound(services.TrainingSessions.Current.SessionId).Data.ProposedCorrectionCm.y, Is.EqualTo(.64f).Within(.001f));
            CaptureP1Evidence(zeroingRangeUi, "04-analysis-clockwise-blue-up");
        }

        [UnityTest]
        public IEnumerator Bdd04_BriefingShowsFourShotsAndBothPassPaths()
        {
            FindButton("Button_MainMenu_OpenZeroing").onClick.Invoke();
            yield return null;
            var text = FindById("Panel_ZeroingBriefing_Rules").GetComponentInChildren<TMP_Text>(true).text;
            Assert.That(text, Does.Contain("4发").And.Contain("集中").And.Contain("≤10cm").And.Contain("<5cm"));
            CaptureP1Evidence(mainMenuUi, "01-briefing");
        }

        IEnumerator OpenAnalysisWithImpacts(params Vector3[] points)
        {
            FindButton("Button_MainMenu_OpenZeroing").onClick.Invoke();
            yield return null;
            FindButton("Button_ZeroingBriefing_Start").onClick.Invoke();
            yield return null;
            foreach (var point in points) Fire(point);
            yield return null;
        }

        void CaptureP1Evidence(TrainingUIRoot ui, string name)
        {
            if (System.Environment.GetEnvironmentVariable("P1_FOUR_CAPTURE") != "1") return;
            var canvas = ui.GetComponent<Canvas>();
            var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera;
            var cameraRoot = new GameObject("P1EvidenceCamera");
            var camera = cameraRoot.AddComponent<Camera>(); camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.cullingMask = 1 << 31;
            var parts = ui.GetComponentsInChildren<Transform>(true); var layers = new int[parts.Length];
            var target = new RenderTexture(1920,1080,24); var texture = new Texture2D(1920,1080,TextureFormat.RGB24,false);
            var oldActive = RenderTexture.active;
            try
            {
                for (var i=0;i<parts.Length;i++) { layers[i]=parts[i].gameObject.layer; parts[i].gameObject.layer=31; }
                camera.targetTexture=target; canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
                Canvas.ForceUpdateCanvases(); foreach(var text in ui.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate();
                camera.Render(); RenderTexture.active=target; texture.ReadPixels(new Rect(0,0,1920,1080),0,0); texture.Apply();
                var folder=System.IO.Path.Combine(UnityEngine.Application.dataPath,"../docs/codex-reports/evidence/p1-four-shots-2026-11-11");
                System.IO.Directory.CreateDirectory(folder); System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,name+".png"),texture.EncodeToPNG());
            }
            finally
            {
                for(var i=0;i<parts.Length;i++) parts[i].gameObject.layer=layers[i];
                canvas.renderMode=oldMode; canvas.worldCamera=oldCamera; RenderTexture.active=oldActive;
                target.Release(); Object.Destroy(target); Object.Destroy(texture); Object.Destroy(cameraRoot);
            }
        }

        IEnumerator OpenHudAndCompleteRound()
        {
            yield return OpenHudAndCompleteRoundAt(new Vector3(-8f, 12f, 100f));
        }

        [UnityTest]
        public IEnumerator Bdd06_HorizontalFractionalMean_UsesWholeClicks()
        {
            yield return OpenHudAndCompleteRoundAt(new Vector3(-3.1f, 12f, 100f));
            var sessionId = services.TrainingSessions.Current.SessionId;
            var initial = services.Zeroing.CompleteRound(sessionId).Data;
            var clicks = initial.ProposedCorrectionCm.x / ZeroingRules.CmPerRearSightClick;
            Assert.That(clicks, Is.Zero.Within(.001f));
            Assert.That(FindText("Text_ZeroingImpactAnalysis_CorrectionX").text, Does.Contain("格"));
            for (var i = 0; i < 12; i++)
            {
                FindButton("Button_ZeroingImpactAnalysis_HorizontalMinus").onClick.Invoke();
                yield return null;
                var current = services.Zeroing.CompleteRound(sessionId).Data;
                Assert.That(current.ProposedCorrectionCm.x, Is.EqualTo(initial.ProposedCorrectionCm.x - (i + 1) * 2f));
                Assert.That(FindText("Text_ZeroingImpactAnalysis_CorrectionX").text, Does.Contain("格"));
                Assert.That(current.ProposedCorrectionCm.y, Is.EqualTo(initial.ProposedCorrectionCm.y));
            }
        }

        IEnumerator OpenHudAndCompleteRoundAt(Vector3 point, bool rawAim = false)
        {
            FindButton("Button_MainMenu_OpenZeroing").onClick.Invoke();
            yield return null;
            FindButton("Button_ZeroingBriefing_Start").onClick.Invoke();
            yield return null;

            Fire(point, rawAim);
            Fire(point, rawAim);
            Fire(point, rawAim);
            Fire(point, rawAim);
            yield return null;
        }

        void Fire(Vector3 hitPoint, bool rawAim = false)
        {
            var sessionId = services.TrainingSessions.Current.SessionId;
            // Most fixtures specify the desired recorded impact, independent of the runtime seed.
            if (!rawAim)
            {
                var offset = services.Zeroing.GetSession(sessionId).Data.FixedImpactOffsetCm;
                hitPoint.x -= offset.x;
                hitPoint.y -= offset.y;
            }
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
            EnsureAdjustmentDoesNotPass();
            FindButton("Button_ZeroingImpactAnalysis_ApplyAdjustment").onClick.Invoke();
            FindButton("Button_ZeroingImpactAnalysis_NextRound").onClick.Invoke();
        }

        void EnsureAdjustmentDoesNotPass()
        {
            var sessionId = services.TrainingSessions.Current.SessionId;
            for (var i = 0; i < 30; i++)
            {
                var offset = services.Zeroing.GetSession(sessionId).Data.FixedImpactOffsetCm;
                var analysis = services.Zeroing.CompleteRound(sessionId).Data;
                if ((analysis.AverageOffsetCm + analysis.ProposedCorrectionCm).magnitude >= 5f) return;
                FindButton("Button_ZeroingImpactAnalysis_HorizontalPlus").onClick.Invoke();
            }
            Assert.Fail("Could not arrange a non-passing adjustment for the next-round fixture.");
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
