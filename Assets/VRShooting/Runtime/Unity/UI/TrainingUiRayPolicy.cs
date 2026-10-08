using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

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
                {
                    ray.gameObject.SetActive(false);
                    continue;
                }

                ray.blockUIOnInteractableSelection = false;
                EnsureActiveHierarchy(ray.transform, xrOrigin.transform);
                ray.enabled = true;
                EnsureUiPressEnabled(ray.uiPressInput);
            }

            foreach (var ray in xrOrigin.GetComponentsInChildren<NearFarInteractor>(true))
            {
                ray.enableUIInteraction = true;
                // XRI resets the UI model when far casting is disabled, even if
                // enableUIInteraction and the trigger action are both enabled.
                ray.enableFarCasting = true;
                ray.blockUIOnInteractableSelection = false;
                ray.enabled = true;
                EnsureActiveHierarchy(ray.transform, xrOrigin.transform);
                EnsureUiPressEnabled(ray.uiPressInput);
            }
        }

        static void EnsureActiveHierarchy(Transform target, Transform origin)
        {
            for (var current = target; current != null && current != origin; current = current.parent)
                current.gameObject.SetActive(true);
        }

        static void EnsureUiPressEnabled(XRInputButtonReader input)
        {
            if (input == null) return;
            if (input.inputSourceMode == XRInputButtonReader.InputSourceMode.InputActionReference)
            {
                input.inputActionReferencePerformed?.action?.Enable();
                input.inputActionReferenceValue?.action?.Enable();
            }
            else
                input.EnableDirectActionIfModeUsed();
        }
    }
}
