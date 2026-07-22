using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VRShooting.Unity;
using VRShooting.Unity.Bootstrap;
using VRShooting.Unity.Player;
using VRShooting.Unity.UI;

namespace VRShooting.Tests.PlayMode.UI
{
    [TestFixture]
    public sealed class Screen02_05_SceneOwnedUIFlowTests
    {
        MainMenuXRModeController xrModeController;

        [UnitySetUp]
        public IEnumerator LoadMainScene()
        {
            yield return SceneManager.LoadSceneAsync("MainScene", LoadSceneMode.Single);
            yield return null;

            xrModeController = Object.FindObjectOfType<MainMenuXRModeController>(true);
            Assert.That(xrModeController, Is.Not.Null);
            xrModeController.SetVrModeForTests(false);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator RestoreAutomaticXrMode()
        {
            if (xrModeController != null)
            {
                xrModeController.ClearForcedModeForTests();
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

            yield return null;
        }

        [UnityTest]
        public IEnumerator Screen02_05_ClickZeroingStartsRangeSceneWithSceneOwnedHud()
        {
            var mainMenu = Object.FindObjectOfType<MainMenuUI>(true);
            Assert.That(mainMenu, Is.Not.Null, "MainScene should contain a scene-owned MainMenuUI.");
            Assert.AreEqual(SceneManager.GetActiveScene(), mainMenu.gameObject.scene);

            FindButton("Button_MainMenu_OpenZeroing").onClick.Invoke();
            yield return null;

            Assert.That(FindById("Screen_ZeroingBriefing").activeSelf, Is.True, "Clicking 100m should show the zeroing briefing screen.");

            FindButton("Button_ZeroingBriefing_Start").onClick.Invoke();
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "ZeroingRangeScene");
            yield return null;

            var zeroingRangeUi = Object.FindObjectOfType<ZeroingRangeUI>(true);
            Assert.That(zeroingRangeUi, Is.Not.Null, "ZeroingRangeScene should contain a scene-owned ZeroingRangeUI.");
            Assert.AreEqual(SceneManager.GetActiveScene(), zeroingRangeUi.gameObject.scene);
            Assert.That(FindActiveSceneComponents<MainMenuUI>(), Is.Empty);
            Assert.That(FindById("Screen_ZeroingHud").activeSelf, Is.True, "Starting the briefing should show the zeroing HUD in the range scene.");
        }

        [UnityTest]
        public IEnumerator Screen02_MainScene_SwitchesBetweenDesktopAndVrUiModes()
        {
            var mainMenu = Object.FindObjectOfType<MainMenuUI>(true);
            var canvas = mainMenu.GetComponent<Canvas>();
            var adapter = mainMenu.GetComponent<TrainingUICanvasAdapter>();
            var eventSystem = Object.FindObjectsOfType<UnityEngine.EventSystems.EventSystem>(true)
                .Single(candidate => candidate.gameObject.activeInHierarchy);

            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(adapter.DesktopRaycaster.enabled, Is.True);
            Assert.That(adapter.TrackedRaycaster.enabled, Is.False);
            Assert.That(PlayerFollowCamera.Instance, Is.Not.Null);
            Assert.That(PlayerFollowCamera.Instance.OutputEnabled, Is.True);
            AssertSinglePlayerView();

            xrModeController.SetVrModeForTests(true);
            yield return null;

            Assert.That(xrModeController.IsVrMode, Is.True);
            Assert.That(xrModeController.VrCamera, Is.Not.Null);
            Assert.That(xrModeController.VrCamera.isActiveAndEnabled, Is.True);
            Assert.That(PlayerFollowCamera.Instance.OutputEnabled, Is.False);
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.WorldSpace));
            Assert.That(canvas.worldCamera, Is.SameAs(xrModeController.VrCamera));
            Assert.That(adapter.DesktopRaycaster.enabled, Is.False);
            Assert.That(adapter.TrackedRaycaster.enabled, Is.True);
            Assert.That(eventSystem.GetComponent<XRUIInputModule>(), Is.Not.Null);

            var uiInteractors = Object.FindObjectsOfType<NearFarInteractor>(true)
                .Where(interactor => interactor.gameObject.scene == SceneManager.GetActiveScene())
                .ToArray();
            Assert.That(uiInteractors.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(uiInteractors.All(interactor => interactor.enableUIInteraction), Is.True);
            AssertSinglePlayerView();
        }

        static void AssertSinglePlayerView()
        {
            var activeCameras = Object.FindObjectsOfType<Camera>(true)
                .Where(camera => camera.isActiveAndEnabled)
                .ToArray();
            var activeListeners = Object.FindObjectsOfType<AudioListener>(true)
                .Where(listener => listener.isActiveAndEnabled)
                .ToArray();

            Assert.That(activeCameras, Has.Length.EqualTo(1), "MainScene must keep exactly one active player camera.");
            Assert.That(activeListeners, Has.Length.EqualTo(1), "MainScene must keep exactly one active AudioListener.");
        }

        static Button FindButton(string id)
        {
            var go = FindById(id);
            Assert.That(go, Is.Not.Null, id);
            var button = go.GetComponent<Button>();
            Assert.That(button, Is.Not.Null, id);
            return button;
        }

        static GameObject FindById(string id)
        {
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

        static T[] FindActiveSceneComponents<T>() where T : Component
        {
            return SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
        }
    }
}
