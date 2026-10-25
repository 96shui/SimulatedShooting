using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SimulatedShooting.Scene
{
    public sealed class CombatActorView : MonoBehaviour
    {
        const float CorpseVisibleSeconds = 5f;
        static readonly RaycastHit[] GroundHits = new RaycastHit[32];
        public string EntityId;
        public Transform VisualRoot;
        public Transform PerceptionOrigin;
        public Transform Muzzle;
        public GameObject MuzzleFlash;
        public Collider HitCollider;
        public AudioSource Audio;
        public AudioClip HitClip;
        public AudioClip ShotClip;
        public AudioClip DeathClip;
        public bool HideCorpseAfterDelay;
        public NavMeshAgent Agent;
        public CombatSoldierAnimation SoldierAnimation;
        public bool IsDead { get; private set; }
        public int HitFeedbackCount { get; private set; }
        public int ShotAudioFeedbackCount { get; private set; }
        public int ShotVisualFeedbackCount { get; private set; }
        public int DeathAudioFeedbackCount { get; private set; }
        public AudioSource DeathVoiceSource => deathVoice;
        AudioSource deathVoice;
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
        Mesh bakedBoots;
        readonly List<Vector3> bootVertices = new List<Vector3>();
        float visualGroundOffset;
        MuzzleBlastVfx muzzleBlast;
        float deathTime;
        bool actionsLocked;
        bool combatAiming, navigationRotation;
        bool soldierWasEnabled;
        readonly Dictionary<Animator,float> frozenAnimators = new Dictionary<Animator,float>();
        public bool ActionsLocked => actionsLocked;
        public void SetCombatAiming(bool aiming)
        {
            if (combatAiming == aiming || Agent == null) return;
            if (aiming) navigationRotation = Agent.updateRotation;
            Agent.updateRotation = aiming ? false : navigationRotation;
            combatAiming = aiming;
        }
        public Vector3 GroundedFeetPosition => new Vector3(transform.position.x,
            groundGeometryRoot != null || groundTerrain != null ? GroundHeight() : transform.position.y, transform.position.z);
        public void SetActionsLocked(bool value)
        {
            if(actionsLocked==value)return;
            if(value)SetCombatAiming(false);
            actionsLocked=value;moving=false;
            if(Agent!=null&&Agent.enabled&&Agent.isOnNavMesh){Agent.ResetPath();Agent.isStopped=value;}
            if(value)
            {
                if(Audio!=null)Audio.Stop();
                if(deathVoice!=null)deathVoice.Stop();
                foreach(var animator in GetComponentsInChildren<Animator>(true))
                {frozenAnimators[animator]=animator.speed;animator.speed=0;}
                if(SoldierAnimation!=null){soldierWasEnabled=SoldierAnimation.enabled;SoldierAnimation.enabled=false;}
            }
            else
            {
                foreach(var item in frozenAnimators)if(item.Key!=null)item.Key.speed=item.Value;
                frozenAnimators.Clear();
                if(SoldierAnimation!=null)SoldierAnimation.enabled=soldierWasEnabled;
            }
        }

        void Awake()
        {
            standingVisualPosition = VisualRoot != null ? VisualRoot.localPosition : Vector3.zero;
            standingVisualRotation = VisualRoot != null ? VisualRoot.localRotation : Quaternion.identity;
            ConfigureShotAudio();
        }

        void ConfigureShotAudio()
        {
            if(Audio==null)Audio=gameObject.AddComponent<AudioSource>();
            Audio.enabled=true;Audio.playOnAwake=false;Audio.loop=false;Audio.mute=false;Audio.volume=1f;
            Audio.spatialBlend=1f;Audio.rolloffMode=AudioRolloffMode.Linear;
            Audio.minDistance=8f;Audio.maxDistance=65f;Audio.dopplerLevel=0f;Audio.priority=32;
        }

        public void TurnTowards(Vector3 target,float seconds)
        {
            if(actionsLocked||IsDead||seconds<=0)return;
            var direction=target-transform.position;direction.y=0;
            if(direction.sqrMagnitude<.0001f)return;
            transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),180f*seconds);
        }

        public void MatchVisualToTerrain(Terrain terrain)
            => MatchVisualToGround(terrain, terrain != null ? terrain.transform.parent : null);

        public void MatchVisualToGround(Terrain terrain, Transform geometry)
        {
            groundTerrain = terrain;
            groundGeometryRoot = geometry;
            if (boots == null && VisualRoot != null)
                foreach (var renderer in VisualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (renderer.name.EndsWith("_Boots", StringComparison.Ordinal)) { boots = renderer; break; }
            if (boots != null && bakedBoots == null) bakedBoots = new Mesh { name = "CombatSoleSample" };
            UpdateVisualHeight();
        }

        void UpdateVisualHeight()
        {
            if (VisualRoot == null) return;
            if (!IsDead)
            {
                VisualRoot.localPosition = standingVisualPosition;
                var sole = transform.position.y;
                if (boots != null && bakedBoots != null)
                {
                    boots.BakeMesh(bakedBoots);
                    bakedBoots.GetVertices(bootVertices);
                    sole = float.PositiveInfinity;
                    for (var i = 0; i < bootVertices.Count; i++)
                        sole = Mathf.Min(sole, boots.transform.TransformPoint(bootVertices[i]).y);
                    if (float.IsPositiveInfinity(sole)) sole = transform.position.y;
                }
                visualGroundOffset = GroundHeight() - sole;
            }
            var worldOffset = Vector3.up * visualGroundOffset;
            VisualRoot.localPosition = standingVisualPosition + VisualRoot.parent.InverseTransformVector(worldOffset)
                + (IsDead && SoldierAnimation == null ? new Vector3(0, .3f, 0) : Vector3.zero);
            if (HitCollider is CapsuleCollider capsule)
            {
                if (standingHitCapsule != capsule)
                {
                    standingHitCapsule = capsule;
                    standingHitCenter = capsule.center;
                }
                capsule.center = standingHitCenter + transform.InverseTransformVector(worldOffset);
            }
        }

        float GroundHeight()
        {
            var best = groundTerrain != null
                ? groundTerrain.SampleHeight(transform.position) + groundTerrain.transform.position.y : transform.position.y;
            var found = false;
            // Probe around the navigation root, so an upper floor cannot snap to terrain below it.
            var ray = new Ray(transform.position + Vector3.up * .5f, Vector3.down);
            var count = Physics.RaycastNonAlloc(ray, GroundHits, 2f, ~0, QueryTriggerInteraction.Ignore);
            for (var index = 0; index < count; index++)
            {
                var hit = GroundHits[index];
                if (groundGeometryRoot == null
                    || !hit.collider.transform.IsChildOf(groundGeometryRoot)
                    || hit.collider.GetComponentInParent<CombatActorView>() != null
                    || hit.normal.y < .55f) continue;
                if (!found || hit.point.y > best) best = hit.point.y;
                found = true;
            }
            return best;
        }

        public void PlayHit(string eventId)
        {
            if (actionsLocked || string.IsNullOrEmpty(eventId) || !feedback.Add("hit:" + eventId)) return;
            HitFeedbackCount++;
            if (Audio != null && HitClip != null) Audio.PlayOneShot(HitClip);
            if (SoldierAnimation != null) SoldierAnimation.Hit();
        }

        public void PlayShot(string eventId, Vector3? targetPosition = null, bool confirmedAttack = false)
        {
            if (actionsLocked || IsDead && !confirmedAttack || string.IsNullOrEmpty(eventId) || !feedback.Add("shot:" + eventId)) return;
            ShotVisualFeedbackCount++;
            if (Audio != null && ShotClip != null)
            {
                ConfigureShotAudio();
                Audio.PlayOneShot(ShotClip, 0.85f);
                ShotAudioFeedbackCount++;
            }
            // The prefab's old solid mesh flash is deliberately kept inactive.
            if (MuzzleFlash != null) MuzzleFlash.SetActive(false);
            if (Muzzle != null)
            {
                if (muzzleBlast == null)
                    muzzleBlast = Muzzle.GetComponent<MuzzleBlastVfx>() ?? Muzzle.gameObject.AddComponent<MuzzleBlastVfx>();
                muzzleBlast.Play();
                if (targetPosition.HasValue)
                {
                    var tracerObject = new GameObject("Tracer_Actor_" + eventId);
                    tracerObject.transform.SetParent(transform.parent, true);
                    var tracer = tracerObject.AddComponent<BallisticTracerVisual>();
                    tracer.Configure(Muzzle.position, targetPosition.Value,
                        null, null, null, null, 0, null, true);
                }
            }
            if (SoldierAnimation != null) SoldierAnimation.Shot();
        }

        public void PlayGrenadeThrow()
        {
            if (actionsLocked || IsDead || SoldierAnimation == null) return;
            SoldierAnimation.GrenadeThrow();
        }

        public void ApplyDead(bool dead)
        {
            if(actionsLocked)return;
            if (IsDead == dead) return;
            IsDead = dead;
            if(dead)SetCombatAiming(false);
            if(dead)PlayDeathCry();
            else if(deathVoice!=null)deathVoice.Stop();
            deathTime = dead ? Time.time : 0f;
            if (!dead && VisualRoot != null) VisualRoot.gameObject.SetActive(true);
            moving = false;
            if (Agent.enabled && Agent.isOnNavMesh) Agent.ResetPath();
            Agent.enabled = !dead;
            UpdateVisualHeight();
            VisualRoot.localRotation = standingVisualRotation * (dead && SoldierAnimation == null ? Quaternion.Euler(-90, 0, 0) : Quaternion.identity);
            if (SoldierAnimation != null) SoldierAnimation.SetDead(dead);
            HitCollider.enabled = !dead;
            if (MuzzleFlash != null) MuzzleFlash.SetActive(false);
        }

        void PlayDeathCry()
        {
            if(DeathClip==null)return;
            if(deathVoice==null)
            {
                var voiceObject=new GameObject("EnemyDeathVoice");
                voiceObject.transform.SetParent(SoldierAnimation!=null?SoldierAnimation.Chest:PerceptionOrigin!=null?PerceptionOrigin:transform,false);
                deathVoice=voiceObject.AddComponent<AudioSource>();
                deathVoice.playOnAwake=false;deathVoice.loop=false;deathVoice.spatialBlend=1;
                deathVoice.rolloffMode=AudioRolloffMode.Logarithmic;
                deathVoice.minDistance=2;deathVoice.maxDistance=35;deathVoice.dopplerLevel=0;
                deathVoice.volume=.9f;
            }
            deathVoice.clip=DeathClip;deathVoice.Play();DeathAudioFeedbackCount++;
        }

        public bool CanSee(Vector3 target, LayerMask mask)
        {
            return !Physics.Linecast(PerceptionOrigin.position, target, mask, QueryTriggerInteraction.Ignore);
        }

        public void MoveTo(Vector3 destination, Quaternion facing)
        {
            if(actionsLocked)return;
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
            if (HideCorpseAfterDelay && IsDead && VisualRoot != null && VisualRoot.gameObject.activeSelf
                && Time.time - deathTime >= CorpseVisibleSeconds)
                VisualRoot.gameObject.SetActive(false);
            if (!moving || Agent.pathPending) return;
            if (Agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                moving = false;
                NavigationReported?.Invoke(EntityId, false);
            }
            else if (Agent.remainingDistance <= Agent.stoppingDistance + 0.08f)
            {
                moving = false;
                if (!combatAiming) transform.rotation = arrivalRotation;
                NavigationReported?.Invoke(EntityId, true);
            }
        }

        void LateUpdate()
        {
            if (!IsDead && (groundTerrain != null || groundGeometryRoot != null)) UpdateVisualHeight();
        }
        void OnDestroy() { if (bakedBoots != null) Destroy(bakedBoots); }
    }
}
