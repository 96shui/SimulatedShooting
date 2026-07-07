using UnityEngine;

namespace SimulatedShooting.Scene
{
    public sealed class TimedSelfDestruct : MonoBehaviour
    {
        [SerializeField] private float lifetimeSeconds = 0.25f;

        public void Configure(float lifetime)
        {
            lifetimeSeconds = Mathf.Max(0.01f, lifetime);
        }

        private void Update()
        {
            lifetimeSeconds -= Time.deltaTime;
            if (lifetimeSeconds > 0f)
            {
                return;
            }

            Destroy(gameObject);
        }
    }
}
