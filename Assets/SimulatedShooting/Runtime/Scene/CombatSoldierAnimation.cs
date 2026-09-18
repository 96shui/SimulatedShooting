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
        Quaternion leftGrip, rightGrip;
        Vector3 riflePosition;
        Quaternion rifleRotation;

        void Awake()
        {
            leftGrip = Quaternion.Inverse(Actor.VisualRoot.rotation) * LeftHand.rotation;
            rightGrip = Quaternion.Inverse(Actor.VisualRoot.rotation) * RightHand.rotation;
            riflePosition = Rifle.localPosition;
            rifleRotation = Rifle.localRotation;
        }

        public void Shot()
        {
            if (dead) return;
            recoil = 1;
            Animator.CrossFadeInFixedTime("Shot", .06f, 0, 0);
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
            // Stable shoulder-height targets keep the two hands on the rifle while legs animate.
            float sway = Mathf.Sin(Time.time * 1.8f) * .004f;
            var right = root.TransformPoint(new Vector3(.14f, 1.30f + sway, .25f - recoil * .035f));
            var left = root.TransformPoint(new Vector3(.12f, 1.27f + sway, .43f - recoil * .035f));
            SolveArm(RightUpperArm, RightForearm, RightHand, right, root.TransformPoint(new Vector3(.48f, 1.08f, .12f)));
            SolveArm(LeftUpperArm, LeftForearm, LeftHand, left, root.TransformPoint(new Vector3(-.40f, 1.08f, .28f)));
            RightHand.rotation = root.rotation * rightGrip;
            LeftHand.rotation = root.rotation * leftGrip;
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
