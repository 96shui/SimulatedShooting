using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using Unity.XR.CoreUtils;
using VRShooting.Unity.UI;

namespace SimulatedShooting.Editor
{
    // Opt-in live diagnostics. Does not replace hardware or launch a test run.
    [InitializeOnLoad]
    public static class MainMenuXrLiveDiagnostics
    {
        const string Request = "Logs/mainmenu-xr-recovery/live-diagnostics.request";
        const string Recover = "Logs/mainmenu-xr-recovery/clean-start.request";
        static double nextPoll;
        static double nextPlayerCapture;
        static MainMenuXrLiveDiagnostics()
        {
            EditorApplication.update += Poll;
            InputSystem.onAfterUpdate += CapturePlayerInput;
        }

        static void CapturePlayerInput()
        {
            // Editor updates use a separate state buffer; sample actual playing-device
            // values only while the Dynamic/BeforeRender player buffer is selected.
            if (!EditorApplication.isPlaying || !File.Exists(Request) ||
                (InputState.currentUpdateType != InputUpdateType.Dynamic && InputState.currentUpdateType != InputUpdateType.BeforeRender) ||
                EditorApplication.timeSinceStartup < nextPlayerCapture) return;
            nextPlayerCapture = EditorApplication.timeSinceStartup + 1;
            File.WriteAllText("Logs/mainmenu-xr-recovery/live-play-state.txt", Capture());
        }

        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < nextPoll)
                return;
            nextPoll = EditorApplication.timeSinceStartup + 1;
            if (!File.Exists(Request)) return;
            var report = Capture();
            if (File.Exists(Recover) && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.WriteAllText("Logs/mainmenu-xr-recovery/live-before-clean-start.txt", report);
                EditorSettings.enterPlayModeOptionsEnabled = false;
                EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.None;
                foreach (var device in InputSystem.devices.ToArray())
                    if (device.native && (device is XRHMD || device is XRController) && !device.enabled)
                        InputSystem.EnableDevice(device);
                foreach (var asset in Resources.FindObjectsOfTypeAll<InputActionAsset>())
                {
                    if (!asset.devices.HasValue || !asset.devices.Value.Any(d => !d.added ||
                        d.layout == "SceneHandoffController" || d.layout == "P3BindingTestController")) continue;
                    var enabled = asset.Where(a => a.enabled).ToArray();
                    asset.Disable(); asset.devices = null;
                    foreach (var action in enabled) action.Enable();
                }
                File.Delete(Recover);
                report = Capture();
            }
            File.WriteAllText("Logs/mainmenu-xr-recovery/live-state.txt", report);
            if (EditorApplication.isPlaying)
                File.WriteAllText("Logs/mainmenu-xr-recovery/live-play-editor-state.txt", report);
        }

        static string Capture()
        {
            var text = new StringBuilder();
            text.AppendLine($"time={System.DateTime.Now:O} playing={EditorApplication.isPlaying} inputBuffer={InputState.currentUpdateType} reloadOptionsEnabled={EditorSettings.enterPlayModeOptionsEnabled} options={EditorSettings.enterPlayModeOptions}");
            foreach (var device in InputSystem.devices)
            {
                if (!(device is XRHMD) && !(device is XRController)) continue;
                text.AppendLine($"device id={device.deviceId} name={device.name} layout={device.layout} native={device.native} enabled={device.enabled} usages={string.Join(",", device.usages)}");
                foreach (var name in new[] { "isTracked", "trackingState", "centerEyePosition", "devicePosition", "primary2DAxis", "thumbstick" })
                {
                    var control = device.TryGetChildControl(name);
                    if (control != null) text.AppendLine($"  {name}={control.ReadValueAsObject()}");
                }
            }
            foreach (var menu in Object.FindObjectsOfType<MainMenuXRModeController>(true))
                text.AppendLine($"menu scene={menu.gameObject.scene.name} enabled={menu.enabled} vr={menu.IsVrMode} camera={(menu.VrCamera != null ? menu.VrCamera.name : "null")}");
            foreach (var origin in Object.FindObjectsOfType<XROrigin>(true))
            {
                text.AppendLine($"rig scene={origin.gameObject.scene.name} active={origin.gameObject.activeInHierarchy} world={origin.transform.position} mode={origin.CurrentTrackingOriginMode} offset={origin.CameraYOffset}");
                foreach (var pose in origin.GetComponentsInChildren<TrackedPoseDriver>(true))
                    text.AppendLine($"  pose {pose.name} enabled={pose.enabled} world={pose.transform.position} local={pose.transform.localPosition} positionAction={pose.positionInput.action?.enabled} controls={string.Join(",", pose.positionInput.action?.controls.Select(c => c.path + ":" + c.device.enabled) ?? Enumerable.Empty<string>())}");
                foreach (var manager in origin.GetComponentsInChildren<InputActionManager>(true))
                    foreach (var asset in manager.actionAssets)
                        text.AppendLine($"  actions {asset.name} manager={manager.isActiveAndEnabled} filter={(asset.devices.HasValue ? string.Join(",", asset.devices.Value.Select(d => d.name + ":" + d.added + ":" + d.enabled)) : "none")} enabled={asset.Count(a => a.enabled)}/{asset.Count()}");
            }
            return text.ToString();
        }
    }
}
