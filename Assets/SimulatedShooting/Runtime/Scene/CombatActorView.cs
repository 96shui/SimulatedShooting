using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SimulatedShooting.Scene
{
    public sealed class CombatActorView : MonoBehaviour
    {
        static readonly RaycastHit[] GroundHits = new RaycastHit[32];
        public string EntityId;
        public Transform VisualRoot;
        public Transform PerceptionOrigin;
        public Transform Muzzle;
        public GameObject MuzzleFlash;
        public Collider HitCollider;
        public AudioSource Audio;
        public AudioClip HitClip;
        public NavMeshAgent Agent;
        public CombatSoldierAnimation SoldierAnimation;
        public bool IsDead { get; private set; }
        public int HitFeedbackCount { get; private set; }
        public event Action<string, bool> NavigationReported;
        readonly HashSet<string> feedback = new HashSet<string>();
        bool moving;
        Quaternion arrivalRotation;
        Vector3 standingVisualPosition;
        Quaternion standingVisualRotation;
        Terrain groundTerrain;
        Transform groundGeometryRoot;
        SkinnedMeshRenderer boots;
        CapsuleCollider standingHitCapsule;
        Vector3 standingHitCenter;
        bool feetAligned;
        float flashUntil;

        void Awake()
        {
            standingVisualPosition = VisualRoot.localPosition;
            standingVisualRotation = VisualRoot.localRotation;
        }

        public void MatchVisualToTerrain(Terrain terrain)
        {
            groundTerrain = terrain;
            groundGeometryRoot = terrain != null ? terrain.transform.parent : null;
            UpdateVisualHeight();
            if (!feetAligned && groundTerrain != null)
            {
                foreach (var renderer in VisualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (renderer.name.EndsWith("_Boots", StringComparison.Ordinal)) { boots = renderer; break; }
                if (boots != null)
                {
                    var ground = GroundHeight();
                    standingVisualPosition += Vector3.up * (ground - boots.bounds.min.y);
                    feetAligned = true;
                    UpdateVisualHeight();
                }
            }
        }

        void UpdateVisualHeight()
        {
            var groundOffset = groundTerrain == null ? 0f : GroundHeight() - transform.position.y;
            VisualRoot.localPosition = standingVisualPosition + Vector3.up * groundOffset
                + (IsDead && SoldierAnimation == null ? new Vector3(0, .3f, 0) : Vector3.zero);
            if (HitCollider is CapsuleCollider capsule)
            {
                if (standingHitCapsule != capsule)
                {
                    standingHitCapsule = capsule;
                    standingHitCenter = capsule.center;
                }
                capsule.center = standingHitCenter + Vector3.up * groundOffset;
            }
        }

        float GroundHeight()
        {
            var terrainHeight = groundTerrain.SampleHeight(transform.position) + groundTerrain.transform.position.y;
            var best = terrainHeight;
            var ray = new Ray(new Vector3(transform.position.x, terrainHeight + 1.35f, transform.position.z), Vector3.down);
            var count = Physics.RaycastNonAlloc(ray, GroundHits, 1.4f, ~0, QueryTriggerInteraction.Ignore);
            for (var index = 0; index < count; index++)
            {
                var hit = GroundHits[index];
                if (!(hit.collider is MeshCollider) || groundGeometryRoot == null
                    || !hit.collider.transform.IsChildOf(groundGeometryRoot)
                    || hit.normal.y < .55f) continue;
                if (hit.point.y > best) best = hit.point.y;
            }
            return best;
        }

        public void PlayHit(string eventId)
        {
            if (string.IsNullOrEmpty(eventId) || !feedback.Add("hit:" + eventId)) return;
            HitFeedbackCount++;
            Audio.PlayOneShot(HitClip);
            if (SoldierAnimation != null) SoldierAnimation.Hit();
        }

        public void PlayShot(string eventId)
        {
            if (IsDead || string.IsNullOrEmpty(eventId) || !feedback.Add("shot:" + eventId)) return;
            MuzzleFlash.SetActive(true);
            flashUntil = Time.time + 0.08f;
            if (SoldierAnimation != null) SoldierAnimation.Shot();
        }

        public void PlayGrenadeThrow()
        {
            if (IsDead || SoldierAnimation == null) return;
            SoldierAnimation.GrenadeThrow();
        }

        public void ApplyDead(bool dead)
        {
            if (IsDead == dead) return;
            IsDead = dead;
            moving = false;
            if (Agent.enabled && Agent.isOnNavMesh) Agent.ResetPath();
            Agent.enabled = !dead;
            UpdateVisualHeight();
            VisualRoot.localRotation = standingVisualRotation * (dead && SoldierAnimation == null ? Quaternion.Euler(-90, 0, 0) : Quaternion.identity);
            if (SoldierAnimation != null) SoldierAnimation.SetDead(dead);
            HitCollider.enabled = !dead;
            MuzzleFlash.SetActive(false);
        }

        public bool CanSee(Vector3 target, LayerMask mask)
        {
            return !Physics.Linecast(PerceptionOrigin.position, target, mask, QueryTriggerInteraction.Ignore);
        }

        public void MoveTo(Vector3 destination, Quaternion facing)
        {
            moving = false;
            if (Agent.enabled && Agent.isOnNavMesh) Agent.ResetPath();
            var path = new NavMeshPath();
            if (IsDead || !Agent.enabled || !Agent.isOnNavMesh ||
                !NavMesh.SamplePosition(destination, out var hit, 0.6f, NavMesh.AllAreas) ||
                !Agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete)
            {
                NavigationReported?.Invoke(EntityId, false);
                return;
            }
            Agent.SetPath(path);
            arrivalRotation = facing;
            moving = true;
        }

        void Update()
        {
            if (MuzzleFlash.activeSelf && Time.time >= flashUntil) MuzzleFlash.SetActive(false);
            if (!moving || Agent.pathPending) return;
            if (Agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                moving = false;
                NavigationReported?.Invoke(EntityId, false);
            }
            else if (Agent.remainingDistance <= Agent.stoppingDistance + 0.08f)
            {
                moving = false;
                transform.rotation = arrivalRotation;
                NavigationReported?.Invoke(EntityId, true);
            }
        }

        void LateUpdate()
        {
            if (groundTerrain != null) UpdateVisualHeight();
        }
    }
}
