using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.OpenXR;
using Debug = UnityEngine.Debug;

namespace SimulatedShooting.Editor
{
    [InitializeOnLoad]
    public static class Pico4BuildTool
    {
        public const string PackageId = "com.vrtraining.pico4";
        public const string ApkPath = "Builds/PICO4/VRTraining-PICO4.apk";
        const string Pending = "VR.Pico4.PendingBuild";
        const string InstallAfter = "VR.Pico4.InstallAfterBuild";
        static bool building;

        static Pico4BuildTool() { EditorApplication.update += ResumeBuild; }

        [MenuItem("VR Shooting/PICO 4/配置 Android 与 OpenXR")]
        public static void Configure()
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PackageId);
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            EditorUserBuildSettings.buildAppBundle = false;
            var general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (general == null || general.Manager == null)
                throw new InvalidOperationException("Android XR Management settings are missing.");
            foreach (var loader in general.Manager.activeLoaders.ToArray())
                if (loader.GetType().FullName != "UnityEngine.XR.OpenXR.OpenXRLoader")
                    XRPackageMetadataStore.RemoveLoader(general.Manager, loader.GetType().FullName, BuildTargetGroup.Android);
            if (!XRPackageMetadataStore.AssignLoader(general.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Android))
                throw new InvalidOperationException("Cannot enable Android OpenXR loader.");
            general.InitManagerOnStart = true;
            general.Manager.automaticLoading = true;
            general.Manager.automaticRunning = true;
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (settings == null) throw new InvalidOperationException("Android OpenXR settings missing.");
            bool pico = false;
            foreach (var feature in settings.GetFeatures<UnityEngine.XR.OpenXR.Features.OpenXRFeature>())
            {
                var name = feature.GetType().Name;
                if (name == "PICOFeature" || name == "OpenXRExtensions" || name == "PICO4ControllerProfile" || name == "PICONeo3ControllerProfile")
                { feature.enabled = true; pico |= name == "PICOFeature"; }
                if (feature is UnityEngine.XR.OpenXR.Features.OpenXRInteractionFeature &&
                    name != "PICO4ControllerProfile" && name != "PICONeo3ControllerProfile")
                    feature.enabled = false;
                if (name.Contains("MetaQuest") || name == "OculusQuestFeature" || name.Contains("MockRuntime"))
                    feature.enabled = false;
                EditorUtility.SetDirty(feature);
            }
            if (!pico) throw new InvalidOperationException("Install the official PICO OpenXR Plugin before building.");
            EditorUtility.SetDirty(general);
            EditorUtility.SetDirty(general.Manager);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("PICO 4 configured: Android ARM64 / IL2CPP / OpenXR / OpenGLES3.");
        }

        [MenuItem("VR Shooting/PICO 4/打包 APK")]
        public static void BuildApk() => Queue(false);
        [MenuItem("VR Shooting/PICO 4/打包并安装启动")]
        public static void BuildInstall() => Queue(true);

        static void Queue(bool install)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before building.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Unity Android Build Support is not installed; add it through Unity Hub.");
            Configure();
            if (!EditorSceneManager.SaveOpenScenes()) throw new InvalidOperationException("Could not save scenes.");
            SessionState.SetBool(InstallAfter, install);
            SessionState.SetBool(Pending, true);
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                !EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Android, BuildTarget.Android))
            { SessionState.SetBool(Pending, false); throw new InvalidOperationException("Cannot switch to Android."); }
        }

        static void ResumeBuild()
        {
            if (building || !SessionState.GetBool(Pending, false) || EditorApplication.isCompiling ||
                EditorApplication.isUpdating || EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android) return;
            building = true;
            SessionState.SetBool(Pending, false);
            try
            {
                Configure();
                var scenes = EditorBuildSettings.scenes.Where(s => s.enabled && File.Exists(s.path)).Select(s => s.path).ToArray();
                if (scenes.Length == 0 || scenes[0] != "Assets/Scenes/MainScene.unity")
                    throw new InvalidOperationException("MainScene must be the first enabled build scene.");
                Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                { scenes = scenes, locationPathName = ApkPath, target = BuildTarget.Android, options = BuildOptions.Development });
                File.WriteAllText("Builds/PICO4/build-result.txt", report.summary.result + "\nAPK bytes: " + (report.summary.result == BuildResult.Succeeded && File.Exists(ApkPath) ? new FileInfo(ApkPath).Length : 0) + "\nBuild data bytes: " + report.summary.totalSize + "\n");
                if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("APK build failed; see Unity Console.");
                Debug.Log("PICO APK created: " + Path.GetFullPath(ApkPath));
                if (SessionState.GetBool(InstallAfter, false)) InstallAndLaunch();
            }
            catch (Exception e) { Debug.LogException(e); }
            finally { building = false; SessionState.SetBool(InstallAfter, false); }
        }

        [MenuItem("VR Shooting/PICO 4/检查设备")]
        public static void CheckDevice() => Debug.Log(RunAdb("devices -l", 15000));

        [MenuItem("VR Shooting/PICO 4/安装已有 APK 并启动")]
        public static void InstallAndLaunch()
        {
            if (!File.Exists(ApkPath)) throw new FileNotFoundException("Build the APK first.", ApkPath);
            var devices = RunAdb("devices -l", 15000).Split('\n')
                .Where(l => l.Contains("\tdevice") || l.Contains(" device ")).ToArray();
            if (devices.Length != 1) throw new InvalidOperationException("Connect exactly one authorized ADB headset. Unauthorized devices require confirmation in the headset.");
            var serial = devices[0].Split(new[] { ' ', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries)[0];
            var prefix = "-s " + serial + " ";
            var installed = RunAdb(prefix + "install -r \"" + Path.GetFullPath(ApkPath) + "\"", 300000);
            if (!installed.Contains("Success")) throw new InvalidOperationException(installed);
            Debug.Log(installed);
            Debug.Log(RunAdb(prefix + "shell monkey -p " + PackageId + " -c android.intent.category.LAUNCHER 1", 20000));
        }

        static string RunAdb(string arguments, int timeout)
        {
            var adb = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe");
            if (!File.Exists(adb)) adb = Path.GetFullPath("Temp/PicoBuildTools/platform-tools/adb.exe");
            if (!File.Exists(adb)) throw new FileNotFoundException("ADB not found; install Android SDK platform-tools.");
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo(adb, arguments)
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                process.Start();
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(timeout)) { process.Kill(); throw new TimeoutException("ADB operation timed out."); }
                var text = output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult();
                if (process.ExitCode != 0) throw new InvalidOperationException(text);
                return text;
            }
        }
    }
}
