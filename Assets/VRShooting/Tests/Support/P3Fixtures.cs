using System;
using System.Collections.Generic;
using UnityEngine;
using VRShooting.Common;

namespace VRShooting.P3.TestSupport
{
    /// <summary>Test data only. No scene/AI/session services are created here.</summary>
    public static class P3Fixtures
    {
        public static SquadStatusDto Squad(string sessionId) => new SquadStatusDto
        {
            Members = Array.AsReadOnly(new[]
            {
                new SquadMemberDto { MemberId = sessionId + ".player", Role = SquadMemberRole.Player, Health = 100 },
                new SquadMemberDto { MemberId = sessionId + ".teammate-2", Role = SquadMemberRole.TeammateTwo, Health = 100, State = SquadMemberState.Following, WorldPosition = Vector3.back * 1.5f },
                new SquadMemberDto { MemberId = sessionId + ".teammate-3", Role = SquadMemberRole.TeammateThree, Health = 100, State = SquadMemberState.Following, WorldPosition = Vector3.back * 3f }
            }),
            CommandMenuAvailable = false
        };
        public static MiniMapDto MiniMap(string mapId) => new MiniMapDto
        {
            MapId = mapId, Visible = true,
            Markers = Array.AsReadOnly(new[] { new MapMarkerDto { MarkerId = mapId + ".estimate-001", Type = MarkerType.EnemyEstimate, NormalizedPosition = new Vector2(0.5f, 0.5f) } }),
            Areas = Array.Empty<MapAreaDto>()
        };
        public static TrenchMapDto TrenchMap => new TrenchMapDto
        {
            MapId = P3ContractIds.TrenchMap, DisplayName = "堑壕地图 A", Difficulty = DifficultyLevel.Medium,
            MinEnemyCount = 3, MaxEnemyCount = 5, SearchNodes = TrenchDefinition.SearchNodes,
            EnemyEstimateAreas = TrenchEstimateAreas()
        };
        public static UrbanMapDto UrbanMap => new UrbanMapDto
        {
            MapId = P3ContractIds.UrbanMap, DisplayName = "城镇地图 A", BuildingEntranceId = P3ContractIds.UrbanEntrance,
            StreetEnemyMin = 1, StreetEnemyMax = 2, BuildingEnemyMin = 3, BuildingEnemyMax = 6, Floors = UrbanDefinition.Floors
        };
        public static TrenchSessionDto TrenchSession(string id, long revision, SessionState state = SessionState.Running) => new TrenchSessionDto
        {
            SessionId = id, Revision = revision, MapId = P3ContractIds.TrenchMap, State = state, EnemyTotal = 3,
            Ammo = CombatConfigDto.Default.InitialAmmo, Player = PlayerStatusDto.Default, Squad = Squad(id), MiniMap = MiniMap(P3ContractIds.TrenchMap)
        };
        public static UrbanSessionDto UrbanSession(string id, long revision, UrbanPhase phase = UrbanPhase.Street) => new UrbanSessionDto
        {
            SessionId = id, Revision = revision, MapId = P3ContractIds.UrbanMap, State = SessionState.Running, Phase = phase,
            StreetEnemyTotal = 2, BuildingEnemyTotal = 3, RoomsTotal = 3, Ammo = CombatConfigDto.Default.InitialAmmo,
            Player = PlayerStatusDto.Default, Squad = Squad(id), MiniMap = MiniMap(P3ContractIds.UrbanMap),
            CurrentFloorId = phase == UrbanPhase.Building ? "urban-a.floor-1" : string.Empty, Floors = UrbanDefinition.Floors
        };
        public static CombatSceneDefinitionDto TrenchDefinition => new CombatSceneDefinitionDto
        {
            MapId = P3ContractIds.TrenchMap, SceneId = P3ContractIds.TrenchScene, Mode = TrainingMode.Trench,
            Projections = new[] { Projection(string.Empty, "trench-a.map") },
            SearchNodes = new[] { new SearchNodeDto { NodeId = "trench-a.node-001", WorldPosition = Vector3.zero } },
            SpawnPoints = Spawns(false)
        };
        public static CombatSceneDefinitionDto UrbanDefinition
        {
            get
            {
                var floors = new List<FloorDto>();
                var projections = new List<MapProjectionDto> { Projection(string.Empty, "urban-a.street") };
                for (var i = 1; i <= 3; i++)
                {
                    var floorId = "urban-a.floor-" + i;
                    projections.Add(Projection(floorId, floorId + ".map"));
                    floors.Add(new FloorDto { FloorId = floorId, DisplayName = i + "F", MiniMap = MiniMap(floorId),
                        Rooms = new[] { new RoomDto { RoomId = "urban-a.room-" + i.ToString("000"), DisplayName = "房间" + i } } });
                }
                return new CombatSceneDefinitionDto
                {
                    MapId = P3ContractIds.UrbanMap, SceneId = P3ContractIds.UrbanScene, Mode = TrainingMode.Urban,
                    EntranceId = P3ContractIds.UrbanEntrance, Projections = projections, Floors = floors, SpawnPoints = Spawns(true)
                };
            }
        }
        static MapProjectionDto Projection(string floorId, string resourceKey) => new MapProjectionDto
        {
            FloorId = floorId, ResourceKey = resourceKey, WorldBoundsXZ = new Rect(-10, -10, 30, 30)
        };
        static IReadOnlyList<MapAreaDto> TrenchEstimateAreas()
        {
            var areas = new List<MapAreaDto>();
            foreach (var point in TrenchDefinition.SpawnPoints)
            {
                // Project the estimate center, never the random/actual spawn offset.
                var x = (point.EstimatePosition.x + 10f) / 30f;
                var y = (point.EstimatePosition.z + 10f) / 30f;
                areas.Add(new MapAreaDto { AreaId = point.EstimateAreaId, Label = "敌情预估", NormalizedRect = new Rect(x - 0.03f, y - 0.03f, 0.06f, 0.06f) });
            }
            return areas.AsReadOnly();
        }
        static IReadOnlyList<SceneSpawnPointDto> Spawns(bool urban)
        {
            var list = new List<SceneSpawnPointDto>();
            var map = urban ? P3ContractIds.UrbanMap : P3ContractIds.TrenchMap;
            for (var i = 0; i < (urban ? 8 : 5); i++)
            {
                var building = urban && i >= 2;
                var floor = building ? (i - 2) / 2 + 1 : 0;
                var estimate = new Vector3(i, floor * 3, i);
                list.Add(new SceneSpawnPointDto
                {
                    PointId = map + ".spawn-" + (i + 1).ToString("000"), EstimateAreaId = map + ".estimate-" + (i + 1).ToString("000"),
                    Group = urban ? (building ? EncounterGroup.Building : EncounterGroup.Street) : EncounterGroup.Trench,
                    FloorId = building ? "urban-a.floor-" + floor : string.Empty,
                    RoomId = building ? "urban-a.room-" + floor.ToString("000") : string.Empty,
                    EstimatePosition = estimate, WorldPosition = estimate + Vector3.right * 0.5f, Navigable = true
                });
            }
            return list.AsReadOnly();
        }
    }
}
