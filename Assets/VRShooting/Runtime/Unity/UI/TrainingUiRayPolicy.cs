using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRShooting.Unity.UI
{
    /// <summary>Keeps fixed training menus on the XR UI ray instead of the teleport ray.</summary>
    public static class TrainingUiRayPolicy
    {
        const string ControllerActionManagerType =
            "UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets.ControllerInputActionManager";

        public static void KeepUiRayAvailable(GameObject xrOrigin)
        {
            if (xrOrigin == null)
                return;

            // The starter controller manager replaces NearFarInteractor with a
            // teleport ray when the teleport action fires. That ray cannot hit UI.
            foreach (var behaviour in xrOrigin.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour != null && behaviour.GetType().FullName == ControllerActionManagerType)
                    behaviour.enabled = false;
            }

            foreach (var ray in xrOrigin.GetComponentsInChildren<XRRayInteractor>(true))
            {
                if (!ray.enableUIInteraction)
                    ray.gameObject.SetActive(false);
            }

            foreach (var ray in xrOrigin.GetComponentsInChildren<NearFarInteractor>(true))
            {
                ray.enableUIInteraction = true;
                for (var current = ray.transform; current != null && current != xrOrigin.transform;
                     current = current.parent)
                    current.gameObject.SetActive(true);
            }
        }
    }
}
