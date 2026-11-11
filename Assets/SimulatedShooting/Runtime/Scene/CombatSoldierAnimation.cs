using System.Collections.Generic;
using UnityEngine;

namespace SimulatedShooting.Scene
{
    // Presentation only: all combat decisions remain in CombatActorView's callers.
    public sealed class CombatSoldierAnimation : MonoBehaviour
    {
        public Animator Animator;
        public CombatActorView Actor;
        public Transform LeftUpperArm, LeftForearm, LeftHand;
        public Transform RightUpperArm, RightForearm, RightHand;
        public Transform Rifle;
        public Transform Chest;
        public bool Friendly;
        float recoil;
        bool dead;
        float grenadeThrowUntil;
        public bool IsThrowingGrenade => Time.time < grenadeThrowUntil;
        Quaternion leftGrip, rightGrip;
        Vector3 riflePosition;
        Quaternion rifleRotation;
        readonly List<(Transform bone, Quaternion rest, float curl)> gripFingers = new List<(Transform, Quaternion, float)>();

        void Awake()
        {
            leftGrip = Quaternion.Inverse(Actor.VisualRoot.rotation) * LeftHand.rotation;
            rightGrip = Quaternion.Inverse(Actor.VisualRoot.rotation) * RightHand.rotation;
            riflePosition = Rifle.localPosition;
            rifleRotation = Rifle.localRotation;
            CacheFingers(RightHand, "R_");
            CacheFingers(LeftHand, "L_");
        }

        public void Shot()
        {
            if (dead) return;
            recoil = 1;
            Animator.CrossFadeInFixedTime("Shot", .06f, 0, 0);
        }

        public void GrenadeThrow()
        {
            if (dead || Animator == null) return;
            grenadeThrowUntil = Time.time + .62f;
            var state = Animator.StringToHash("GrenadeThrow");
            if (Animator.HasState(0, state)) Animator.CrossFadeInFixedTime("GrenadeThrow", .06f, 0, 0);
        }

        public void Hit()
        {
            if (!dead) Animator.CrossFadeInFixedTime("Hit", .05f, 0, 0);
        }

        public void SetDead(bool value)
        {
            if (dead == value) return;
            dead = value;
            recoil = 0;
            Animator.SetBool("Dead", value);
            Animator.SetFloat("Speed", 0);
            if (value) Animator.CrossFadeInFixedTime("Death", .12f, 0, 0);
            else Animator.Play("Locomotion", 0, 0);
            if (!value)
            {
                Rifle.localPosition = riflePosition;
                Rifle.localRotation = rifleRotation;
            }
        }

        void Update()
        {
            if (dead) return;
            var agent = Actor.Agent;
            float speed = agent != null && agent.enabled && agent.isOnNavMesh ? agent.velocity.magnitude : 0;
            Animator.SetFloat("Speed", speed, .1f, Time.deltaTime);
            recoil = Mathf.MoveTowards(recoil, 0, Time.deltaTime * 8);
        }

