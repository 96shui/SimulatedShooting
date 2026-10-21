using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using SimulatedShooting.Scene;

namespace SimulatedShooting.Editor
{
    public static class RangeSideSloganAuthoring
    {
        const string Art = "Assets/SimulatedShooting/Art/Environment/MovingTargetRange/";
        public static void ApplyToExistingScenes()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop play before authoring signs.");
            foreach (var prefix in new[] { "ZeroingRange", "MovingTargetRange" })
            {
                var path = "Assets/Scenes/" + prefix + "Scene.unity";
                var scene = SceneManager.GetSceneByPath(path);
                var owned = !scene.isLoaded;
                if (owned) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try { PatchScene(scene, prefix); EditorSceneManager.SaveScene(scene); }
                finally { if (owned) EditorSceneManager.CloseScene(scene, true); }
            }
            AssetDatabase.SaveAssets();
        }

        public static void PatchScene(UnityEngine.SceneManagement.Scene scene, string prefix)
        {
            var root = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .First(t => t.name == prefix).Find("Environment");
            for (var side = 0; side < 2; side++)
            {
                var label = side == 0 ? "Left" : "Right";
                var name = "Slogan_Side_" + label;
                var group = root.Find(name);
                if (group == null)
                {
                    group = new GameObject(name).transform;
                    group.SetParent(root, false);
                    group.gameObject.AddComponent<SceneTestId>().Id = prefix + ".Visual.Slogan.Side." + label;
                }
                group.position = new Vector3(side == 0 ? -53 : 53, 9.38f, 110.9f);
                var materialPath = Art + "RangeSideSlogan" + label + ".mat";
                var texturePath = Art + "RangeSideSlogan" + label + ".png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 4096;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.anisoLevel = 4;
                importer.SaveAndReimport();
                var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (mat == null)
                {
                    mat = new Material(Shader.Find("SimulatedShooting/World Space Text Occluded"));
                    AssetDatabase.CreateAsset(mat, materialPath);
                }
                mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                mat.color = new Color(.82f, .04f, .02f, 1);
                EditorUtility.SetDirty(mat);
                var board = group.Find("WhiteBoard");
                if (board == null)
                {
                    board = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                    board.name = "WhiteBoard"; board.SetParent(group, false);
                    UnityEngine.Object.DestroyImmediate(board.GetComponent<Collider>());
                }
                board.localPosition = Vector3.zero;
                board.localScale = new Vector3(52, 10.8f, .3f);
                board.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/SimulatedShooting/Art/Materials/RangeIronFistVictoryWall.mat");
                var face = group.Find("RedSlogan_" + label);
                if (face == null)
                {
                    face = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
                    face.name = "RedSlogan_" + label; face.SetParent(group, false);
                    UnityEngine.Object.DestroyImmediate(face.GetComponent<Collider>());
                }
                face.localPosition = new Vector3(0, 0, -.18f);
                face.localScale = new Vector3(50, 10, 1);
                var renderer = face.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = mat;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
