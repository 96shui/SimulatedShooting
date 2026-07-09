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
        P1MainMenuZeroingBriefingUI ui;

        [SetUp]
        public void SetUp()
        {
            services = ApplicationServices.CreateDefault();
            root = new GameObject("Test_ZeroingImpactAnalysisUI", typeof(RectTransform));
            root.SetActive(false);
            ui = root.AddComponent<P1MainMenuZeroingBriefingUI>();
            typeof(P1MainMenuZeroingBriefingUI)
                .GetField("buildOnAwake", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(ui, false);
            root.SetActive(true);
            ui.Initialize(services);
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
            Assert.That(FindText("Text_ZeroingImpactAnalysis_VerticalOffset").text, Does.Contain("偏上"));
            Assert.That(FindText("Text_ZeroingImpactAnalysis_HorizontalOffset").text, Does.Contain("水平偏差"));
            Assert.That(FindText("Text_ZeroingImpactAnalysis_FrontSight").text, Does.Contain("逆时针"));
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

            Fire(new Vector3(-8f, 12f, 100f));
            Fire(new Vector3(-8f, 12f, 100f));
            Fire(new Vector3(-8f, 12f, 100f));
            yield return null;
            ApplyAndNext();
            yield return null;

            Fire(new Vector3(-8f, 12f, 100f));
            Fire(new Vector3(-8f, 12f, 100f));
            Fire(new Vector3(-8f, 12f, 100f));
            yield return null;

            var apply = FindButton("Button_ZeroingImpactAnalysis_ApplyAdjustment");
            apply.onClick.Invoke();
            yield return null;

            var next = FindButton("Button_ZeroingImpactAnalysis_NextRound");
            Assert.That(FindText("Text_ZeroingImpactAnalysis_NextRound").text, Does.Contain("查看评级"));

            next.onClick.Invoke();
            yield return null;

            Assert.AreEqual(ScreenId.ZeroingFinalRating, services.Router.Current);
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
            var result = services.WeaponControl.Fire(new WeaponFireInputDto
            {
                SessionId = services.TrainingSessions.Current.SessionId,
                MuzzlePosition = Vector3.zero,
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
