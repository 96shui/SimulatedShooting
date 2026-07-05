using System;
using NUnit.Framework;
using VRShooting.Application;
using VRShooting.Application.Events;
using VRShooting.Common;

namespace VRShooting.Tests.EditMode.Application
{
    /// <summary>
    /// 事件总线测试。追溯 task002 / docs/接口文档/00-UI与玩法服务层交互总约束.md。
    /// </summary>
    [TestFixture]
    public class Screen02_GameEventBusTests
    {
        [Test]
        public void Screen02_EventBus_PublishSubscribeAndUnsubscribe_Works()
        {
            var bus = new GameEventBus();
            var received = 0;

            var subscription = bus.Subscribe<ScreenChangedEvent>(_ => received++);
            bus.Publish(new ScreenChangedEvent
            {
                PreviousScreen = ScreenId.MainMenu,
                CurrentScreen = ScreenId.ZeroingBriefing
            });

            Assert.AreEqual(1, received);

            subscription.Dispose();
            bus.Publish(new ScreenChangedEvent
            {
                PreviousScreen = ScreenId.ZeroingBriefing,
                CurrentScreen = ScreenId.ZeroingHud
            });

            Assert.AreEqual(1, received);
        }

        [Test]
        public void Screen04_SessionStartedEvent_IsPublishedWhenSessionStarts()
        {
            var bus = new GameEventBus();
            SessionStartedEvent? received = null;
            bus.Subscribe<SessionStartedEvent>(evt => received = evt);

            var sessions = new TrainingSessionService(bus);
            var createResult = sessions.Create(TrainingMode.Zeroing100m, string.Empty, string.Empty, default);
            var startResult = sessions.Start(createResult.Data.SessionId);

            Assert.IsTrue(startResult.Success);
            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(SessionState.Running, received.Value.Session.State);
        }
    }
}
