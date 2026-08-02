using System.Collections.Generic;
using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SimulatedShooting.Editor
{
    [CustomEditor(typeof(VRControllerHandVisual))]
    public sealed class VRControllerHandVisualEditor : UnityEditor.Editor
    {
        SerializedProperty handSide;
        SerializedProperty modelRoot;
        SerializedProperty grabInteractable;
        SerializedProperty gripAnchor;
        SerializedProperty gripPositionOffset;
        SerializedProperty gripRotationOffset;
        SerializedProperty poseBlendSpeed;
        SerializedProperty useCustomFingerPose;
        SerializedProperty customFingerPose;

        void OnEnable()
        {
            handSide = serializedObject.FindProperty("handSide");
            modelRoot = serializedObject.FindProperty("modelRoot");
            grabInteractable = serializedObject.FindProperty("grabInteractable");
            gripAnchor = serializedObject.FindProperty("gripAnchor");
            gripPositionOffset = serializedObject.FindProperty("gripPositionOffset");
            gripRotationOffset = serializedObject.FindProperty("gripRotationOffset");
            poseBlendSpeed = serializedObject.FindProperty("poseBlendSpeed");
            useCustomFingerPose = serializedObject.FindProperty("useCustomFingerPose");
            customFingerPose = serializedObject.FindProperty("customFingerPose");
        }

        public override void OnInspectorGUI()
        {
            var visual = (VRControllerHandVisual)target;
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "无需进入 Play Mode 或连接 VR。先点击“开始静态握姿预览”，再调整握把偏移和逐关节旋转。预览使用不保存到场景的临时手部副本，不会改变 XR 控制器下的原手模型。",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(handSide, new GUIContent("手侧"));
            EditorGUILayout.PropertyField(modelRoot, new GUIContent("手部模型根节点"));
            EditorGUILayout.PropertyField(grabInteractable, new GUIContent("枪械抓取组件"));
            EditorGUILayout.PropertyField(gripAnchor, new GUIContent("握把锚点"));
            EditorGUILayout.PropertyField(gripPositionOffset, new GUIContent("握姿位置偏移（米）"));
            EditorGUILayout.PropertyField(gripRotationOffset, new GUIContent("握姿旋转偏移（度）"));
            EditorGUILayout.PropertyField(poseBlendSpeed, new GUIContent("姿势混合速度"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("手指握姿", EditorStyles.boldLabel);
            if (!useCustomFingerPose.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "当前使用代码默认握姿。启用后会复制当前值，并允许每个关节分别调整 X/Y/Z 三轴。",
                    MessageType.None);
                if (GUILayout.Button("启用逐关节三轴调整"))
                {
                    serializedObject.ApplyModifiedProperties();
                    Undo.RecordObject(visual, "Enable Custom VR Hand Pose");
                    visual.EnableCustomFingerPoseFromDefaults();
                    EditorUtility.SetDirty(visual);
                    serializedObject.Update();
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "X 通常控制弯曲；Y/Z 用于拇指对向、手指张开和扭转。建议每次只改 2°–5°。",
                    MessageType.None);
                EditorGUILayout.PropertyField(customFingerPose, new GUIContent("逐关节旋转偏移"), true);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("重置为当前默认值"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        Undo.RecordObject(visual, "Reset Custom VR Hand Pose");
                        visual.ResetCustomFingerPoseToDefaults();
                        EditorUtility.SetDirty(visual);
                        serializedObject.Update();
                    }

                    if (GUILayout.Button("关闭逐关节调整"))
                    {
                        useCustomFingerPose.boolValue = false;
                    }
                }
            }

            var changed = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();
            if (changed && VRHandPosePreviewManager.IsPreviewing(visual))
            {
                VRHandPosePreviewManager.RefreshPreview(visual);
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space();
            if (VRHandPosePreviewManager.IsPreviewing(visual))
            {
                EditorGUILayout.HelpBox(
                    "预览已激活。请在 Scene 标签中查看；临时手部副本已显示在武器架上的枪旁。",
                    MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(Application.isPlaying || gripAnchor.objectReferenceValue == null))
            {
                if (!VRHandPosePreviewManager.IsPreviewing(visual))
                {
                    if (GUILayout.Button("开始预览并聚焦枪与手", GUILayout.Height(28f)))
                    {
                        VRHandPosePreviewManager.BeginPreview(visual);
                        VRHandPosePreviewManager.FramePreview(visual);
                    }
                }
                else
                {
                    if (GUILayout.Button("在 Scene 视图中重新聚焦", GUILayout.Height(24f)))
                    {
                        VRHandPosePreviewManager.FramePreview(visual);
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("刷新预览", GUILayout.Height(28f)))
                        {
                            VRHandPosePreviewManager.RefreshPreview(visual);
                            SceneView.RepaintAll();
                        }

                        if (GUILayout.Button("结束预览", GUILayout.Height(28f)))
                        {
                            VRHandPosePreviewManager.EndPreview(visual);
                            SceneView.RepaintAll();
                        }
                    }
                }
            }
        }
    }

    [InitializeOnLoad]
    public static class VRHandPosePreviewManager
    {
        static readonly Dictionary<VRControllerHandVisual, VRControllerHandVisual> ActivePreviews =
            new Dictionary<VRControllerHandVisual, VRControllerHandVisual>();

        static VRHandPosePreviewManager()
        {
            AssemblyReloadEvents.beforeAssemblyReload += StopAll;
            EditorApplication.quitting += StopAll;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.sceneSaving += OnSceneSaving;
        }

        public static bool IsPreviewing(VRControllerHandVisual source)
        {
            return source != null &&
                   ActivePreviews.TryGetValue(source, out var preview) &&
                   preview != null;
        }

        public static VRControllerHandVisual GetPreviewVisual(VRControllerHandVisual source)
        {
            return source != null && ActivePreviews.TryGetValue(source, out var preview)
                ? preview
                : null;
        }

        public static void BeginPreview(VRControllerHandVisual source)
        {
            if (Application.isPlaying || source == null || source.GripAnchor == null)
                return;

            EndPreview(source);

            // The in-scene virtual hand is normally below disabled XR controller
            // objects in Edit Mode. Preview a non-saveable detached clone so the
            // hand remains visible without activating the XR Origin hierarchy.
            var previewObject = Object.Instantiate(source.gameObject);
            previewObject.name = $"__VRHandPosePreview_{source.HandSide}";
            previewObject.transform.SetParent(null, true);
            previewObject.hideFlags = HideFlags.HideAndDontSave;
            previewObject.SetActive(true);

            var preview = previewObject.GetComponent<VRControllerHandVisual>();
            preview.BeginGripPosePreview();
            ActivePreviews[source] = preview;
        }

        public static void RefreshPreview(VRControllerHandVisual source)
        {
            if (!IsPreviewing(source))
                return;

            BeginPreview(source);
        }

        public static void EndPreview(VRControllerHandVisual source)
        {
            if (source == null || !ActivePreviews.TryGetValue(source, out var preview))
                return;

            ActivePreviews.Remove(source);
            if (preview != null)
                Object.DestroyImmediate(preview.gameObject);
        }

        public static void FramePreview(VRControllerHandVisual visual)
        {
            FramePreviews(visual != null
                ? new[] { visual }
                : Enumerable.Empty<VRControllerHandVisual>());
        }

        [MenuItem("Tools/Simulated Shooting/VR Hand Pose/Preview Both Hands")]
        static void PreviewBothHands()
        {
            if (Application.isPlaying)
                return;

            foreach (var visual in Resources.FindObjectsOfTypeAll<VRControllerHandVisual>()
                         .Where(visual => visual.gameObject.scene.IsValid() &&
                                          visual.gameObject.scene.isLoaded &&
                                          !visual.gameObject.name.StartsWith("__VRHandPosePreview_")))
            {
                BeginPreview(visual);
            }

            FramePreviews(ActivePreviews.Keys);
        }

        [MenuItem("Tools/Simulated Shooting/VR Hand Pose/Stop All Previews")]
        public static void StopAll()
        {
            foreach (var preview in ActivePreviews.Values.ToArray())
            {
                if (preview != null)
                    Object.DestroyImmediate(preview.gameObject);
            }

            ActivePreviews.Clear();
            SceneView.RepaintAll();
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                StopAll();
        }

        static void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
        {
            StopAll();
        }

        static void FramePreviews(IEnumerable<VRControllerHandVisual> visuals)
        {
            var renderers = new HashSet<Renderer>();
            var gripPoints = new List<Vector3>();
            foreach (var visual in visuals)
            {
                if (visual == null)
                    continue;

                var preview = GetPreviewVisual(visual);
                if (preview == null)
                    continue;

                if (preview.ModelRoot != null)
                {
                    foreach (var renderer in preview.ModelRoot.GetComponentsInChildren<Renderer>(true))
                        renderers.Add(renderer);
                }

                if (preview.GripAnchor != null)
                    gripPoints.Add(preview.GripAnchor.position);
            }

            var hasBounds = false;
            var bounds = new Bounds();
            foreach (var renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            foreach (var gripPoint in gripPoints)
            {
                if (!hasBounds)
                {
                    bounds = new Bounds(gripPoint, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(gripPoint);
                }
            }

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null && SceneView.sceneViews.Count > 0)
                sceneView = SceneView.sceneViews[0] as SceneView;
            sceneView ??= EditorWindow.GetWindow<SceneView>("Scene");

            if (!hasBounds || sceneView == null)
            {
                SceneView.RepaintAll();
                return;
            }

            // Rifle renderers may report scene-sized editor bounds. Grip points
            // plus hand meshes are stable; keep a tighter single-hand view and a
            // slightly wider two-hand view for actual pose adjustment.
            bounds.Expand(gripPoints.Count > 1 ? 0.35f : 0.25f);
            sceneView.Show();
            sceneView.Frame(bounds, true);
            sceneView.Focus();
            SceneView.RepaintAll();
        }
    }
}
