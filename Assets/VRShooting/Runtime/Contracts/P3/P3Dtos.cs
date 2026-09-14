using System;
using System.Collections.Generic;
using UnityEngine;
using VRShooting.Contracts;

namespace VRShooting.Common
{
    public enum DifficultyLevel { Low = 0, Medium = 1, High = 2 }
    public enum RoomSearchState { Unsearched = 0, Searching = 1, Searched = 2 }
    public enum UrbanPhase { Street = 0, Building = 1, Results = 2 }
    public enum CombatInputKind { PlayerPose = 0, Posture = 1, Hit = 2, Perception = 3, AreaPresence = 4, NavigationResult = 5 }
    public enum CombatEntityRole { Player = 0, Teammate = 1, Enemy = 2 }
    public enum CombatEntityState { Spawned = 0, Idle = 1, Attacking = 2, Dead = 3 }
    public enum EncounterGroup { Trench = 0, Street = 1, Building = 2 }
    public enum SquadCommandType { Grenade = 0, FireSupport = 1, CoverReload = 2, MoveForward = 3, Stop = 4 }

    /// <summary>P3.Contracts.v1; see docs/接口文档/14-P3战斗契约与独立测试基础.md.</summary>
    public readonly struct TrenchMapDto
    {
        readonly string mapId;
        public string MapId { get => mapId ?? string.Empty; init => mapId = value ?? string.Empty; }
        readonly string displayName;
        public string DisplayName { get => displayName ?? string.Empty; init => displayName = value ?? string.Empty; }
        public DifficultyLevel Difficulty { get; init; }
        public int MinEnemyCount { get; init; }
        public int MaxEnemyCount { get; init; }
        readonly IReadOnlyList<MapAreaDto> enemyEstimateAreas;
        public IReadOnlyList<MapAreaDto> EnemyEstimateAreas { get => enemyEstimateAreas ?? Array.Empty<MapAreaDto>(); init => enemyEstimateAreas = ContractCollection.Copy(value); }
        readonly IReadOnlyList<SearchNodeDto> searchNodes;
        public IReadOnlyList<SearchNodeDto> SearchNodes { get => searchNodes ?? Array.Empty<SearchNodeDto>(); init => searchNodes = ContractCollection.Copy(value); }
        public static TrenchMapDto Empty => default;
    }

    /// <summary>P3.Contracts.v1; see docs/接口文档/14-P3战斗契约与独立测试基础.md.</summary>
    public readonly struct SearchNodeDto
    {
        readonly string nodeId;
        public string NodeId { get => nodeId ?? string.Empty; init => nodeId = value ?? string.Empty; }
        public Vector3 WorldPosition { get; init; }
        public bool Searched { get; init; }
        public static SearchNodeDto Empty => default;
    }

    /// <summary>P3.Contracts.v1; see docs/接口文档/14-P3战斗契约与独立测试基础.md.</summary>
    public readonly struct TrenchBriefingDto
    {
        readonly string mapId;
        public string MapId { get => mapId ?? string.Empty; init => mapId = value ?? string.Empty; }
        public SquadStatusDto PlannedSquad { get; init; }
        public MiniMapDto ProjectedMap { get; init; }
        public int EnemyEstimateMin { get; init; }
        public int EnemyEstimateMax { get; init; }
        public static TrenchBriefingDto Empty => default;
    }

    /// <summary>P3.Contracts.v1; see docs/接口文档/14-P3战斗契约与独立测试基础.md.</summary>
    public readonly struct TrenchSessionDto
    {
        readonly string sessionId;
        public string SessionId { get => sessionId ?? string.Empty; init => sessionId = value ?? string.Empty; }
        public long Revision { get; init; }
        readonly string mapId;
        public string MapId { get => mapId ?? string.Empty; init => mapId = value ?? string.Empty; }
        public SessionState State { get; init; }
        public int EnemyTotal { get; init; }
        public int EnemyKilled { get; init; }
        public float SearchProgress01 { get; init; }
        public AmmoDto Ammo { get; init; }
        public PlayerStatusDto Player { get; init; }
        public SquadStatusDto Squad { get; init; }
        public MiniMapDto MiniMap { get; init; }
        public static TrenchSessionDto Empty => default;
    }

