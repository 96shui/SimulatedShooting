using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SimulatedShooting.Editor
{
    public static class P1XrFloorOriginUpgrader
    {
        static readonly string[] ScenePaths =
        {
            "Assets/Scenes/MainScene.unity",
            "Assets/Scenes/ZeroingRangeScene.unity"
        };

        [MenuItem("Tools/Simulated Shooting/Configure P1 XR Floor Tracking")]
        public static void UpgradeP1Scenes()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.isDirty)
            {
                Debug.LogError(
                    "[P1XrFloorOriginUpgrader] Save the active scene before configuring floor tracking.");
                return;
            }

            var activeScenePath = activeScene.path;
            foreach (var scenePath in ScenePaths)
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var origins = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<XROrigin>(true))
                    .ToArray();
                if (origins.Length == 0)
                {
                    Debug.LogError($"[P1XrFloorOriginUpgrader] No XR Origin found in {scenePath}.");
                    continue;
                }

                foreach (var origin in origins)
                {
                    ConfigureFloorOrigin(origin);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            if (!string.IsNullOrEmpty(activeScenePath))
            {
                EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "[P1XrFloorOriginUpgrader] MainScene and ZeroingRangeScene now use Floor tracking without an artificial eye-height offset.");
        }

        public static bool ConfigureFloorOrigin(GameObject originObject)
        {
            return originObject != null &&
                   ConfigureFloorOrigin(originObject.GetComponentInChildren<XROrigin>(true));
        }

        static bool ConfigureFloorOrigin(XROrigin origin)
        {
            if (origin == null)
            {
                return false;
            }

            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            origin.CameraYOffset = 0f;
            EditorUtility.SetDirty(origin);
            PrefabUtility.RecordPrefabInstancePropertyModifications(origin);

            var floorOffset = origin.CameraFloorOffsetObject;
            if (floorOffset != null)
            {
                var offsetTransform = floorOffset.transform;
                var localPosition = offsetTransform.localPosition;
                localPosition.y = 0f;
                offsetTransform.localPosition = localPosition;
                EditorUtility.SetDirty(offsetTransform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(offsetTransform);
            }

            return true;
        }
    }
}
