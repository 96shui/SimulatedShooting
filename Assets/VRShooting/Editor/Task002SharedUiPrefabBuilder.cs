using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRShooting.Common;
using VRShooting.Unity;
using VRShooting.Unity.UI;

namespace VRShooting.Editor
{
    public static class Task002SharedUiPrefabBuilder
    {
        public const string PrefabPath = "Assets/VRShooting/Prefabs/UI/Training_SharedWorldSpaceUI.prefab";

        [MenuItem("VRShooting/P2/Build Task002 Shared World Space UI")]
        public static void Build()
        {
            EnsureFolder("Assets/VRShooting/Prefabs");
            EnsureFolder("Assets/VRShooting/Prefabs/UI");

            var root = new GameObject("Training_SharedWorldSpaceUI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(UITestId),
                typeof(TrainingPresentationView), typeof(TrainingPresentationPresenter), typeof(TrainingUIAnchorBinder));
            try
            {
                var rootRect = root.GetComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(1920f, 1080f);
                rootRect.localScale = Vector3.one * 0.001f;
                root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                root.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
                SetTestId(root, "Training.Shared.WorldSpaceUI");

                var largeRoot = CreateRect("LargePanelRoot", rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                largeRoot.gameObject.AddComponent<CanvasGroup>();
                SetTestId(largeRoot.gameObject, "Training.Shared.LargePanelRoot");

                var minimalRoot = CreateRect("MinimalHudRoot", rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var minimalGroup = minimalRoot.gameObject.AddComponent<CanvasGroup>();
                minimalGroup.interactable = false;
                minimalGroup.blocksRaycasts = false;
                SetTestId(minimalRoot.gameObject, "Training.Shared.MinimalHudRoot");

                var briefing = CreatePanel("Screen_ZeroingBriefing", largeRoot, new Color32(8, 22, 28, 235));
                var pickupPrompt = CreateText("Text_TrainingShared_PickupPrompt", briefing.transform,
                    "请用右手近距取枪", new Vector2(440f, 430f), new Vector2(1480f, 560f), 42f);
                SetTestId(pickupPrompt.gameObject, "Training.Shared.PickupPrompt");
                var station = CreateText("Text_TrainingShared_FiringStationState", briefing.transform,
                    "射击位：等待绑定", new Vector2(540f, 580f), new Vector2(1380f, 650f), 24f);
                SetTestId(station.gameObject, "Training.Shared.FiringStationState");
                var start = CreateButton("Button_ZeroingBriefing_Start", briefing.transform,
                    new Vector2(805f, 720f), new Vector2(1115f, 800f), "确认并开始");

                var analysis = CreatePanel("Screen_ZeroingImpactAnalysis", largeRoot, new Color32(8, 22, 28, 235));
                CreateText("Text_ZeroingImpactAnalysis_Title", analysis.transform, "本轮弹着分析",
                    new Vector2(560f, 190f), new Vector2(1360f, 280f), 46f);
                var next = CreateButton("Button_ZeroingImpactAnalysis_NextRound", analysis.transform,
                    new Vector2(805f, 760f), new Vector2(1115f, 840f), "进入下一轮");

                var results = CreatePanel("Screen_ZeroingFinalRating", largeRoot, new Color32(8, 22, 28, 235));
                CreateText("Text_ZeroingFinalRating_Title", results.transform, "100m射校 训练结算",
                    new Vector2(510f, 190f), new Vector2(1410f, 280f), 46f);
                var retry = CreateButton("Button_ZeroingFinalRating_Retry", results.transform,
                    new Vector2(805f, 760f), new Vector2(1115f, 840f), "重新训练");

                var hud = CreateRect("Screen_ZeroingHud", minimalRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                SetTestId(hud.gameObject, "Screen_ZeroingHud");
                CreateHudText("Hud_Zeroing_Round", hud, "轮次 --/--", new Vector2(60f, 60f), new Vector2(390f, 145f));
                CreateHudText("Hud_Zeroing_Ammo", hud, "弹数 --/--", new Vector2(1530f, 60f), new Vector2(1860f, 145f));
                CreateHudText("Hud_Zeroing_Stability", hud, "稳定度 --", new Vector2(60f, 880f), new Vector2(430f, 970f));

                briefing.SetActive(false);
                analysis.SetActive(false);
                results.SetActive(false);
                hud.gameObject.SetActive(false);
                largeRoot.gameObject.SetActive(false);
                minimalRoot.gameObject.SetActive(false);

                var view = root.GetComponent<TrainingPresentationView>();
                view.Configure(
                    largeRoot.gameObject,
                    minimalRoot.gameObject,
                    pickupPrompt,
                    station,
                    start,
                    next,
                    retry,
                    new List<TrainingPresentationPanelBinding>
                    {
                        new TrainingPresentationPanelBinding(ScreenId.ZeroingBriefing, briefing),
                        new TrainingPresentationPanelBinding(ScreenId.ZeroingImpactAnalysis, analysis),
                        new TrainingPresentationPanelBinding(ScreenId.ZeroingFinalRating, results),
                        new TrainingPresentationPanelBinding(ScreenId.ZeroingHud, hud.gameObject)
                    });
                root.GetComponent<TrainingUIAnchorBinder>().Configure(largeRoot, minimalRoot);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Task002] Built " + PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        static GameObject CreatePanel(string name, RectTransform parent, Color color)
        {
            var panel = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var image = panel.gameObject.AddComponent<Image>();
            image.color = color;
            SetTestId(panel.gameObject, name);
            return panel.gameObject;
        }

        static TextMeshProUGUI CreateText(string name, Transform parent, string value, Vector2 min, Vector2 max, float size)
        {
            var rect = CreateRect(name, parent, Vector2.zero, Vector2.zero, min, max);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color32(224, 246, 255, 255);
            text.raycastTarget = false;
            SetTestId(rect.gameObject, name);
            return text;
        }

        static Button CreateButton(string name, Transform parent, Vector2 min, Vector2 max, string label)
        {
            var rect = CreateRect(name, parent, Vector2.zero, Vector2.zero, min, max);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color32(20, 118, 155, 245);
            var button = rect.gameObject.AddComponent<Button>();
            CreateText("Text_" + name.Substring("Button_".Length), rect, label, Vector2.zero, Vector2.zero, 28f)
                .rectTransform.anchorMax = Vector2.one;
            SetTestId(rect.gameObject, name);
            return button;
        }

        static void CreateHudText(string name, RectTransform parent, string value, Vector2 min, Vector2 max)
        {
            var rect = CreateRect(name, parent, Vector2.zero, Vector2.zero, min, max);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color32(5, 34, 47, 205);
            var text = CreateText("Text_" + name.Substring("Hud_".Length), rect, value, Vector2.zero, Vector2.zero, 28f);
            text.rectTransform.anchorMax = Vector2.one;
            SetTestId(rect.gameObject, name);
        }

        static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        static void SetTestId(GameObject target, string id)
        {
            var testId = target.GetComponent<UITestId>() ?? target.AddComponent<UITestId>();
            testId.SetId(id);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var separator = path.LastIndexOf('/');
            var parent = path.Substring(0, separator);
            var folder = path.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