        void LateUpdate()
        {
            if (dead) return;
            var root = Actor.VisualRoot;
            if (Time.time < grenadeThrowUntil)
            {
                var phase = Mathf.InverseLerp(grenadeThrowUntil - .62f, grenadeThrowUntil, Time.time);
                var target = root.TransformPoint(new Vector3(.18f, phase < .55f ? 1.42f : 1.53f, phase < .55f ? .30f : .62f));
                SolveArm(RightUpperArm, RightForearm, RightHand, target, root.TransformPoint(new Vector3(.42f, 1.22f, .08f)));
                SolveArm(LeftUpperArm, LeftForearm, LeftHand, target + root.right * -.14f, root.TransformPoint(new Vector3(-.36f, 1.22f, .15f)));
                return;
            }
            // Stable shoulder-height targets keep the two hands on the rifle while legs animate.
            float sway = Mathf.Sin(Time.time * 1.8f) * .004f;
            var right = root.TransformPoint(new Vector3(.14f, 1.30f + sway, .23f - recoil * .035f));
            SolveArm(RightUpperArm, RightForearm, RightHand, right, root.TransformPoint(new Vector3(.48f, 1.08f, .12f)));
            RightHand.rotation = root.rotation * rightGrip;
            AlignPalm(RightHand, "R_", (root.forward - root.up).normalized, -root.right);
            // Align the palm, not the wrist, to the grip.
            Rifle.rotation = root.rotation;
            Rifle.position += PalmCenter(RightHand, "R_") - Rifle.TransformPoint(new Vector3(.006f, -.075f, -.085f));
            var left = Rifle.TransformPoint(new Vector3(-.035f, -.065f, .04f));
            SolveArm(LeftUpperArm, LeftForearm, LeftHand, left, root.TransformPoint(new Vector3(-.40f, 1.08f, .28f)));
            LeftHand.rotation = root.rotation * leftGrip;
            AlignPalm(LeftHand, "L_", root.forward, root.up);
            foreach (var finger in gripFingers)
                if (finger.bone != null) finger.bone.localRotation = finger.rest * Quaternion.Euler(finger.curl, 0, 0);
        }

        static Vector3 PalmCenter(Transform hand, string prefix)
        {
            var middle = hand.Find(prefix + "middle1");
            return middle != null ? Vector3.Lerp(hand.position, middle.position, .55f) : hand.position;
        }

        void CacheFingers(Transform hand, string prefix)
        {
            foreach (var bone in hand.GetComponentsInChildren<Transform>(true))
            {
                var name = bone.name.ToLowerInvariant();
                if (!name.StartsWith(prefix.ToLowerInvariant()) || name.EndsWith("_end")) continue;
                if (!(name.Contains("index") || name.Contains("middle") || name.Contains("ring") ||
                      name.Contains("pinky") || name.Contains("thumb"))) continue;
                var curl = name.Contains("thumb") ? 10f : name.Contains("index") && prefix == "R_" ? 25f : prefix == "R_" ? 20f : 35f;
                if (prefix == "L_") curl = -curl;
                gripFingers.Add((bone, bone.localRotation, curl));
            }
        }

        static void AlignPalm(Transform hand, string prefix, Vector3 forward, Vector3 normal)
        {
            Transform index = null, middle = null, pinky = null;
            foreach (Transform child in hand)
            {
                if (child.name == prefix + "index1") index = child;
                else if (child.name == prefix + "middle1") middle = child;
                else if (child.name == prefix + "pinky1") pinky = child;
            }
            if (index == null || middle == null || pinky == null) return;
            var currentForward = (middle.position - hand.position).normalized;
            var currentNormal = Vector3.Cross(index.position - hand.position, pinky.position - hand.position).normalized;
            if (prefix == "L_") currentNormal = -currentNormal;
            if (currentForward.sqrMagnitude < .001f || currentNormal.sqrMagnitude < .001f) return;
            hand.rotation = Quaternion.LookRotation(forward, normal) *
                            Quaternion.Inverse(Quaternion.LookRotation(currentForward, currentNormal)) * hand.rotation;
        }

        public static void SolveArm(Transform upper, Transform lower, Transform hand, Vector3 target, Vector3 hint)
        {
            var origin = upper.position;
            float a = Vector3.Distance(origin, lower.position), b = Vector3.Distance(lower.position, hand.position);
            var delta = target - origin;
            float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(a - b) + .001f, a + b - .001f);
            var direction = delta.normalized;
            var bend = Vector3.ProjectOnPlane(hint - origin, direction).normalized;
            float along = (a * a - b * b + distance * distance) / (2 * distance);
            var elbow = origin + direction * along + bend * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            upper.rotation = Quaternion.FromToRotation(lower.position - origin, elbow - origin) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, target - lower.position) * lower.rotation;
        }
    }
}
