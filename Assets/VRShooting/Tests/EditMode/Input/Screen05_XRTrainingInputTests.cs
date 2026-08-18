using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VRShooting.Application;
using VRShooting.Common;
using VRShooting.Input;

namespace VRShooting.Tests.EditMode.Input
{
    /// <summary>
    /// Traces Screen05 Zeroing HUD no-VR input, reload, and shoulder switch scenarios.
    /// </summary>
    [TestFixture]
    public class Screen05_XRTrainingInputTests
    {
        [Test]
        public void Screen05_NoVrInputSubstitute_SetAndClearButtonStates_Works()
        {
            var input = new ManualXRTrainingInput();

            input.Press(XRTrainingInputButton.Confirm);
            input.Press(XRTrainingInputButton.Back);
            input.Press(XRTrainingInputButton.Trigger);
            input.Press(XRTrainingInputButton.Reload);
            input.Press(XRTrainingInputButton.SwitchShoulder);
            input.Press(XRTrainingInputButton.Aim);
            input.Press(XRTrainingInputButton.CommandMenu);
            input.SetAxes(new Vector2(0.25f, 0.5f), new Vector2(-0.5f, 0.75f));

            Assert.IsTrue(input.ConfirmPressed);
            Assert.IsTrue(input.BackPressed);
            Assert.IsTrue(input.TriggerPressed);
            Assert.IsTrue(input.ReloadPressed);
            Assert.IsTrue(input.SwitchShoulderPressed);
            Assert.IsTrue(input.AimPressed);
            Assert.IsTrue(input.AimHeld);
            Assert.IsTrue(input.CommandMenuHeld);
            Assert.AreEqual(new Vector2(0.25f, 0.5f), input.MoveAxis);
            Assert.AreEqual(new Vector2(-0.5f, 0.75f), input.TurnAxis);

            input.Clear();

            Assert.IsFalse(input.ConfirmPressed);
            Assert.IsFalse(input.BackPressed);
            Assert.IsFalse(input.TriggerPressed);
            Assert.IsFalse(input.ReloadPressed);
            Assert.IsFalse(input.SwitchShoulderPressed);
            Assert.IsFalse(input.AimPressed);
            Assert.IsFalse(input.AimHeld);
            Assert.IsFalse(input.CommandMenuHeld);
            Assert.AreEqual(Vector2.zero, input.MoveAxis);
            Assert.AreEqual(Vector2.zero, input.TurnAxis);
        }

        [Test]
        public void Screen05_NoVrInputSubstitute_AdvanceFrame_DoesNotRepeatOneShotCommands()
        {
            var input = new ManualXRTrainingInput();

            input.Press(XRTrainingInputButton.Trigger);
            Assert.IsTrue(input.TriggerPressed);

            input.AdvanceFrame();
            Assert.IsFalse(input.TriggerPressed);

            input.SetButton(XRTrainingInputButton.Trigger, true);
            Assert.IsFalse(input.TriggerPressed);

            input.Release(XRTrainingInputButton.Trigger);
            input.Press(XRTrainingInputButton.Trigger);
            Assert.IsTrue(input.TriggerPressed);
        }

        [Test]
        public void Screen05_NoVrInputSubstitute_TriggerReloadAndShoulderSwitch_PublishAbstractCommandsOnly()
        {
            var input = new ManualXRTrainingInput();
            var bus = new GameEventBus();
            var dispatcher = new XRTrainingInputCommandDispatcher(input, bus);
            var received = new List<XRTrainingInputCommandEvent>();
            bus.Subscribe<XRTrainingInputCommandEvent>(received.Add);

            input.Press(XRTrainingInputButton.Trigger);
            input.Press(XRTrainingInputButton.Reload);
            input.Press(XRTrainingInputButton.SwitchShoulder);

            var result = dispatcher.ProcessFrame(new XRTrainingInputDispatchContext
            {
                SourceScreen = ScreenId.ZeroingHud
            });

            Assert.IsTrue(result.Success);
            Assert.AreEqual(4, result.Data);
            Assert.AreEqual(4, received.Count);
            Assert.AreEqual(XRTrainingInputCommandType.Trigger, received[0].CommandType);
            Assert.AreEqual(XRTrainingInputCommandType.TriggerHeld, received[1].CommandType);
            Assert.AreEqual(XRTrainingInputCommandType.Reload, received[2].CommandType);
            Assert.AreEqual(XRTrainingInputCommandType.SwitchShoulder, received[3].CommandType);
            Assert.AreEqual(ScreenId.ZeroingHud, received[0].SourceScreen);
        }

