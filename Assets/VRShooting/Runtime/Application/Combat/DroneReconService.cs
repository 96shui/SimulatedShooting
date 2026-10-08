using System;
using System.Collections.Generic;
using VRShooting.Common;
using VRShooting.Contracts;

namespace VRShooting.Application.Combat
{
    /// <summary>BDD28. Advances only after correlated scene facts; combat time is never advanced here.</summary>
    public sealed class DroneReconService : IDroneReconService, IDisposable
    {
        readonly ICombatClock clock;
        readonly IDroneReconScenePort scene;
        readonly DroneReconConfigDto config;
        readonly Func<bool> suspended;
        readonly Func<string> sequenceFactory;
        readonly Queue<DroneReconSceneFactDto> facts = new Queue<DroneReconSceneFactDto>();
        readonly HashSet<string> usedSessions = new HashSet<string>();
        readonly Dictionary<string,DroneReconSceneFactDto> completionFacts=new Dictionary<string,DroneReconSceneFactDto>();
        ErrorCode failureCode;
        DroneReconSnapshotDto snapshot;
        double lastNow, stepAge, elapsed;
        bool completed, publishing, disposed, lastSuspended;
        long revision;
        public event Action<DroneReconSnapshotDto> Changed;

        public DroneReconService(ICombatClock clock, IDroneReconScenePort scene,
            DroneReconConfigDto? config = null, Func<bool> suspended = null, Func<string> sequenceFactory = null)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.scene = scene ?? throw new ArgumentNullException(nameof(scene));
            this.config = config ?? DroneReconConfigDto.Default;
            this.suspended = suspended ?? (() => false);
            this.sequenceFactory = sequenceFactory ?? (() => Guid.NewGuid().ToString("N"));
            scene.FactReceived += Receive;
        }

        public ServiceResult<DroneReconSnapshotDto> Begin(string sessionId)
        {
            if (disposed || publishing) return Fail<DroneReconSnapshotDto>(ErrorCode.Busy);
            if (string.IsNullOrWhiteSpace(sessionId) || !config.Validate().Success || !ValidTime())
                return Fail<DroneReconSnapshotDto>(ErrorCode.InvalidInput);
            if (snapshot.SessionId == sessionId) return snapshot.Phase==DroneReconPhase.Cancelled
                ? Fail<DroneReconSnapshotDto>(ErrorCode.InvalidState) : ServiceResult<DroneReconSnapshotDto>.Ok(snapshot);
            if (snapshot.SessionId != null && snapshot.Phase != DroneReconPhase.Cancelled)
                return Fail<DroneReconSnapshotDto>(ErrorCode.Busy);
            if (usedSessions.Contains(sessionId)) return Fail<DroneReconSnapshotDto>(ErrorCode.InvalidState);
            var sequence = sequenceFactory();
            if (string.IsNullOrWhiteSpace(sequence)) return Fail<DroneReconSnapshotDto>(ErrorCode.InvalidInput);
            usedSessions.Add(sessionId);completionFacts.Clear();failureCode=ErrorCode.None;
            facts.Clear(); revision = 0; elapsed = stepAge = 0; lastNow = clock.Now;
            completed = lastSuspended = false;
            snapshot = new DroneReconSnapshotDto { SessionId = sessionId, SequenceId = sequence,
                Phase = DroneReconPhase.NotStarted, CharacterActionsLocked = true,
                FeedBindingId = config.FeedBindingId };
            var locked = Call(() => scene.SetCharacterActionsLocked(sessionId, true));
            if (!locked.Success) { Error(); return Fail<DroneReconSnapshotDto>(ErrorCode.ResourceUnavailable); }
            var prepared = Call(() => scene.Prepare(Command(DroneReconPhase.NotStarted)));
            if (!prepared.Success) { Error(); return Fail<DroneReconSnapshotDto>(ErrorCode.ResourceUnavailable); }
            Enter(DroneReconPhase.PlayerTakeoff);
            return snapshot.Phase == DroneReconPhase.Error ? Fail<DroneReconSnapshotDto>(ErrorCode.ResourceUnavailable)
                : ServiceResult<DroneReconSnapshotDto>.Ok(snapshot);
        }

        public ServiceResult<DroneReconSnapshotDto> GetSnapshot(string sessionId) => Matches(sessionId)
            ? ServiceResult<DroneReconSnapshotDto>.Ok(snapshot) : Fail<DroneReconSnapshotDto>(ErrorCode.NotFound);

