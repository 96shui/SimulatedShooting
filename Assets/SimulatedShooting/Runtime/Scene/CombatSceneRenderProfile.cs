using UnityEngine;
using UnityEngine.Rendering;

namespace SimulatedShooting.Scene
{
    public sealed class CombatSceneRenderProfile : MonoBehaviour
    {
        public RenderPipelineAsset Profile;
        RenderPipelineAsset previous;
        ShadowQuality previousShadows;
        float previousDistance;
        bool applied;

        void OnEnable()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Do not replace the headset budget with the desktop four-cascade profile.
            var mobile = Resources.Load<RenderPipelineAsset>(PicoStandaloneRenderPolicy.ProfilePath);
            if (mobile != null) QualitySettings.renderPipeline = mobile;
            return;
#else
            if (Profile == null || applied) return;
            previous = QualitySettings.renderPipeline;
            previousShadows = QualitySettings.shadows;
            previousDistance = QualitySettings.shadowDistance;
            QualitySettings.renderPipeline = Profile;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = 90;
            applied = true;
#endif
        }

        void OnDisable()
        {
            if (!applied) return;
            if (QualitySettings.renderPipeline == Profile)
            {
                QualitySettings.renderPipeline = previous;
                QualitySettings.shadows = previousShadows;
                QualitySettings.shadowDistance = previousDistance;
            }
            applied = false;
        }
    }
}