    /// <summary>P3.Contracts.v1; see docs/接口文档/14-P3战斗契约与独立测试基础.md.</summary>
    public readonly struct TrenchResultDto
    {
        readonly string sessionId;
        public string SessionId { get => sessionId ?? string.Empty; init => sessionId = value ?? string.Empty; }
        public long Revision { get; init; }
        public bool Victory { get; init; }
        readonly string mapName;
        public string MapName { get => mapName ?? string.Empty; init => mapName = value ?? string.Empty; }
        public int EnemyKilled { get; init; }
        public int EnemyTotal { get; init; }
        public float SearchProgress01 { get; init; }
        public int RemainingAmmo { get; init; }
        public SquadStatusDto Squad { get; init; }
        public float ElapsedSeconds { get; init; }
        public MiniMapDto ResultMap { get; init; }
        public static TrenchResultDto Empty => default;
    }

    /// <summary>P3.Contracts.v1; see docs/接口文档/14-P3战斗契约与独立测试基础.md.</summary>
    public readonly struct UrbanMapDto
    {
        readonly string mapId;
        public string MapId { get => mapId ?? string.Empty; init => mapId = value ?? string.Empty; }
        readonly string displayName;
        public string DisplayName { get => displayName ?? string.Empty; init => displayName = value ?? string.Empty; }
        public int StreetEnemyMin { get; init; }
        public int StreetEnemyMax { get; init; }
        public int BuildingEnemyMin { get; init; }
        public int BuildingEnemyMax { get; init; }
        readonly IReadOnlyList<FloorDto> floors;
        public IReadOnlyList<FloorDto> Floors { get => floors ?? Array.Empty<FloorDto>(); init => floors = ContractCollection.Copy(value); }
        readonly string buildingEntranceId;
        public string BuildingEntranceId { get => buildingEntranceId ?? string.Empty; init => buildingEntranceId = value ?? string.Empty; }
        public static UrbanMapDto Empty => default;
    }

    /// <summary>P3.Contracts.v1; see docs/接口文档/14-P3战斗契约与独立测试基础.md.</summary>
    public readonly struct FloorDto
    {
        readonly string floorId;
        public string FloorId { get => floorId ?? string.Empty; init => floorId = value ?? string.Empty; }
        readonly string displayName;
        public string DisplayName { get => displayName ?? string.Empty; init => displayName = value ?? string.Empty; }
        readonly IReadOnlyList<RoomDto> rooms;
        public IReadOnlyList<RoomDto> Rooms { get => rooms ?? Array.Empty<RoomDto>(); init => rooms = ContractCollection.Copy(value); }
        public MiniMapDto MiniMap { get; init; }
        public static FloorDto Empty => default;
    }

    /// <summary>P3.Contracts.v1; see docs/接口文档/14-P3战斗契约与独立测试基础.md.</summary>
    public readonly struct RoomDto
    {
        readonly string roomId;
        public string RoomId { get => roomId ?? string.Empty; init => roomId = value ?? string.Empty; }
        readonly string displayName;
        public string DisplayName { get => displayName ?? string.Empty; init => displayName = value ?? string.Empty; }
        public RoomSearchState SearchState { get; init; }
        public bool HasPossibleEnemyArea { get; init; }
        public bool DoorOpen { get; init; }
        public static RoomDto Empty => default;
    }

    /// <summary>P3.Contracts.v1; see docs/接口文档/14-P3战斗契约与独立测试基础.md.</summary>
    public readonly struct UrbanSessionDto
    {
        readonly string sessionId;
        public string SessionId { get => sessionId ?? string.Empty; init => sessionId = value ?? string.Empty; }
        public long Revision { get; init; }
        readonly string mapId;
        public string MapId { get => mapId ?? string.Empty; init => mapId = value ?? string.Empty; }
        public SessionState State { get; init; }
        public UrbanPhase Phase { get; init; }
        public bool StreetCleared { get; init; }
        public int StreetEnemyKilled { get; init; }
        public int StreetEnemyTotal { get; init; }
        public int BuildingEnemyKilled { get; init; }
        public int BuildingEnemyTotal { get; init; }
        public int RoomsSearched { get; init; }
        public int RoomsTotal { get; init; }
        public AmmoDto Ammo { get; init; }
        public PlayerStatusDto Player { get; init; }
        public SquadStatusDto Squad { get; init; }
        public MiniMapDto MiniMap { get; init; }
        readonly string currentFloorId;
        public string CurrentFloorId { get => currentFloorId ?? string.Empty; init => currentFloorId = value ?? string.Empty; }
        readonly IReadOnlyList<FloorDto> floors;
        public IReadOnlyList<FloorDto> Floors { get => floors ?? Array.Empty<FloorDto>(); init => floors = ContractCollection.Copy(value); }
        public static UrbanSessionDto Empty => default;
    }

