using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimulatedShooting.Scene;

namespace SimulatedShooting.Editor
{
    // Bake the actual scene meshes/materials; UI never maintains a second silhouette.
    public static class SceneBustTargetExporter
    {
        public const string OutputPath="Assets/Resources/UI/Tactical/SceneBustTarget.png";
        [MenuItem("VR Shooting/UI/Export Actual Scene Chest Target")]
        public static void Export()
        {
            var scene=SceneManager.GetSceneByPath("Assets/Scenes/ZeroingRangeScene.unity");
            var opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/ZeroingRangeScene.unity",OpenSceneMode.Additive);
            GameObject clone=null,cameraRoot=null;
            RenderTexture rt=null;Texture2D image=null;var previous=RenderTexture.active;
            try
            {
                var source=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<TargetImpactSurface>(true)).First();
                clone=Object.Instantiate(source.transform.parent.gameObject);
                clone.name="SceneChestTarget_ExportOnly";clone.hideFlags=HideFlags.HideAndDontSave;
                foreach(var t in clone.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
                var surface=clone.GetComponentInChildren<TargetImpactSurface>(true);
                var center=surface.TargetCenter;
                cameraRoot=new GameObject("SceneChestTarget_ExportCamera"){hideFlags=HideFlags.HideAndDontSave};
                var camera=cameraRoot.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;
                camera.orthographicSize=surface.FaceHeightCm*.005f;camera.aspect=surface.FaceWidthCm/surface.FaceHeightCm;
                camera.transform.SetPositionAndRotation(center.position-center.forward*2,Quaternion.LookRotation(center.forward,center.up));
                camera.nearClipPlane=.01f;camera.farClipPlane=4;camera.cullingMask=1<<30;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
                camera.allowHDR=false;camera.allowMSAA=false;
                rt=new RenderTexture(1024,1024,24,RenderTextureFormat.ARGB32);rt.Create();camera.targetTexture=rt;
                camera.Render();RenderTexture.active=rt;
                image=new Texture2D(1024,1024,TextureFormat.RGBA32,false);
                image.ReadPixels(new Rect(0,0,1024,1024),0,0);image.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));File.WriteAllBytes(OutputPath,image.EncodeToPNG());
                AssetDatabase.ImportAsset(OutputPath,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(OutputPath);
                importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;
                importer.sRGBTexture=true;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
                Debug.Log("Exported actual scene chest target: "+OutputPath);
            }
            finally
            {
                RenderTexture.active=previous;
                if(cameraRoot!=null)Object.DestroyImmediate(cameraRoot);
                if(rt!=null){rt.Release();Object.DestroyImmediate(rt);}
                if(image!=null)Object.DestroyImmediate(image);
                if(clone!=null)Object.DestroyImmediate(clone);
                if(opened)EditorSceneManager.CloseScene(scene,true);
            }
        }
    }
}
