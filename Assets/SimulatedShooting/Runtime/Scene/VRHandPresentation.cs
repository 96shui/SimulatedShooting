using System.Collections.Generic;
using UnityEngine;

namespace SimulatedShooting.Scene
{
    // Presentation on a tracked controller only; never disables interactors or UI rays.
    [DisallowMultipleComponent]
    public sealed class VRHandPresentation : MonoBehaviour
    {
        readonly List<Renderer> hidden = new List<Renderer>();
        readonly List<Renderer> renderers = new List<Renderer>();
        float nextScan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= InstallSceneHands;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += InstallSceneHands;
        }
        static void InstallSceneHands(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            foreach(var root in scene.GetRootGameObjects())
                foreach(var origin in root.GetComponentsInChildren<Unity.XR.CoreUtils.XROrigin>(true))
                    EnsureOnRig(origin.gameObject);
        }

        public static void EnsureOnRig(GameObject rig, TrainingRifleGrabInteractable grab = null)
        {
            if (rig == null) return;
            foreach (var controller in rig.GetComponentsInChildren<Transform>(true))
            {
                if (controller.name != "Left Controller" && controller.name != "Right Controller") continue;
                if (controller.GetComponentInChildren<VRControllerHandVisual>(true) != null) continue;
                var side = controller.name.StartsWith("Left") ? "Left" : "Right";
                var prefab = Resources.Load<GameObject>("Combat/Hands/VirtualHand_" + side);
                if (prefab != null) Instantiate(prefab, controller, false);
            }
            foreach (var hand in rig.GetComponentsInChildren<VRControllerHandVisual>(true))
            {
                if (hand.ModelRoot == null || hand.transform.parent == null) continue;
                if (grab != null) hand.BindWeapon(grab);
                var controller = hand.transform.parent.gameObject;
                var presentation = controller.GetComponent<VRHandPresentation>() ?? controller.AddComponent<VRHandPresentation>();
                presentation.RefreshModels();
            }
        }

        void OnEnable() { RefreshModels(); Application.onBeforeRender += HideHardware; }
        void OnDisable() { Application.onBeforeRender -= HideHardware; }
        void LateUpdate()
        {
            if (Time.unscaledTime >= nextScan) RefreshModels();
            HideHardware();
        }
        void RefreshModels()
        {
            nextScan = Time.unscaledTime + .5f;
            hidden.Clear();
            GetComponentsInChildren(true, renderers);
            foreach (var renderer in renderers)
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                // Scene rigs are inactive during hand installation and scene handoff.
                // Include inactive parents so human hands never enter the hardware hide list.
                if (renderer.GetComponentInParent<VRControllerHandVisual>(true) != null) continue;
                // A selected weapon can be parented by a custom rig; its mesh is not hardware.
                if (renderer.GetComponentInParent<TrainingRifleGrabInteractable>(true) != null) continue;
                hidden.Add(renderer);
            }
            HideHardware();
        }
        void HideHardware()
        {
            foreach (var renderer in hidden) if (renderer != null) renderer.enabled = false;
        }
    }
}
