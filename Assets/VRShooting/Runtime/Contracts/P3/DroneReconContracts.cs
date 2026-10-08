using System;
using VRShooting.Contracts;

namespace VRShooting.Common
{
    public enum DroneReconPhase
    {
        NotStarted, PlayerTakeoff, DroneRecon, DroneReturn,
        DroneLanding, RestoringPlayerView, Ready, Cancelled, Error
    }
    public enum DroneReconViewMode { Player, DroneFeed }
    public enum DroneReconFactKind { StepCompleted, FeedState, Telemetry, StepFailed }

    public readonly struct DroneReconSnapshotDto
    {
        public string SessionId { get; init; }
        public long Revision { get; init; }
        public string SequenceId { get; init; }
        public string StepId { get; init; }
        public DroneReconPhase Phase { get; init; }
        public DroneReconViewMode ViewMode { get; init; }
        public bool CharacterActionsLocked { get; init; }
        public bool FeedAvailable { get; init; }
        public string FeedBindingId { get; init; }
        public float ReconElapsedSeconds { get; init; }
        public bool TelemetryValid { get; init; }
        public float HeightAboveGroundMeters { get; init; }
        public float SpeedMetersPerSecond { get; init; }
        public ErrorCode ErrorCode { get; init; }
    }

    public readonly struct DroneReconStepCommandDto
    {
        public string SessionId { get; init; }
        public string SequenceId { get; init; }
        public string StepId { get; init; }
        public DroneReconPhase Phase { get; init; }
        public string RouteId { get; init; }
        public float DurationSeconds { get; init; }
        public float FlightSpeedMetersPerSecond { get; init; }
        public string FeedBindingId { get; init; }
    }

    public readonly struct DroneReconSceneFactDto
    {
        public string SessionId { get; init; }
        public string SequenceId { get; init; }
        public string StepId { get; init; }
        public DroneReconPhase Phase { get; init; }
        public DroneReconFactKind Kind { get; init; }
        public bool FeedAvailable { get; init; }
        public string FeedBindingId { get; init; }
        public bool TelemetryValid { get; init; }
        public float HeightAboveGroundMeters { get; init; }
        public float SpeedMetersPerSecond { get; init; }
        public ErrorCode ErrorCode { get; init; }
        public string Reason { get; init; }
    }

    public readonly struct DroneReconConfigDto
    {
        public string RouteId { get; init; }
        public string FeedBindingId { get; init; }
        public float TakeoffSeconds { get; init; }
        public float LandingSeconds { get; init; }
        public float FlightSpeedMetersPerSecond { get; init; }
        public float StepTimeoutSeconds { get; init; }
        public float MaximumOpeningSeconds { get; init; }
        public static DroneReconConfigDto Default => new DroneReconConfigDto
        {
            RouteId = "mode3.trench-recon", FeedBindingId = "mode3.drone-feed",
            TakeoffSeconds = 3f, LandingSeconds = 2f, FlightSpeedMetersPerSecond = 2.5f,
            StepTimeoutSeconds = 120f, MaximumOpeningSeconds = 20f
        };
        public ServiceResult<Unit> Validate()
        {
            if (string.IsNullOrWhiteSpace(RouteId) || string.IsNullOrWhiteSpace(FeedBindingId)
                || !Positive(TakeoffSeconds) || !Positive(LandingSeconds)
                || !Positive(FlightSpeedMetersPerSecond) || !Positive(StepTimeoutSeconds)
                || MaximumOpeningSeconds<0f || float.IsNaN(MaximumOpeningSeconds) || float.IsInfinity(MaximumOpeningSeconds)
                || StepTimeoutSeconds < Math.Max(TakeoffSeconds, LandingSeconds))
                return ServiceResult<Unit>.Fail(ErrorCode.InvalidInput);
            return ServiceResult<Unit>.Ok(Unit.Value);
        }
        static bool Positive(float value) => value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

namespace VRShooting.Application
{
    using VRShooting.Common;

    public interface IDroneReconService
    {
        ServiceResult<DroneReconSnapshotDto> Begin(string sessionId);
        ServiceResult<DroneReconSnapshotDto> GetSnapshot(string sessionId);
        ServiceResult<Unit> Advance(string sessionId);
        ServiceResult<Unit> Cancel(string sessionId);
        event Action<DroneReconSnapshotDto> Changed;
    }

    // Texture, Camera and Transform references stay in the Unity adapter, outside these contracts.
    public interface IDroneReconScenePort
    {
        ServiceResult<Unit> Prepare(DroneReconStepCommandDto command);
        ServiceResult<Unit> PlayStep(DroneReconStepCommandDto command);
        ServiceResult<Unit> SetCharacterActionsLocked(string sessionId, bool locked);
        ServiceResult<Unit> SetSuspended(string sessionId, bool suspended);
        ServiceResult<Unit> RestorePlayerView(DroneReconStepCommandDto command);
        ServiceResult<Unit> StopAndReset(string sessionId, string sequenceId);
        event Action<DroneReconSceneFactDto> FactReceived;
    }
}
