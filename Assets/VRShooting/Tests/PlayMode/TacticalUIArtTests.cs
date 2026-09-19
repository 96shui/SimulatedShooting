using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRShooting.Unity.UI;
using VRShooting.Common;
using System.Linq;

namespace VRShooting.Tests.PlayMode
{
    // BDD 01/02/04/11/17: build-safe art, non-blocking decoration and legible map selection.
    public sealed class TacticalUIArtTests
    {
        [Test]
        public void ThemeShipsWithPaddedChineseFontAndAllMapArtwork()
        {
            var theme = TacticalUITheme.Current;
            Assert.That(theme, Is.Not.Null);
            Assert.That(theme.Font, Is.Not.Null);
            Assert.That(theme.Font.sourceFontFile, Is.Not.Null, "Dynamic Chinese glyphs must also be available in player builds.");
            Assert.That(theme.Font.atlasPadding, Is.GreaterThanOrEqualTo(5));
            Assert.That(theme.Hall, Is.Not.Null);
            Assert.That(theme.Trench, Is.Not.Null);
            Assert.That(theme.Urban, Is.Not.Null);
            Assert.That(theme.Floors, Has.Length.EqualTo(3));
            foreach (var floor in theme.Floors) Assert.That(floor, Is.Not.Null);
        }

