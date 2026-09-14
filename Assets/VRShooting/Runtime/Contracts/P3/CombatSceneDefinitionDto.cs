using System;
using System.Collections.Generic;
using UnityEngine;
using VRShooting.Contracts;

namespace VRShooting.Common
{
    /// <summary>Scene data only. Does not generate actors or run navigation.</summary>
    public readonly struct CombatSceneDefinitionDto
    {
        readonly string mapId, sceneId, entranceId;
        readonly IReadOnlyList<MapProjectionDto> projections;
        readonly IReadOnlyList<SceneSpawnPointDto> spawnPoints;
        readonly IReadOnlyList<SearchNodeDto> searchNodes;
        readonly IReadOnlyList<FloorDto> floors;
        public string MapId { get => mapId ?? string.Empty; init => mapId = value ?? string.Empty; }
        public string SceneId { get => sceneId ?? string.Empty; init => sceneId = value ?? string.Empty; }
        public string EntranceId { get => entranceId ?? string.Empty; init => entranceId = value ?? string.Empty; }
        public Vector3? EntranceWorldPosition { get; init; }
        public TrainingMode Mode { get; init; }
        public IReadOnlyList<MapProjectionDto> Projections { get => projections ?? Array.Empty<MapProjectionDto>(); init => projections = ContractCollection.Copy(value); }
        public IReadOnlyList<SceneSpawnPointDto> SpawnPoints { get => spawnPoints ?? Array.Empty<SceneSpawnPointDto>(); init => spawnPoints = ContractCollection.Copy(value); }
        public IReadOnlyList<SearchNodeDto> SearchNodes { get => searchNodes ?? Array.Empty<SearchNodeDto>(); init => searchNodes = ContractCollection.Copy(value); }
        public IReadOnlyList<FloorDto> Floors { get => floors ?? Array.Empty<FloorDto>(); init => floors = ContractCollection.Copy(value); }

        public ServiceResult<Unit> Validate() => Validate(CombatConfigDto.Default);
        public ServiceResult<Unit> Validate(CombatConfigDto config)
        {
            var settings = config.Validate();
            if (!settings.Success) return settings;
            var trench = Mode == TrainingMode.Trench;
            if (!trench && Mode != TrainingMode.Urban) return Invalid("Mode");
            if (MapId != (trench ? P3ContractIds.TrenchMap : P3ContractIds.UrbanMap)) return Invalid("MapId");
            if (SceneId != (trench ? P3ContractIds.TrenchScene : P3ContractIds.UrbanScene)) return Invalid("SceneId");
            if (Projections.Count == 0) return Invalid("Projections");
            var projectionFloors = new HashSet<string>(StringComparer.Ordinal);
            foreach (var projection in Projections)
            {
                var bounds = projection.WorldBoundsXZ;
                if (string.IsNullOrWhiteSpace(projection.ResourceKey) || !projectionFloors.Add(projection.FloorId)
                    || !Finite(bounds.x) || !Finite(bounds.y) || !Finite(bounds.width) || !Finite(bounds.height)
                    || bounds.width <= 0 || bounds.height <= 0) return Invalid("Projection bounds/key/floor");
            }
            var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
            var roomFloors = new Dictionary<string, string>(StringComparer.Ordinal);
            var floorIds = new HashSet<string>(StringComparer.Ordinal);
            if (trench)
            {
                if (SearchNodes.Count == 0 || Floors.Count != 0) return Invalid("Trench search nodes/floors");
                foreach (var node in SearchNodes)
                    if (!AddId(uniqueIds, node.NodeId) || !Finite(node.WorldPosition) || !InsideProjection(node.WorldPosition, string.Empty)) return Invalid("SearchNode");
            }
            else
            {
                if (!AddId(uniqueIds, EntranceId) || Floors.Count != 3) return Invalid("Entrance/three floors");
                if(EntranceWorldPosition.HasValue&&(!Finite(EntranceWorldPosition.Value)||!InsideProjection(EntranceWorldPosition.Value,string.Empty)))return Invalid("Entrance position");
                foreach (var floor in Floors)
                {
                    if (!AddId(uniqueIds, floor.FloorId) || !projectionFloors.Contains(floor.FloorId)) return Invalid("Floor/projection");
                    floorIds.Add(floor.FloorId);
                    foreach (var room in floor.Rooms)
                    {
                        if (!AddId(uniqueIds, room.RoomId)) return Invalid("RoomId");
                        if(room.MapPosition.HasValue)
                        {
                            var p=room.MapPosition.Value;
                            if(!Finite(p.x)||!Finite(p.y)||p.x<0||p.y<0||p.x>1||p.y>1)return Invalid("Room map position");
                        }
                        roomFloors.Add(room.RoomId, floor.FloorId);
                    }
                }
                if (roomFloors.Count < 3 || roomFloors.Count > 5) return Invalid("Total rooms must be 3-5");
            }
            // Check identifiers before capacity so duplicates remain a configuration error.
            foreach (var point in SpawnPoints)
                if (!AddId(uniqueIds, point.PointId) || string.IsNullOrWhiteSpace(point.EstimateAreaId)) return Invalid("Spawn ID/estimate");
            var counts = new int[3];
            foreach (var point in SpawnPoints)
            {
                if (!Enum.IsDefined(typeof(EncounterGroup), point.Group)) return Invalid("EncounterGroup");
                if (trench != (point.Group == EncounterGroup.Trench)) return Invalid("Spawn group/mode");
                if (point.Group == EncounterGroup.Building)
                {
                    if (!floorIds.Contains(point.FloorId)) return Invalid("Spawn FloorId");
                    if (point.RoomId.Length > 0 && (!roomFloors.TryGetValue(point.RoomId, out var floor) || floor != point.FloorId)) return Invalid("Spawn RoomId");
                }
                else if (point.FloorId.Length > 0 || point.RoomId.Length > 0) return Invalid("Outdoor spawn floor/room");
                if (!Finite(point.WorldPosition) || !Finite(point.EstimatePosition)) return Invalid("Spawn position");
                if (!point.Navigable || Vector3.Distance(point.WorldPosition, point.EstimatePosition) > config.SpawnOffsetRadius
                    || !InsideProjection(point.WorldPosition, point.FloorId) || !InsideProjection(point.EstimatePosition, point.FloorId))
                    return ServiceResult<Unit>.Fail(ErrorCode.ResourceUnavailable, "Invalid spawn candidate: " + point.PointId);
                counts[(int)point.Group]++;
            }
            if (trench ? counts[0] < 5 : counts[1] < 2 || counts[2] < 6)
                return ServiceResult<Unit>.Fail(ErrorCode.ResourceUnavailable, "Insufficient spawn candidates for maximum configured encounter");
            return ServiceResult<Unit>.Ok(Unit.Value);
        }

        public CombatSceneDefinitionDto WithSpawnPoints(IReadOnlyList<SceneSpawnPointDto> points) => new CombatSceneDefinitionDto
        {
            MapId = MapId, SceneId = SceneId, Mode = Mode, EntranceId = EntranceId,
            Projections = Projections, SearchNodes = SearchNodes, Floors = Floors, SpawnPoints = points, EntranceWorldPosition=EntranceWorldPosition
        };
        bool InsideProjection(Vector3 point, string floorId)
        {
            foreach (var projection in Projections)
                if (projection.FloorId == floorId && projection.WorldBoundsXZ.Contains(new Vector2(point.x, point.z))) return true;
            return false;
        }
        static bool AddId(HashSet<string> values, string id) => !string.IsNullOrWhiteSpace(id) && values.Add(id);
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
        static ServiceResult<Unit> Invalid(string field) => ServiceResult<Unit>.Fail(ErrorCode.InvalidInput, "Invalid scene configuration: " + field);
    }

    public static class P3ContractIds
    {
        public const string Version = "P3.Contracts.v1";
        public const string TrenchMap = "trench-a";
        public const string UrbanMap = "urban-a";
        public const string TrenchScene = "TrenchScene";
        public const string UrbanScene = "UrbanScene";
        public const string UrbanEntrance = "urban-a.entrance";
        public const string TrainingWeapon = "training-rifle";
        public static string SceneAnchor(bool urban, string slot) => (urban ? "Urban" : "Trench") + ".Anchor." + slot;
        public static string Button(ScreenId screen, string action) => "Button_" + screen + "_" + action;
    }
}
