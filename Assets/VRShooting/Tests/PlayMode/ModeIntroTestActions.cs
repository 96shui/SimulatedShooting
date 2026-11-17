using System;
using NUnit.Framework;
using UnityEngine.UI;

namespace VRShooting.Tests.PlayMode
{
    // BDD02/37: all flow gates cross the same introduction instead of duplicating click-throughs.
    public static class ModeIntroTestActions
    {
        public static void EnterTraining(Func<string, Button> findButton, string entry = "OpenZeroing")
        {
            findButton("Button_MainMenu_" + entry).onClick.Invoke();
            ConfirmIntroduction(findButton, entry);
        }

        public static void ConfirmIntroduction(Func<string, Button> findButton, string entry = "OpenZeroing",
            Action<Button> click = null)
        {
            var enter = FindEnterTrainingButton(findButton, entry);
            if (click != null) click(enter);
            else enter.onClick.Invoke();
        }

        public static Button FindEnterTrainingButton(Func<string, Button> findButton, string entry = "OpenZeroing")
        {
            var mode = entry == "OpenZeroing" ? "Zeroing" : entry == "OpenMovingTarget" ? "MovingTarget" : entry;
            var enter = findButton("Button_ModeIntro_" + mode + "_Enter");
            Assert.That(enter, Is.Not.Null);
            Assert.That(enter.gameObject.activeInHierarchy, Is.True, "The introduction must be displayed before entering training.");
            return enter;
        }
    }
}
