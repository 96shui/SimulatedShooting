using System.Collections.Generic;
using UnityEngine;

namespace SimulatedShooting.Scene
{
    /// <summary>Lightweight copper-jacketed rifle projectile mesh for visible tracer heads.</summary>
    internal static class RealisticProjectileVisual
    {
        static readonly Vector2[] Profile =
        {
            new Vector2(0f, 0.48f),       // boat tail
            new Vector2(0.06f, 0.74f),
            new Vector2(0.14f, 1f),
            new Vector2(0.29f, 1f),
            new Vector2(0.32f, 0.94f),    // cannelure
            new Vector2(0.35f, 1f),
            new Vector2(0.57f, 1f),
            new Vector2(0.68f, 0.9f),
            new Vector2(0.79f, 0.7f),
            new Vector2(0.88f, 0.43f),
            new Vector2(0.96f, 0.17f),
            new Vector2(1f, 0.015f)
        };
        const int RadialSegments = 20;
        static Mesh sharedMesh;
        static Material defaultJacketMaterial;

        internal static Transform Create(Transform parent, Material jacketMaterial)
        {
            var gameObject = new GameObject("ProjectileVisual_training-rifle_CopperJacket");
            gameObject.transform.SetParent(parent, false);
            gameObject.AddComponent<MeshFilter>().sharedMesh = GetMesh();
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = jacketMaterial != null ? jacketMaterial : GetDefaultJacketMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return gameObject.transform;
        }

        static Mesh GetMesh()
        {
            if (sharedMesh != null) return sharedMesh;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            const float length = 0.027f;
            const float radius = 0.003f;
            for (var ring = 0; ring < Profile.Length; ring++)
            {
                var profile = Profile[ring];
                for (var segment = 0; segment <= RadialSegments; segment++)
                {
                    var angle = segment * Mathf.PI * 2f / RadialSegments;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * radius * profile.y,
                        Mathf.Sin(angle) * radius * profile.y, profile.x * length));
                    uvs.Add(new Vector2(segment / (float)RadialSegments, profile.x));
                }
            }
            for (var ring = 0; ring < Profile.Length - 1; ring++)
            for (var segment = 0; segment < RadialSegments; segment++)
            {
                var a = ring * (RadialSegments + 1) + segment;
                var b = a + RadialSegments + 1;
                triangles.Add(a); triangles.Add(a + 1); triangles.Add(b);
                triangles.Add(a + 1); triangles.Add(b + 1); triangles.Add(b);
            }
            sharedMesh = new Mesh
            {
                name = "RifleProjectile_556_CopperJacket",
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray(),
                uv = uvs.ToArray()
            };
            sharedMesh.RecalculateNormals();
            sharedMesh.RecalculateBounds();
            return sharedMesh;
        }

        static Material GetDefaultJacketMaterial()
        {
            if (defaultJacketMaterial != null) return defaultJacketMaterial;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            defaultJacketMaterial = new Material(shader)
            {
                name = "Runtime_CopperBulletJacket",
                color = new Color(0.52f, 0.31f, 0.18f)
            };
            if (defaultJacketMaterial.HasProperty("_Metallic"))
                defaultJacketMaterial.SetFloat("_Metallic", 0.72f);
            if (defaultJacketMaterial.HasProperty("_Smoothness"))
                defaultJacketMaterial.SetFloat("_Smoothness", 0.55f);
            return defaultJacketMaterial;
        }
    }
}
