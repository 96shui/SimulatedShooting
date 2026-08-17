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
    public class Screen07_ZeroingFinalRatingUITests
    {
        GameObject root;
        ApplicationServices services;
        MainMenuUI mainMenuUi;
        ZeroingRangeUI zeroingRangeUi;

        [SetUp]
        public void SetUp()
        {
            services = ApplicationServices.CreateDefault();
            root = new GameObject("Test_ZeroingFinalRatingUI", typeof(RectTransform));
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
        public IEnumerator Screen07_FirstRoundPassShowsExcellentFinalRating()
        {
            yield return OpenHudAndCompletePassingRound();

            FindButton("Button_ZeroingImpactAnalysis_ApplyAdjustment").onClick.Invoke();
            yield return null;

            FindButton("Button_ZeroingImpactAnalysis_NextRound").onClick.Invoke();
            yield return null;

            Assert.AreEqual(ScreenId.ZeroingFinalRating, services.Router.Current);
            Assert.IsTrue(FindById("Screen_ZeroingFinalRating").activeSelf);
            Assert.That(FindText("Text_ZeroingFinalRating_Grade").text, Does.Contain("优秀"));
            Assert.That(FindText("Text_ZeroingFinalRating_Rounds").text, Does.Contain("第1轮：通过"));
            Assert.That(FindText("Text_ZeroingFinalRating_Rounds").text, Does.Contain("第2轮：未使用"));
            Assert.That(FindText("Text_ZeroingFinalRating_ImpactThumbnails").text, Does.Contain("第1轮"));
        }

        [UnityTest]
        public IEnumerator Screen07_RetryReturnsToZeroingBriefing()
        {
            yield return OpenFinalRating();

            FindButton("Button_ZeroingFinalRating_Retry").onClick.Invoke();
            yield return null;

            Assert.AreEqual(ScreenId.ZeroingBriefing, services.Router.Current);
            Assert.IsTrue(FindById("Screen_ZeroingBriefing").activeSelf);
            Assert.IsFalse(services.TrainingSessions.HasActiveSession);
        }

        [UnityTest]
        public IEnumerator Screen07_BackToModeSelectionReturnsToMainMenuFallback()
        {
            yield return OpenFinalRating();

            FindButton("Button_ZeroingFinalRating_BackToModeSelection").onClick.Invoke();
            yield return null;

            Assert.AreEqual(ScreenId.MainMenu, services.Router.Current);
            Assert.IsTrue(FindById("Screen_MainMenu").activeSelf);
            Assert.IsFalse(services.TrainingSessions.HasActiveSession);
        }

        IEnumerator OpenFinalRating()
        {
            yield return OpenHudAndCompletePassingRound();
            FindButton("Button_ZeroingImpactAnalysis_ApplyAdjustment").onClick.Invoke();
            yield return null;
            FindButton("Button_ZeroingImpactAnalysis_NextRound").onClick.Invoke();
            yield return null;
        }

        IEnumerator OpenHudAndCompletePassingRound()
        {
            FindButton("Button_MainMenu_OpenZeroing").onClick.Invoke();
            yield return null;
            FindButton("Button_ZeroingBriefing_Start").onClick.Invoke();
            yield return null;

            var sessionId = services.TrainingSessions.Current.SessionId;
            var pickup = services.WeaponControl.SetGripState(new WeaponGripStateInputDto
            {
                SessionId = sessionId,
                HoldState = WeaponHoldState.TwoHandHeld,
                RearHandTracked = true,
                FrontHandTracked = true,
                Stability01 = 0.95f
            });
            Assert.IsTrue(pickup.Success, pickup.Message);
            yield return null;

            RecordZeroingShot();
            RecordZeroingShot();
            RecordZeroingShot();
            yield return null;
        }

        void RecordZeroingShot()
        {
            var offset = services.Zeroing.GetSession(services.TrainingSessions.Current.SessionId).Data.FixedImpactOffsetCm;
            var aim = new Vector2(1f, 1f) - offset;
            var result = services.Zeroing.RecordShot(services.TrainingSessions.Current.SessionId, new ShotInputDto
            {
                AimDirection = new Vector3(aim.x, aim.y, ZeroingRules.DistanceMeters),
                WeaponPosition = Vector3.zero,
                WeaponStability = 0.95f,
                FireTime = Time.timeAsDouble
            });

            Assert.IsTrue(result.Success, result.Message);
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
