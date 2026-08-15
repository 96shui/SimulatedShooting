using UnityEngine;

namespace SimulatedShooting.Scene
{
    [DisallowMultipleComponent]
    public sealed class MovingTargetFakeTimeline : MonoBehaviour
    {
        static readonly MovingTargetVisualState[] Frames =
        {
            new MovingTargetVisualState(0f, MovingTargetTravelDirection.Stationary, true, false, 0f),
            new MovingTargetVisualState(0.25f, MovingTargetTravelDirection.RightToLeft, false, true, 3f),
            new MovingTargetVisualState(0.50f, MovingTargetTravelDirection.RightToLeft, false, true, 4f),
            new MovingTargetVisualState(0.75f, MovingTargetTravelDirection.RightToLeft, false, true, 5f),
            new MovingTargetVisualState(1f, MovingTargetTravelDirection.Stationary, true, false, 0f),
            new MovingTargetVisualState(0.75f, MovingTargetTravelDirection.LeftToRight, false, true, 5f),
            new MovingTargetVisualState(0f, MovingTargetTravelDirection.Stationary, true, false, 0f),
            new MovingTargetVisualState(0f, MovingTargetTravelDirection.RightToLeft, true, false, 0f)
        };

        [SerializeField] MovingTargetVisualDriver visualDriver;
        [SerializeField, Range(0, 7)] int previewFrame;

        public int FrameCount => Frames.Length;
        public int PreviewFrame => previewFrame;

        public void Configure(MovingTargetVisualDriver driver)
        {
            visualDriver = driver;
            ApplyFrame(0);
        }

        public MovingTargetVisualState GetFrame(int index)
        {
            return Frames[Mathf.Clamp(index, 0, Frames.Length - 1)];
        }

        public void ApplyFrame(int index)
        {
            previewFrame = Mathf.Clamp(index, 0, Frames.Length - 1);
            if (visualDriver != null)
                visualDriver.Apply(Frames[previewFrame]);
        }

        [ContextMenu("Preview Next Fake Timeline Frame")]
        public void PreviewNextFrame()
        {
            ApplyFrame((previewFrame + 1) % Frames.Length);
        }

        void Awake()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            enabled = false;
#endif
        }
    }
}