        [Test]
        public void Screen05_NoVrInputSubstitute_AdvanceFrame_PreventsDispatcherDuplicateTrigger()
        {
            var input = new ManualXRTrainingInput();
            var bus = new GameEventBus();
            var dispatcher = new XRTrainingInputCommandDispatcher(input, bus);
            var triggerCount = 0;
            bus.Subscribe<XRTrainingInputCommandEvent>(evt =>
            {
                if (evt.CommandType == XRTrainingInputCommandType.Trigger)
                {
                    triggerCount++;
                }
            });

            input.Press(XRTrainingInputButton.Trigger);
            dispatcher.ProcessFrame(new XRTrainingInputDispatchContext { SourceScreen = ScreenId.ZeroingHud });
            input.AdvanceFrame();
            dispatcher.ProcessFrame(new XRTrainingInputDispatchContext { SourceScreen = ScreenId.ZeroingHud });

            Assert.AreEqual(1, triggerCount);
        }

        [Test]
        public void Screen05_NoVrInputSubstitute_AimButtonPublishesAimCommands()
        {
            var input = new ManualXRTrainingInput();
            var bus = new GameEventBus();
            var dispatcher = new XRTrainingInputCommandDispatcher(input, bus);
            var received = new List<XRTrainingInputCommandEvent>();
            bus.Subscribe<XRTrainingInputCommandEvent>(received.Add);

            input.Press(XRTrainingInputButton.Aim);

            var result = dispatcher.ProcessFrame(new XRTrainingInputDispatchContext
            {
                SourceScreen = ScreenId.ZeroingHud
            });

            Assert.IsTrue(result.Success);
            Assert.AreEqual(2, result.Data);
            Assert.AreEqual(XRTrainingInputCommandType.AimPressed, received[0].CommandType);
            Assert.AreEqual(XRTrainingInputCommandType.AimHeld, received[1].CommandType);
            Assert.IsTrue(received[1].AimHeld);
        }

        [Test]
        public void Task013_NoVrInputSubstitute_GripEdgesAreObservable()
        {
            var input = new ManualXRTrainingInput();

            input.Press(XRTrainingInputButton.RightGrip);
            input.Press(XRTrainingInputButton.LeftGrip);

            Assert.IsTrue(input.RightGripPressed);
            Assert.IsTrue(input.RightGripHeld);
            Assert.IsTrue(input.LeftGripPressed);
            Assert.IsTrue(input.LeftGripHeld);

            input.AdvanceFrame();
            input.Release(XRTrainingInputButton.LeftGrip);

            Assert.IsFalse(input.LeftGripHeld);
            Assert.IsTrue(input.LeftGripReleased);
            Assert.IsTrue(input.RightGripHeld);
        }

        [Test]
        public void Task013_NoVrInputSubstitute_TriggerUsesHysteresis()
        {
            var input = new ManualXRTrainingInput();

            input.SetRightTriggerValue(0.74f);
            Assert.IsFalse(input.TriggerPressed);
            input.SetRightTriggerValue(0.76f);
            Assert.IsTrue(input.TriggerPressed);

            input.AdvanceFrame();
            input.SetRightTriggerValue(0.5f);
            input.SetRightTriggerValue(0.8f);
            Assert.IsFalse(input.TriggerPressed, "trigger must stay latched until it returns below the release threshold");

            input.SetRightTriggerValue(0.2f);
            Assert.IsTrue(input.TriggerReleased);
            Assert.IsFalse(input.TriggerHeld);
            input.SetRightTriggerValue(0.8f);
            Assert.IsTrue(input.TriggerPressed);
            Assert.IsTrue(input.TriggerHeld);
        }

        [Test]
        public void Task007_NoVrInputSubstitute_TriggerHeldAndReleasedAreObservable()
        {
            var input = new ManualXRTrainingInput();

            input.Press(XRTrainingInputButton.Trigger);
            Assert.IsTrue(input.TriggerPressed);
            Assert.IsTrue(input.TriggerHeld);
            Assert.IsFalse(input.TriggerReleased);

            input.AdvanceFrame();
            Assert.IsFalse(input.TriggerPressed);
            Assert.IsTrue(input.TriggerHeld);

            input.Release(XRTrainingInputButton.Trigger);
            Assert.IsTrue(input.TriggerReleased);
            Assert.IsFalse(input.TriggerHeld);
        }
    }
}
