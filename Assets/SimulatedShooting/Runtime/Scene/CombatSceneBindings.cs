using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SimulatedShooting.Scene
{
    [Serializable]
    public sealed class CombatMapBinding
    {
        public string Id;
        public Vector2 Min;
        public Vector2 Max;
        public Texture2D Plan;
        public Vector2 WorldToMap(Vector3 world) => new Vector2((world.x - Min.x) / (Max.x - Min.x), (world.z - Min.y) / (Max.y - Min.y));
    }

    public sealed class CombatSceneBindings : MonoBehaviour
    {
        public const string TrenchSceneId = "TrenchScene";
        public const string UrbanSceneId = "UrbanScene";
        public Transform GeometryRoot;
        public Transform PlayerSpawn;
        public Transform TrenchEntry;
        public Transform UrbanEntry;
        public Transform[] TeammateSpawns;
        public Transform WeaponAnchor;
        public Transform BriefingAnchor;
        public Transform HudAnchor;
        public Transform ResultsAnchor;
        public Transform ProjectionAnchor;
        public Transform Drone;
        public CombatActorView EnemyPrefab;
        public CombatActorView TeammatePrefab;
        public CombatScenePoint[] Points = Array.Empty<CombatScenePoint>();
        public CombatDoorView[] Doors = Array.Empty<CombatDoorView>();
        public CombatMapBinding[] Maps = Array.Empty<CombatMapBinding>();
        public NavMeshData NavigationData;
        NavMeshDataInstance navigation;

        void OnEnable()
        {
            if (NavigationData != null) navigation = NavMesh.AddNavMeshData(NavigationData);
        }

        void OnDisable()
        {
            if (navigation.valid) navigation.Remove();
        }

        public List<string> ValidateBindings()
        {
            var errors = new List<string>();
            Check(GeometryRoot, nameof(GeometryRoot), errors);
            Check(PlayerSpawn, nameof(PlayerSpawn), errors);
            Check(TrenchEntry, nameof(TrenchEntry), errors);
            Check(UrbanEntry, nameof(UrbanEntry), errors);
            Check(WeaponAnchor, nameof(WeaponAnchor), errors);
            Check(BriefingAnchor, nameof(BriefingAnchor), errors);
            Check(HudAnchor, nameof(HudAnchor), errors);
            Check(ResultsAnchor, nameof(ResultsAnchor), errors);
            Check(ProjectionAnchor, nameof(ProjectionAnchor), errors);
            Check(Drone, nameof(Drone), errors);
            Check(EnemyPrefab, nameof(EnemyPrefab), errors);
            Check(TeammatePrefab, nameof(TeammatePrefab), errors);
            Check(NavigationData, nameof(NavigationData), errors);
            var ids = new HashSet<string>();
            foreach (var point in Points)
            {
                if (point == null) { errors.Add("Missing point"); continue; }
                if (string.IsNullOrWhiteSpace(point.Id)) errors.Add("Empty Id: " + point.name);
                else if (!ids.Add(point.Id)) errors.Add("Duplicate Id: " + point.Id);
                if (point.Volume == null) errors.Add("Missing volume: " + point.Id);
                if (point.Kind == CombatPointKind.Room && point.Door == null) errors.Add("Missing door: " + point.Id);
                if (string.IsNullOrEmpty(point.RegionId)) errors.Add("Missing region: " + point.Id);
                var map = Array.Find(Maps, m => m != null && m.Id == point.RegionId);
                if (map == null) errors.Add("Missing map: " + point.Id);
                else
                {
                    var uv = map.WorldToMap(point.transform.position);
                    if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) errors.Add("Outside map: " + point.Id);
                }
            }
            if (TeammateSpawns == null || TeammateSpawns.Length != 2) errors.Add("Expected two teammate spawns");
            else foreach (var spawn in TeammateSpawns) Check(spawn, "TeammateSpawn", errors);
            foreach (var door in Doors)
            {
                if (door == null) { errors.Add("Missing door"); continue; }
                Check(door.Hinge, "Door hinge: " + door.RoomId, errors);
                Check(door.Obstacle, "Door obstacle: " + door.RoomId, errors);
            }
            foreach (var map in Maps)
            {
                if (map == null) { errors.Add("Missing map"); continue; }
                if (map.Plan == null || map.Max.x <= map.Min.x || map.Max.y <= map.Min.y) errors.Add("Invalid map: " + map.Id);
                if (!ids.Add(map.Id)) errors.Add("Duplicate map Id: " + map.Id);
            }
            return errors;
        }

        static void Check(UnityEngine.Object value, string label, List<string> errors)
        {
            if (value == null) errors.Add("Missing " + label);
        }
    }
}