    /// <summary>P3.Contracts.v1; see docs/接口文档/14-P3战斗契约与独立测试基础.md.</summary>
    public readonly struct UrbanResultDto
    {
        readonly string sessionId;
        public string SessionId { get => sessionId ?? string.Empty; init => sessionId = value ?? string.Empty; }
        public long Revision { get; init; }
        public bool Victory { get; init; }
        public bool StreetCleared { get; init; }
        public float BuildingSearchProgress01 { get; init; }
        public int RoomsSearched { get; init; }
        public int RoomsTotal { get; init; }
        public int EnemyKilled { get; init; }
        public int EnemyTotal { get; init; }
        public int RemainingAmmo { get; init; }
        public SquadStatusDto Squad { get; init; }
        public MiniMapDto ResultMap { get; init; }
        readonly IReadOnlyList<MiniMapDto> floorMaps;
        public IReadOnlyList<MiniMapDto> FloorMaps { get => floorMaps ?? Array.Empty<MiniMapDto>(); init => floorMaps = ContractCollection.Copy(value); }
        public static UrbanResultDto Empty => default;
    }

    /// <summary>P3.Contracts.v1; see docs/接口文档/14-P3战斗契约与独立测试基础.md.</summary>
    public readonly struct CombatInputDto
    {
        readonly string sessionId;
        public string SessionId { get => sessionId ?? string.Empty; init => sessionId = value ?? string.Empty; }
        readonly string eventId;
        public string EventId { get => eventId ?? string.Empty; init => eventId = value ?? string.Empty; }
        public long Tick { get; init; }
        public CombatInputKind Kind { get; init; }
        readonly string entityId;
        public string EntityId { get => entityId ?? string.Empty; init => entityId = value ?? string.Empty; }
        readonly string targetId;
        public string TargetId { get => targetId ?? string.Empty; init => targetId = value ?? string.Empty; }
        readonly string shotId;
        public string ShotId { get => shotId ?? string.Empty; init => shotId = value ?? string.Empty; }
        public Vector3 Position { get; init; }
        public Vector3 Direction { get; init; }
        public float Value { get; init; }
        public bool Flag { get; init; }
        public static CombatInputDto Empty => default;
    }

    /// <summary>P3.Contracts.v1; see docs/接口文档/14-P3战斗契约与独立测试基础.md.</summary>
    public readonly struct CombatEntityVisualDto
    {
        readonly string entityId;
        public string EntityId { get => entityId ?? string.Empty; init => entityId = value ?? string.Empty; }
        public CombatEntityRole Role { get; init; }
        public CombatEntityState State { get; init; }
        public Vector3 Position { get; init; }
        public Vector3 Forward { get; init; }
        public bool CorpseVisible { get; init; }
        public static CombatEntityVisualDto Empty => default;
    }

    /// <summary>P3.Contracts.v1; see docs/接口文档/14-P3战斗契约与独立测试基础.md.</summary>
    public readonly struct CombatDoorVisualDto
    {
        readonly string roomId;
        public string RoomId { get => roomId ?? string.Empty; init => roomId = value ?? string.Empty; }
        public bool Open { get; init; }
        public static CombatDoorVisualDto Empty => default;
    }

    /// <summary>P3.Contracts.v1; see docs/接口文档/14-P3战斗契约与独立测试基础.md.</summary>
    public readonly struct CombatVisualSnapshotDto
    {
        readonly string sessionId;
        public string SessionId { get => sessionId ?? string.Empty; init => sessionId = value ?? string.Empty; }
        public long Revision { get; init; }
        readonly IReadOnlyList<CombatEntityVisualDto> entities;
        public IReadOnlyList<CombatEntityVisualDto> Entities { get => entities ?? Array.Empty<CombatEntityVisualDto>(); init => entities = ContractCollection.Copy(value); }
        readonly IReadOnlyList<CombatDoorVisualDto> doors;
        public IReadOnlyList<CombatDoorVisualDto> Doors { get => doors ?? Array.Empty<CombatDoorVisualDto>(); init => doors = ContractCollection.Copy(value); }
        public static CombatVisualSnapshotDto Empty => default;
    }

    /// <summary>P3.Contracts.v1; see docs/接口文档/14-P3战斗契约与独立测试基础.md.</summary>
    public readonly struct CombatNavigationRequestDto
    {
        readonly string sessionId;
        public string SessionId { get => sessionId ?? string.Empty; init => sessionId = value ?? string.Empty; }
        readonly string requestId;
        public string RequestId { get => requestId ?? string.Empty; init => requestId = value ?? string.Empty; }
        readonly string entityId;
        public string EntityId { get => entityId ?? string.Empty; init => entityId = value ?? string.Empty; }
        public long Tick { get; init; }
        public Vector3 Destination { get; init; }
        public Vector3 Forward { get; init; }
        public static CombatNavigationRequestDto Empty => default;
    }

