using System;
using UnityEngine;

namespace SimulatedShooting.Scene
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class BallisticTracerVisual : MonoBehaviour
    {
        const string CorditeResourcePath = "Combat/VFX/CorditeTracer/tracer-round";
        static Sprite[] corditeFrames;
        static Material corditeMaterial;
        static Material defaultTrailMaterial;
        [SerializeField] private float projectileSpeedMetresPerSecond = 720f;
        [SerializeField] private float maximumTrailLengthMetres = 0.9f;
        [SerializeField] private float minimumVisibleSeconds = 0.035f;

        Vector3 start;
        Vector3 end;
        float elapsed;
        float flightDuration;
        LineRenderer trail;
        Transform projectileVisual;
        SpriteRenderer corditeRenderer;
        AudioSource flybySource;
        Action arrival;
        bool configured;
        bool arrived;
        bool enemyShot;

        public float Progress01 =>
            flightDuration > 0f ? Mathf.Clamp01(elapsed / flightDuration) : (arrived ? 1f : 0f);
        public bool HasProjectileVisual => projectileVisual != null;

        public void Configure(
            Vector3 startPoint,
            Vector3 endPoint,
            GameObject projectilePrefab,
            Material projectileMaterial,
            Material trailMaterial,
            AudioClip flybyClip,
            int shotIndex,
            Action onArrival,
            bool readableEnemyShot = false)
        {
            enemyShot = readableEnemyShot;
            if (enemyShot) { minimumVisibleSeconds = .09f; maximumTrailLengthMetres = 1.8f; }
            start = startPoint;
            end = endPoint;
            arrival = onArrival;
            elapsed = 0f;
            arrived = false;
            var distance = Vector3.Distance(start, end);
            flightDuration = Mathf.Max(
                minimumVisibleSeconds,
                distance / Mathf.Max(1f, projectileSpeedMetresPerSecond));

            trail = GetComponent<LineRenderer>();
            trail.useWorldSpace = true;
            trail.positionCount = 2;
            trail.numCapVertices = 2;
            trail.startWidth = enemyShot ? .006f : .0008f;
            trail.endWidth = enemyShot ? .012f : .0024f;
            trail.sharedMaterial = trailMaterial != null ? trailMaterial : GetDefaultTrailMaterial();
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.startColor = new Color(0.72f, 0.69f, 0.64f, 0.012f);
            trail.endColor = new Color(1f, 0.91f, 0.77f, 0.22f);
            trail.SetPosition(0, start);
            trail.SetPosition(1, start);

            transform.position = start;
            transform.rotation = ResolveRotation(start, end);
            CreateProjectileVisual(projectileMaterial);
            ConfigureFlybyAudio(flybyClip, shotIndex);
            configured = true;
        }

        void Update()
        {
            if (!configured || arrived)
            {
                return;
            }

            elapsed += Mathf.Max(0f, Time.deltaTime);
            var progress = Progress01;
            var distance = Vector3.Distance(start, end);
            var trailFraction = distance > 0.001f
                ? Mathf.Clamp01(maximumTrailLengthMetres / distance)
                : 1f;
            var head = Vector3.Lerp(start, end, progress);
            var tail = Vector3.Lerp(start, end, Mathf.Max(0f, progress - trailFraction));

            // Daylight bullets have no persistent luminous beam. Keep a tiny,
            // rapidly fading cue so the accepted shot is still readable in VR.
            var travelled = distance * progress;
            var visibility = Mathf.Lerp(1f, 0.25f, Mathf.Clamp01(travelled / 35f));
            trail.startColor = new Color(0.72f, 0.69f, 0.64f, (enemyShot ? .16f : .012f) * visibility);
            trail.endColor = new Color(1f, 0.91f, 0.77f, (enemyShot ? .85f : .22f) * visibility);

            transform.position = head;
            UpdateCorditeHead(progress, visibility);
            if (trail != null)
            {
                trail.SetPosition(0, tail);
                trail.SetPosition(1, head);
            }

            if (progress < 1f)
            {
                return;
            }

            arrived = true;
            arrival?.Invoke();
            if (trail != null)
            {
                trail.enabled = false;
            }

            if (projectileVisual != null)
            {
                projectileVisual.gameObject.SetActive(false);
            }

            if (flybySource != null)
            {
                flybySource.Stop();
            }

            Destroy(gameObject, 0.05f);
        }

        void CreateProjectileVisual(Material material)
        {
            // The licensed CORDITE sheet is the live shot visual; the small 3D
            // projectile remains a fallback if the sheet is unavailable.
            if (TryLoadCordite())
            {
                var effect = new GameObject("ProjectileVisual_CorditeTracer");
                effect.transform.SetParent(transform, false);
                effect.transform.localScale = Vector3.one * 0.1f;
                corditeRenderer = effect.AddComponent<SpriteRenderer>();
                corditeRenderer.sharedMaterial = corditeMaterial;
                corditeRenderer.sprite = corditeFrames[5];
                corditeRenderer.color = new Color(1f, 1f, 1f, 0.55f);
                projectileVisual = effect.transform;
            }
            else projectileVisual = RealisticProjectileVisual.Create(transform, material);
            projectileVisual.localPosition = Vector3.zero;
        }

        void UpdateCorditeHead(float progress, float visibility)
        {
            if (corditeRenderer == null) return;
            var frame = Mathf.Clamp(Mathf.FloorToInt(5f + progress * 13f), 0, corditeFrames.Length - 1);
            corditeRenderer.sprite = corditeFrames[frame];
            corditeRenderer.color = new Color(1f, 1f, 1f, (enemyShot ? .9f : .55f) * visibility);
            var camera = Camera.main;
            if (camera == null) return;
            var toCamera = camera.transform.position - projectileVisual.position;
            if (toCamera.sqrMagnitude < 0.0001f) return;
            var forward = toCamera.normalized;
            var right = Vector3.ProjectOnPlane(end - start, forward);
            if (right.sqrMagnitude > 0.0001f)
                projectileVisual.rotation = Quaternion.LookRotation(forward, Vector3.Cross(forward, right.normalized));
            else projectileVisual.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        static bool TryLoadCordite()
        {
            if (corditeFrames == null)
            {
                var sheet = Resources.Load<Texture2D>(CorditeResourcePath);
                if (sheet == null) return false;
                corditeFrames = new Sprite[24];
                for (var frame = 0; frame < corditeFrames.Length; frame++)
                {
                    var column = frame % 8;
                    var row = frame / 8;
                    var rect = new Rect(column * 256, (2 - row) * 256, 256, 256);
                    corditeFrames[frame] = Sprite.Create(sheet, rect, new Vector2(0.5f, 0.5f), 256f);
                }
            }
            if (corditeMaterial == null)
            {
                var shader = Shader.Find("SimulatedShooting/CorditeTracerTint");
                if (shader == null) return false;
                corditeMaterial = new Material(shader) { name = "Runtime_CorditeDaylightTracer" };
                corditeMaterial.SetColor("_Tint", new Color(0.92f, 0.87f, 0.78f, 1f));
            }
            return true;
        }

        void ConfigureFlybyAudio(AudioClip clip, int shotIndex)
        {
            if (clip == null)
            {
                return;
            }

            flybySource = gameObject.AddComponent<AudioSource>();
            flybySource.clip = clip;
            flybySource.playOnAwake = false;
            flybySource.loop = false;
            flybySource.spatialBlend = 1f;
            flybySource.rolloffMode = AudioRolloffMode.Logarithmic;
            flybySource.minDistance = 0.25f;
            flybySource.maxDistance = 35f;
            flybySource.dopplerLevel = 0.35f;
            flybySource.volume = 0.34f;
            if (clip.length > 0.4f)
            {
                flybySource.time = (shotIndex * 0.73f) % (clip.length - 0.35f);
            }

            flybySource.Play();
        }

        static Quaternion ResolveRotation(Vector3 from, Vector3 to)
        {
            var direction = to - from;
            return direction.sqrMagnitude > 0.000001f
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : Quaternion.identity;
        }

        static Material GetDefaultTrailMaterial()
        {
            if (defaultTrailMaterial != null) return defaultTrailMaterial;
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
            defaultTrailMaterial = new Material(shader)
            {
                name = "Runtime_SubtleBulletWake",
                color = Color.white
            };
            return defaultTrailMaterial;
        }
    }
}
