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

        // BDD02 2026-11-15: preserve dimensions at the revised menu positions.
        [TestCase("Button_MainMenu_OpenZeroing", 539f, 624f)]
        [TestCase("Button_MainMenu_OpenMovingTarget", 411f, 496f)]
        [TestCase("Button_MainMenu_Trench", 283f, 368f)]
        [TestCase("Button_MainMenu_Urban", 155f, 240f)]
        public void Bdd02_TrainingButtonGeometryIsUnchanged(string id, float bottom, float top)
        {
            var rect = Find(id).GetComponent<RectTransform>();
            Assert.That(rect.offsetMin, Is.EqualTo(new Vector2(1360f, bottom)));
            Assert.That(rect.offsetMax, Is.EqualTo(new Vector2(1750f, top)));
        }

        [Test]
        public void Bdd02_ControlsAboveHeadingAndAllMenuRowsStayInsideFrame()
        {
            var controls = Find("Button_MainMenu_Controls").GetComponent<RectTransform>();
            var heading = Find("Panel_MainMenu_Menu").GetComponent<RectTransform>();
            Assert.That(controls.offsetMin, Is.EqualTo(new Vector2(1360, 780)));
            Assert.That(controls.offsetMax, Is.EqualTo(new Vector2(1750, 865)));
            Assert.That(heading.offsetMin, Is.EqualTo(new Vector2(1360, 667)));
            Assert.That(heading.offsetMax, Is.EqualTo(new Vector2(1750, 737)));
            Assert.That(controls.offsetMax.y, Is.GreaterThan(heading.offsetMax.y));

            var rows = new[] { controls, heading }.Concat(new[] { "Button_MainMenu_OpenZeroing",
                "Button_MainMenu_OpenMovingTarget", "Button_MainMenu_Trench", "Button_MainMenu_Urban" }
                .Select(id => Find(id).GetComponent<RectTransform>())).ToArray();
            var frame = Find("Panel_MainMenu_Frame").GetComponent<RectTransform>();
            var status = Find("Panel_MainMenu_BottomStatus").GetComponent<RectTransform>();
            for (var i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                Assert.That(row.offsetMin.x, Is.GreaterThanOrEqualTo(frame.offsetMin.x), row.name);
                Assert.That(row.offsetMax.x, Is.LessThanOrEqualTo(frame.offsetMax.x), row.name);
                Assert.That(row.offsetMin.y, Is.GreaterThanOrEqualTo(frame.offsetMin.y), row.name);
                Assert.That(row.offsetMax.y, Is.LessThanOrEqualTo(frame.offsetMax.y), row.name);
                Assert.That(row.offsetMin.y, Is.GreaterThan(status.offsetMax.y), row.name);
                if (i > 0)
                    Assert.That(rows[i - 1].offsetMin.y - row.offsetMax.y, Is.EqualTo(43f), row.name);
            }
        }

        [Test]
        // Keep the historical method name/fullname for exact baseline comparison (BDD02).
        public void Bdd02_HeadingUsesPrimaryVisualWithoutInteractionAndEntriesUseSecondaryVisual()
        {
            var heading = Find("Panel_MainMenu_Menu");
            var image = heading.GetComponent<Image>();
            Assert.That(image.sprite, Is.Not.Null);
            Assert.That(image.sprite.name, Is.EqualTo("hud_capsule"));
            Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That((Color32)image.color, Is.EqualTo(new Color32(17, 29, 24, 230)), "Dark panel fill.");
            Assert.That(heading.transform.Find("Image_MainMenu_MenuFill"), Is.Null, "No extra green fill layer.");
            var outline = heading.GetComponent<Outline>();
            Assert.That(outline, Is.Not.Null);
            Assert.That(outline.enabled, Is.True);
            Assert.That((Color32)outline.effectColor, Is.EqualTo(new Color32(247, 185, 85, 255)));
            Assert.That(outline.effectDistance, Is.EqualTo(new Vector2(2f, -2f)));
            var border = heading.GetComponentInChildren<MainMenuHeadingBorderGraphic>(true);
            Assert.That(border, Is.Not.Null, "Render the orange stroke without multiplying cyan sprite pixels.");
            Assert.That(border.GetComponent<CanvasRenderer>(), Is.Not.Null);
            Assert.That((Color32)border.color, Is.EqualTo(new Color32(247, 185, 85, 255)));
            Assert.That(heading.GetComponentsInChildren<Selectable>(true), Is.Empty);
            Assert.That(heading.GetComponentsInChildren<Graphic>(true).All(graphic => !graphic.raycastTarget), Is.True);
            var label = Find("Text_MainMenu_Menu").GetComponent<TMP_Text>();
            Assert.That(label.text, Is.EqualTo("选择训练科目"));
            Assert.That(label.fontSize, Is.EqualTo(20f));
            Assert.That(label.fontStyle, Is.EqualTo(FontStyles.Normal));
            Assert.That(label.fontWeight, Is.EqualTo(FontWeight.Regular));
            Assert.That((Color32)label.color, Is.EqualTo(new Color32(231, 242, 235, 255)));

            var secondary = Find("Button_MainMenu_OpenMovingTarget").GetComponent<Image>();
            Assert.That(secondary.sprite.name, Is.EqualTo("button_secondary_cyan"));
            foreach (var id in new[] { "Button_MainMenu_OpenZeroing", "Button_MainMenu_Trench",
                "Button_MainMenu_Urban", "Button_MainMenu_Controls" })
            {
                var entry = Find(id);
                var entryImage = entry.GetComponent<Image>();
                Assert.That(entryImage.sprite, Is.SameAs(secondary.sprite), id);
                Assert.That(entryImage.color, Is.EqualTo(secondary.color), id);
                Assert.That(entry.GetComponent<Button>().spriteState.highlightedSprite,
                    Is.SameAs(secondary.GetComponent<Button>().spriteState.highlightedSprite), id);
                Assert.That(entry.GetComponentInChildren<TMP_Text>().color,
                    Is.EqualTo(secondary.GetComponentInChildren<TMP_Text>().color), id);
            }
        }

        GameObject Find(string id)
        {
            var found = root.GetComponentsInChildren<UITestId>(true).FirstOrDefault(candidate => candidate.Id == id);
            Assert.That(found, Is.Not.Null, id);
            return found.gameObject;
        }
    }
}