        public ServiceResult<Unit> Advance(string sessionId)
        {
            if (disposed || !Matches(sessionId)) return Fail<Unit>(ErrorCode.NotFound);
            if (publishing) return Fail<Unit>(ErrorCode.Busy);
            if (!ValidTime() || clock.Now < lastNow) return Fail<Unit>(ErrorCode.InvalidInput);
            if (snapshot.Phase == DroneReconPhase.Error) return Fail<Unit>(snapshot.ErrorCode);
            if (snapshot.Phase == DroneReconPhase.Ready || snapshot.Phase == DroneReconPhase.Cancelled) return Ok();
            var paused = suspended();
            var delta = clock.Now - lastNow; lastNow = clock.Now;
            if (paused != lastSuspended)
            {
                delta = 0;
                var result = Call(() => scene.SetSuspended(sessionId, paused));
                lastSuspended = paused;
                if (!result.Success) { Error(); return Fail<Unit>(ErrorCode.ResourceUnavailable); }
            }
            if (paused) return Ok();
            stepAge += delta; elapsed += delta;
            while (facts.Count > 0)
            {
                var fact = facts.Dequeue();
                if (!Current(fact)) continue;
                if (fact.Kind == DroneReconFactKind.StepFailed) { Error(); return Fail<Unit>(ErrorCode.ResourceUnavailable); }
                if (fact.Kind == DroneReconFactKind.StepCompleted) completed = true;
                if (fact.Kind == DroneReconFactKind.FeedState)
                {
                    if (fact.FeedAvailable && fact.FeedBindingId != config.FeedBindingId)
                    { Error(); return Fail<Unit>(ErrorCode.ResourceUnavailable); }
                    Update(snapshot.Phase, fact.FeedAvailable, snapshot.TelemetryValid,
                        snapshot.HeightAboveGroundMeters, snapshot.SpeedMetersPerSecond);
                    if (!fact.FeedAvailable && snapshot.ViewMode == DroneReconViewMode.DroneFeed)
                    { Error(); return Fail<Unit>(ErrorCode.ResourceUnavailable); }
                }
                if (fact.Kind == DroneReconFactKind.Telemetry)
                {
                    var valid = fact.TelemetryValid && Finite(fact.HeightAboveGroundMeters)
                        && Finite(fact.SpeedMetersPerSecond) && fact.HeightAboveGroundMeters >= 0f && fact.SpeedMetersPerSecond >= 0f;
                    Update(snapshot.Phase, snapshot.FeedAvailable, valid,
                        valid ? fact.HeightAboveGroundMeters : 0f, valid ? fact.SpeedMetersPerSecond : 0f);
                }
            }
            if(config.MaximumOpeningSeconds>0&&snapshot.Phase==DroneReconPhase.DroneRecon&&stepAge>=config.MaximumOpeningSeconds)
                Enter(DroneReconPhase.DroneReturn);
            if (stepAge > config.StepTimeoutSeconds && snapshot.Phase!=DroneReconPhase.DroneRecon
                && snapshot.Phase!=DroneReconPhase.DroneReturn) { Error(); return Fail<Unit>(ErrorCode.ResourceUnavailable); }
            if (completed)
            {
                switch (snapshot.Phase)
                {
                    case DroneReconPhase.PlayerTakeoff:
                        if (stepAge >= config.TakeoffSeconds && snapshot.FeedAvailable) Enter(DroneReconPhase.DroneRecon);
                        break;
                    case DroneReconPhase.DroneRecon: Enter(DroneReconPhase.DroneReturn); break;
                    case DroneReconPhase.DroneReturn: Enter(DroneReconPhase.DroneLanding); break;
                    case DroneReconPhase.DroneLanding: Enter(DroneReconPhase.RestoringPlayerView); break;
                    case DroneReconPhase.RestoringPlayerView: Enter(DroneReconPhase.Ready); break;
                }
            }
            Update(snapshot.Phase, snapshot.FeedAvailable, snapshot.TelemetryValid,
                snapshot.HeightAboveGroundMeters, snapshot.SpeedMetersPerSecond);
            return snapshot.Phase == DroneReconPhase.Error ? Fail<Unit>(ErrorCode.ResourceUnavailable) : Ok();
        }

        // Owned by the application coordinator, after TrenchService has accepted BeginCombat.
        public ServiceResult<Unit> ConfirmCombatStarted(string sessionId)
        {
            if (!Matches(sessionId)) return Fail<Unit>(ErrorCode.NotFound);
            if (publishing) return Fail<Unit>(ErrorCode.Busy);
            if (snapshot.Phase != DroneReconPhase.Ready) return Fail<Unit>(ErrorCode.InvalidState);
            if (!snapshot.CharacterActionsLocked) return Ok();
            var result = Call(() => scene.SetCharacterActionsLocked(sessionId, false));
            if (!result.Success) { Error(); return result; }
            Update(snapshot.Phase, snapshot.FeedAvailable, snapshot.TelemetryValid,
                snapshot.HeightAboveGroundMeters, snapshot.SpeedMetersPerSecond, false);
            return Ok();
        }

        public ServiceResult<Unit> Cancel(string sessionId)
        {
            if (!Matches(sessionId)) return Fail<Unit>(ErrorCode.NotFound);
            if (publishing) return Fail<Unit>(ErrorCode.Busy);
            if (snapshot.Phase == DroneReconPhase.Cancelled) return Ok();
            var result = Call(() => scene.StopAndReset(sessionId, snapshot.SequenceId));
            facts.Clear(); Update(DroneReconPhase.Cancelled, false, false, 0f, 0f); return result;
        }

