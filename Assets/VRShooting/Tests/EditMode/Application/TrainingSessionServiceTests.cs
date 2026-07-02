using NUnit.Framework;
using VRShooting.Application;
using VRShooting.Application.Events;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Tests.EditMode.Application
{
    /// <summary>
    /// Session 生命周期测试。追溯 docs/BDD/screens/04-100m任务说明.feature.md。
    /// </summary>
    [TestFixture]
    public class Screen04_TrainingSessionServiceTests
    {
        [Test]
        public void Screen04_CreateAndStartZeroingSession_EntersRunningState()
        {
            var bus = new GameEventBus();
            var sessions = new TrainingSessionService(bus);

            var createResult = sessions.Create(
                TrainingMode.Zeroing100m,
                string.Empty,
                string.Empty,
                RandomSeed.Fixed(42));

            Assert.IsTrue(createResult.Success);
            Assert.AreEqual(SessionState.Preparing, createResult.Data.State);
            Assert.IsFalse(string.IsNullOrEmpty(createResult.Data.SessionId));

            var startResult = sessions.Start(createResult.Data.SessionId);

            Assert.IsTrue(startResult.Success);
            Assert.AreEqual(SessionState.Running, startResult.Data.State);
            Assert.AreEqual(SessionState.Running, sessions.Current.State);
        }

        [Test]
        public void Screen04_StartSameSessionTwice_DoesNotCreateDuplicateSession()
        {
            var bus = new GameEventBus();
            var sessions = new TrainingSessionService(bus);
            var publishedCount = 0;
            bus.Subscribe<SessionStartedEvent>(_ => publishedCount++);

            var createResult = sessions.Create(TrainingMode.Zeroing100m, string.Empty, string.Empty, default);
            var sessionId = createResult.Data.SessionId;

            var firstStart = sessions.Start(sessionId);
            var secondStart = sessions.Start(sessionId);

            Assert.IsTrue(firstStart.Success);
            Assert.IsTrue(secondStart.Success);
            Assert.AreEqual(sessionId, secondStart.Data.SessionId);
            Assert.AreEqual(1, publishedCount);
        }

        [Test]
        public void Screen04_CreateWhileActiveSessionExists_ReturnsInvalidState()
        {
            var bus = new GameEventBus();
            var sessions = new TrainingSessionService(bus);

            var first = sessions.Create(TrainingMode.Zeroing100m, string.Empty, string.Empty, default);
            var second = sessions.Create(TrainingMode.Zeroing100m, string.Empty, string.Empty, default);

            Assert.IsTrue(first.Success);
            Assert.IsFalse(second.Success);
            Assert.AreEqual(ErrorCode.InvalidState, second.ErrorCode);
            Assert.AreEqual(first.Data.SessionId, second.Data.SessionId);
        }

        [Test]
        public void Screen04_EndSession_PublishesSessionEndedEvent()
        {
            var bus = new GameEventBus();
            var sessions = new TrainingSessionService(bus);
            SessionEndedEvent? received = null;
            bus.Subscribe<SessionEndedEvent>(evt => received = evt);

            var createResult = sessions.Create(TrainingMode.Zeroing100m, string.Empty, string.Empty, default);
            sessions.Start(createResult.Data.SessionId);

            var endResult = sessions.End(createResult.Data.SessionId, SessionEndReason.Completed);

            Assert.IsTrue(endResult.Success);
            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(SessionEndReason.Completed, received.Value.Reason);
            Assert.AreEqual(SessionState.Completed, received.Value.Session.State);
        }

        [Test]
        public void Screen04_PauseAndResume_PreservesSessionId()
        {
            var bus = new GameEventBus();
            var sessions = new TrainingSessionService(bus);

            var createResult = sessions.Create(TrainingMode.Zeroing100m, string.Empty, string.Empty, default);
            var sessionId = createResult.Data.SessionId;
            sessions.Start(sessionId);

            var pauseResult = sessions.Pause(sessionId);
            var resumeResult = sessions.Resume(sessionId);

            Assert.IsTrue(pauseResult.Success);
            Assert.AreEqual(SessionState.Paused, pauseResult.Data.State);
            Assert.IsTrue(resumeResult.Success);
            Assert.AreEqual(SessionState.Running, resumeResult.Data.State);
            Assert.AreEqual(sessionId, resumeResult.Data.SessionId);
        }
    }
}
