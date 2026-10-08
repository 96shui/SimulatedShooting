using UnityEngine;

namespace SimulatedShooting.Scene
{
    /// <summary>Short detonation flash followed by a sourced dust-cloud flipbook and debris.</summary>
    public sealed class CombatExplosionVfx : MonoBehaviour
    {
        const string AtlasResourcePath = "Combat/VFX/GrenadeBlast/Explosion01-nofire_5x5";
        const float FireballSeconds = 0.78f;
        static Sprite[] fireballFrames;
        public float Lifetime = 1.2f;
        ParticleSystem particles;
        SpriteRenderer fireball;
        Transform fireballTransform;
        Light flash;
        AudioSource sound;
        float age;

        void Awake()
        {
            EnsureFireballFrames();
            if (fireballFrames.Length > 0)
            {
                var visual = new GameObject("GrenadeDustCloud_Flipbook");
                visual.transform.SetParent(transform, false);
                visual.transform.localPosition = Vector3.up * 0.85f;
                visual.transform.localScale = Vector3.one * 2.6f;
                fireballTransform = visual.transform;
                fireball = visual.AddComponent<SpriteRenderer>();
                fireball.sprite = fireballFrames[0];
                fireball.sortingOrder = 10;
            }
            particles = gameObject.AddComponent<ParticleSystem>();
            var main = particles.main; main.duration = Lifetime; main.loop = false; main.playOnAwake = false; main.startLifetime = new ParticleSystem.MinMaxCurve(.18f, .5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 11f); main.startSize = new ParticleSystem.MinMaxCurve(.015f, .065f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(.17f,.16f,.14f,1f), new Color(.47f,.43f,.36f,.8f));
            main.gravityModifier = 1.3f;
            var emission = particles.emission; emission.enabled = true; emission.rateOverTime = 0f; emission.SetBursts(new[]{new ParticleSystem.Burst(0, 25)});
            var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .18f;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null) renderer.material = new Material(shader);
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 1.3f;
            flash = gameObject.AddComponent<Light>(); flash.type = LightType.Point; flash.color = new Color(1f,.86f,.68f); flash.intensity = 5f; flash.range = 6f;
            sound = gameObject.AddComponent<AudioSource>(); sound.spatialBlend = 1f; sound.minDistance = 2f; sound.maxDistance = 28f; sound.playOnAwake = false;
            var clip = AudioClip.Create("GrenadeExplosionProcedural", 22050 / 2, 1, 22050, false);
            var samples = new float[22050 / 2];
            var noise = new System.Random(7319);
            var low = 0f;
            for (var i=0;i<samples.Length;i++) { var t=i/(float)samples.Length; low=Mathf.Lerp(low,(float)noise.NextDouble()*2f-1f,.14f); var envelope=Mathf.Exp(-8f*t); samples[i]=(low*.75f+Mathf.Sin(i*.012f)*.25f)*envelope*.65f; }
            clip.SetData(samples,0); sound.clip=clip; sound.Play();
            particles.Play();
        }

        void Update()
        {
            age += Time.deltaTime;
            if (fireball != null)
            {
                var frame = Mathf.Min(fireballFrames.Length - 1,
                    Mathf.FloorToInt(age / FireballSeconds * fireballFrames.Length));
                fireball.sprite = fireballFrames[frame];
                fireball.enabled = age < FireballSeconds;
                var camera = Camera.main;
                if (camera != null)
                {
                    var direction = camera.transform.position - fireballTransform.position;
                    if (direction.sqrMagnitude > 0.001f)
                        fireballTransform.rotation = Quaternion.LookRotation(direction);
                }
            }
            flash.intensity = Mathf.Lerp(5f, 0f, Mathf.Clamp01(age / .07f));
            if (age >= Lifetime) Destroy(gameObject);
        }

        static void EnsureFireballFrames()
        {
            if (fireballFrames != null) return;
            var atlasSprite = Resources.Load<Sprite>(AtlasResourcePath);
            var atlas = atlasSprite != null ? atlasSprite.texture : Resources.Load<Texture2D>(AtlasResourcePath);
            if (atlas == null) { fireballFrames = new Sprite[0]; return; }
            fireballFrames = new Sprite[25];
            var tileWidth = atlas.width / 5f;
            var tileHeight = atlas.height / 5f;
            for (var i = 0; i < fireballFrames.Length; i++)
                fireballFrames[i] = Sprite.Create(atlas,
                    new Rect((i % 5) * tileWidth, (4 - i / 5) * tileHeight, tileWidth, tileHeight),
                    new Vector2(.5f, .5f), 100f);
        }
    }
}
