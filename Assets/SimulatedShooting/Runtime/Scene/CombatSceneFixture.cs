using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SimulatedShooting.Scene
{
    public sealed class CombatSceneFixture : MonoBehaviour
    {
        public CombatSceneBindings Bindings;
        public CombatSceneWalker Walker;
        public Font InspectionFont;
        public List<CombatActorView> Actors { get; } = new List<CombatActorView>();
        public string LastFact { get; private set; } = "場景巡檢已就緒";
        int sequence;
        int mapIndex;
        bool showMap;

        void Awake()
        {
            if(UnityCombatSceneLoader.PreparingProductionScene)enabled=false;
        }

        void Start()
        {
            foreach (var p in Bindings.Points) p.FactReported += OnFact;
            ResetFixture();
        }

        void OnDestroy()
        {
            foreach (var p in Bindings.Points) if (p != null) p.FactReported -= OnFact;
        }

        void OnFact(CombatScenePoint point, Transform actor, string action)
        {
            LastFact = PointLabel(point.Kind) + " / " + (action == "confirm" ? "確認互動" : action == "enter" ? "進入" : "離開");
            // A fake accepted output, only for demonstrating the scene adapter.
            if (action == "confirm" && point.Door != null) point.Door.Apply("fixture-door-" + sequence++, !point.Door.IsOpen);
        }

        public void ResetFixture()
        {
            foreach (var actor in Actors) if (actor != null) { actor.gameObject.SetActive(false); Destroy(actor.gameObject); }
            Actors.Clear();
            foreach (var point in Bindings.Points.Where(p => p.Kind == CombatPointKind.EnemySpawn)) Spawn(Bindings.EnemyPrefab, point.Id, point.transform);
            for (int i = 0; i < Bindings.TeammateSpawns.Length; i++) Spawn(Bindings.TeammatePrefab, "teammate-" + (i + 2), Bindings.TeammateSpawns[i]);
            foreach (var door in Bindings.Doors) door.ResetView();
            Walker.SetObservationMode(false);
            var controller = Walker.GetComponent<CharacterController>();
            controller.enabled = false;
            Walker.transform.SetPositionAndRotation(Bindings.PlayerSpawn.position + Vector3.up * 0.05f, Bindings.PlayerSpawn.rotation);
            controller.enabled = true;
            LastFact = "場景巡檢已重設";
        }

        void Spawn(CombatActorView prefab, string id, Transform at)
        {
            var actor = Instantiate(prefab, at.position, at.rotation, transform);
            actor.EntityId = id;
            actor.name = "Actor_" + id;
            actor.NavigationReported += (entity, arrived) => LastFact = "角色" + (arrived ? "已抵達" : "無法抵達");
            Actors.Add(actor);
        }

        void Update()
        {
            if (!Walker.View.enabled) return;
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;
            if (keyboard.rKey.wasPressedThisFrame) ResetFixture();
            if (keyboard.mKey.wasPressedThisFrame) showMap = !showMap;
            if (keyboard.tabKey.wasPressedThisFrame) mapIndex = (mapIndex + 1) % Bindings.Maps.Length;
            if (keyboard.digit0Key.wasPressedThisFrame) JumpToObservationPoint(0);
            if (keyboard.digit1Key.wasPressedThisFrame) JumpToObservationPoint(1);
            if (keyboard.digit2Key.wasPressedThisFrame) JumpToObservationPoint(2);
            if (keyboard.digit3Key.wasPressedThisFrame) JumpToObservationPoint(3);
            if (keyboard.digit4Key.wasPressedThisFrame) JumpToObservationPoint(4);
            if (keyboard.digit5Key.wasPressedThisFrame) JumpToObservationPoint(5);
            if (keyboard.digit6Key.wasPressedThisFrame) JumpToObservationPoint(6);
            var ray = Walker.View.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            if (keyboard.eKey.wasPressedThisFrame)
                foreach (var point in Bindings.Points) if (point.Confirm(Walker.transform, ray)) break;
            if (mouse.leftButton.wasPressedThisFrame && Physics.Raycast(ray, out var hit, 100, ~0, QueryTriggerInteraction.Ignore))
            {
                var actor = hit.collider.GetComponentInParent<CombatActorView>();
                if (actor != null) { actor.PlayHit("fixture-hit-" + sequence++); actor.ApplyDead(true); LastFact = "假人倒地"; }
            }
            if (keyboard.fKey.wasPressedThisFrame)
                foreach (var actor in Actors.Where(a => !a.IsDead)) actor.PlayShot("fixture-shot-" + sequence++);
            if (keyboard.nKey.wasPressedThisFrame)
                foreach (var actor in Actors.Where(a => a.EntityId.StartsWith("teammate"))) actor.MoveTo(Walker.transform.position, Walker.transform.rotation);
#endif
        }

        public void JumpToObservationPoint(int index)
        {
            var positions = new[]
            {
                new Vector3(24, 28, 55),
                new Vector3(12, 4, 15),
                new Vector3(24, 8, 48),
                new Vector3(46, 4.5f, 68),
                new Vector3(27, 2.1f, 68),
                new Vector3(27, 5.7f, 80),
                new Vector3(28, 9.3f, 72)
            };
            var targets = new[]
            {
                new Vector3(24, 0, 62),
                new Vector3(12, 0, 25),
                new Vector3(24, 0, 60),
                new Vector3(40, 1, 72),
                new Vector3(31, 1.2f, 72),
                new Vector3(31, 4.8f, 76),
                new Vector3(34, 8.4f, 78)
            };
            if (index < 0 || index >= positions.Length) return;
            Walker.JumpToObservationPoint(positions[index], targets[index]);
            LastFact = index == 0 ? "鳥瞰全景" : index == 1 ? "戰壕觀察點" : index == 2 ? "街道觀察點" : index == 3 ? "巷道觀察點" : "主樓第" + (index - 3) + "層";
        }

        static string PointLabel(CombatPointKind kind)
        {
            switch(kind)
            {
                case CombatPointKind.SearchNode: return "搜索點";
                case CombatPointKind.EnemySpawn: return "假人位置";
                case CombatPointKind.EstimateArea: return "測距區";
                case CombatPointKind.Corner: return "轉角";
                case CombatPointKind.Entrance: return "入口";
                case CombatPointKind.Room: return "房間";
                case CombatPointKind.Floor: return "樓層";
                default: return "通道";
            }
        }

        void OnGUI()
        {
            var previousFont=GUI.skin.font;
            GUI.skin.font=InspectionFont;
            GUI.Label(new Rect(Screen.width / 2f - 5, Screen.height / 2f - 10, 20, 20), "+");
            if (showMap)
            {
                var map = Bindings.Maps[mapIndex];
                GUI.DrawTexture(new Rect(12, 110, 320, 320), map.Plan, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(12, 432, 320, 24), mapIndex == 0 ? "戰壕平面圖" : mapIndex == 1 ? "街區平面圖" : "建築樓層平面圖 " + (mapIndex - 1));
            }
            GUI.skin.font=previousFont;
        }
    }
}