        [Test]
        public void StylingKeepsTextNonInteractiveAndUsesSharedFont()
        {
            var root = new GameObject("ArtTest", typeof(RectTransform));
            try
            {
                var text = new GameObject("Text_Test", typeof(RectTransform), typeof(TextMeshProUGUI));
                text.transform.SetParent(root.transform, false);
                TacticalUIStyle.Apply(root.transform);
                Assert.That(text.GetComponent<TMP_Text>().font, Is.SameAs(TacticalUITheme.Current.Font));
                Assert.That(text.GetComponent<TMP_Text>().raycastTarget, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void Screen15_TrenchBarsAndMapSymbolsUseOnlyDtoValues()
        {
            var go = new GameObject("ArtTestCombat", typeof(RectTransform));
            go.SetActive(false);
            try
            {
                var root = go.AddComponent<P3CombatUIRoot>();
                root.BuildIfNeeded();
                var map = new MiniMapDto {MapId="trench-a", Visible=true, Markers=new[] {
                    new MapMarkerDto {MarkerId="player",Type=MarkerType.Player,NormalizedPosition=new Vector2(.2f,.3f)},
                    new MapMarkerDto {MarkerId="estimate",Type=MarkerType.EnemyEstimate,NormalizedPosition=new Vector2(.7f,.6f)}
                }};
                root.TrenchHudView.Apply(new TrenchSessionDto {SessionId="art-test",Player=new PlayerStatusDto {Health=32,IsAlive=true},SearchProgress01=.65f,MiniMap=map},default);
                var health = root.TrenchHudView.GetComponentsInChildren<RectTransform>(true).First(t=>t.name=="Hud_Combat_HealthFill");
                var search = root.TrenchHudView.GetComponentsInChildren<RectTransform>(true).First(t=>t.name=="Hud_Trench_SearchFill");
                Assert.That(health.anchorMax.x, Is.EqualTo(.32f).Within(.001f));
                Assert.That(search.anchorMax.x, Is.EqualTo(.65f).Within(.001f));
                var symbols = root.TrenchHudView.MiniMap.GetComponentsInChildren<TacticalMapMarkerGraphic>(true);
                Assert.That(symbols.Length, Is.EqualTo(2));
                Assert.That(symbols.All(s=>!s.raycastTarget), Is.True);
                var image = root.TrenchHudView.MiniMap.GetComponentsInChildren<Image>(true).First(i=>i.name.EndsWith("_Background"));
                Assert.That(image.sprite, Is.SameAs(TacticalUITheme.Current.Trench));
                Assert.That(image.color, Is.EqualTo(Color.white));
                var frame = root.TrenchHudView.GetComponentsInChildren<Image>(true).First(i=>i.name=="Panel_TrenchHud_Frame_Image");
                Assert.That(frame.color.a, Is.Zero, "A combat frame must not obscure the aiming corridor.");
                var card = root.TrenchMapView.GetComponentsInChildren<P3MapCardView>(true).First();
                Assert.That(card.Button.targetGraphic.raycastTarget, Is.True, "Decorative styling must not disable map selection raycasts.");
                Assert.That(card.GetComponent<Image>().fillCenter, Is.False, "Selection borders must not cover the preview.");
            }
            finally {Object.DestroyImmediate(go);}
        }

        [Test]
        public void SelectedButtonKeepsItsPaletteAcrossPointerStates()
        {
            var go = new GameObject("Button_Test", typeof(RectTransform), typeof(Image), typeof(Button));
            try
            {
                var button = go.GetComponent<Button>();
                button.targetGraphic = go.GetComponent<Image>();
                foreach (var selected in new[] { true, false })
                {
                    TacticalUIStyle.StyleButton(button, selected);
                    var prefix = selected ? "button_primary_orange" : "button_secondary_cyan";
                    Assert.That(button.image.sprite, Is.SameAs(TacticalUIStyle.Sprite(prefix)));
                    Assert.That(button.spriteState.highlightedSprite, Is.SameAs(TacticalUIStyle.Sprite(prefix + "_highlighted")));
                    Assert.That(button.spriteState.pressedSprite, Is.SameAs(TacticalUIStyle.Sprite(prefix + "_pressed")));
                }
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Screen15_HealthWarningResetsAndSquadBackingLeavesAimClear()
        {
            var go = new GameObject("HudArtTest", typeof(RectTransform));
            go.SetActive(false);
            try
            {
                var root = go.AddComponent<P3CombatUIRoot>();
                root.BuildIfNeeded();
                var view = root.TrenchHudView;
                var health = view.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "Hud_Trench_Health");
                view.ApplyHud(new HudDto { Player = new PlayerStatusDto { Health = 20, IsAlive = true } }, default, default);
                Assert.That(health.color, Is.EqualTo((Color)new Color32(255, 114, 90, 255)));
                view.ApplyHud(new HudDto { Player = new PlayerStatusDto { Health = 100, IsAlive = true } }, default, default);
                Assert.That(health.color, Is.EqualTo((Color)new Color32(218, 241, 249, 255)));
                Assert.That(health.enableAutoSizing, Is.True);
                var ammo = view.GetComponentsInChildren<TMP_Text>(true).First(t => t.name == "Hud_Trench_Ammo");
                view.ApplyHud(new HudDto { Ammo = new AmmoDto { IsReloading = true } }, default, default);
                Assert.That(ammo.color, Is.EqualTo((Color)new Color32(247, 185, 85, 255)));
                view.ApplyHud(new HudDto { Ammo = new AmmoDto { CurrentMagazine = 0 } }, default, default);
                Assert.That(ammo.color, Is.EqualTo((Color)new Color32(255, 114, 90, 255)));
                view.ApplyHud(new HudDto { Ammo = new AmmoDto { CurrentMagazine = 30 } }, default, default);
                Assert.That(ammo.color, Is.EqualTo((Color)new Color32(218, 241, 249, 255)));
                var backing = view.GetComponentsInChildren<Image>(true).First(i => i.name == "Panel_TrenchHud_Squad_Image");
                Assert.That(backing.raycastTarget, Is.False);
                Assert.That(backing.rectTransform.offsetMax.y, Is.LessThanOrEqualTo(200));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Screen07_TargetPlotCopiesImpactsAndHasCanvasRenderer()
        {
            var go = new GameObject("Plot",typeof(RectTransform),typeof(TacticalTargetPlot));
            try
            {
                var plot=go.GetComponent<TacticalTargetPlot>();
                plot.SetImpacts(new[]{Vector2.zero,new Vector2(2,-3)});
                Assert.That(plot.ImpactCount,Is.EqualTo(2));
                Assert.That(plot.canvasRenderer,Is.Not.Null);
                plot.SetImpacts(null);
                Assert.That(plot.ImpactCount,Is.Zero);
            }
            finally {Object.DestroyImmediate(go);}
        }
    }
}
