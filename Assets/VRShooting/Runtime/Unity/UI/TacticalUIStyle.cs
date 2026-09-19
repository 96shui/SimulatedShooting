using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VRShooting.Unity.UI
{
    /// <summary>Presentation only: never changes service state, commands or DTO values.</summary>
    public static class TacticalUIStyle
    {
        public static Sprite Sprite(string name) => Resources.Load<Sprite>("UI/TrainingGenerated/Sprites/" + name);

        public static void Apply(Transform root)
        {
            TacticalUILayout.Apply(root);
            var theme = TacticalUITheme.Current;
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (theme != null && theme.Font != null)
                {
                    text.font = theme.Font;
                    text.fontSharedMaterial = theme.Font.material;
                }
                text.fontStyle &= ~FontStyles.Bold;
                text.extraPadding = false;
                text.raycastTarget = false;
                if (text.name == "Text_TrenchHud_Title") text.fontSize = 28;
                if (text.name.Contains("TargetCenter") || text.name == "Text_ZeroingHud_Prompt") text.color = new Color32(218, 241, 249, 255);
                var parentImage = text.transform.parent != null ? text.transform.parent.GetComponent<Image>() : null;
                if (parentImage != null && parentImage.name.StartsWith("Panel_") && parentImage.rectTransform.rect.height <= 140)
                {
                    text.rectTransform.offsetMin = new Vector2(30, 4);
                    text.rectTransform.offsetMax = new Vector2(-30, -4);
                }
            }
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                var id = image.name;
                if ((id.StartsWith("Panel_") || (id.StartsWith("Hud_") && id.EndsWith("_Image"))) && image.GetComponent<Button>() == null && image.color.a > 0)
                {
                    image.sprite = Sprite(image.rectTransform.rect.height < 140 ? "hud_capsule" : "panel_holo_large");
                    image.type = Image.Type.Sliced;
                    image.color = new Color(.65f, .8f, .9f, 1);
                    if (id.EndsWith("_Frame_Image"))
                    {
                        image.sprite = null;
                        image.color = new Color32(7, 17, 28, 245);
                    }
                    var ownerButton = image.transform.parent != null ? image.transform.parent.GetComponent<Button>() : null;
                    image.raycastTarget = ownerButton != null && ownerButton.targetGraphic == image;
                    var outline = image.GetComponent<Outline>();
                    if (outline != null) outline.enabled = false;
                }
                if (id.Contains("TargetRing_") || id.Contains("TargetRing") || id.Contains("Ring_"))
                {
                    image.enabled = false;
                    var ring = image.GetComponentInChildren<TacticalRingGraphic>(true);
                    if (ring == null)
                    {
                        var ringObject = new GameObject("RingGeometry", typeof(RectTransform), typeof(TacticalRingGraphic));
                        var rect = (RectTransform)ringObject.transform;
                        rect.SetParent(image.transform, false);
                        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                        rect.offsetMin = rect.offsetMax = Vector2.zero;
                        ring = ringObject.GetComponent<TacticalRingGraphic>();
                    }
                    ring.color = new Color32(163, 224, 239, 255);
                    ring.raycastTarget = false;
                    var outline = image.GetComponent<Outline>();
                    if (outline != null) outline.enabled = false;
                }
            }
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (!button.name.StartsWith("Button_")) continue;
                var image = button.targetGraphic as Image;
                if (image == null) continue;
                var primary = button.name.Contains("Start") || button.name.Contains("Confirm") || button.name.Contains("SelectMap") || button.name.Contains("Continue") || button.name.Contains("OpenZeroing") || button.name.Contains("Retry");
                StyleButton(button, primary);
            }
        }

        public static void StyleButton(Button button, bool primary)
        {
            var image = button.targetGraphic as Image;
            if (image == null) return;
            var prefix = primary ? "button_primary_orange" : "button_secondary_cyan";
            image.sprite = Sprite(prefix);
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = true;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { highlightedSprite = Sprite(prefix + "_highlighted"), selectedSprite = Sprite(prefix + "_highlighted"), pressedSprite = Sprite(prefix + "_pressed") };
            if (button.GetComponent<TacticalButtonState>() == null) button.gameObject.AddComponent<TacticalButtonState>();
        }

        public static Image Artwork(Transform parent, string name, Sprite sprite, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.offsetMin = min;
            rect.offsetMax = max;
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }
    }
}
