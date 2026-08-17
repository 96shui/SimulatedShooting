using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SimulatedShooting.Scene;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Input;
using VRShooting.Unity;
using VRShooting.Unity.Bootstrap;
using VRShooting.Unity.Player;
using VRShooting.Unity.UI;

namespace VRShooting.Tests.PlayMode.Flow
{
    /// <summary>
    /// task014 P1 端到端 PlayMode 联调门禁。
    /// 追溯 docs/BDD/screens/02、04、05、06、07.feature.md。
    /// 失败断言消息带 [UI]/[场景]/[功能A]/[功能B] 前缀，便于定位责任边界。
    /// </summary>
    [TestFixture]
    public sealed class P1FlowPlayModeGateTests
    {
        const string FailImpactObjectId = "Target_100m";
        static readonly Vector2 FailImpactCm = new Vector2(-8f, 12f);
        static readonly Vector2 PassImpactCm = new Vector2(1f, 1f);

        GameObject uiRoot;
        ApplicationServices services;
        ManualXRTrainingInput trainingInput;
        XRTrainingInputCommandDispatcher inputDispatcher;
        MainMenuXRModeController xrModeController;
        bool sceneOwnedMode;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (uiRoot != null)
            {
                Object.Destroy(uiRoot);
                uiRoot = null;
            }

            services = null;
            trainingInput = null;
            inputDispatcher = null;

            if (sceneOwnedMode)
            {
                if (xrModeController != null)
                {
                    xrModeController.ClearForcedModeForTests();
                    xrModeController = null;
                }

                if (GameMain.Instance != null)
                {
                    Object.Destroy(GameMain.Instance.gameObject);
                }

                if (TrainingUIHost.Instance != null)
                {
                    Object.Destroy(TrainingUIHost.Instance.gameObject);
                }

                if (PlayerFollowCamera.Instance != null)
                {
                    Object.Destroy(PlayerFollowCamera.Instance.gameObject);
                }

                sceneOwnedMode = false;
            }

            yield return null;
        }

        /// <summary>
        /// BDD 02/04/05/06/07：输入替身进入 100m → 开始 → 3 发通过 → 分析 → 应用调整 → 结算优秀。
        /// </summary>
        [UnityTest]
        public IEnumerator P1Flow_InputStub_Round1Pass_ShowsExcellentFinalRating()
        {
            yield return SetUpInMemoryFixture();
            yield return OpenZeroingViaInputStubAndStartTraining();
            yield return FireThreeShotsForImpact(PassImpactCm);

            AssertLayer(
                "UI",
                ScreenId.ZeroingImpactAnalysis,
                "Screen_ZeroingImpactAnalysis",
                "three passing shots should open impact analysis");

            yield return ApplyAdjustmentAndAdvance();

            AssertLayer(
                "UI",
                ScreenId.ZeroingFinalRating,
                "Screen_ZeroingFinalRating",
                "round-1 pass should open final rating");
            Assert.That(
                FindText("Text_ZeroingFinalRating_Grade").text,
                Does.Contain("优秀"),
                "[功能A] final grade DTO should map round-1 pass to 优秀");
            Assert.That(
                FindText("Text_ZeroingFinalRating_Rounds").text,
                Does.Contain("第1轮：通过"),
                "[功能A] settlement should mark round 1 as passed");

            var final = services.Zeroing.GetFinalResult(services.TrainingSessions.Current.SessionId);
            Assert.IsTrue(final.Success, "[功能A] GetFinalResult failed: " + final.Message);
            Assert.AreEqual(ResultGrade.Excellent, final.Data.Grade, "[功能A] ResultGrade.Excellent expected");
        }

