using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Input;

namespace VRShooting.Tests.PlayMode.Input
{
    /// <summary>
    /// Traces Screen05 Zeroing HUD no-VR input, reload, and shoulder switch scenarios.
    /// </summary>
    [TestFixture]
    public class Screen05_XRTrainingInputPlayModeTests
    {
        [UnityTest]
        public IEnumerator Screen02_MainMenuConfirmWithNoVrInputSubstitute_Opens100mZeroingEntry()
        {
            var input = new ManualXRTrainingInput();
            var bus = new GameEventBus();
            var router = new UIRouter(bus);
            var dispatcher = new XRTrainingInputCommandDispatcher(input, bus, router);

            input.Press(XRTrainingInputButton.Confirm);
            var result = dispatcher.ProcessFrame(new XRTrainingInputDispatchContext
            {
                SourceScreen = ScreenId.MainMenu,
                ConfirmUIEvent = UIEventId.MainMenu_OpenZeroing
            });
            input.AdvanceFrame();

            yield return null;

            Assert.IsTrue(result.Success);
            Assert.AreEqual(ScreenId.ZeroingBriefing, router.Current);
            Assert.AreEqual(TrainingMode.Zeroing100m, router.SelectedMode);

            input.Press(XRTrainingInputButton.Back);
            var backResult = dispatcher.ProcessFrame(new XRTrainingInputDispatchContext
            {
                SourceScreen = router.Current
            });

            yield return null;

            Assert.IsTrue(backResult.Success);
            Assert.AreEqual(ScreenId.MainMenu, router.Current);
        }

        [UnityTest]
        public IEnumerator Screen05_NoVrInputSubstitute_TriggerReloadAndShoulderSwitch_ArePublishedForServiceLayer()
        {
            var input = new ManualXRTrainingInput();
            var bus = new GameEventBus();
            var dispatcher = new XRTrainingInputCommandDispatcher(input, bus);
            var received = new List<XRTrainingInputCommandType>();
            bus.Subscribe<XRTrainingInputCommandEvent>(evt => received.Add(evt.CommandType));

            input.Press(XRTrainingInputButton.Trigger);
            input.Press(XRTrainingInputButton.Reload);
            input.Press(XRTrainingInputButton.SwitchShoulder);
            var result = dispatcher.ProcessFrame(new XRTrainingInputDispatchContext
            {
                SourceScreen = ScreenId.ZeroingHud
            });
            input.AdvanceFrame();

            yield return null;

            Assert.IsTrue(result.Success);
            Assert.AreEqual(3, result.Data);
            CollectionAssert.AreEqual(
                new[]
                {
                    XRTrainingInputCommandType.Trigger,
                    XRTrainingInputCommandType.Reload,
                    XRTrainingInputCommandType.SwitchShoulder
                },
                received);
        }
    }
}
