using UnityEngine;

namespace SimulatedShooting.Scene
{
    /// <summary>Self-contained grenade blast VFX; no external particle asset is required.</summary>
    public sealed class CombatExplosionVfx : MonoBehaviour
    {
        public float Lifetime = 1.15f;
        ParticleSystem particles;
        Light flash;
        AudioSource sound;
        float age;

        void Awake()
        {
            particles = gameObject.AddComponent<ParticleSystem>();
            var main = particles.main; main.duration = Lifetime; main.loop = false; main.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 7f); main.startSize = new ParticleSystem.MinMaxCurve(.06f, .22f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f,.28f,.03f,1f), new Color(1f,.88f,.25f,1f));
            var emission = particles.emission; emission.enabled = true; emission.SetBursts(new[]{new ParticleSystem.Burst(0, 42)});
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .18f;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null) renderer.material = new Material(shader);
            var smokeObject = new GameObject("Smoke"); smokeObject.transform.SetParent(transform, false);
            var smoke = smokeObject.AddComponent<ParticleSystem>();
            var smokeMain = smoke.main; smokeMain.duration = Lifetime; smokeMain.loop = false;
            smokeMain.startLifetime = new ParticleSystem.MinMaxCurve(.65f, 1.1f);
            smokeMain.startSpeed = new ParticleSystem.MinMaxCurve(.7f, 2.1f);
            smokeMain.startSize = new ParticleSystem.MinMaxCurve(.35f, .8f);
            smokeMain.startColor = new ParticleSystem.MinMaxGradient(new Color(.21f,.21f,.21f,.7f), new Color(.43f,.4f,.37f,.45f));
            var smokeEmission = smoke.emission; smokeEmission.enabled = true; smokeEmission.SetBursts(new[]{new ParticleSystem.Burst(0, 30)});
            var smokeShape = smoke.shape; smokeShape.shapeType = ParticleSystemShapeType.Sphere; smokeShape.radius = .24f;
            var smokeRenderer = smoke.GetComponent<ParticleSystemRenderer>(); if(shader!=null)smokeRenderer.material = new Material(shader);
            smoke.Play();
            flash = gameObject.AddComponent<Light>(); flash.type = LightType.Point; flash.color = new Color(1f,.35f,.08f); flash.intensity = 8f; flash.range = 6f;
            sound = gameObject.AddComponent<AudioSource>(); sound.spatialBlend = 1f; sound.minDistance = 2f; sound.maxDistance = 28f; sound.playOnAwake = false;
            var clip = AudioClip.Create("GrenadeExplosionProcedural", 22050 / 2, 1, 22050, false);
            var samples = new float[22050 / 2];
            for (var i=0;i<samples.Length;i++) { var t=i/(float)samples.Length; var envelope=Mathf.Exp(-7f*t); samples[i]=(Mathf.Sin(i*.22f)+Mathf.Sin(i*.071f)*.45f+UnityEngine.Random.value*2f-1f)*envelope*.45f; }
            clip.SetData(samples,0); sound.clip=clip; sound.Play();
            particles.Play();
        }

        void Update()
        {
            age += Time.deltaTime;
            flash.intensity = Mathf.Lerp(8f, 0f, Mathf.Clamp01(age / .22f));
            if (age >= Lifetime) Destroy(gameObject);
        }
    }
}
