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
        public List<CombatActorView> Actors { get; } = new List<CombatActorView>();
        public string LastFact { get; private set; } = "Scene fixture ready";
        int sequence;
        int mapIndex;
        bool showMap;
        Vector3 droneStart;

        void Start()
        {
            droneStart = Bindings.Drone.position;
            foreach (var p in Bindings.Points) p.FactReported += OnFact;
            ResetFixture();
        }

        void OnDestroy()
        {
            foreach (var p in Bindings.Points) if (p != null) p.FactReported -= OnFact;
        }

        void OnFact(CombatScenePoint point, Transform actor, string action)
        {
            LastFact = point.Id + " / " + action;
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
            var controller = Walker.GetComponent<CharacterController>();
            controller.enabled = false;
            Walker.transform.SetPositionAndRotation(Bindings.PlayerSpawn.position + Vector3.up * 0.05f, Bindings.PlayerSpawn.rotation);
            controller.enabled = true;
            LastFact = "Scene fixture reset";
        }

        void Spawn(CombatActorView prefab, string id, Transform at)
        {
            var actor = Instantiate(prefab, at.position, at.rotation, transform);
            actor.EntityId = id;
            actor.name = "Actor_" + id;
            actor.NavigationReported += (entity, arrived) => LastFact = entity + (arrived ? " arrived" : " navigation failed");
            Actors.Add(actor);
        }

        void Update()
        {
            Bindings.Drone.position = droneStart + Vector3.up * Mathf.Min(Time.timeSinceLevelLoad * 0.6f, 2f);
            if (!Walker.View.enabled) return;
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null) return;
            if (keyboard.rKey.wasPressedThisFrame) ResetFixture();
            if (keyboard.mKey.wasPressedThisFrame) showMap = !showMap;
            if (keyboard.tabKey.wasPressedThisFrame) mapIndex = (mapIndex + 1) % Bindings.Maps.Length;
            var ray = Walker.View.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            if (keyboard.eKey.wasPressedThisFrame)
                foreach (var point in Bindings.Points) if (point.Confirm(Walker.transform, ray)) break;
            if (mouse.leftButton.wasPressedThisFrame && Physics.Raycast(ray, out var hit, 100, ~0, QueryTriggerInteraction.Ignore))
            {
                var actor = hit.collider.GetComponentInParent<CombatActorView>();
                if (actor != null) { actor.PlayHit("fixture-hit-" + sequence++); actor.ApplyDead(true); LastFact = actor.EntityId + " corpse visual"; }
            }
            if (keyboard.fKey.wasPressedThisFrame)
                foreach (var actor in Actors.Where(a => !a.IsDead)) actor.PlayShot("fixture-shot-" + sequence++);
            if (keyboard.nKey.wasPressedThisFrame)
                foreach (var actor in Actors.Where(a => a.EntityId.StartsWith("teammate"))) actor.MoveTo(Walker.transform.position, Walker.transform.rotation);
#endif
        }

        void OnGUI()
        {
            GUI.Box(new Rect(12, 12, 610, 84), "SCENE 3 | Trench + Urban | scene inspection fixture");
            GUI.Label(new Rect(24, 36, 590, 24), "WASD walk | RMB look | E interact | LMB hit/death | F fire visual");
            GUI.Label(new Rect(24, 58, 590, 24), "N navigation | R reset | M plan | Tab floor    " + LastFact);
            GUI.Label(new Rect(Screen.width / 2f - 5, Screen.height / 2f - 10, 20, 20), "+");
            if (showMap)
            {
                var map = Bindings.Maps[mapIndex];
                GUI.DrawTexture(new Rect(12, 110, 320, 320), map.Plan, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(12, 432, 320, 24), map.Id);
            }
        }
    }
}
