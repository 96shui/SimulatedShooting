using UnityEngine;

namespace SimulatedShooting.Scene
{
    /// <summary>Brief rifle muzzle combustion and light shared by player and actors.</summary>
    [DisallowMultipleComponent]
    public sealed class MuzzleBlastVfx : MonoBehaviour
    {
        static Material flameMaterial;
        ParticleSystem flame;
        Light flashLight;
        float lightUntil;

        public void Play()
        {
            EnsureCreated();
            flame.Play(true);
            flashLight.enabled = true;
            flashLight.intensity = 1.2f;
            lightUntil = Time.time + 0.03f;
        }

        void Update()
        {
            if (flashLight != null && flashLight.enabled && Time.time >= lightUntil)
                flashLight.enabled = false;
        }

        void EnsureCreated()
        {
            if (flame != null) return;
            flame = CreateParticles("MuzzleCombustion", 0.04f, 0.018f, 0.038f,
                1.1f, 2.5f, 0.028f, 0.065f, 4,
                new Color(1f, 0.96f, 0.78f, 0.8f), new Color(1f, 0.58f, 0.23f, 0.38f));
            flashLight = gameObject.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.color = new Color(1f, 0.56f, 0.22f);
            flashLight.range = 1.8f;
            flashLight.shadows = LightShadows.None;
            flashLight.enabled = false;
        }

        ParticleSystem CreateParticles(string objectName, float duration, float minLifetime,
            float maxLifetime, float minSpeed, float maxSpeed, float minSize, float maxSize,
            short count, Color minColor, Color maxColor)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(transform, false);
            child.transform.localPosition = Vector3.forward * 0.025f;
            var system = child.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = duration;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLifetime, maxLifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startColor = new ParticleSystem.MinMaxGradient(minColor, maxColor);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 20;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 11f;
            shape.radius = 0.008f;
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = objectName == "MuzzleCombustion" ? 1.6f : 0.5f;
            renderer.velocityScale = 0.07f;
            renderer.sharedMaterial = GetFlameMaterial();
            return system;
        }

        static Material GetFlameMaterial()
        {
            if (flameMaterial != null) return flameMaterial;
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false)
            {
                name = "Runtime_SoftMuzzleParticle",
                wrapMode = TextureWrapMode.Clamp
            };
            for (var y = 0; y < 64; y++)
            for (var x = 0; x < 64; x++)
            {
                var dx = (x - 31.5f) / 31.5f;
                var dy = (y - 31.5f) / 31.5f;
                var alpha = Mathf.Pow(Mathf.Clamp01(1f - dx * dx - dy * dy), 2.5f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            texture.Apply(false, true);
            var shader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");
            flameMaterial = new Material(shader) { name = "Runtime_MuzzleCombustion" };
            flameMaterial.mainTexture = texture;
            return flameMaterial;
        }
    }
}
