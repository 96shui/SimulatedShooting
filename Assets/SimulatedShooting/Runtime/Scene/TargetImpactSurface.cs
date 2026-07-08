using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimulatedShooting.Scene
{
    public readonly struct TargetImpactPoint
    {
        public TargetImpactPoint(Vector3 worldPoint, Vector2 offsetCm, bool insideTenRing)
        {
            WorldPoint = worldPoint;
            OffsetCm = offsetCm;
            InsideTenRing = insideTenRing;
        }

        public Vector3 WorldPoint { get; }
        public Vector2 OffsetCm { get; }
        public bool InsideTenRing { get; }
    }

    [DisallowMultipleComponent]
    public sealed class TargetImpactSurface : MonoBehaviour
    {
        [SerializeField] private Collider impactCollider;
        [SerializeField] private Transform targetCenter;
        [SerializeField] private Transform impactMarkerRoot;
        [SerializeField] private Material impactMarkerMaterial;
        [SerializeField] private float faceWidthMetres = 0.5f;
        [SerializeField] private float faceHeightMetres = 0.5f;
        [SerializeField] private float tenRingRadiusMetres = 0.05f;
        [SerializeField] private float markerDiameterMetres = 0.018f;

        private readonly List<TargetImpactPoint> impacts = new List<TargetImpactPoint>();

        public event Action<TargetImpactPoint> ImpactRecorded;

        public Transform TargetCenter => targetCenter;
        public Transform ImpactMarkerRoot => impactMarkerRoot;
        public IReadOnlyList<TargetImpactPoint> Impacts => impacts;
        public float FaceWidthCm => faceWidthMetres * 100f;
        public float FaceHeightCm => faceHeightMetres * 100f;
        public float TenRingRadiusCm => tenRingRadiusMetres * 100f;

        public void Configure(Collider surfaceCollider, Transform center, Transform markerRoot, Material markerMaterial)
        {
            impactCollider = surfaceCollider;
            targetCenter = center;
            impactMarkerRoot = markerRoot;
            impactMarkerMaterial = markerMaterial;
        }

        public bool TryRecordRay(Ray ray, float maxDistance, out TargetImpactPoint impact)
        {
            if (!Physics.Raycast(ray, out var hit, maxDistance) || hit.collider != impactCollider)
            {
                impact = default;
                return false;
            }

            return TryRecordWorldPoint(hit.point, out impact);
        }

        public bool TryRecordWorldPoint(Vector3 worldPoint, out TargetImpactPoint impact)
        {
            if (targetCenter == null || impactMarkerRoot == null || !TryComputeOffsetCm(worldPoint, out var offsetCm))
            {
                impact = default;
                return false;
            }

            var localPoint = new Vector3(offsetCm.x * 0.01f, offsetCm.y * 0.01f, 0f);
            var pointOnFace = targetCenter.TransformPoint(localPoint);
            impact = new TargetImpactPoint(
                pointOnFace,
                offsetCm,
                offsetCm.sqrMagnitude <= TenRingRadiusCm * TenRingRadiusCm);

            impacts.Add(impact);
            CreateImpactMarker(impact);
            ImpactRecorded?.Invoke(impact);
            return true;
        }

        public bool TryComputeOffsetCm(Vector3 worldPoint, out Vector2 offsetCm)
        {
            offsetCm = default;
            if (targetCenter == null)
            {
                return false;
            }

            var localPoint = targetCenter.InverseTransformPoint(worldPoint);
            if (Mathf.Abs(localPoint.x) > faceWidthMetres * 0.5f ||
                Mathf.Abs(localPoint.y) > faceHeightMetres * 0.5f)
            {
                return false;
            }

            offsetCm = new Vector2(localPoint.x, localPoint.y) * 100f;
            return true;
        }

        public void ClearImpacts()
        {
            impacts.Clear();
            if (impactMarkerRoot == null)
                return;

            for (var index = impactMarkerRoot.childCount - 1; index >= 0; index--)
            {
                var marker = impactMarkerRoot.GetChild(index).gameObject;
                if (Application.isPlaying)
                    Destroy(marker);
                else
                    DestroyImmediate(marker);
            }
        }

        private void CreateImpactMarker(TargetImpactPoint impact)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = $"Impact_{impacts.Count:00}";
            marker.transform.SetParent(impactMarkerRoot, true);
            marker.transform.position = impact.WorldPoint - targetCenter.forward * 0.002f;
            marker.transform.localScale = new Vector3(markerDiameterMetres, markerDiameterMetres, 0.004f);
            Destroy(marker.GetComponent<Collider>());

            if (impactMarkerMaterial != null)
                marker.GetComponent<Renderer>().sharedMaterial = impactMarkerMaterial;
        }

        private void OnDrawGizmosSelected()
        {
            if (targetCenter == null)
                return;

            var previousMatrix = Gizmos.matrix;
            Gizmos.matrix = targetCenter.localToWorldMatrix;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(faceWidthMetres, faceHeightMetres, 0.002f));

            Gizmos.color = Color.red;
            const int segments = 32;
            var previous = new Vector3(tenRingRadiusMetres, 0f, 0f);
            for (var index = 1; index <= segments; index++)
            {
                var angle = index * Mathf.PI * 2f / segments;
                var next = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * tenRingRadiusMetres;
                Gizmos.DrawLine(previous, next);
                previous = next;
            }

            Gizmos.matrix = previousMatrix;
        }
    }
}
