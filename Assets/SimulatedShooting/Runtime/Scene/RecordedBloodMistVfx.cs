using UnityEngine;

namespace SimulatedShooting.Scene
{
    /// <summary>ActionVFX's recorded Blood Mist 1, keyed from its white background.</summary>
    public sealed class RecordedBloodMistVfx : MonoBehaviour
    {
        static Material sharedMaterial;
        MaterialPropertyBlock properties;
        MeshRenderer meshRenderer;
        Transform viewer;
        float started;
        const float Lifetime = .85f;

        public bool Initialize(int shotIndex)
        {
            if (sharedMaterial == null)
                sharedMaterial = Resources.Load<Material>("Combat/VFX/BloodMist/ActionVFX_BloodMist1");
            if (sharedMaterial == null) { enabled = false; return false; }
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "RecordedBloodMist";
            quad.transform.SetParent(transform, false);
            var collider = quad.GetComponent<Collider>();
            collider.enabled = false;
            if (Application.isPlaying) Destroy(collider);
            else DestroyImmediate(collider);
            quad.transform.localScale = new Vector3(1.05f, .554f, 1);
            meshRenderer = quad.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = sharedMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            properties = new MaterialPropertyBlock();
            viewer = Camera.main != null ? Camera.main.transform : null;
            if (viewer != null)
            {
                transform.position += (viewer.position - transform.position).normalized * .06f;
                transform.rotation = Quaternion.LookRotation(transform.position - viewer.position);
            }
            started = Time.time;
            // Reproducible variation; no unseeded random state.
            transform.localScale = Vector3.one * (1f + (shotIndex % 3) * .06f);
            UpdateFrame(0);
            return true;
        }

        void LateUpdate()
        {
            var age = Time.time - started;
            if (age >= Lifetime) { Destroy(gameObject); return; }
            if (viewer == null && Camera.main != null) viewer = Camera.main.transform;
            if (viewer != null)
                transform.rotation = Quaternion.LookRotation(transform.position - viewer.position);
            UpdateFrame(Mathf.Clamp(Mathf.FloorToInt(age / Lifetime * 64), 0, 63));
        }

        void UpdateFrame(int frame)
        {
            properties.SetVector("_FrameST", new Vector4(.125f, .125f,
                (frame % 8) * .125f, (7 - frame / 8) * .125f));
            meshRenderer.SetPropertyBlock(properties);
        }
    }
}
