using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRShooting.Common;

namespace SimulatedShooting.Scene
{
    // Presentation on a tracked controller only; never disables interactors or UI rays.
    [DisallowMultipleComponent]
    public sealed class VRHandPresentation : MonoBehaviour
    {
        readonly List<Renderer> hidden = new List<Renderer>();
        readonly List<Renderer> renderers = new List<Renderer>();
        readonly Dictionary<Renderer,bool> menuHandRenderers = new Dictionary<Renderer,bool>();
        Unity.XR.CoreUtils.XROrigin menuOrigin;
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
            if (grab != null) ApplyShoulder(grab, grab.Shoulder);
        }

        public static void ApplyShoulder(TrainingRifleGrabInteractable weapon, ShoulderSide side)
        {
            var hands = FindObjectsOfType<VRControllerHandVisual>(true).Where(h => h.Weapon == weapon).ToArray();
            // Capture all home parents before moving either source hand to the other controller.
            var homes = hands.ToDictionary(h => h, h => h.HomeControllerParent);
            foreach (var hand in hands)
            {
                var home = homes[hand];
                var partner = hands.FirstOrDefault(h => h.SourceHandSide != hand.SourceHandSide &&
                    (homes[h] == home || home != null && homes[h] != null && homes[h].parent == home.parent));
                var parent = side == ShoulderSide.Left && partner != null ? homes[partner] : home;
                hand.SetShoulderPresentation(side, parent);
            }
        }

        void OnEnable() { RefreshModels(); Application.onBeforeRender += HideHardware; }
        void OnDisable() { Application.onBeforeRender -= HideHardware; RestoreMenuHands(); }
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
                if (renderer.GetComponentInParent<VRControllerHandVisual>(true) != null)
                {
                    if(gameObject.scene.name=="MainScene"&&!menuHandRenderers.ContainsKey(renderer))
                        menuHandRenderers.Add(renderer,renderer.forceRenderingOff);
                    continue;
                }
                // A selected weapon can be parented by a custom rig; its mesh is not hardware.
                if (renderer.GetComponentInParent<TrainingRifleGrabInteractable>(true) != null) continue;
                hidden.Add(renderer);
            }
            HideHardware();
        }
        [BeforeRenderOrder(UnityEngine.XR.Interaction.Toolkit.XRInteractionUpdateOrder.k_BeforeRenderOrder + 2)]
        void HideHardware()
        {
            foreach (var renderer in hidden) if (renderer != null) renderer.enabled = false;
            UpdateMenuHandVisibility();
        }

        void RestoreMenuHands()
        {
            foreach(var entry in menuHandRenderers)
                if(entry.Key!=null)entry.Key.forceRenderingOff=entry.Value;
            menuHandRenderers.Clear();
        }

        void UpdateMenuHandVisibility()
        {
            if(gameObject.scene.name!="MainScene")
            {
                if(menuHandRenderers.Count>0)RestoreMenuHands();
                return;
            }
            if(menuOrigin==null)menuOrigin=GetComponentInParent<Unity.XR.CoreUtils.XROrigin>(true);
            var camera=menuOrigin!=null?menuOrigin.Camera:null;
            if(camera==null)return;
            var eye=camera.transform.position;
            var visible=HasPositionTracking()&&Vector3.SqrMagnitude(transform.position-eye)>=.15f*.15f;
            foreach(var entry in menuHandRenderers)
            {
                var renderer=entry.Key;
                if(renderer==null)continue;
                // Hide only the mesh, never the tracked controller, UI ray or its input.
                // The forearm can extend behind a valid hand and still enclose the eye.
                renderer.forceRenderingOff=entry.Value||!visible||renderer.bounds.SqrDistance(eye)<.03f*.03f;
            }
        }

        bool HasPositionTracking()
        {
            var left=gameObject.name.StartsWith("Left");
            var device=left?UnityEngine.InputSystem.XR.XRController.leftHand:UnityEngine.InputSystem.XR.XRController.rightHand;
            if(device!=null&&device.added)
                return device.enabled&&device.isTracked.isPressed&&(device.trackingState.ReadValue()&1)!=0;
            var legacy=UnityEngine.XR.InputDevices.GetDeviceAtXRNode(left?UnityEngine.XR.XRNode.LeftHand:UnityEngine.XR.XRNode.RightHand);
            return legacy.isValid&&legacy.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked,out var tracked)&&tracked&&
                legacy.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trackingState,out var state)&&
                (state&UnityEngine.XR.InputTrackingState.Position)!=0;
        }
    }
}
