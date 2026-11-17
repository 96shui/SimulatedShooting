"""Reuse the previous gallery camera and production UI, capturing exactly five PNGs."""
from pathlib import Path

out = Path(__file__).resolve().parent
source = (out.parent/'mode-intro-pages-2026-11-16/ModeIntroEvidence.cs.txt').read_text(encoding='utf8')
source = source.replace('public static class ModeIntroEvidence', 'public static class ModeIntroPanelFrameEvidence')
start = source.index('            camera.orthographicSize = 325;')
end = source.index('            var inspection = new List<string>();', start)
source = source[:start] + '            var buttons = mainPage.GetComponentsInChildren<Button>(true);\n' + source[end:]
needle = '                var scroll = intro.GetComponentInChildren<ScrollRect>();'
source = source.replace(needle, needle + '''
                var panel = intro.Find("Panel_ModeIntro_" + mode);
                var panelRect = (RectTransform)panel;
                var panelImage = panel.GetComponent<Image>();
                inspection.Add(mode + " panelMin=" + panelRect.offsetMin + " panelMax=" + panelRect.offsetMax +
                    " panelSize=" + panelRect.rect.size + " sprite=" + panelImage.sprite.name +
                    " color=" + panelImage.color + " imageType=" + panelImage.type +
                    " outline=" + (panel.GetComponent<Outline>() != null));
                foreach (var action in new[] { "Back", "Enter" })
                {
                    var button = buttons.Single(t => t.name == "Button_ModeIntro_" + mode + "_" + action);
                    inspection.Add(button.name + " sprite=" + button.image.sprite.name +
                        " highlighted=" + button.spriteState.highlightedSprite.name);
                }''')
(out/'ModeIntroPanelFrameEvidence.cs.txt').write_text(source, encoding='utf8')
print('Prepared five-frame capture helper; no Unity asset added yet.')
