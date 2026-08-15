using System;
using System.Linq;
using System.Reflection;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Editor
{
    public static class MovingTargetWeaponInstaller
    {
        const string MovingScenePath = "Assets/Scenes/MovingTargetRangeScene.unity";
        const string ZeroingScenePath = "Assets/Scenes/ZeroingRangeScene.unity";
        const string HandMaterialPath = "Assets/SimulatedShooting/Art/Materials/RangeSandbag.mat";

        [MenuItem("Tools/Simulated Shooting/Add P1 Training Rifle To Moving Target Range")]
        public static void Install()
        {
            var targetScene = EditorSceneManager.OpenScene(MovingScenePath, OpenSceneMode.Single);
            if (FindComponentInScene<FirstPersonTrainingWeaponController>(targetScene) != null)
            {
                Debug.Log("[MovingTargetWeaponInstaller] MovingTargetRangeScene already contains the training rifle.");
                return;
            }

            ReadSourceWeaponPose(out var sourceLocalPosition, out var sourceLocalRotation);
            SceneManager.SetActiveScene(targetScene);

            var targetRoot = FindByTestId(targetScene, "MovingTargetRange.Root")?.transform;
            var anchors = targetRoot == null ? null : FindDescendant(targetRoot, "TrainingAnchors");
            var camera = FindByTestId(targetScene, "MovingTargetRange.Camera.NoVR")?.GetComponent<Camera>();
            var xrOrigin = FindByTestId(targetScene, "MovingTargetRange.Origin.VR")?.transform;
            if (anchors == null || camera == null || xrOrigin == null)
                throw new InvalidOperationException("MovingTargetRangeScene is missing TrainingAnchors, Camera_NoVR, or XR Origin.");

            if (FindDescendant(xrOrigin, "DirectInteractor_Right") == null &&
                FindDescendant(xrOrigin, "DirectInteractor_Left") == null)
            {
                var handMaterial = AssetDatabase.LoadAssetAtPath<Material>(HandMaterialPath);
                InvokeZeroingBuilder("ConfigureDirectInteractors", xrOrigin, handMaterial);
            }

            InvokeZeroingBuilder("CreateFirstPersonTrainingWeapon", anchors, camera, xrOrigin);

            var controller = FindComponentInScene<FirstPersonTrainingWeaponController>(targetScene);
            var binding = FindComponentInScene<WeaponPrefabBinding>(targetScene);
            if (controller == null || binding == null)
                throw new InvalidOperationException("The training rifle was not created with its required controller and binding.");

            binding.transform.SetPositionAndRotation(
                anchors.TransformPoint(sourceLocalPosition),
                anchors.rotation * sourceLocalRotation);
            EditorUtility.SetDirty(binding.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(binding.transform);

            EditorSceneManager.MarkSceneDirty(targetScene);
            if (!EditorSceneManager.SaveScene(targetScene, MovingScenePath))
                throw new InvalidOperationException("Failed to save MovingTargetRangeScene after adding the training rifle.");

            Debug.Log("[MovingTargetWeaponInstaller] Added the complete P1 training-rifle setup to MovingTargetRangeScene without rebuilding existing scene objects.");
        }

        static void ReadSourceWeaponPose(out Vector3 localPosition, out Quaternion localRotation)
        {
            var sourceScene = EditorSceneManager.OpenScene(ZeroingScenePath, OpenSceneMode.Additive);
            try
            {
                var sourceWeapon = FindByTestId(sourceScene, "ZeroingRange.Weapon.TrainingRifle")?.transform;
                var sourceRoot = FindByTestId(sourceScene, "ZeroingRange.Root")?.transform;
                var sourceAnchors = sourceRoot == null ? null : FindDescendant(sourceRoot, "TrainingAnchors");
                if (sourceWeapon == null || sourceAnchors == null)
                    throw new InvalidOperationException("ZeroingRangeScene is missing its training rifle or TrainingAnchors.");

                localPosition = sourceAnchors.InverseTransformPoint(sourceWeapon.position);
                localRotation = Quaternion.Inverse(sourceAnchors.rotation) * sourceWeapon.rotation;
            }
            finally
            {
                EditorSceneManager.CloseScene(sourceScene, true);
            }
        }

        static void InvokeZeroingBuilder(string methodName, params object[] arguments)
        {
            var method = typeof(ZeroingRangeSceneBuilder).GetMethod(
                methodName,
                BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
                throw new MissingMethodException(typeof(ZeroingRangeSceneBuilder).FullName, methodName);

            try
            {
                method.Invoke(null, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        static T FindComponentInScene<T>(UnityEngine.SceneManagement.Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .FirstOrDefault();
        }

        static GameObject FindByTestId(UnityEngine.SceneManagement.Scene scene, string id)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<SceneTestId>(true))
                .FirstOrDefault(testId => testId.Id == id)?.gameObject;
        }

        static Transform FindDescendant(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item.name == name);
        }
    }
}