        /// <summary>
        /// BDD 07 场景大纲「未通过 → 不及格」：三轮均未进 10 环后结算。
        /// </summary>
        [UnityTest]
        public IEnumerator P1Flow_InputStub_ThreeRoundsFail_ShowsFailFinalRating()
        {
            yield return SetUpInMemoryFixture();
            yield return OpenZeroingViaInputStubAndStartTraining();

            for (var round = 1; round <= 3; round++)
            {
                yield return FireThreeShotsForImpact(FailImpactCm);
                AssertLayer(
                    "UI",
                    ScreenId.ZeroingImpactAnalysis,
                    "Screen_ZeroingImpactAnalysis",
                    "round " + round + " should open impact analysis after 3 shots");

                yield return ApplyAdjustmentAndAdvance();

                if (round < 3)
                {
                    AssertLayer(
                        "UI",
                        ScreenId.ZeroingHud,
                        "Screen_ZeroingHud",
                        "failed round " + round + " should return to HUD for next round");
                    Assert.That(
                        FindText("Text_ZeroingHud_Round").text,
                        Does.Contain((round + 1) + "/3"),
                        "[功能A] HUD round text should advance after continue");
                    Assert.That(
                        FindText("Text_ZeroingHud_Ammo").text,
                        Does.Contain("3/3"),
                        "[功能B] ammo should reload for the next round");
                }
            }

            AssertLayer(
                "UI",
                ScreenId.ZeroingFinalRating,
                "Screen_ZeroingFinalRating",
                "three failed rounds should open final rating");
            Assert.That(
                FindText("Text_ZeroingFinalRating_Grade").text,
                Does.Contain("不及格"),
                "[功能A] final grade DTO should map no-pass to 不及格");
            Assert.That(
                FindText("Text_ZeroingFinalRating_Rounds").text,
                Does.Contain("第3轮：未通过"),
                "[功能A] settlement should show round 3 as failed");

            var final = services.Zeroing.GetFinalResult(services.TrainingSessions.Current.SessionId);
            Assert.IsTrue(final.Success, "[功能A] GetFinalResult failed: " + final.Message);
            Assert.AreEqual(ResultGrade.Fail, final.Data.Grade, "[功能A] ResultGrade.Fail expected");
            Assert.AreEqual(0, final.Data.PassedRoundIndex, "[功能A] PassedRoundIndex should stay 0");
        }

        /// <summary>
        /// BDD 02/04/05/06/07：场景联调门禁 — MainScene → ZeroingRangeScene → 可控优秀结算。
        /// </summary>
        [UnityTest]
        public IEnumerator P1Flow_SceneOwned_MainMenuToRange_Round1PassShowsExcellent()
        {
            sceneOwnedMode = true;
            yield return SceneManager.LoadSceneAsync("MainScene", LoadSceneMode.Single);
            yield return null;

            xrModeController = Object.FindObjectOfType<MainMenuXRModeController>(true);
            Assert.That(xrModeController, Is.Not.Null, "[场景] MainScene missing MainMenuXRModeController");
            xrModeController.SetVrModeForTests(false);
            yield return null;

            Assert.That(GameMain.Instance, Is.Not.Null, "[场景] MainScene missing GameMain");
            services = GameMain.Instance.Services;
            Assert.That(services, Is.Not.Null, "[功能A] GameMain.Services was not initialized");

            ClickButton("Button_MainMenu_OpenZeroing");
            yield return null;
            Assert.That(
                FindById("Screen_ZeroingBriefing")?.activeSelf,
                Is.True,
                "[UI] MainMenu_OpenZeroing did not show Screen_ZeroingBriefing");

            ClickButton("Button_ZeroingBriefing_Start");
            var loadDeadline = Time.realtimeSinceStartup + 30f;
            yield return new WaitUntil(() =>
                SceneManager.GetActiveScene().name == "ZeroingRangeScene"
                || Time.realtimeSinceStartup >= loadDeadline);
            yield return null;
            yield return null;

            Assert.AreEqual(
                "ZeroingRangeScene",
                SceneManager.GetActiveScene().name,
                "[场景] Zeroing_Start did not load ZeroingRangeScene within 30s");
            Assert.That(
                FindSceneAnchor("ZeroingRange.Target.Primary"),
                Is.Not.Null,
                "[场景] SceneTestId ZeroingRange.Target.Primary not found after scene load");
            Assert.That(
                FindSceneAnchor("ZeroingRange.ShootingPosition"),
                Is.Not.Null,
                "[场景] SceneTestId ZeroingRange.ShootingPosition not found after scene load");
            Assert.That(
                Object.FindObjectOfType<ZeroingRangeUI>(true),
                Is.Not.Null,
                "[UI] ZeroingRangeScene missing scene-owned ZeroingRangeUI");
            Assert.That(
                FindById("Training.Shared.LargePanelRoot")?.activeSelf,
                Is.True,
                "[UI] range scene should keep pickup guidance visible after start");
            Assert.IsTrue(
                services.TrainingSessions.HasActiveSession,
                "[功能A] training session missing after briefing start");

            PickupWeapon(services.TrainingSessions.Current.SessionId);
            yield return null;
            Assert.That(
                FindById("Screen_ZeroingHud")?.activeSelf,
                Is.True,
                "[UI] range scene should show Screen_ZeroingHud after valid pickup");

            yield return FireThreeShotsForImpact(PassImpactCm);
            AssertLayer(
                "UI",
                ScreenId.ZeroingImpactAnalysis,
                "Screen_ZeroingImpactAnalysis",
                "scene-owned range should open impact analysis after 3 shots");

            yield return ApplyAdjustmentAndAdvance();
            AssertLayer(
                "UI",
                ScreenId.ZeroingFinalRating,
                "Screen_ZeroingFinalRating",
                "scene-owned round-1 pass should open final rating");
            Assert.That(
                FindText("Text_ZeroingFinalRating_Grade").text,
                Does.Contain("优秀"),
                "[功能A] scene-owned settlement should show 优秀");
        }

