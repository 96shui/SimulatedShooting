using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using VRShooting.Unity;
using VRShooting.Unity.Bootstrap;
using VRShooting.Unity.UI;

namespace VRShooting.Tests.PlayMode.UI
{
    [TestFixture]
    public sealed class Screen02_05_SceneOwnedUIFlowTests
    {
        [UnitySetUp]
        public IEnumerator LoadMainScene()
        {
            yield return SceneManager.LoadSceneAsync("MainScene", LoadSceneMode.Single);
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