        void Receive(DroneReconSceneFactDto fact)
        {
            AcceptSceneFact(fact);
        }
        public ServiceResult<Unit> AcceptSceneFact(DroneReconSceneFactDto fact)
        {
            if(!Matches(fact.SessionId))return Fail<Unit>(ErrorCode.NotFound);
            if(publishing)return Fail<Unit>(ErrorCode.Busy);
            if(!Current(fact)||snapshot.Phase==DroneReconPhase.Cancelled||snapshot.Phase==DroneReconPhase.Ready||snapshot.Phase==DroneReconPhase.Error)
                return Fail<Unit>(ErrorCode.InvalidState);
            if(fact.Kind<DroneReconFactKind.StepCompleted||fact.Kind>DroneReconFactKind.StepFailed)
                return Fail<Unit>(ErrorCode.InvalidInput);
            if(fact.Kind==DroneReconFactKind.StepCompleted)
            {
                if(completionFacts.TryGetValue(fact.StepId,out var previous))
                {
                    if(previous.Equals(fact))return Ok();
                    Error(ErrorCode.InvalidInput);return Fail<Unit>(ErrorCode.InvalidInput);
                }
                completionFacts.Add(fact.StepId,fact);
            }
            facts.Enqueue(fact);return Ok();
        }
        bool Current(DroneReconSceneFactDto fact) => fact.SessionId == snapshot.SessionId
            && fact.SequenceId == snapshot.SequenceId && fact.StepId == snapshot.StepId && fact.Phase == snapshot.Phase;
        bool Matches(string id) => !disposed && !string.IsNullOrEmpty(id) && id == snapshot.SessionId;
        bool ValidTime() => !double.IsNaN(clock.Now) && !double.IsInfinity(clock.Now) && clock.Now >= 0;
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        void Enter(DroneReconPhase phase)
        {
            completed = false; stepAge = 0; facts.Clear();
            Update(phase, snapshot.FeedAvailable, snapshot.TelemetryValid,
                snapshot.HeightAboveGroundMeters, snapshot.SpeedMetersPerSecond);
            if (phase == DroneReconPhase.Ready) return;
            var command = Command(phase);
            var result = phase == DroneReconPhase.RestoringPlayerView
                ? Call(() => scene.RestorePlayerView(command)) : Call(() => scene.PlayStep(command));
            if (!result.Success) Error();
        }
        DroneReconStepCommandDto Command(DroneReconPhase phase) => new DroneReconStepCommandDto
        {
            SessionId = snapshot.SessionId, SequenceId = snapshot.SequenceId, StepId = snapshot.StepId,
            Phase = phase, RouteId = config.RouteId, FeedBindingId = config.FeedBindingId,
            DurationSeconds = phase == DroneReconPhase.PlayerTakeoff ? config.TakeoffSeconds : config.LandingSeconds,
            FlightSpeedMetersPerSecond = config.FlightSpeedMetersPerSecond
        };
        void Error(ErrorCode code=ErrorCode.ResourceUnavailable)
        {
            failureCode=code;
            facts.Clear(); Update(DroneReconPhase.Error, false, false, 0f, 0f);
            Call(() => scene.SetCharacterActionsLocked(snapshot.SessionId, true));
            Call(() => scene.RestorePlayerView(Command(DroneReconPhase.Error)));
        }
        void Update(DroneReconPhase phase, bool feed, bool telemetry, float height, float speed, bool locked = true)
        {
            snapshot = new DroneReconSnapshotDto
            {
                SessionId = snapshot.SessionId, SequenceId = snapshot.SequenceId,
                StepId = snapshot.SequenceId + "." + phase, Revision = ++revision, Phase = phase,
                ViewMode = phase == DroneReconPhase.DroneRecon ? DroneReconViewMode.DroneFeed : DroneReconViewMode.Player,
                CharacterActionsLocked = locked, FeedAvailable = feed, FeedBindingId = config.FeedBindingId,
                ReconElapsedSeconds = (float)elapsed, TelemetryValid = telemetry,
                HeightAboveGroundMeters = height, SpeedMetersPerSecond = speed,
                ErrorCode = phase == DroneReconPhase.Error ? failureCode : ErrorCode.None
            };
            publishing = true;
            try { Changed?.Invoke(snapshot); } finally { publishing = false; }
        }
        static ServiceResult<Unit> Call(Func<ServiceResult<Unit>> command)
        { try { return command(); } catch (Exception) { return Fail<Unit>(ErrorCode.ResourceUnavailable); } }
        static ServiceResult<T> Fail<T>(ErrorCode error) => ServiceResult<T>.Fail(error);
        static ServiceResult<Unit> Ok() => ServiceResult<Unit>.Ok(Unit.Value);
        public void Dispose()
        {
            if (disposed) return;
            disposed=true;scene.FactReceived-=Receive;
            if (!string.IsNullOrEmpty(snapshot.SessionId)) Call(() => scene.StopAndReset(snapshot.SessionId, snapshot.SequenceId));
            facts.Clear();completionFacts.Clear();Changed = null;
        }
    }
}
