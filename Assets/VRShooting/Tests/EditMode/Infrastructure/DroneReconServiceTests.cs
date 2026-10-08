using System;
using NUnit.Framework;
using VRShooting.Application;
using VRShooting.Application.Combat;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Tests.EditMode.Infrastructure
{
    // BDD28: minimum takeoff time, full route sequence, camera restore and stale callback isolation.
    public sealed class DroneReconServiceTests
    {
        sealed class Clock : ICombatClock { public double Now { get; set; } public long Tick { get; set; } }
        sealed class Scene : IDroneReconScenePort
        {
            public DroneReconStepCommandDto Last;
            public bool Locked;
            public event Action<DroneReconSceneFactDto> FactReceived;
            public ServiceResult<Unit> Prepare(DroneReconStepCommandDto command) => Ok();
            public ServiceResult<Unit> PlayStep(DroneReconStepCommandDto command) { Last = command; return Ok(); }
            public ServiceResult<Unit> SetCharacterActionsLocked(string id, bool locked) { Locked = locked; return Ok(); }
            public ServiceResult<Unit> SetSuspended(string id, bool suspended) => Ok();
            public ServiceResult<Unit> RestorePlayerView(DroneReconStepCommandDto command) { Last = command; return Ok(); }
            public ServiceResult<Unit> StopAndReset(string id, string sequence) => Ok();
            public void Report(DroneReconFactKind kind, string session = null)
                => FactReceived?.Invoke(new DroneReconSceneFactDto
                {
                    SessionId = session ?? Last.SessionId, SequenceId = Last.SequenceId,
                    StepId = Last.StepId, Phase = Last.Phase, Kind = kind,
                    FeedAvailable = true, FeedBindingId = Last.FeedBindingId
                });
            static ServiceResult<Unit> Ok() => ServiceResult<Unit>.Ok(Unit.Value);
        }

        [Test]
        public void TwentySecondSurveyStartsVisibleReturnAndKeepsCombatLocked()
        {
            var clock=new Clock();var scene=new Scene();
            using var service=new DroneReconService(clock,scene);
            service.Begin("session");scene.Report(DroneReconFactKind.FeedState);
            scene.Report(DroneReconFactKind.StepCompleted);clock.Now=3;service.Advance("session");
            clock.Now=19.99;service.Advance("session");
            Assert.That(service.GetSnapshot("session").Data.Phase,Is.EqualTo(DroneReconPhase.DroneRecon));
            clock.Now=22.99;service.Advance("session");
            Assert.That(scene.Last.Phase,Is.EqualTo(DroneReconPhase.DroneRecon));
            clock.Now=23;service.Advance("session");
            Assert.That(scene.Last.Phase,Is.EqualTo(DroneReconPhase.DroneReturn));
            Assert.That(service.GetSnapshot("session").Data.ViewMode,Is.EqualTo(DroneReconViewMode.Player));
            Assert.That(scene.Locked,Is.True);
            Assert.That(service.ConfirmCombatStarted("session").Success,Is.False);
            Assert.That(service.GetSnapshot("session").Data.Phase,Is.EqualTo(DroneReconPhase.DroneReturn));
            clock.Now=600;service.Advance("session");
            Assert.That(service.GetSnapshot("session").Data.Phase,Is.EqualTo(DroneReconPhase.DroneReturn));
            Assert.That(scene.Locked,Is.True);
            Assert.That(service.GetSnapshot("session").Data.ReconElapsedSeconds,Is.EqualTo(600));
        }
        [Test]
        public void TakeoffRequiresThreeSecondsFeedAndCurrentCompletion()
        {
            var clock = new Clock(); var scene = new Scene();
            using var service = new DroneReconService(clock, scene);
            Assert.That(service.Begin("session").Success, Is.True);
            scene.Report(DroneReconFactKind.StepCompleted, "old-session");
            scene.Report(DroneReconFactKind.FeedState);
            clock.Now = 3; service.Advance("session");
            Assert.That(service.GetSnapshot("session").Data.Phase, Is.EqualTo(DroneReconPhase.PlayerTakeoff));
            scene.Report(DroneReconFactKind.StepCompleted);
            service.Advance("session");
            Assert.That(service.GetSnapshot("session").Data.Phase, Is.EqualTo(DroneReconPhase.DroneRecon));
            Assert.That(scene.Locked, Is.True);
        }
        [Test]
        public void DuplicateCompletionIsIdempotentAndConflictingPayloadKeepsCombatLocked()
        {
            var scene=new Scene();using var service=new DroneReconService(new Clock(),scene);
            service.Begin("session");
            var fact=new DroneReconSceneFactDto {SessionId="session",SequenceId=scene.Last.SequenceId,
                StepId=scene.Last.StepId,Phase=scene.Last.Phase,Kind=DroneReconFactKind.StepCompleted};
            Assert.That(service.AcceptSceneFact(fact).Success,Is.True);
            Assert.That(service.AcceptSceneFact(fact).Success,Is.True);
            Assert.That(service.AcceptSceneFact(new DroneReconSceneFactDto {SessionId="session",
                SequenceId=fact.SequenceId,StepId=fact.StepId,Phase=fact.Phase,Kind=fact.Kind,Reason="conflicting"}).ErrorCode,
                Is.EqualTo(ErrorCode.InvalidInput));
            var snapshot=service.GetSnapshot("session").Data;
            Assert.That(snapshot.Phase,Is.EqualTo(DroneReconPhase.Error));
            Assert.That(snapshot.CharacterActionsLocked,Is.True);
            Assert.That(snapshot.ViewMode,Is.EqualTo(DroneReconViewMode.Player));
            Assert.That(snapshot.ErrorCode,Is.EqualTo(ErrorCode.InvalidInput));
        }
        [Test]
        public void CancelledSessionCannotRestartAndInvalidSequenceDoesNotConsumeSessionId()
        {
            var attempts=0;var scene=new Scene();
            using var service=new DroneReconService(new Clock(),scene,sequenceFactory:()=>++attempts==1?"":"valid");
            Assert.That(service.Begin("session").ErrorCode,Is.EqualTo(ErrorCode.InvalidInput));
            Assert.That(service.Begin("session").Success,Is.True);
            service.Cancel("session");
            Assert.That(service.Begin("session").ErrorCode,Is.EqualTo(ErrorCode.InvalidState));
            Assert.That(service.Begin("new-session").Success,Is.True);
            Assert.That(service.AcceptSceneFact(new DroneReconSceneFactDto {SessionId="session"}).ErrorCode,
                Is.EqualTo(ErrorCode.NotFound));
        }

        [Test]
        public void AllRoutePhasesAndPlayerRestorePrecedeUnlock()
        {
            var clock = new Clock(); var scene = new Scene();
            using var service = new DroneReconService(clock, scene);
            service.Begin("session"); scene.Report(DroneReconFactKind.FeedState);
            scene.Report(DroneReconFactKind.StepCompleted); clock.Now = 2.99;
            service.Advance("session");
            Assert.That(service.GetSnapshot("session").Data.Phase, Is.EqualTo(DroneReconPhase.PlayerTakeoff));
            clock.Now = 3; service.Advance("session");
            foreach (var expected in new[] { DroneReconPhase.DroneReturn, DroneReconPhase.DroneLanding,
                DroneReconPhase.RestoringPlayerView, DroneReconPhase.Ready })
            {
                Assert.That(service.ConfirmCombatStarted("session").ErrorCode, Is.EqualTo(ErrorCode.InvalidState));
                scene.Report(DroneReconFactKind.StepCompleted); service.Advance("session");
                Assert.That(service.GetSnapshot("session").Data.Phase, Is.EqualTo(expected));
                Assert.That(service.GetSnapshot("session").Data.ViewMode, Is.EqualTo(DroneReconViewMode.Player),
                    "The player watches the drone physically return and land.");
                Assert.That(scene.Locked, Is.True);
            }
            Assert.That(service.ConfirmCombatStarted("session").Success, Is.True);
            Assert.That(scene.Locked, Is.False);
        }

        [Test]
        public void SuspensionDoesNotConsumeTakeoffTimeOrTimeout()
        {
            var clock = new Clock(); var scene = new Scene(); var paused = true;
            using var service = new DroneReconService(clock, scene, suspended: () => paused);
            service.Begin("session"); scene.Report(DroneReconFactKind.FeedState);
            scene.Report(DroneReconFactKind.StepCompleted);
            clock.Now = 1000; service.Advance("session");
            paused = false; service.Advance("session");
            Assert.That(service.GetSnapshot("session").Data.ReconElapsedSeconds, Is.Zero);
            clock.Now = 1003; service.Advance("session");
            Assert.That(service.GetSnapshot("session").Data.Phase, Is.EqualTo(DroneReconPhase.DroneRecon));
        }
    }
}
