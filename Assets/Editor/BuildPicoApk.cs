using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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

            EnsureAllSigningSchemes(apkPath, signing);
            size = new FileInfo(apkPath).Length;
            Debug.Log("[BuildPicoApk] APK: " + apkPath + " (" + size + " bytes)");
        }
    }

    static void EnsureAllSigningSchemes(string apkPath, SigningConfiguration signing)
    {
        var androidPlayer = Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer");
        var java = Path.Combine(androidPlayer, "OpenJDK", "bin", Path.DirectorySeparatorChar == '\\' ? "java.exe" : "java");
        if (!File.Exists(java))
            throw new FileNotFoundException("Unity Android OpenJDK java was not found.", java);

        var buildTools = Path.Combine(androidPlayer, "SDK", "build-tools");
        if (!Directory.Exists(buildTools))
            throw new DirectoryNotFoundException("Unity Android SDK build-tools directory was not found: " + buildTools);

        var latest = Directory.GetDirectories(buildTools)
            .Select(path => new
            {
                Path = path,
                Version = ParseBuildToolsVersion(Path.GetFileName(path)),
                Stable = !Path.GetFileName(path).Contains("-")
            })
            .Where(candidate => candidate.Version != null)
            .OrderByDescending(candidate => candidate.Version)
            .ThenByDescending(candidate => candidate.Stable)
            .ThenByDescending(candidate => Path.GetFileName(candidate.Path), Comparer<string>.Create(CompareVersionDirectoryNames))
            .ThenByDescending(candidate => Path.GetFileName(candidate.Path), StringComparer.Ordinal)
            .FirstOrDefault();
        if (latest == null)
            throw new InvalidOperationException("No versioned Android SDK build-tools directory was found in: " + buildTools);

        var apksigner = Path.Combine(latest.Path, "lib", "apksigner.jar");
        if (!File.Exists(apksigner))
            throw new FileNotFoundException("Android SDK apksigner.jar was not found in the latest build-tools directory.", apksigner);

        var temporaryApk = apkPath + ".signed-" + Guid.NewGuid().ToString("N") + ".tmp.apk";
        try
        {
            RunApksigner(java, apksigner, new[]
            {
                "sign", "--ks", signing.KeystorePath, "--ks-pass", "pass:" + signing.KeystorePassword,
                "--key-pass", "pass:" + signing.AliasPassword, "--ks-key-alias", signing.Alias,
                "--min-sdk-version", "21", "--v1-signing-enabled", "true", "--v2-signing-enabled", "true",
                "--v3-signing-enabled", "true", "--v4-signing-enabled", "false", "--out", temporaryApk, apkPath
            });

            var verification = RunApksigner(java, apksigner, new[]
            {
                "verify", "--min-sdk-version", "21", "--verbose", temporaryApk
            });
            var v1 = IsSigningSchemeVerified(verification, 1);
            var v2 = IsSigningSchemeVerified(verification, 2);
            var v3 = IsSigningSchemeVerified(verification, 3);
            Debug.Log("[BuildPicoApk] Signature verification: v1=" + v1 + ", v2=" + v2 + ", v3=" + v3);
            if (!v1 || !v2 || !v3)
                throw new InvalidOperationException("APK verification did not confirm all v1, v2 and v3 signing schemes.\n" + verification);

            File.Replace(temporaryApk, apkPath, null);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("APK signing or verification failed. Original APK was not replaced; " +
                "any temporary APK is retained at: " + temporaryApk, exception);
        }

        using (var stream = File.OpenRead(apkPath))
        using (var sha256 = SHA256.Create())
        {
            var hash = BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            Debug.Log("[BuildPicoApk] Signed APK: " + stream.Length + " bytes; SHA-256: " + hash);
        }
    }

    static Version ParseBuildToolsVersion(string directoryName)
    {
        var suffix = directoryName.IndexOf('-');
        var versionName = suffix < 0 ? directoryName : directoryName.Substring(0, suffix);
        return Version.TryParse(versionName, out var version) ? version : null;
    }

    static int CompareVersionDirectoryNames(string left, string right)
    {
        var leftParts = Regex.Matches(left, @"[0-9]+|[^0-9]+");
        var rightParts = Regex.Matches(right, @"[0-9]+|[^0-9]+");
        for (var i = 0; i < Math.Min(leftParts.Count, rightParts.Count); i++)
        {
            var leftPart = leftParts[i].Value;
            var rightPart = rightParts[i].Value;
            int comparison;
            if (leftPart[0] >= '0' && leftPart[0] <= '9' && rightPart[0] >= '0' && rightPart[0] <= '9')
            {
                leftPart = leftPart.TrimStart('0');
                rightPart = rightPart.TrimStart('0');
                comparison = leftPart.Length.CompareTo(rightPart.Length);
                if (comparison == 0)
                    comparison = string.CompareOrdinal(leftPart, rightPart);
            }
            else
            {
                comparison = string.CompareOrdinal(leftPart, rightPart);
            }
            if (comparison != 0)
                return comparison;
        }
        return leftParts.Count.CompareTo(rightParts.Count);
    }

    static bool IsSigningSchemeVerified(string output, int scheme)
    {
        return Regex.IsMatch(output, @"(?im)^[ \t]*Verified using v" + scheme +
            @" scheme\b[^:\r\n]*:[ \t]*true[ \t]*\r?$");
    }

    static string RunApksigner(string java, string apksigner, string[] arguments)
    {
        var allArguments = new[] { "-jar", apksigner }.Concat(arguments);
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = java,
            Arguments = string.Join(" ", allArguments.Select(QuoteArgument).ToArray()),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using (var process = System.Diagnostics.Process.Start(startInfo))
        {
            if (process == null)
                throw new InvalidOperationException("Could not start Android apksigner.");
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            var result = output.GetAwaiter().GetResult();
            var errorText = error.GetAwaiter().GetResult();
            if (process.ExitCode != 0)
                throw new InvalidOperationException("Android apksigner exited with code " + process.ExitCode + ".\n" + result + "\n" + errorText);
            return result;
        }
    }

    static string QuoteArgument(string value)
    {
        var result = new StringBuilder("\"");
        var backslashes = 0;
        foreach (var character in value)
        {
            if (character == '\\')
            {
                backslashes++;
                continue;
            }
            if (character == '"')
                result.Append('\\', backslashes * 2 + 1);
            else
                result.Append('\\', backslashes);
            result.Append(character);
            backslashes = 0;
        }
        result.Append('\\', backslashes * 2);
        return result.Append('"').ToString();
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

        public string KeystorePath => release ? keystorePath : Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".android", "debug.keystore");
        public string KeystorePassword => release ? keystorePassword : "android";
        public string Alias => release ? alias : "androiddebugkey";
        public string AliasPassword => release ? aliasPassword : "android";

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
