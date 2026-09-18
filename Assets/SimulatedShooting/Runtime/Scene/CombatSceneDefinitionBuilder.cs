using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using VRShooting.Common;

namespace SimulatedShooting.Scene
{
    public static class CombatSceneDefinitionBuilder
    {
        public static CombatSceneDefinitionDto Build(CombatSceneBindings binding, TrainingMode mode)
        {
            bool trench = mode == TrainingMode.Trench;
            string map = trench ? P3ContractIds.TrenchMap : P3ContractIds.UrbanMap;
            var points = binding.Points.Where(p => p != null && p.RegionId == map).ToArray();
            var projections = binding.Maps.Where(m => m.Id == map || (!trench && m.Id.StartsWith("urban-" ) && m.Id.EndsWith("f")))
                .Select(m => new MapProjectionDto { FloorId = m.Id == map ? "" : "floor-" + m.Id[6],
                    ResourceKey = m.Id, WorldBoundsXZ = Rect.MinMaxRect(m.Min.x,m.Min.y,m.Max.x,m.Max.y) }).ToArray();
            var floors = trench ? Array.Empty<FloorDto>() : points.Where(p => p.Kind == CombatPointKind.Floor).OrderBy(p => p.FloorId)
                .Select(f => new FloorDto { FloorId = f.FloorId, DisplayName = f.FloorId,
                    Rooms = points.Where(r => r.Kind == CombatPointKind.Room && r.FloorId == f.FloorId)
                        .Select(r => new RoomDto { RoomId = r.RoomId, DisplayName = r.RoomId,
                            MapPosition = binding.Maps.First(m => m.Id == "urban-" + f.FloorId.Last() + "f").WorldToMap(r.transform.position) }).ToArray() }).ToArray();
            return new CombatSceneDefinitionDto { MapId = map, SceneId = trench ? P3ContractIds.TrenchScene : P3ContractIds.UrbanScene,
                Mode = mode, Projections = projections, Floors = floors,
                PlayerSpawnPosition=(trench?binding.TrenchEntry:binding.UrbanEntry).position,
                PlayerSpawnForward=(trench?binding.TrenchEntry:binding.UrbanEntry).forward,
                EntranceId = trench ? "" : P3ContractIds.UrbanEntrance,
                EntranceWorldPosition = trench ? (Vector3?)null : points.First(p => p.Kind == CombatPointKind.Entrance).transform.position,
                SearchNodes = points.Where(p => p.Kind == CombatPointKind.SearchNode).Select(p => new SearchNodeDto { NodeId=p.Id, WorldPosition=p.transform.position }).ToArray(),
                SpawnPoints = points.Where(p => p.Kind == CombatPointKind.EnemySpawn).Select(p => new SceneSpawnPointDto {
                    PointId=p.Id, EstimateAreaId=p.Id+".estimate", Group=trench ? EncounterGroup.Trench : string.IsNullOrEmpty(p.FloorId) ? EncounterGroup.Street : EncounterGroup.Building,
                    FloorId=p.FloorId, RoomId=p.RoomId, WorldPosition=p.transform.position,
                    EstimatePosition=p.EstimateAnchor!=null?p.EstimateAnchor.position:throw new InvalidOperationException("Missing estimate anchor: "+p.Id),
                    Navigable=NavMesh.SamplePosition(p.transform.position,out var hit,.75f,NavMesh.AllAreas) && Mathf.Abs(hit.position.y-p.transform.position.y)<.75f }).ToArray() };
        }
    }
}
