using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using VRShooting.Unity.Weapons;

namespace VRShooting.EditorTools
{
    /// <summary>
    /// Batch-only environment captures. Opens scenes in memory and never saves them.
    /// VRBG_SCENE accepts an exact scene name or path; VRBG_OUT overrides the output directory.
    /// </summary>
    public static class SceneBackgroundCapture
    {
        const int Width = 1920;
        const int Height = 1080;
        const string DefaultOutput = "docs/codex-reports/evidence/backgrounds-2026-11-11";

        sealed class CaptureSpec
        {
            public readonly string SceneName, FileName;
            public CaptureSpec(string sceneName, string fileName) { SceneName = sceneName; FileName = fileName; }
            public string Path => "Assets/Scenes/" + SceneName + ".unity";
        }

        static readonly CaptureSpec[] Specs =
        {
            new CaptureSpec("ZeroingRangeScene", "01-ZeroingRange.png"),
            new CaptureSpec("MovingTargetRangeScene", "02-MovingTargetRange.png"),
            new CaptureSpec("BunkersTrenchCombatScene", "03-Trench.png"),
            new CaptureSpec("CombatScene", "04-Urban.png")
        };

        public static void CaptureAll()
        {
            if (!UnityEngine.Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("CaptureAll requires batch mode outside Play Mode; it opens scenes without saving.");
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                throw new InvalidOperationException("A graphics context is required; do not use -nographics.");

            var filter = Environment.GetEnvironmentVariable("VRBG_SCENE");
            var selected = string.IsNullOrWhiteSpace(filter) ? Specs : Specs.Where(s =>
                string.Equals(filter, s.SceneName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(filter.Replace('\\', '/'), s.Path, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (selected.Length == 0) throw new ArgumentException("VRBG_SCENE does not match a capture scene: " + filter);
            var output = Environment.GetEnvironmentVariable("VRBG_OUT");
            output = Path.GetFullPath(string.IsNullOrWhiteSpace(output) ? DefaultOutput : output);
            Directory.CreateDirectory(output);
            Debug.Log("[VRBG] graphics=" + SystemInfo.graphicsDeviceType + " device=" + SystemInfo.graphicsDeviceName + " output=" + output);

            var failures = new List<string>();
            foreach (var spec in selected)
            {
                try { Capture(spec, output); }
                catch (Exception ex)
                {
                    failures.Add(spec.SceneName);
                    Debug.LogError("[VRBG] FAILED " + spec.SceneName + ": " + ex);
                }
            }
            if (failures.Count != 0) throw new InvalidOperationException("Background capture failed: " + string.Join(", ", failures));
            Debug.Log("[VRBG] COMPLETE " + selected.Length + " scene(s)");
        }

        static void Capture(CaptureSpec spec, string output)
        {
            Debug.Log("[VRBG] BEGIN " + spec.SceneName);
            var scene = EditorSceneManager.OpenScene(spec.Path, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var cameras = Find<Camera>(roots);
            foreach (var camera in cameras)
                Debug.Log("[VRBG] CAMERA " + ObjectPath(camera.transform) + " active=" + camera.gameObject.activeInHierarchy +
                    " enabled=" + camera.enabled + " pos=" + F(camera.transform.position) + " euler=" + F(camera.transform.eulerAngles) +
                    " fov=" + camera.fieldOfView.ToString("F2") + " near=" + camera.nearClipPlane + " far=" + camera.farClipPlane);

            var modeControllers = Find<ZeroingRangeXRModeController>(roots);
            var source = modeControllers.Select(c => c.NoVrCamera).FirstOrDefault(c => c != null) ??
                cameras.SingleOrDefault(c => c.name == "Camera_NoVR");
            if (source == null) throw new InvalidOperationException("The scene has no bound Camera_NoVR player camera.");
            var position = source.transform.position;
            var yaw = source.transform.eulerAngles.y;
            var poseSource = "authored Camera_NoVR";

            var combat = Find<CombatSceneBindings>(roots).SingleOrDefault();
            if (combat != null)
            {
                var anchor = spec.SceneName == "CombatScene" ? combat.UrbanEntry : combat.TrenchEntry;
                if (anchor == null || combat.GeometryRoot == null) throw new InvalidOperationException("Combat entry or geometry binding missing.");
                var standingHeight = StandingEyeHeight(combat);
                Physics.SyncTransforms();
                var feet = CombatSpawnPlacement.Resolve(combat.GeometryRoot, anchor.position, .22f, standingHeight + .10f);
                position = feet + Vector3.up * (standingHeight * .75f);
                yaw = anchor.eulerAngles.y;
                poseSource = ObjectPath(anchor) + ", CombatSpawnPlacement.Resolve, desktop eye height=" + (standingHeight * .75f).ToString("F3");
                if (spec.SceneName == "CombatScene")
                {
                    // The authored spawn faces a facade. Turn the player's view from the
                    // same spawn toward the street, without relocating the camera.
                    yaw += 45f;
                    poseSource += ", horizontal view +45 degrees toward street";
                }
                Debug.Log("[VRBG] SPAWN anchor=" + F(anchor.position) + " resolvedFeet=" + F(feet) + " standingHeight=" + standingHeight.ToString("F3"));
            }

            Debug.Log("[VRBG] POSE source=" + poseSource + " camera=" + ObjectPath(source.transform) +
                " pos=" + F(position) + " euler=(0.000," + yaw.ToString("F3") + ",0.000) fov=" + source.fieldOfView.ToString("F3"));
            Debug.Log("[VRBG] SKY " + (RenderSettings.skybox == null ? "<none>" : RenderSettings.skybox.name + " shader=" + RenderSettings.skybox.shader.name));
            HidePresentation(roots);
            Render(source, position, yaw, Path.Combine(output, spec.FileName));
            Debug.Log("[VRBG] END " + spec.SceneName);
        }

        static float StandingEyeHeight(CombatSceneBindings binding)
        {
            if (binding.TeammatePrefab == null) return 1.85f;
            var heads = binding.TeammatePrefab.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Head").ToArray();
            var boots = binding.TeammatePrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(r => r.name.EndsWith("_Boots", StringComparison.Ordinal)).ToArray();
            return heads.Length == 0 || boots.Length == 0 ? 1.85f :
                Mathf.Clamp(heads.Max(t => t.position.y) - boots.Min(r => r.bounds.min.y), 1.5f, 2.1f);
        }

        static void HidePresentation(GameObject[] roots)
        {
            foreach (var canvas in Find<Canvas>(roots))
            {
                Debug.Log("[VRBG] HIDE Canvas " + ObjectPath(canvas.transform) + " active=" + canvas.gameObject.activeInHierarchy + " enabled=" + canvas.enabled);
                canvas.enabled = false;
            }
            foreach (var raycaster in Find<BaseRaycaster>(roots))
            {
                Debug.Log("[VRBG] HIDE UI raycaster " + ObjectPath(raycaster.transform) + " type=" + raycaster.GetType().Name + " enabled=" + raycaster.enabled);
                raycaster.enabled = false;
            }

            var objects = new HashSet<GameObject>();
            foreach (var weapon in Find<WeaponPrefabBinding>(roots)) objects.Add(weapon.gameObject);
            foreach (var transform in Find<Transform>(roots))
            {
                if (transform.name == "VirtualHand_Left" || transform.name == "VirtualHand_Right" ||
                    transform.name == "WeaponReference_Blockout" || transform.name == "TrenchProjection" ||
                    transform.name == "Anchor_Drone") objects.Add(transform.gameObject);
            }
            foreach (var obj in objects)
            {
                Debug.Log("[VRBG] HIDE object " + ObjectPath(obj.transform) + " active=" + obj.activeInHierarchy + " self=" + obj.activeSelf);
                obj.SetActive(false);
            }
            foreach (var line in Find<LineRenderer>(roots))
            {
                var path = ObjectPath(line.transform);
                if (path.IndexOf("controller", StringComparison.OrdinalIgnoreCase) < 0 &&
                    path.IndexOf("ray", StringComparison.OrdinalIgnoreCase) < 0 &&
                    path.IndexOf("laser", StringComparison.OrdinalIgnoreCase) < 0) continue;
                Debug.Log("[VRBG] HIDE UI line " + path + " enabled=" + line.enabled);
                line.enabled = false;
            }
        }

        static void Render(Camera source, Vector3 position, float yaw, string path)
        {
            GameObject holder = null;
            RenderTexture target = null;
            Texture2D image = null;
            var previous = RenderTexture.active;
            try
            {
                holder = new GameObject("VRBG_TemporaryCamera") { hideFlags = HideFlags.HideAndDontSave };
                var camera = holder.AddComponent<Camera>();
                camera.CopyFrom(source);
                camera.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.stereoTargetEye = StereoTargetEyeMask.None;
                camera.enabled = false;
                var data = holder.AddComponent<UniversalAdditionalCameraData>();
                data.renderType = CameraRenderType.Base;
                data.renderPostProcessing = false;
                data.antialiasing = AntialiasingMode.None;
                target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
                target.Create();
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                Debug.Log("[VRBG] SAVED " + path + " bytes=" + new FileInfo(path).Length);
            }
            finally
            {
                RenderTexture.active = previous;
                if (holder != null) UnityEngine.Object.DestroyImmediate(holder);
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            }
        }

        static T[] Find<T>(IEnumerable<GameObject> roots) where T : Component =>
            roots.SelectMany(r => r.GetComponentsInChildren<T>(true)).Where(c => c != null).ToArray();

        static string ObjectPath(Transform transform)
        {
            var names = new List<string>();
            while (transform != null) { names.Add(transform.name); transform = transform.parent; }
            names.Reverse();
            return string.Join("/", names);
        }

        static string F(Vector3 value) => string.Format("({0:F3},{1:F3},{2:F3})", value.x, value.y, value.z);
    }
}
