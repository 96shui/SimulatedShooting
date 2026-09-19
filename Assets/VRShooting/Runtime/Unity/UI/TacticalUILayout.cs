using System.Collections.Generic;
using UnityEngine;

namespace VRShooting.Unity.UI
{
    /// <summary>Named presentation bounds in the shared 1920x1080 canvas; keeps command bindings intact.</summary>
    public static class TacticalUILayout
    {
        public static void Apply(Transform root)
        {
            var rects = new Dictionary<string, RectTransform>();
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true)) rects[rect.name] = rect;
            void Place(string id, float x, float y, float w, float h)
            {
                if (!rects.TryGetValue(id, out var rect)) return;
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.offsetMin = new Vector2(x, y); rect.offsetMax = new Vector2(x + w, y + h);
            }
            Place("Panel_ZeroingBriefing_Frame", 100, 90, 1720, 910);
            Place("Text_ZeroingBriefing_Header", 170, 920, 1200, 45);
            Place("Text_ZeroingBriefing_Title", 170, 820, 1200, 80);
            Place("Panel_ZeroingBriefing_Rules", 190, 310, 550, 440);
            Place("Panel_ZeroingBriefing_Target", 840, 310, 485, 440);
            Place("Text_ZeroingBriefing_TargetTitle", 870, 695, 425, 45);
            Place("Text_ZeroingBriefing_TargetSize", 880, 325, 405, 40);
            Place("Placeholder_ZeroingBriefing_Range", 1450, 440, 280, 280);
            Place("Button_ZeroingBriefing_Back", 570, 125, 310, 75);
            Place("Button_ZeroingBriefing_Start", 970, 125, 350, 75);
            Place("Panel_ZeroingBriefing_SafetyNote", 160, 230, 720, 50);
            Place("Panel_ZeroingBriefing_Status", 960, 230, 780, 50);
            // Shared presentation state is outside the briefing body, never on top of static footnotes.
            if (rects.ContainsKey("Screen_ZeroingBriefing"))
            {
                Place("Text_TrainingShared_PickupPrompt", 480, 46, 960, 38);
                Place("Text_TrainingShared_FiringStationState", 580, 9, 760, 32);
            }
            Place("Panel_MovingTargetSetup_Content", 290, 110, 1340, 850);
            Place("Text_MovingTargetSetup_Title", 420, 815, 1080, 85);
            Place("Text_MovingTargetSetup_Rules", 400, 735, 1120, 55);
            Place("Text_MovingTargetSetup_SelectedSpeed", 630, 345, 660, 60);
            Place("Text_MovingTargetSetup_Status", 500, 280, 920, 60);
            Place("Text_MovingTargetSetup_Error", 500, 230, 920, 40);
            Place("Button_MovingTargetSetup_Back", 525, 145, 280, 75);
            Place("Button_MovingTargetSetup_Start", 1115, 145, 280, 75);
            Place("Text_MovingTargetResults_Title", 500, 845, 920, 75);
            Place("Text_MovingTargetResults_Summary", 350, 640, 1220, 180);
            Place("Viewport_MovingTargetResults_Sequences", 350, 250, 1220, 340);
            Place("Text_MovingTargetResults_ScrollHint", 350, 597, 1220, 35);
            Place("Button_MovingTargetResults_BackToModeSelection", 485, 150, 350, 75);
            Place("Button_MovingTargetResults_Retry", 1085, 150, 350, 75);
            foreach (var mode in new[] { "Trench", "Urban" })
            {
                Place("Text_" + mode + "Results_Outcome", 50, 610, 750, 90);
                Place("Text_" + mode + "Results_Summary", 50, 510, 750, 95);
                Place("Text_" + mode + "Results_Stats", 65, 65, 720, 430);
            }
            Place("Text_TrenchBriefing_Map", 50, 585, 750, 65);
            Place("Text_TrenchBriefing_EnemyEstimate", 50, 515, 750, 60);
            Place("Text_TrenchBriefing_Squad", 50, 290, 750, 205);
            Place("Text_TrenchBriefing_Objectives", 50, 55, 750, 215);
            // Keep the upper and central aiming corridor clear in active combat.
            Place("Text_TrenchHud_Title", 610, 990, 700, 44);
            Place("Hud_Trench_Squad", 650, 35, 620, 145);
        }
    }
}
