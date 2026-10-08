using System;
using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SimulatedShooting.Editor
{
    public static class CombatPresentationInstaller
    {
        [MenuItem("Tools/Simulated Shooting/Scene 3/Install Combat Hands And Rifle")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before installing presentation assets.");
            const string folder = "Assets/Resources/Combat/Hands";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resources/Combat", "Hands");
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var preview = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/ZeroingRangeScene.unity");
            var openedForInstall = !preview.IsValid() || !preview.isLoaded;
            if(openedForInstall)preview = EditorSceneManager.OpenScene("Assets/Scenes/ZeroingRangeScene.unity", OpenSceneMode.Additive);
            try
            {
                var hands = preview.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<VRControllerHandVisual>(true)).ToArray();
                foreach (var side in new[] { VirtualHandSide.Left, VirtualHandSide.Right })
                {
                    var source = hands.Single(h => h.HandSide == side);
                    var copy = Object.Instantiate(source.gameObject);
                    try
                    {
                        copy.name = "VirtualHand_" + side;
                        copy.transform.localPosition = source.transform.localPosition;
                        copy.transform.localRotation = source.transform.localRotation;
                        copy.transform.localScale = source.transform.localScale;
                        copy.SetActive(true);
                        copy.GetComponent<VRControllerHandVisual>().BindWeapon(null);
                        PrefabUtility.SaveAsPrefabAsset(copy, folder + "/" + copy.name + ".prefab");
                    }
                    finally { Object.DestroyImmediate(copy); }
                }
            }
            finally
            {
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);
                if(openedForInstall)EditorSceneManager.CloseScene(preview, true);
            }
            const string enemyPath = "Assets/SimulatedShooting/Prefabs/Combat/Actor_Enemy.prefab";
            var enemy = PrefabUtility.LoadPrefabContents(enemyPath);
            try
            {
                var actor = enemy.GetComponent<CombatActorView>();
                // Keep the removed flamethrower/back cylinders hidden; restore the training rifle only.
                var rifle = actor.SoldierAnimation.Rifle;
                foreach (var renderer in rifle.GetComponentsInChildren<MeshRenderer>(true)) renderer.enabled = true;
                PrefabUtility.SaveAsPrefabAsset(enemy, enemyPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(enemy); }
            AssetDatabase.SaveAssets();
        }
    }
}
