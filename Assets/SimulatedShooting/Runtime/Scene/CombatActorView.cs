using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SimulatedShooting.Scene
{
    public sealed class CombatActorView : MonoBehaviour
    {
        public string EntityId;
        public Transform VisualRoot;
        public Transform PerceptionOrigin;
        public Transform Muzzle;
        public GameObject MuzzleFlash;
        public Collider HitCollider;
        public AudioSource Audio;
        public AudioClip HitClip;
        public NavMeshAgent Agent;
        public bool IsDead { get; private set; }
        public int HitFeedbackCount { get; private set; }
        public event Action<string, bool> NavigationReported;
        readonly HashSet<string> feedback = new HashSet<string>();
        bool moving;
        Quaternion arrivalRotation;
        float flashUntil;

        public void PlayHit(string eventId)
        {
            if (string.IsNullOrEmpty(eventId) || !feedback.Add("hit:" + eventId)) return;
            HitFeedbackCount++;
            Audio.PlayOneShot(HitClip);
        }

        public void PlayShot(string eventId)
        {
            if (IsDead || string.IsNullOrEmpty(eventId) || !feedback.Add("shot:" + eventId)) return;
            MuzzleFlash.SetActive(true);
            flashUntil = Time.time + 0.08f;
        }

        public void ApplyDead(bool dead)
        {
            IsDead = dead;
            moving = false;
            if (Agent.enabled && Agent.isOnNavMesh) Agent.ResetPath();
            Agent.enabled = !dead;
            VisualRoot.localPosition = dead ? new Vector3(0, 0.3f, 0) : Vector3.zero;
            VisualRoot.localRotation = dead ? Quaternion.Euler(-90, 0, 0) : Quaternion.identity;
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
    }
}