        IEnumerator SetUpInMemoryFixture()
        {
            sceneOwnedMode = false;
            trainingInput = new ManualXRTrainingInput();
            services = ApplicationServices.CreateDefault(trainingInput);
            inputDispatcher = new XRTrainingInputCommandDispatcher(
                trainingInput,
                services.EventBus,
                services.Router);

            uiRoot = new GameObject("Test_P1FlowGate", typeof(RectTransform));
            uiRoot.SetActive(false);
            CreateUiRoot<MainMenuUI>("MainMenuUI");
            CreateUiRoot<ZeroingRangeUI>("ZeroingRangeUI");
            uiRoot.SetActive(true);

            var mainMenu = uiRoot.GetComponentInChildren<MainMenuUI>(true);
            var rangeUi = uiRoot.GetComponentInChildren<ZeroingRangeUI>(true);
            Assert.That(mainMenu, Is.Not.Null, "[UI] failed to create MainMenuUI fixture");
            Assert.That(rangeUi, Is.Not.Null, "[UI] failed to create ZeroingRangeUI fixture");
            mainMenu.Initialize(services);
            rangeUi.Initialize(services);
            yield return null;
        }

        IEnumerator OpenZeroingViaInputStubAndStartTraining()
        {
            trainingInput.Clear();
            trainingInput.Press(XRTrainingInputButton.Confirm);
            var confirm = inputDispatcher.ProcessFrame(new XRTrainingInputDispatchContext
            {
                SourceScreen = ScreenId.MainMenu,
                ConfirmUIEvent = UIEventId.MainMenu_OpenZeroing
            });
            trainingInput.AdvanceFrame();
            Assert.IsTrue(confirm.Success, "[功能B] Confirm input stub failed: " + confirm.Message);
            yield return null;

            Assert.AreEqual(
                ScreenId.ZeroingBriefing,
                services.Router.Current,
                "[功能B/UI] input stub Confirm should route MainMenu → ZeroingBriefing");
            Assert.That(
                FindById("Screen_ZeroingBriefing")?.activeSelf,
                Is.True,
                "[UI] Screen_ZeroingBriefing should be visible after Confirm");

            ClickButton("Button_ZeroingBriefing_Start");
            yield return null;

            Assert.IsTrue(
                services.TrainingSessions.HasActiveSession,
                "[功能A] Start should create a Running Zeroing100m session");
            Assert.AreEqual(
                TrainingMode.Zeroing100m,
                services.TrainingSessions.Current.Mode,
                "[功能A] session mode should be Zeroing100m");
            Assert.AreEqual(
                ScreenId.ZeroingBriefing,
                services.Router.Current,
                "[UI] briefing should remain visible while awaiting weapon pickup");

            PickupWeapon(services.TrainingSessions.Current.SessionId);
            yield return null;
            AssertLayer(
                "UI",
                ScreenId.ZeroingHud,
                "Screen_ZeroingHud",
                "valid pickup should open ZeroingHud");
            Assert.That(
                FindText("Text_ZeroingHud_Ammo").text,
                Does.Contain("3/3"),
                "[功能B] HUD ammo should start at 3/3");
        }

