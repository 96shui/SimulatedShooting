using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.P3.TestSupport;
using VRShooting.Unity;
using VRShooting.Unity.UI;

namespace VRShooting.Tests.EditMode.Application
{
    // BDD02/37, P1/UI task008: local introductions do not issue training commands before confirmation.
    public class MainMenuControlsTests
    {
        GameObject root;
        ApplicationServices services;
        static readonly string[] Entries = { "OpenZeroing", "OpenMovingTarget", "Trench", "Urban" };
        static readonly string[] Modes = { "Zeroing", "MovingTarget", "Trench", "Urban" };
        static readonly string[] Titles = { "模式一 · 100m 精度射校", "模式二 · 移动目标射击", "模式三 · 堑壕射击", "模式四 · 城镇攻防" };

        [SetUp]
        public void SetUp()
        {
            services = ApplicationServices.CreateDefault(combatScenes: new CombatApplicationFixture());
            root = new GameObject("ControlsTest", typeof(RectTransform));
            root.AddComponent<MainMenuUI>().Initialize(services);
        }

        [TearDown]
        public void TearDown()
        {
            services.Combat.Dispose();
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Bdd02_ControlsOpenCloseAndDescribeActualVrOperations()
        {
            foreach (var id in new[] { "Button_MainMenu_Controls", "Panel_MainMenu_Controls", "Text_MainMenu_Controls_Title",
                "Text_MainMenu_Controls_Left", "Text_MainMenu_Controls_Right", "Button_MainMenu_Controls_Back" })
                Assert.That(root.GetComponentsInChildren<Transform>(true).Any(t => t.name == id), Is.False, id);
            for (var i = 0; i < Modes.Length; i++)
            {
                var panel = Find("Screen_ModeIntro_" + Modes[i]);
                Assert.That(panel.activeSelf, Is.False);
                for (var attempt = 0; attempt < 3; attempt++)
                {
                    Click("Button_MainMenu_" + Entries[i]);
                    Assert.That(panel.activeInHierarchy, Is.True);
                    Assert.That(Find("Text_ModeIntro_" + Modes[i] + "_Title").GetComponent<TMP_Text>().text, Is.EqualTo(Titles[i]));
                    Assert.That(Find("Button_ModeIntro_" + Modes[i] + "_Enter").GetComponent<Button>().interactable, Is.True);
                    var text = Copy(Modes[i]);
                    foreach (var word in new[] { "按键操作", "界面与按钮", "侧握键抓枪", "辅助抓握", "右扳机射击" })
                        Assert.That(text, Does.Contain(word), Modes[i] + word);
                    Assert.That(text.Split('\n').Length, Is.InRange(10, 14), "Explicit copy lines including both headings");
                    Assert.That(text.Replace("cm", "").Replace("100m", ""), Does.Not.Match("[a-zA-Z]{2,}"), "Only key letters and units are retained after excluding units below");
                    Assert.That(services.Router.Current, Is.EqualTo(ScreenId.MainMenu));
                    Assert.That(services.TrainingSessions.HasActiveSession, Is.False);
                    Assert.That(services.Presentation.Get(string.Empty).Success, Is.False);
                    Assert.That(services.Combat.Snapshot.Mode, Is.Null);
                    Click("Button_ModeIntro_" + Modes[i] + "_Back");
                    Assert.That(panel.activeSelf, Is.False);
                    Assert.That(services.Router.Current, Is.EqualTo(ScreenId.MainMenu));
                    Assert.That(services.TrainingSessions.HasActiveSession, Is.False);
                    Assert.That(services.Combat.Snapshot.Mode, Is.Null);
                }
            }
            foreach (var mode in new[] { "Trench", "Urban" })
                foreach (var word in new[] { "X 换弹", "Y 切换姿态", "B 切换左右肩", "A 请求队友投掷手雷", "左摇杆移动", "右摇杆转向", "左扳机射击" })
                    Assert.That(Copy(mode), Does.Contain(word), mode + word);
            foreach (var word in new[] { "顺时针 5°", "逆时针 5°", "水平方向", "向前 1cm", "向后 1cm", "垂直方向", "立即判定", "本轮作废", "蓝点", "禁用", "每轮 4 发", "共 3 轮" })
                Assert.That(Copy("Zeroing"), Does.Contain(word), word);
            foreach (var word in new[] { "起射两发", "逐发连射", "速度档位", "跟随射击", "等待射击", "剩余弹药", "命中数", "已用时间", "重新训练", "返回主菜单" })
                Assert.That(Copy("MovingTarget"), Does.Contain(word), word);
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

        [Test]
        public void Bdd02_ControlsAboveHeadingAndAllMenuRowsStayInsideFrame()
        {
            var heading = Find("Panel_MainMenu_Menu").GetComponent<RectTransform>();
            Assert.That(heading.offsetMin, Is.EqualTo(new Vector2(1360, 805)));
            Assert.That(heading.offsetMax, Is.EqualTo(new Vector2(1750, 865)));
            var rows = new[] { heading }.Concat(Entries.Select(e => Find("Button_MainMenu_" + e).GetComponent<RectTransform>())).ToArray();
            var frame = Find("Panel_MainMenu_Frame").GetComponent<RectTransform>();
            var status = Find("Panel_MainMenu_BottomStatus").GetComponent<RectTransform>();
            for (var i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                Assert.That(row.offsetMin.x, Is.GreaterThanOrEqualTo(frame.offsetMin.x));
                Assert.That(row.offsetMax.x, Is.LessThanOrEqualTo(frame.offsetMax.x));
                Assert.That(row.offsetMin.y, Is.GreaterThanOrEqualTo(frame.offsetMin.y));
                Assert.That(row.offsetMax.y, Is.LessThanOrEqualTo(frame.offsetMax.y));
                Assert.That(row.offsetMin.y, Is.GreaterThan(status.offsetMax.y));
                if (i > 0) Assert.That(rows[i - 1].offsetMin.y - row.offsetMax.y, Is.EqualTo(i == 1 ? 55f : 45f));
            }
        }

        [Test]
        public void Bdd02_HeadingUsesPrimaryVisualWithoutInteractionAndEntriesUseSecondaryVisual()
        {
            var heading = Find("Panel_MainMenu_Menu");
            var image = heading.GetComponent<Image>();
            Assert.That(image.sprite.name, Is.EqualTo("hud_capsule"));
            Assert.That(image.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(image.color, Is.EqualTo(Color.white));
            Assert.That(heading.GetComponent<Outline>(), Is.Null);
            Assert.That(heading.transform.Find("Image_MainMenu_MenuBorder"), Is.Null);
            Assert.That(heading.transform.Find("Image_MainMenu_MenuFill"), Is.Null);
            Assert.That(root.GetComponentsInChildren<Component>(true).Any(c => c != null && c.GetType().Name == "MainMenuHeadingBorderGraphic"), Is.False);
            Assert.That(heading.GetComponentsInChildren<Selectable>(true), Is.Empty);
            Assert.That(heading.GetComponentsInChildren<Graphic>(true).All(g => !g.raycastTarget), Is.True);
            var label = Find("Text_MainMenu_Menu").GetComponent<TMP_Text>();
            Assert.That(label.text, Is.EqualTo("选择训练科目"));
            Assert.That(label.fontSize, Is.EqualTo(20f));
            Assert.That(label.fontStyle, Is.EqualTo(FontStyles.Normal));
            Assert.That(label.fontWeight, Is.EqualTo(FontWeight.Regular));
            Assert.That(label.extraPadding, Is.False);
            Assert.That(label.alignment, Is.EqualTo(TextAlignmentOptions.Left));
            Assert.That(label.rectTransform.offsetMin, Is.EqualTo(new Vector2(30, 4)));
            Assert.That(label.rectTransform.offsetMax, Is.EqualTo(new Vector2(-30, -4)));
            Assert.That((Color32)label.color, Is.EqualTo(new Color32(231, 242, 235, 255)));
            label.ForceMeshUpdate();
            Assert.That(label.textInfo.characterInfo.Take(label.textInfo.characterCount).Count(c => c.isVisible), Is.EqualTo(6),
                "The title's six glyphs must be rendered within the original heading layout.");
            Assert.That(label.isTextOverflowing, Is.False, "The restored 52px text container must not ellipsize the six glyphs");
            AssertHeadingRasterMatchesAcceptedFont(label);
            foreach (var entry in Entries)
            {
                var button = Find("Button_MainMenu_" + entry).GetComponent<Button>();
                Assert.That(button.image.sprite.name, Is.EqualTo("button_secondary_cyan"));
                Assert.That(button.image.color, Is.EqualTo(Color.white));
                Assert.That(button.spriteState.highlightedSprite.name, Is.EqualTo("button_secondary_cyan_highlighted"));
                Assert.That(button.spriteState.selectedSprite, Is.SameAs(button.image.sprite));
                Assert.That(button.GetComponentInChildren<TMP_Text>().color, Is.EqualTo(label.color));
            }
        }

        void AssertHeadingRasterMatchesAcceptedFont(TMP_Text label)
        {
            // BDD02 / P1 UI task008. Same Camera.Render pipeline as the 9a1c012 worktree:
            // 1920x1080, 1 design unit = 1 pixel. Actual raster baseline: 117x20px,
            // left inset 30px, right inset 243px. Both approved Nov 12/15 PNGs agree.
            // Render only the label into its 390x60 heading area; do not count capsule glow.
            var originalScene = root.scene;
            var preview = EditorSceneManager.NewPreviewScene();
            var previous = RenderTexture.active;
            var target = new RenderTexture(390, 60, 24);
            var texture = new Texture2D(390, 60, TextureFormat.RGB24, false);
            Camera camera = null;
            try
            {
                SceneManager.MoveGameObjectToScene(root, preview);
                var cameraRoot = new GameObject("HeadingRasterTestCamera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraRoot, preview);
                camera = cameraRoot.GetComponent<Camera>();
                camera.scene = preview;
                camera.orthographic = true;
                camera.orthographicSize = 30;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 2000;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.transform.position = new Vector3(1555, 835, -1000);
                root.GetComponent<TrainingUICanvasAdapter>().enabled = false;
                root.GetComponent<CanvasScaler>().enabled = false;
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                var rect = (RectTransform)root.transform;
                rect.sizeDelta = new Vector2(1920, 1080);
                rect.pivot = new Vector2(.5f, .5f);
                rect.position = new Vector3(960, 540, 0);
                rect.localScale = Vector3.one;
                foreach (var group in root.GetComponentsInChildren<CanvasGroup>(true)) group.alpha = 1;
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                    if (child.name == "LargePanelRoot" || child.name == "MinimalHudRoot") child.gameObject.SetActive(true);
                foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
                    graphic.enabled = graphic == label;
                Canvas.ForceUpdateCanvases();
                label.ForceMeshUpdate();
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 390, 60), 0, 0);
                texture.Apply();
                var left = 390; var right = -1; var bottom = 60; var top = -1;
                var ink = 0; var bright = 0;
                var pixels = texture.GetPixels32();
                for (var y = 0; y < 60; y++)
                    for (var x = 0; x < 390; x++)
                    {
                        var pixel = pixels[y * 390 + x];
                        if (Mathf.Min(pixel.r, pixel.g, pixel.b) < 80 ||
                            Mathf.Max(pixel.r, pixel.g, pixel.b) - Mathf.Min(pixel.r, pixel.g, pixel.b) > 45) continue;
                        left = Mathf.Min(left, x); right = Mathf.Max(right, x);
                        bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y);
                        ink++;
                        if (.2126f * pixel.r + .7152f * pixel.g + .0722f * pixel.b >= 200) bright++;
                    }
                Assert.That(ink, Is.GreaterThan(0), "Heading must produce visible pixels");
                Assert.That(top - bottom + 1, Is.EqualTo(20).Within(1), "9a1c012 raster height");
                Assert.That(left, Is.EqualTo(30).Within(1), "9a1c012 raster left inset");
                Assert.That(389 - right, Is.EqualTo(243).Within(1), "9a1c012 raster right inset");
                Assert.That(right - left + 1, Is.EqualTo(117).Within(1), "9a1c012 raster width");
                Assert.That((float)bright / ink, Is.EqualTo(484f / 1291f).Within(.015f),
                    "9a1c012 bright ink fraction catches synthetic bold without changing font size");
            }
            finally
            {
                if (camera != null) camera.targetTexture = null;
                RenderTexture.active = previous;
                Object.DestroyImmediate(texture);
                target.Release();
                Object.DestroyImmediate(target);
                SceneManager.MoveGameObjectToScene(root, originalScene);
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        [Test]
        public void Bdd37_NumberColumnHasIdenticalLeftEdgeAndClearsSpriteIcon()
        {
            Canvas.ForceUpdateCanvases();
            float? firstLeft = null;
            foreach (var entry in Entries)
            {
                var number = Find("Text_MainMenu_" + entry + "_Number").GetComponent<TMP_Text>();
                var name = Find("Text_MainMenu_" + entry).GetComponent<TMP_Text>();
                number.ForceMeshUpdate(); name.ForceMeshUpdate();
                var left = number.rectTransform.TransformPoint(number.textInfo.characterInfo[0].bottomLeft).x;
                if (firstLeft.HasValue) Assert.That(left, Is.EqualTo(firstLeft.Value).Within(1f));
                firstLeft = left;
                var icon = Find("Placeholder_MainMenu_" + entry + "_IconBounds").GetComponent<RectTransform>();
                Assert.That(number.rectTransform.offsetMin.x, Is.GreaterThan(icon.offsetMax.x));
                Assert.That(name.rectTransform.offsetMin.x - number.rectTransform.offsetMax.x, Is.EqualTo(6f));
                Assert.That(name.textInfo.lineInfo[0].baseline, Is.EqualTo(number.textInfo.lineInfo[0].baseline).Within(.1f));
            }
        }

        [TestCase("OpenZeroing")]
        [TestCase("OpenMovingTarget")]
        [TestCase("Trench")]
        [TestCase("Urban")]
        public void Bdd37_PointerEnterHighlightsAndExitRestoresSecondary(string entry)
        {
            var button = Find("Button_MainMenu_" + entry).GetComponent<Button>();
            var data = new PointerEventData(EventSystem.current);
            var normal = button.image.sprite;
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerEnterHandler);
            Assert.That(button.image.overrideSprite, Is.SameAs(button.spriteState.highlightedSprite));
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerExitHandler);
            Assert.That(button.image.overrideSprite, Is.SameAs(normal));
            Assert.That(button.navigation.mode, Is.EqualTo(Navigation.Mode.None));
        }

        [TestCase("OpenZeroing", "Zeroing", ScreenId.ZeroingBriefing)]
        [TestCase("OpenMovingTarget", "MovingTarget", ScreenId.MovingTargetSettings)]
        [TestCase("Trench", "Trench", ScreenId.TrenchMapSelection)]
        [TestCase("Urban", "Urban", ScreenId.UrbanMapSelection)]
        public void Bdd37_ConfirmUsesOriginalEntryExactlyOnce(string entry, string mode, ScreenId expected)
        {
            var events = 0;
            services.EventBus.Subscribe<VRShooting.Application.Events.ScreenChangedEvent>(_ => events++);
            Click("Button_MainMenu_" + entry);
            Click("Button_ModeIntro_" + mode + "_Enter");
            Assert.That(services.Router.Current, Is.EqualTo(expected));
            Assert.That(Find("Screen_ModeIntro_" + mode).activeSelf, Is.False);
            Click("Button_ModeIntro_" + mode + "_Enter");
            Assert.That(events, Is.EqualTo(1));
        }

        string Copy(string mode) => string.Join("\n", new[] { "Left", "Right" }.Select(side => Find("Text_ModeIntro_" + mode + "_" + side).GetComponent<TMP_Text>().text));
        void Click(string id) => Find(id).GetComponent<Button>().onClick.Invoke();
        GameObject Find(string id)
        {
            var found = root.GetComponentsInChildren<UITestId>(true).FirstOrDefault(candidate => candidate.Id == id);
            Assert.That(found, Is.Not.Null, id);
            return found.gameObject;
        }
    }
}
