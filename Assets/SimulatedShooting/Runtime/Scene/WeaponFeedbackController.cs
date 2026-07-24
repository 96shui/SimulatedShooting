using UnityEngine;

namespace SimulatedShooting.Scene
{
    [DisallowMultipleComponent]
    public sealed class WeaponFeedbackController : MonoBehaviour
    {
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform tracerRoot;
        [SerializeField] private Transform weaponAudioAnchor;
        [SerializeField] private TrainingRifleGrabInteractable grabInteractable;
        [SerializeField] private GameObject projectileVisualPrefab;
        [SerializeField] private AudioClip rifleShotClip;
        [SerializeField] private AudioClip pickupClip;
        [SerializeField] private AudioClip bulletFlybyClip;
        [SerializeField] private AudioClip[] targetImpactClips;

        AudioSource weaponAudioSource;
        ParticleSystem muzzleFlash;
        ParticleSystemRenderer muzzleFlashRenderer;
        Material tracerMaterial;
        Material projectileMaterial;
        Material impactMaterial;
        bool grabSubscribed;
        bool lastRearSelected;
        int validShotFeedbackCount;
        int pickupFeedbackCount;
        int impactFeedbackCount;

        public bool HasRequiredAudio =>
            rifleShotClip != null &&
            pickupClip != null &&
            bulletFlybyClip != null &&
            targetImpactClips != null &&
            targetImpactClips.Length > 0;
        public bool HasProjectileVisualPrefab => projectileVisualPrefab != null;
        public int ValidShotFeedbackCount => validShotFeedbackCount;
        public int PickupFeedbackCount => pickupFeedbackCount;
        public int ImpactFeedbackCount => impactFeedbackCount;
        public AudioClip RifleShotClip => rifleShotClip;
        public AudioClip PickupClip => pickupClip;
        public AudioClip BulletFlybyClip => bulletFlybyClip;

        public void Configure(
            Transform muzzlePoint,
            Transform tracerContainer,
            Transform audioAnchor,
            TrainingRifleGrabInteractable grab,
            GameObject projectilePrefab,
            AudioClip shotClip,
            AudioClip weaponPickupClip,
            AudioClip flybyClip,
            AudioClip[] impactClips)
        {
            UnsubscribeGrab();
            muzzle = muzzlePoint;
            tracerRoot = tracerContainer;
            weaponAudioAnchor = audioAnchor;
            grabInteractable = grab;
            projectileVisualPrefab = projectilePrefab;
            rifleShotClip = shotClip;
            pickupClip = weaponPickupClip;
            bulletFlybyClip = flybyClip;
            targetImpactClips = impactClips;
            EnsureRuntimeResources();
            SubscribeGrab();
        }

        void Awake()
        {
            EnsureRuntimeResources();
        }

        void OnEnable()
        {
            SubscribeGrab();
        }

        void OnDisable()
        {
            UnsubscribeGrab();
        }

        void OnDestroy()
        {
            UnsubscribeGrab();
            DestroyRuntimeMaterial(tracerMaterial);
            DestroyRuntimeMaterial(projectileMaterial);
            DestroyRuntimeMaterial(impactMaterial);
        }

        public void PlayValidShot(
            int shotIndex,
            Vector3 start,
            Vector3 end,
            bool hit,
            Vector3 hitPoint,
            Vector3 hitNormal)
        {
            EnsureRuntimeResources();
            validShotFeedbackCount++;
            if (weaponAudioSource != null && rifleShotClip != null)
            {
                weaponAudioSource.pitch = 0.985f + (shotIndex % 3) * 0.012f;
                weaponAudioSource.PlayOneShot(rifleShotClip, 0.92f);
            }

            if (muzzleFlash != null)
            {
                if (muzzleFlashRenderer != null)
                {
                    muzzleFlashRenderer.sharedMaterial = impactMaterial;
                }
                muzzleFlash.Play(true);
            }

            if (tracerRoot == null)
            {
                return;
            }

            var tracerObject = new GameObject($"Tracer_training-rifle_{shotIndex:000}");
            tracerObject.transform.SetParent(tracerRoot, true);
            tracerObject.AddComponent<SceneTestId>().Id = "ZeroingRange.Weapon.Tracer";
            var visual = tracerObject.AddComponent<BallisticTracerVisual>();
            visual.Configure(
                start,
                end,
                projectileVisualPrefab,
                projectileMaterial,
                tracerMaterial,
                bulletFlybyClip,
                shotIndex,
                hit ? () => PlayImpact(hitPoint, hitNormal, shotIndex) : null);
        }

        public void PlayPickupForTests()
        {
            PlayPickup();
        }

        void HandleHoldStateChanged(VRShooting.Common.WeaponHoldState _, bool rearSelected, bool __)
        {
            if (rearSelected && !lastRearSelected)
            {
                PlayPickup();
            }

            lastRearSelected = rearSelected;
        }

        void PlayPickup()
        {
            EnsureRuntimeResources();
            pickupFeedbackCount++;
            if (weaponAudioSource != null && pickupClip != null)
            {
                weaponAudioSource.pitch = 1f;
                weaponAudioSource.PlayOneShot(pickupClip, 0.72f);
            }
        }

