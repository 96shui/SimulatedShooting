using UnityEngine;

namespace SimulatedShooting.Scene
{
    [DisallowMultipleComponent]
    public sealed class PlayerFootstepAudio : MonoBehaviour
    {
        [SerializeField] private Transform trackedTransform;
        [SerializeField] private AudioClip[] footstepClips;
        [SerializeField] private float strideLengthMetres = 0.62f;
        [SerializeField] private float minimumPlanarSpeed = 0.20f;
        [SerializeField] private float teleportDistanceMetres = 1.8f;

        AudioSource audioSource;
        Vector3 lastPosition;
        Vector3 lastStepPosition;
        bool hasPosition;
        int nextClipIndex;
        int playedStepCount;

        public bool HasFootstepClips => footstepClips != null && footstepClips.Length > 0;
        public int PlayedStepCount => playedStepCount;
        public Transform TrackedTransform => trackedTransform;

        public void Configure(Transform movementSource, AudioClip[] clips)
        {
            trackedTransform = movementSource;
            footstepClips = clips;
            ResetTracking();
            EnsureAudioSource();
        }

        void Awake()
        {
            EnsureAudioSource();
            ResetTracking();
        }

        void Update()
        {
            if (trackedTransform != null)
            {
                EvaluatePosition(trackedTransform.position, Time.deltaTime);
            }
        }

        public void EvaluatePositionForTests(Vector3 position, float deltaTime)
        {
            EvaluatePosition(position, deltaTime);
        }

        public void ResetTracking()
        {
            hasPosition = false;
        }

        void EvaluatePosition(Vector3 worldPosition, float deltaTime)
        {
            var planar = new Vector3(worldPosition.x, 0f, worldPosition.z);
            if (!hasPosition)
            {
                lastPosition = planar;
                lastStepPosition = planar;
                hasPosition = true;
                return;
            }

            var frameDistance = Vector3.Distance(planar, lastPosition);
            lastPosition = planar;
            if (frameDistance >= teleportDistanceMetres)
            {
                lastStepPosition = planar;
                return;
            }

            var speed = deltaTime > 0.0001f ? frameDistance / deltaTime : 0f;
            if (speed < minimumPlanarSpeed)
            {
                return;
            }

            if (Vector3.Distance(planar, lastStepPosition) < strideLengthMetres)
            {
                return;
            }

            lastStepPosition = planar;
            PlayNextStep();
        }

        void PlayNextStep()
        {
            if (!HasFootstepClips)
            {
                return;
            }

            EnsureAudioSource();
            var clip = footstepClips[nextClipIndex % footstepClips.Length];
            nextClipIndex++;
            if (clip == null)
            {
                return;
            }

            playedStepCount++;
            audioSource.pitch = 0.97f + (playedStepCount % 3) * 0.025f;
            audioSource.PlayOneShot(clip, 0.58f);
        }

        void EnsureAudioSource()
        {
            if (audioSource != null)
            {
                return;
            }

            var audioObject = new GameObject("Audio_Player_Footsteps");
            audioObject.transform.SetParent(transform, false);
            audioObject.AddComponent<SceneTestId>().Id = "ZeroingRange.Player.Footsteps";
            audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0.72f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 0.4f;
            audioSource.maxDistance = 18f;
            audioSource.dopplerLevel = 0f;
        }
    }
}
