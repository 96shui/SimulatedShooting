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
        MuzzleBlastVfx muzzleBlast;
        Material tracerMaterial;
        Material projectileMaterial;
        Material impactMaterial;
        Material fleshImpactMaterial;
        Texture2D fleshImpactTexture;
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
            DestroyRuntimeMaterial(fleshImpactMaterial);
            if (fleshImpactTexture != null) Destroy(fleshImpactTexture);
        }

        public void PlayValidShot(
            int shotIndex,
            Vector3 start,
            Vector3 end,
            bool hit,
            Vector3 hitPoint,
            Vector3 hitNormal,
            bool hitFlesh = false)
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
            if (muzzleBlast != null) muzzleBlast.Play();

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
                hit ? () => PlayImpact(hitPoint, hitNormal, shotIndex, hitFlesh) : null);
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

        public void PlayConfirmedFleshImpact(Vector3 point, Vector3 normal, int shotIndex)
        {
            PlayImpact(point, normal, shotIndex, true);
        }

        void PlayImpact(Vector3 point, Vector3 normal, int shotIndex, bool hitFlesh)
        {
            impactFeedbackCount++;
            var impact = new GameObject(hitFlesh
                ? $"ImpactFeedback_Flesh_{shotIndex:000}"
                : $"ImpactFeedback_ZeroingTarget_{shotIndex:000}");
            impact.transform.SetPositionAndRotation(
                point + normal.normalized * 0.006f,
                normal.sqrMagnitude > 0.0001f
                    ? Quaternion.LookRotation(normal.normalized)
                    : Quaternion.identity);
            impact.AddComponent<SceneTestId>().Id = hitFlesh
                ? "Combat.Actor.FleshImpactFeedback" : "ZeroingRange.Target.ImpactFeedback";

            // Surface hits retain their event marker and audio, without the artificial spark burst.
            if (hitFlesh)
            {
                var mist = impact.AddComponent<RecordedBloodMistVfx>();
                if (!mist.Initialize(shotIndex)) Debug.LogWarning("ActionVFX Blood Mist material is missing.", impact);

            }

            if (!hitFlesh && targetImpactClips != null && targetImpactClips.Length > 0)
            {
                var clip = targetImpactClips[Mathf.Abs(shotIndex) % targetImpactClips.Length];
                if (clip != null)
                {
                    AudioSource.PlayClipAtPoint(clip, point, 0.72f);
                }
            }

            impact.AddComponent<TimedSelfDestruct>().Configure(hitFlesh ? .9f : .75f);
        }

        void ConfigureFleshImpactParticles(ParticleSystem particles)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.08f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.042f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.22f, 0.035f, 0.025f, 0.7f),
                new Color(0.36f, 0.09f, 0.06f, 0.3f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.2f;
            main.maxParticles = 12;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 5, 8) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.01f;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = fleshImpactMaterial;
        }

        void EnsureRuntimeResources()
        {
            if (tracerMaterial == null)
            {
                tracerMaterial = CreateMaterial(
                    "Runtime_Tracer_training-rifle",
                    Color.white,
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

            if (fleshImpactMaterial == null)
            {
                fleshImpactMaterial = CreateMaterial("Runtime_FleshImpact_training-rifle", Color.white, true);
                fleshImpactTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false)
                { name = "Runtime_SoftFleshImpact", wrapMode = TextureWrapMode.Clamp };
                for (var y = 0; y < 32; y++)
                for (var x = 0; x < 32; x++)
                {
                    var dx = (x - 15.5f) / 15.5f;
                    var dy = (y - 15.5f) / 15.5f;
                    var alpha = Mathf.Pow(Mathf.Clamp01(1f - dx * dx - dy * dy), 2f);
                    fleshImpactTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                fleshImpactTexture.Apply(false, true);
                fleshImpactMaterial.mainTexture = fleshImpactTexture;
            }

            if (muzzleBlast == null)
                muzzleBlast = muzzle.GetComponent<MuzzleBlastVfx>() ?? muzzle.gameObject.AddComponent<MuzzleBlastVfx>();

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
            main.duration = 0.045f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.018f, 0.04f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.024f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.94f, 0.68f, 0.75f),
                new Color(1f, 0.45f, 0.16f, 0.35f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 20;

            var emission = muzzleFlash.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 3, 5) });

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
