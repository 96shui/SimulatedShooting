using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

public static class BuildPicoApk
{
    const string PackageId = "com.vrtraining.pico4";
    const string OpenXrLoader = "UnityEngine.XR.OpenXR.OpenXRLoader";
    const string PicoFeatureId = "com.unity.openxr.feature.pico";
    const string PicoExtensionsId = "com.unity.openxr.pico.features";
    const string PicoControllerId = "com.unity.openxr.feature.input.PICO4touch";

    public static void Build()
    {
        var exitCode = 1;
        try
        {
            BuildApk();
            exitCode = 0;
        }
        catch (Exception exception)
        {
            Debug.LogError("[BuildPicoApk] Build failed. See the Unity log for details.");
            Debug.LogException(exception);
        }
        finally
        {
            EditorApplication.Exit(exitCode);
        }
    }

    static void BuildApk()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("Android Build Support is not installed for this Unity Editor.");

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            if (Application.isBatchMode)
                throw new InvalidOperationException("Unity cannot switch build targets in batch mode. Restart with -buildTarget Android.");
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Could not switch the active build target to Android.");
        }

        var signing = SigningConfiguration.FromEnvironment();
        var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        if (scenes.Length == 0 || scenes[0] != "Assets/Scenes/MainScene.unity")
            throw new InvalidOperationException("Assets/Scenes/MainScene.unity must be the first enabled build scene.");
        var missingScene = scenes.FirstOrDefault(scene => !File.Exists(scene));
        if (missingScene != null)
            throw new FileNotFoundException("An enabled build scene is missing.", missingScene);

        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PackageId);
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel33;
        PlayerSettings.Android.useAPKExpansionFiles = false;
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

        ConfigureOpenXr();

        var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build", "PICO"));
        Directory.CreateDirectory(directory);
        var apkPath = Path.Combine(directory, "SimulateShooting-pico-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".apk");

        using (signing.Apply())
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });

            if (report == null || report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Unity did not complete the Android APK build successfully.");
            if (!File.Exists(apkPath))
                throw new FileNotFoundException("Unity reported success, but the APK was not found.", apkPath);

            var size = new FileInfo(apkPath).Length;
            if (size <= 0)
                throw new InvalidOperationException("Unity produced an empty APK.");
            Debug.Log("[BuildPicoApk] APK: " + apkPath + " (" + size + " bytes)");
        }
    }

    static void ConfigureOpenXr()
    {
        var general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
        if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget allSettings) || allSettings == null)
            throw new InvalidOperationException("XR Plug-in Management settings are missing from the project.");

        if (general == null)
        {
            allSettings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
            general = allSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
        }
        if (general.Manager == null)
            allSettings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);

        var manager = general.Manager;
        foreach (var loader in manager.activeLoaders.ToArray())
        {
            if (loader != null && loader.GetType().FullName != OpenXrLoader &&
                !XRPackageMetadataStore.RemoveLoader(manager, loader.GetType().FullName, BuildTargetGroup.Android))
                throw new InvalidOperationException("Could not remove conflicting Android XR loader: " + loader.GetType().FullName);
        }
        if (!manager.activeLoaders.Any(loader => loader != null && loader.GetType().FullName == OpenXrLoader) &&
            !XRPackageMetadataStore.AssignLoader(manager, OpenXrLoader, BuildTargetGroup.Android))
            throw new InvalidOperationException("Could not enable the Android OpenXR loader.");
        if (!manager.activeLoaders.Any(loader => loader != null && loader.GetType().FullName == OpenXrLoader))
            throw new InvalidOperationException("The Android OpenXR loader was not assigned.");

        general.InitManagerOnStart = true;
        manager.automaticLoading = true;
        manager.automaticRunning = true;

        FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        if (settings == null)
            throw new InvalidOperationException("Android OpenXR settings are missing.");

        EnableFeature(PicoFeatureId);
        EnableFeature(PicoExtensionsId);
        EnableFeature(PicoControllerId);

        var compatibleProfiles = new[]
        {
            "PICO4ControllerProfile", "PICONeo3ControllerProfile", "PICO4UltraControllerProfile",
            "PICOG3ControllerProfile", "KHRSimpleControllerProfile", "EyeGazeInteraction",
            "HandInteractionProfile", "PalmPoseInteraction"
        };
        foreach (var feature in settings.GetFeatures<OpenXRFeature>())
        {
            if (feature == null || !feature.enabled)
                continue;
            var name = feature.GetType().Name;
            var incompatibleProfile = feature is OpenXRInteractionFeature && !compatibleProfiles.Contains(name);
            var incompatibleRuntime = name.Contains("MetaQuest") || name == "OculusQuestFeature" || name.Contains("MockRuntime");
            if (!incompatibleProfile && !incompatibleRuntime)
                continue;
            feature.enabled = false;
            EditorUtility.SetDirty(feature);
            Debug.Log("[BuildPicoApk] Disabled conflicting Android OpenXR feature: " + name);
        }

        EditorUtility.SetDirty(allSettings);
        EditorUtility.SetDirty(general);
        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log("[BuildPicoApk] Android OpenXR loader, PICO support, extensions and PICO 4 controller are enabled.");
    }

    static void EnableFeature(string featureId)
    {
        var feature = FeatureHelpers.GetFeatureWithIdForBuildTarget(BuildTargetGroup.Android, featureId);
        if (feature == null)
            throw new InvalidOperationException("PICO OpenXR feature not found: " + featureId + ". Check the PICO OpenXR package.");
        feature.enabled = true;
        EditorUtility.SetDirty(feature);
    }

    sealed class SigningConfiguration : IDisposable
    {
        readonly bool release;
        readonly string keystorePath;
        readonly string keystorePassword;
        readonly string alias;
        readonly string aliasPassword;
        bool previousUseCustomKeystore;
        string previousKeystorePath;
        string previousKeystorePassword;
        string previousAlias;
        string previousAliasPassword;

        SigningConfiguration(bool release, string keystorePath, string keystorePassword, string alias, string aliasPassword)
        {
            this.release = release;
            this.keystorePath = keystorePath;
            this.keystorePassword = keystorePassword;
            this.alias = alias;
            this.aliasPassword = aliasPassword;
        }

        public static SigningConfiguration FromEnvironment()
        {
            var mode = Environment.GetEnvironmentVariable("PICO_BUILD_SIGNING");
            if (string.IsNullOrEmpty(mode) || string.Equals(mode, "debug", StringComparison.OrdinalIgnoreCase))
                return new SigningConfiguration(false, null, null, null, null);
            if (!string.Equals(mode, "release", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("PICO_BUILD_SIGNING must be debug or release.");

            var path = RequireEnvironment("PICO_KEYSTORE_PATH");
            if (!Path.IsPathRooted(path) || !File.Exists(path))
                throw new InvalidOperationException("PICO_KEYSTORE_PATH must be the absolute path to an existing keystore.");
            return new SigningConfiguration(true, path, RequireEnvironment("PICO_KEYSTORE_PASSWORD"),
                RequireEnvironment("PICO_KEY_ALIAS"), RequireEnvironment("PICO_KEY_ALIAS_PASSWORD"));
        }

        static string RequireEnvironment(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(value))
                throw new InvalidOperationException("The release signing environment variable " + name + " is required.");
            return value;
        }

        public SigningConfiguration Apply()
        {
            previousUseCustomKeystore = PlayerSettings.Android.useCustomKeystore;
            previousKeystorePath = PlayerSettings.Android.keystoreName;
            previousKeystorePassword = PlayerSettings.Android.keystorePass;
            previousAlias = PlayerSettings.Android.keyaliasName;
            previousAliasPassword = PlayerSettings.Android.keyaliasPass;
            try
            {
                PlayerSettings.Android.useCustomKeystore = release;
                if (release)
                {
                    PlayerSettings.Android.keystoreName = keystorePath;
                    PlayerSettings.Android.keystorePass = keystorePassword;
                    PlayerSettings.Android.keyaliasName = alias;
                    PlayerSettings.Android.keyaliasPass = aliasPassword;
                }
                Debug.Log("[BuildPicoApk] Signing: " + (release ? "release keystore" : "Unity debug keystore"));
                return this;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            PlayerSettings.Android.keystoreName = previousKeystorePath;
            PlayerSettings.Android.keystorePass = previousKeystorePassword;
            PlayerSettings.Android.keyaliasName = previousAlias;
            PlayerSettings.Android.keyaliasPass = previousAliasPassword;
            PlayerSettings.Android.useCustomKeystore = previousUseCustomKeystore;
        }
    }
}