    /// <summary>P3.Contracts.v1; see docs/接口文档/14-P3战斗契约与独立测试基础.md.</summary>
    public readonly struct CombatPlayerSnapshotDto
    {
        readonly string sessionId;
        public string SessionId { get => sessionId ?? string.Empty; init => sessionId = value ?? string.Empty; }
        public long Revision { get; init; }
        public PlayerStatusDto Player { get; init; }
        public static CombatPlayerSnapshotDto Empty => default;
    }

    /// <summary>P3.Contracts.v1; see docs/接口文档/14-P3战斗契约与独立测试基础.md.</summary>
    public readonly struct CombatSquadSnapshotDto
    {
        readonly string sessionId;
        public string SessionId { get => sessionId ?? string.Empty; init => sessionId = value ?? string.Empty; }
        public long Revision { get; init; }
        public SquadStatusDto Squad { get; init; }
        public static CombatSquadSnapshotDto Empty => default;
    }

    /// <summary>P3.Contracts.v1; see docs/接口文档/14-P3战斗契约与独立测试基础.md.</summary>
    public readonly struct CombatSummaryDto
    {
        readonly string sessionId;
        public string SessionId { get => sessionId ?? string.Empty; init => sessionId = value ?? string.Empty; }
        public TrainingMode Mode { get; init; }
        readonly string mapId;
        public string MapId { get => mapId ?? string.Empty; init => mapId = value ?? string.Empty; }
        public bool Victory { get; init; }
        public float ElapsedSeconds { get; init; }
        public int RemainingAmmo { get; init; }
        public DateTime CompletedAtUtc { get; init; }
        public static CombatSummaryDto Empty => default;
    }

    /// <summary>P3.Contracts.v1; see docs/接口文档/14-P3战斗契约与独立测试基础.md.</summary>
    public readonly struct SceneSpawnPointDto
    {
        readonly string pointId;
        public string PointId { get => pointId ?? string.Empty; init => pointId = value ?? string.Empty; }
        readonly string estimateAreaId;
        public string EstimateAreaId { get => estimateAreaId ?? string.Empty; init => estimateAreaId = value ?? string.Empty; }
        public EncounterGroup Group { get; init; }
        readonly string floorId;
        public string FloorId { get => floorId ?? string.Empty; init => floorId = value ?? string.Empty; }
        readonly string roomId;
        public string RoomId { get => roomId ?? string.Empty; init => roomId = value ?? string.Empty; }
        public Vector3 WorldPosition { get; init; }
        public Vector3 EstimatePosition { get; init; }
        public bool Navigable { get; init; }
        public static SceneSpawnPointDto Empty => default;
    }

    /// <summary>P3.Contracts.v1; see docs/接口文档/14-P3战斗契约与独立测试基础.md.</summary>
    public readonly struct MapProjectionDto
    {
        readonly string floorId;
        public string FloorId { get => floorId ?? string.Empty; init => floorId = value ?? string.Empty; }
        readonly string resourceKey;
        public string ResourceKey { get => resourceKey ?? string.Empty; init => resourceKey = value ?? string.Empty; }
        public Rect WorldBoundsXZ { get; init; }
        public static MapProjectionDto Empty => default;
    }

    /// <summary>P3.Contracts.v1; see docs/接口文档/14-P3战斗契约与独立测试基础.md.</summary>
    public readonly struct SquadCommandRequest
    {
        readonly string sessionId;
        public string SessionId { get => sessionId ?? string.Empty; init => sessionId = value ?? string.Empty; }
        public SquadCommandType CommandType { get; init; }
        readonly string targetAreaId;
        public string TargetAreaId { get => targetAreaId ?? string.Empty; init => targetAreaId = value ?? string.Empty; }
        public Vector3? TargetWorldPosition { get; init; }
        public static SquadCommandRequest Empty => default;
    }

    /// <summary>P3.Contracts.v1; see docs/接口文档/14-P3战斗契约与独立测试基础.md.</summary>
    public readonly struct SquadCommandResult
    {
        readonly string commandId;
        public string CommandId { get => commandId ?? string.Empty; init => commandId = value ?? string.Empty; }
        public SquadCommandType CommandType { get; init; }
        public bool Accepted { get; init; }
        readonly string rejectReason;
        public string RejectReason { get => rejectReason ?? string.Empty; init => rejectReason = value ?? string.Empty; }
        public SquadStatusDto SquadStatus { get; init; }
        public static SquadCommandResult Empty => default;
    }

}