        void PlayImpact(Vector3 point, Vector3 normal, int shotIndex)
        {
            impactFeedbackCount++;
            var impact = new GameObject($"ImpactFeedback_ZeroingTarget_{shotIndex:000}");
            impact.transform.SetPositionAndRotation(
                point + normal.normalized * 0.006f,
                normal.sqrMagnitude > 0.0001f
                    ? Quaternion.LookRotation(normal.normalized)
                    : Quaternion.identity);
            impact.AddComponent<SceneTestId>().Id = "ZeroingRange.Target.ImpactFeedback";

            var particles = impact.AddComponent<ParticleSystem>();
            ConfigureImpactParticles(particles);
            particles.Play(true);

            if (targetImpactClips != null && targetImpactClips.Length > 0)
            {
                var clip = targetImpactClips[Mathf.Abs(shotIndex) % targetImpactClips.Length];
                if (clip != null)
                {
                    AudioSource.PlayClipAtPoint(clip, point, 0.72f);
                }
            }

            impact.AddComponent<TimedSelfDestruct>().Configure(0.75f);
        }

        void EnsureRuntimeResources()
        {
            if (tracerMaterial == null)
            {
                tracerMaterial = CreateMaterial(
                    "Runtime_Tracer_training-rifle",
                    new Color(1f, 0.70f, 0.10f, 1f),
                    true);
            }

            if (projectileMaterial == null)
            {
                projectileMaterial = CreateMaterial(
                    "Runtime_Projectile_training-rifle",
                    new Color(0.71f, 0.39f, 0.12f, 1f),
                    false);
                if (projectileMaterial.HasProperty("_Metallic"))
                    projectileMaterial.SetFloat("_Metallic", 0.72f);
                if (projectileMaterial.HasProperty("_Smoothness"))
                    projectileMaterial.SetFloat("_Smoothness", 0.58f);
            }

            if (impactMaterial == null)
            {
                impactMaterial = CreateMaterial(
                    "Runtime_ImpactSparks_training-rifle",
                    new Color(1f, 0.48f, 0.08f, 1f),
                    true);
            }

            EnsureWeaponAudioSource();
            EnsureMuzzleFlash();
        }

        void Update()
        {
            if (muzzleFlash != null &&
                muzzleFlashRenderer != null &&
                !muzzleFlash.isPlaying &&
                muzzleFlashRenderer.sharedMaterial != null)
            {
                muzzleFlashRenderer.sharedMaterial = null;
            }
        }

        void EnsureWeaponAudioSource()
        {
            if (weaponAudioSource != null)
            {
                return;
            }

            var anchor = weaponAudioAnchor != null ? weaponAudioAnchor : transform;
            var audioObject = new GameObject("Audio_training-rifle_Feedback");
            audioObject.transform.SetParent(anchor, false);
            audioObject.AddComponent<SceneTestId>().Id = "ZeroingRange.Weapon.Feedback";
            weaponAudioSource = audioObject.AddComponent<AudioSource>();
            weaponAudioSource.playOnAwake = false;
            weaponAudioSource.loop = false;
            weaponAudioSource.spatialBlend = 1f;
            weaponAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            weaponAudioSource.minDistance = 0.35f;
            weaponAudioSource.maxDistance = 160f;
            weaponAudioSource.dopplerLevel = 0f;
        }

        void EnsureMuzzleFlash()
        {
            if (muzzle == null)
            {
                return;
            }

            if (muzzleFlash == null)
            {
                var flashObject = new GameObject("MuzzleFlash_training-rifle");
                flashObject.transform.SetParent(muzzle, false);
                flashObject.transform.localPosition = Vector3.forward * 0.012f;
                flashObject.AddComponent<SceneTestId>().Id = "ZeroingRange.Weapon.MuzzleFlash";
                muzzleFlash = flashObject.AddComponent<ParticleSystem>();
            }

            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = muzzleFlash.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.08f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.025f, 0.065f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.085f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.94f, 0.45f, 1f),
                new Color(1f, 0.20f, 0.03f, 0.75f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 20;

            var emission = muzzleFlash.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 7, 12) });

            var shape = muzzleFlash.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 9f;
            shape.radius = 0.006f;

            muzzleFlashRenderer = muzzleFlash.GetComponent<ParticleSystemRenderer>();
            muzzleFlashRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            muzzleFlashRenderer.lengthScale = 2.2f;
            muzzleFlashRenderer.velocityScale = 0.06f;
            muzzleFlashRenderer.sharedMaterial = null;
        }

        void ConfigureImpactParticles(ParticleSystem particles)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.12f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.34f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.7f, 2.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.026f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.72f, 0.12f, 1f),
                new Color(0.45f, 0.28f, 0.12f, 0.3f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.55f;
            main.maxParticles = 24;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 7, 13) });

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.012f;

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.2f;
            renderer.velocityScale = 0.12f;
            renderer.sharedMaterial = impactMaterial;
        }

        void SubscribeGrab()
        {
            if (grabSubscribed || grabInteractable == null)
            {
                return;
            }

            lastRearSelected = grabInteractable.RearHandSelected;
            grabInteractable.HoldStateChanged += HandleHoldStateChanged;
            grabSubscribed = true;
        }

        void UnsubscribeGrab()
        {
            if (!grabSubscribed || grabInteractable == null)
            {
                return;
            }

            grabInteractable.HoldStateChanged -= HandleHoldStateChanged;
            grabSubscribed = false;
        }

        static Material CreateMaterial(string materialName, Color color, bool unlit)
        {
            var shader = unlit
                ? Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit")
                : Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            shader ??= Shader.Find("Standard");
            var material = new Material(shader)
            {
                name = materialName,
                color = color
            };
            return material;
        }

        static void DestroyRuntimeMaterial(Material material)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }
    }
}
