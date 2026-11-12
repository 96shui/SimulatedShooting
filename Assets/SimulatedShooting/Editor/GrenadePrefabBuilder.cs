#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using SimulatedShooting.Scene;

namespace SimulatedShooting.Editor
{
    public static class GrenadePrefabBuilder
    {
        const string Source = "Assets/Resources/Combat/m67_low.fbx";
        const string Folder = "Assets/Resources/Combat";
        const string Output = Folder + "/Grenade_M67.prefab";

        public static void Build()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Source);
            if (model == null) throw new System.InvalidOperationException("Grenade model not found: " + Source);
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Resources", "Combat");
            var instance = Object.Instantiate(model);
            instance.name = "Grenade_M67";
            // Visual prefab only; CombatSceneRuntime owns the projectile driver.
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true)) collider.isTrigger = true;
            PrefabUtility.SaveAsPrefabAsset(instance, Output);
            Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("Built grenade prefab: " + Output);
        }
    }
}
#endif