        IEnumerator FireThreeShotsForImpact(Vector2 desiredImpactCm)
        {
            Assert.IsTrue(
                services.TrainingSessions.HasActiveSession,
                "[功能A] cannot fire without an active training session");

            var sessionId = services.TrainingSessions.Current.SessionId;
            var offsetResult = services.Zeroing.GetSession(sessionId);
            Assert.IsTrue(offsetResult.Success, "[功能A] GetSession failed: " + offsetResult.Message);

            var aimCm = desiredImpactCm - offsetResult.Data.FixedImpactOffsetCm;
            var hitPoint = new Vector3(aimCm.x, aimCm.y, ZeroingRules.DistanceMeters);

            for (var i = 0; i < ZeroingRules.ShotsPerRound; i++)
            {
                FireWeaponShot(sessionId, hitPoint);
            }

            yield return null;
        }

        void FireWeaponShot(string sessionId, Vector3 hitPoint)
        {
            PickupWeapon(sessionId);

            var fire = services.WeaponControl.Fire(new WeaponFireInputDto
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
                HitObjectId = FailImpactObjectId
            });
            Assert.IsTrue(fire.Success, "[功能B] WeaponControl.Fire failed: " + fire.Message);
            Assert.IsTrue(fire.Data.IsValidShot, "[功能B] Fire returned an invalid shot result");
        }

        void PickupWeapon(string sessionId)
        {
            var grip = services.WeaponControl.SetGripState(new WeaponGripStateInputDto
            {
                SessionId = sessionId,
                HoldState = WeaponHoldState.TwoHandHeld,
                RearHandTracked = true,
                FrontHandTracked = true,
                Stability01 = 0.95f
            });
            Assert.IsTrue(grip.Success, "[功能B] SetGripState TwoHandHeld failed: " + grip.Message);
        }

        IEnumerator ApplyAdjustmentAndAdvance()
        {
            ClickButton("Button_ZeroingImpactAnalysis_ApplyAdjustment");
            yield return null;
            Assert.That(
                FindText("Text_ZeroingImpactAnalysis_AppliedState").text,
                Does.Contain("已应用"),
                "[功能A/UI] ApplyAdjustment should mark analysis as applied");

            ClickButton("Button_ZeroingImpactAnalysis_NextRound");
            yield return null;
        }

        void AssertLayer(string owner, ScreenId expectedScreen, string screenId, string reason)
        {
            Assert.AreEqual(
                expectedScreen,
                services.Router.Current,
                "[" + owner + "] router.Current mismatch: " + reason);
            var screen = FindById(screenId);
            Assert.That(screen, Is.Not.Null, "[" + owner + "] missing " + screenId + ": " + reason);
            Assert.That(screen.activeSelf, Is.True, "[" + owner + "] inactive " + screenId + ": " + reason);
        }

        T CreateUiRoot<T>(string objectName) where T : TrainingUIRoot
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(uiRoot.transform, false);
            var ui = go.AddComponent<T>();
            typeof(TrainingUIRoot)
                .GetField("buildOnAwake", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(ui, false);
            return ui;
        }

        void ClickButton(string id)
        {
            var button = FindButton(id);
            Assert.That(button.interactable, Is.True, "[UI] button not interactable: " + id);
            button.onClick.Invoke();
        }

        Button FindButton(string id)
        {
            var go = FindById(id);
            Assert.That(go, Is.Not.Null, "[UI] missing button id: " + id);
            var button = go.GetComponent<Button>();
            Assert.That(button, Is.Not.Null, "[UI] GameObject has no Button: " + id);
            return button;
        }

        TextMeshProUGUI FindText(string id)
        {
            var go = FindById(id);
            Assert.That(go, Is.Not.Null, "[UI] missing text id: " + id);
            var text = go.GetComponent<TextMeshProUGUI>();
            Assert.That(text, Is.Not.Null, "[UI] GameObject has no TextMeshProUGUI: " + id);
            return text;
        }

        GameObject FindById(string id)
        {
            if (uiRoot != null)
            {
                var localIds = uiRoot.GetComponentsInChildren<UITestId>(true);
                for (var i = 0; i < localIds.Length; i++)
                {
                    if (localIds[i].Id == id)
                    {
                        return localIds[i].gameObject;
                    }
                }
            }

            var allIds = Object.FindObjectsOfType<UITestId>(true);
            for (var i = 0; i < allIds.Length; i++)
            {
                if (allIds[i].Id == id)
                {
                    return allIds[i].gameObject;
                }
            }

            return null;
        }

        static GameObject FindSceneAnchor(string id)
        {
            return SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<SceneTestId>(true))
                .FirstOrDefault(testId => testId.Id == id)
                ?.gameObject;
        }
    }
}
