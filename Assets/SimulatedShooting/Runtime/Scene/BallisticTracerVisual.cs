using System;
using System.Linq;
using UnityEngine;

namespace SimulatedShooting.Scene
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class BallisticTracerVisual : MonoBehaviour
    {
        [SerializeField] private float projectileSpeedMetresPerSecond = 720f;
        [SerializeField] private float maximumTrailLengthMetres = 2.8f;
        [SerializeField] private float minimumVisibleSeconds = 0.06f;

        Vector3 start;
        Vector3 end;
        float elapsed;
        float flightDuration;
        LineRenderer trail;
        Transform projectileVisual;
        AudioSource flybySource;
        Action arrival;
        bool configured;
        bool arrived;

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
            Action onArrival)
        {
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
            trail.numCapVertices = 4;
            trail.startWidth = 0.014f;
            trail.endWidth = 0.003f;
            trail.sharedMaterial = trailMaterial;
            trail.startColor = new Color(1f, 0.92f, 0.52f, 0.95f);
            trail.endColor = new Color(1f, 0.28f, 0.04f, 0.08f);
            trail.SetPosition(0, start);
            trail.SetPosition(1, start);

            transform.position = start;
            transform.rotation = ResolveRotation(start, end);
            CreateProjectileVisual(projectilePrefab, projectileMaterial);
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

            transform.position = head;
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

        void CreateProjectileVisual(GameObject prefab, Material material)
        {
            GameObject visual;
            if (prefab != null)
            {
                visual = Instantiate(prefab, transform);
                visual.name = "ProjectileVisual_training-rifle";
            }
            else
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.name = "ProjectileVisual_training-rifle_Fallback";
                visual.transform.SetParent(transform, false);
                var collider = visual.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }
            }

            projectileVisual = visual.transform;
            projectileVisual.localPosition = Vector3.zero;
            NormalizeProjectileVisual(projectileVisual, material);
        }

        static void NormalizeProjectileVisual(Transform visual, Material material)
        {
            var renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            if (material != null)
            {
                foreach (var renderer in renderers)
                {
                    var materials = renderer.sharedMaterials;
                    for (var index = 0; index < materials.Length; index++)
                    {
                        materials[index] = material;
                    }

                    renderer.sharedMaterials = materials;
                }
            }

            var filters = visual.GetComponentsInChildren<MeshFilter>(true);
            var largestFilter = filters
                .Where(filter => filter.sharedMesh != null)
                .OrderByDescending(filter => filter.sharedMesh.bounds.size.magnitude)
                .FirstOrDefault();
            if (largestFilter == null)
            {
                visual.localScale = Vector3.one * 0.018f;
                return;
            }

            var size = largestFilter.sharedMesh.bounds.size;
            var largest = Mathf.Max(size.x, size.y, size.z);
            var longAxis = size.x >= size.y && size.x >= size.z
                ? Vector3.right
                : size.y >= size.z ? Vector3.up : Vector3.forward;
            visual.localRotation = Quaternion.FromToRotation(longAxis, Vector3.forward);
            visual.localScale = Vector3.one * (0.045f / Mathf.Max(0.0001f, largest));
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
    }
}
