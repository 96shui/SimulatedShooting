using VRShooting.Application;
using VRShooting.Unity.Bootstrap;
using UnityEngine;

namespace VRShooting.Unity.UI
{
    /// <summary>
    /// Scene-owned UI for the 100m zeroing range.
    /// </summary>
    public sealed class ZeroingRangeUI : TrainingUIRoot
    {
        protected override TrainingUIScreenGroup ScreenGroup => TrainingUIScreenGroup.ZeroingRange;

        protected override string RootObjectName => nameof(ZeroingRangeUI);

        public static ZeroingRangeUI EnsureExistsInScene(ApplicationServices applicationServices = null)
        {
            var ui = FindObjectOfType<ZeroingRangeUI>(true);
            if (ui == null)
            {
                var uiRoot = new GameObject(nameof(ZeroingRangeUI), typeof(RectTransform));
                ui = uiRoot.AddComponent<ZeroingRangeUI>();
            }

            var services = applicationServices ?? GameMain.Instance?.Services;
            if (services != null && !ui.IsInitialized)
            {
                ui.Initialize(services);
            }

            return ui;
        }
    }
}
