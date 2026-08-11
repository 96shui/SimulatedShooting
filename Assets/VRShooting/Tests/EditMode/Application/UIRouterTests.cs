using NUnit.Framework;
using VRShooting.Application;
using VRShooting.Application.Events;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Tests.EditMode.Application
{
    /// <summary>
    /// 路由测试。追溯 docs/BDD/screens/02-游戏主界面.feature.md。
    /// </summary>
    [TestFixture]
    public class Screen02_UIRouterTests
    {
        [Test]
        public void Screen02_MainMenuOpenZeroing_GoesToZeroingBriefing()
        {
            var bus = new GameEventBus();
            var router = new UIRouter(bus);

            var result = router.HandleUIEvent(UIEventId.MainMenu_OpenZeroing, ScreenId.MainMenu);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(ScreenId.ZeroingBriefing, router.Current);
            Assert.AreEqual(TrainingMode.Zeroing100m, router.SelectedMode);
        }

        [Test]
        public void Screen02_MainMenuOpenMovingTarget_GoesToMovingTargetSettings()
        {
            var bus = new GameEventBus();
            var router = new UIRouter(bus);

            var result = router.HandleUIEvent(UIEventId.MainMenu_OpenMovingTarget, ScreenId.MainMenu);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(ScreenId.MovingTargetSettings, router.Current);
            Assert.AreEqual(TrainingMode.MovingTarget, router.SelectedMode);
        }

        [Test]
        public void Screen02_RouterOpen_PublishesScreenChangedEvent()
        {
            var bus = new GameEventBus();
            var router = new UIRouter(bus);
            ScreenChangedEvent? received = null;
            bus.Subscribe<ScreenChangedEvent>(evt => received = evt);

            router.Open(ScreenId.ZeroingBriefing, new NavigationArgs { Mode = TrainingMode.Zeroing100m });

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(ScreenId.MainMenu, received.Value.PreviousScreen);
            Assert.AreEqual(ScreenId.ZeroingBriefing, received.Value.CurrentScreen);
        }

        [Test]
        public void Screen02_RouterBack_ReturnsPreviousScreen()
        {
            var bus = new GameEventBus();
            var router = new UIRouter(bus);

            router.HandleUIEvent(UIEventId.MainMenu_OpenZeroing, ScreenId.MainMenu);
            var backResult = router.Back();

            Assert.IsTrue(backResult.Success);
            Assert.AreEqual(ScreenId.MainMenu, router.Current);
        }

        [Test]
        public void Screen04_ZeroingStart_GoesToZeroingHudWithSession()
        {
            var bus = new GameEventBus();
            var router = new UIRouter(bus, ScreenId.ZeroingBriefing);
            ScreenChangedEvent? received = null;
            bus.Subscribe<ScreenChangedEvent>(evt => received = evt);

            var result = router.HandleUIEvent(UIEventId.Zeroing_Start, ScreenId.ZeroingBriefing, new NavigationArgs
            {
                Mode = TrainingMode.Zeroing100m,
                SessionId = "session-001",
                ReturnToScreen = ScreenId.ZeroingBriefing.ToString()
            });

            Assert.IsTrue(result.Success);
            Assert.AreEqual(ScreenId.ZeroingHud, router.Current);
            Assert.IsTrue(received.HasValue);
            Assert.AreEqual("session-001", received.Value.Args.SessionId);
            Assert.AreEqual(TrainingMode.Zeroing100m, router.SelectedMode);
        }

        [Test]
        public void Screen02_RouterOpenSameScreen_IsIdempotent()
        {
            var bus = new GameEventBus();
            var router = new UIRouter(bus);

            var first = router.Open(ScreenId.MainMenu);
            var second = router.Open(ScreenId.MainMenu);

            Assert.IsTrue(first.Success);
            Assert.IsTrue(second.Success);
            Assert.AreEqual(ScreenId.MainMenu, router.Current);
        }

        [Test]
        public void Screen02_RouterBackWithoutHistory_ReturnsInvalidState()
        {
            var bus = new GameEventBus();
            var router = new UIRouter(bus);

            var result = router.Back();

            Assert.IsFalse(result.Success);
            Assert.AreEqual(ErrorCode.InvalidState, result.ErrorCode);
        }
    }
}
