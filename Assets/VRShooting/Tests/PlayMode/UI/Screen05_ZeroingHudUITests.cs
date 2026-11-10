using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Unity;
using VRShooting.Unity.UI;

namespace VRShooting.Tests.PlayMode.UI
{
    /// <summary>
    /// task009 100m 射击 HUD 测试。追溯 docs/BDD/screens/05-100m射击HUD.feature.md。
    /// </summary>
    [TestFixture]
    public class Screen05_ZeroingHudUITests
    {
        GameObject root;
        ApplicationServices services;
        MainMenuUI mainMenuUi;
        ZeroingRangeUI zeroingRangeUi;

        [SetUp]
        public void SetUp()
        {
            services = ApplicationServices.CreateDefault();
            root = new GameObject("Test_ZeroingHudUI", typeof(RectTransform));
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
        public IEnumerator Screen05_ZeroingHud_LoadsInitialDtoValues()
        {
            yield return StartZeroingHud();

            Assert.IsTrue(FindById("Screen_ZeroingHud").activeSelf);
            Assert.That(FindText("Text_ZeroingHud_Round").text, Does.Contain("1/3"));
            Assert.That(FindText("Text_ZeroingHud_Distance").text, Does.Contain("100m"));
            Assert.That(FindText("Text_ZeroingHud_Ammo").text, Does.Contain("3/3"));
            Assert.That(FindText("Text_ZeroingHud_Sights").text, Does.Contain("向前 1 格").And.Contain("顺时针 5°"));
            Assert.IsNotNull(FindById("Hud_Zeroing_Stability"));
            Assert.IsNotNull(FindById("Hud_Zeroing_ImpactRecord"));
            Assert.That(FindText("Text_ZeroingHud_Prompt").text, Does.Contain("稳定据枪"));
        }

        [UnityTest]
        public IEnumerator Bdd04_RangePickupPromptAndInstructionsHaveValidSafeLayout()
        {
            var view = zeroingRangeUi.GetComponent<TrainingPresentationView>();
            view.Apply(new TrainingPresentationDto
            {
                LargePanelVisible = true,
                ActiveScreen = ScreenId.ZeroingBriefing,
                AwaitingWeaponPickup = true
            });
            yield return null;
            Canvas.ForceUpdateCanvases();
            var prompt = FindRangeRect("Training.Shared.PickupPrompt");
            var station = FindRangeRect("Training.Shared.FiringStationState");
            var frame = FindRangeRect("Panel_Training_AwaitingWeaponPickup_Content");
            var start = FindRangeRect("Button_ZeroingBriefing_Start");
            var promptText = prompt.GetComponent<TMP_Text>();
            Assert.That(promptText.text, Is.EqualTo("请用右手近距取枪"));
            promptText.ForceMeshUpdate();
            Assert.That(promptText.textInfo.characterCount, Is.GreaterThan(0), "The safe-area rect must actually fit visible glyphs.");
            Assert.That(prompt.offsetMin, Is.EqualTo(new Vector2(480, 46)));
            Assert.That(prompt.offsetMax, Is.EqualTo(new Vector2(1440, 84)));
            Assert.That(prompt.offsetMax.y, Is.LessThan(frame.offsetMin.y));
            Assert.That(prompt.offsetMax.y, Is.LessThan(start.offsetMin.y));
            Assert.That(prompt.offsetMax.y, Is.LessThan(station.offsetMin.y));
            Assert.That(FindRangeRect("Text_Training_AwaitingWeaponPickup_Title").rect.width, Is.GreaterThan(0f));
            Assert.That(FindRangeRect("Text_Training_AwaitingWeaponPickup_Rules").rect.height, Is.GreaterThan(0f));
        }

        RectTransform FindRangeRect(string id)
        {
            foreach (var item in zeroingRangeUi.GetComponentsInChildren<UITestId>(true))
                if (item.Id == id) return item.GetComponent<RectTransform>();
            Assert.Fail("Missing range UI test ID: " + id);
            return null;
        }

        [UnityTest]
        public IEnumerator Screen05_ZeroingHud_FireRefreshesAmmoAndImpactRecord()
        {
            yield return StartZeroingHud();

            FireShot(true);
            yield return null;

            Assert.That(FindText("Text_ZeroingHud_Ammo").text, Does.Contain("2/3"));
            Assert.That(FindText("Text_ZeroingHud_ImpactRecord").text, Does.Contain("已记录 1/3"));
            Assert.That(FindText("Text_ZeroingHud_ImpactRecord").text, Does.Contain("命中"));
        }

        [UnityTest]
        public IEnumerator Screen05_ZeroingHud_ToggleShoulderRefreshesPrompt()
        {
            yield return StartZeroingHud();

            var result = services.WeaponControl.ToggleShoulder(services.TrainingSessions.Current.SessionId);
            Assert.IsTrue(result.Success, result.Message);
            yield return null;

            Assert.That(FindText("Text_ZeroingHud_Shoulder").text, Does.Contain("左肩"));
        }

        [UnityTest]
        public IEnumerator Screen05_ZeroingHud_ZeroAmmoShowsBlockedPrompt()
        {
            yield return StartZeroingHud();

            FireShot(true);
            FireShot(true);
            FireShot(true);
            yield return null;

            Assert.That(FindText("Text_ZeroingHud_Ammo").text, Does.Contain("0/3"));
            Assert.That(FindText("Text_ZeroingHud_Prompt").text, Does.Contain("本轮射击已完成"));
        }

        IEnumerator StartZeroingHud()
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

            Assert.AreEqual(ScreenId.ZeroingHud, services.Router.Current);
            Assert.IsTrue(services.TrainingSessions.HasActiveSession);
        }

        void FireShot(bool hit)
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
                ShoulderSide = services.TrainingSessions.Current.Player.Shoulder,
                Hit = hit,
                HitPoint = new Vector3(0f, 0f, 100f),
                HitObjectId = "Target_100m"
            });

            Assert.IsTrue(result.Success, result.Message);
        }

        UnityEngine.UI.Button FindButton(string id)
        {
            var go = FindById(id);
            Assert.IsNotNull(go, id);
            var button = go.GetComponent<UnityEngine.UI.Button>();
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
