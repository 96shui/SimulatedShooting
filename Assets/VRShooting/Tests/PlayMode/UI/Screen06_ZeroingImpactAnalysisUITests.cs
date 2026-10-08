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

        [SetUp]
        public void SetUp()
        {
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
        public IEnumerator Screen06_ThreeShotsOpenImpactAnalysisAndRenderDto()
        {
            yield return OpenHudAndCompleteRound();

            Assert.AreEqual(ScreenId.ZeroingImpactAnalysis, services.Router.Current);
            Assert.IsTrue(FindById("Screen_ZeroingImpactAnalysis").activeSelf);
            Assert.IsNotNull(FindById("Placeholder_ZeroingImpactAnalysis_Target"));
            Assert.IsTrue(FindById("Image_ZeroingImpactAnalysis_Impact_1").activeSelf);
            Assert.IsTrue(FindById("Image_ZeroingImpactAnalysis_Impact_2").activeSelf);
            Assert.IsTrue(FindById("Image_ZeroingImpactAnalysis_Impact_3").activeSelf);

            Assert.IsFalse(FindById("Text_ZeroingImpactAnalysis_Suggestion").activeSelf);
            FindButton("Button_ZeroingImpactAnalysis_Help").onClick.Invoke();
            Assert.IsTrue(FindById("Text_ZeroingImpactAnalysis_Suggestion").activeSelf);
            Assert.That(FindText("Text_ZeroingImpactAnalysis_Suggestion").text, Does.Contain("调整1度约0.064厘米"));
            Assert.That(FindText("Text_ZeroingImpactAnalysis_FrontSight").text, Does.Contain("顺时针"));
            Assert.That(FindText("Text_ZeroingImpactAnalysis_RearSight").text, Does.Contain("觇孔"));
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
            FindButton("Button_MainMenu_OpenZeroing").onClick.Invoke();
            yield return null;
            FindButton("Button_ZeroingBriefing_Start").onClick.Invoke();
            yield return null;

            Fire(new Vector3(-8f, 12f, 100f));
            Fire(new Vector3(-8f, 12f, 100f));
            Fire(new Vector3(-8f, 12f, 100f));
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
