using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRShooting.Application;
using VRShooting.Unity;
using VRShooting.Unity.UI;

namespace VRShooting.Tests.EditMode.Application
{
    // BDD02 and task008: generated menu controls use the same UI and raycast path.
    public class MainMenuControlsTests
    {
        GameObject root;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("ControlsTest", typeof(RectTransform));
            root.AddComponent<MainMenuUI>().Initialize(ApplicationServices.CreateDefault());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Bdd02_ControlsOpenCloseAndDescribeActualVrOperations()
        {
            var panel = Find("Panel_MainMenu_Controls");
            Assert.That(panel.activeSelf, Is.False);
            Assert.That(Find("Button_MainMenu_Controls").GetComponent<Button>(), Is.Not.Null);
            Find("Button_MainMenu_Controls").GetComponent<Button>().onClick.Invoke();
            Assert.That(panel.activeSelf, Is.True);
            var body = panel.GetComponentsInChildren<TMP_Text>(true);
            var text = string.Join("\n", body.Select(label => label.text));
            foreach (var word in new[] { "抓枪", "射击", "手雷", "换弹", "姿态", "切换左右肩", "模式一", "模式二", "模式三" })
                Assert.That(text, Does.Contain(word), word);
            Assert.That(text, Does.Contain("模式三").And.Contain("固定射击点"));
            foreach (var label in body) Assert.That(label.fontSize, Is.GreaterThanOrEqualTo(20f), label.name);
            Canvas.ForceUpdateCanvases();
            foreach (var label in body.Where(label => label.name == "Text_MainMenu_Controls_Left" || label.name == "Text_MainMenu_Controls_Right"))
            {
                label.ForceMeshUpdate();
                Assert.That(label.isTextOverflowing, Is.False, label.name + " must fit on a single screen");
            }
            Find("Button_MainMenu_Controls_Back").GetComponent<Button>().onClick.Invoke();
            Assert.That(panel.activeSelf, Is.False);
        }

        [TestCase("Button_MainMenu_OpenZeroing", 665f, 750f)]
        [TestCase("Button_MainMenu_OpenMovingTarget", 535f, 620f)]
        [TestCase("Button_MainMenu_Trench", 405f, 490f)]
        [TestCase("Button_MainMenu_Urban", 275f, 360f)]
        public void Bdd02_TrainingButtonGeometryIsUnchanged(string id, float bottom, float top)
        {
            var rect = Find(id).GetComponent<RectTransform>();
            Assert.That(rect.offsetMin, Is.EqualTo(new Vector2(1360f, bottom)));
            Assert.That(rect.offsetMax, Is.EqualTo(new Vector2(1750f, top)));
        }

        GameObject Find(string id)
        {
            var found = root.GetComponentsInChildren<UITestId>(true).FirstOrDefault(candidate => candidate.Id == id);
            Assert.That(found, Is.Not.Null, id);
            return found.gameObject;
        }
    }
}
