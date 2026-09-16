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
            if (Profile == null || applied) return;
            previous = QualitySettings.renderPipeline;
            previousShadows = QualitySettings.shadows;
            previousDistance = QualitySettings.shadowDistance;
            QualitySettings.renderPipeline = Profile;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = 90;
            applied = true;
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
