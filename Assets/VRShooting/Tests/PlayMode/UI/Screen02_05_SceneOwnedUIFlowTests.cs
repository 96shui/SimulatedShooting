using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
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

            var legacyPrompt = FindSceneRoot("LegacyMainMenuPrompt_Disabled");
            Assert.That(legacyPrompt, Is.Not.Null, "The obsolete cube-based menu prompt should remain identifiable in MainScene.");
            Assert.That(legacyPrompt.activeSelf, Is.False,
                "The obsolete cube-based menu prompt must stay disabled so it cannot cover the HMD view.");

            var uiInteractors = Object.FindObjectsOfType<NearFarInteractor>(true)
                .Where(interactor => interactor.gameObject.scene == SceneManager.GetActiveScene())
                .ToArray();
            Assert.That(uiInteractors.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(uiInteractors.All(interactor => interactor.enableUIInteraction), Is.True);
            AssertSinglePlayerView();
        }

        [UnityTest]
        public IEnumerator Screen02_MainScene_VrUsesFloorTrackingAndReadableStabilizedMenu()
        {
            var origin = Object.FindObjectOfType<XROrigin>(true);
            Assert.That(origin, Is.Not.Null, "MainScene must contain an XR Origin.");
            Assert.That(origin.RequestedTrackingOriginMode, Is.EqualTo(XROrigin.TrackingOriginMode.Floor));
            Assert.That(origin.CameraYOffset, Is.EqualTo(0f).Within(0.001f));
            Assert.That(origin.CameraFloorOffsetObject, Is.Not.Null);
            Assert.That(origin.CameraFloorOffsetObject.transform.localPosition.y,
                Is.EqualTo(0f).Within(0.001f));

            xrModeController.SetVrModeForTests(true);
            var vrCamera = xrModeController.VrCamera;
            vrCamera.transform.SetPositionAndRotation(
                new Vector3(vrCamera.transform.position.x, 1.65f, vrCamera.transform.position.z),
                Quaternion.identity);
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();

            var mainMenu = Object.FindObjectOfType<MainMenuUI>(true);
            var menuRect = mainMenu.transform as RectTransform;
            var horizontalDistance = Vector3.ProjectOnPlane(
                menuRect.position - vrCamera.transform.position, Vector3.up).magnitude;
            var worldWidth = menuRect.rect.width * Mathf.Abs(menuRect.lossyScale.x);
            var angularWidth = 2f * Mathf.Atan(worldWidth * 0.5f / horizontalDistance) * Mathf.Rad2Deg;

            Assert.That(ResolveCanvasBottomHeight(menuRect), Is.GreaterThanOrEqualTo(0.10f));
            Assert.That(horizontalDistance, Is.InRange(1.2f, 1.5f));
            Assert.That(angularWidth, Is.GreaterThanOrEqualTo(65f));
            Assert.That(menuRect.position.y - vrCamera.transform.position.y, Is.InRange(-0.15f, -0.04f));
        }

        static float ResolveCanvasBottomHeight(RectTransform rectTransform)
        {
            var worldHeight = rectTransform.rect.height * Mathf.Abs(rectTransform.lossyScale.y);
            return rectTransform.position.y - worldHeight * 0.5f;
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

        static GameObject FindSceneRoot(string objectName)
        {
            return SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .SingleOrDefault(root => root.name == objectName);
        }
    }
}
