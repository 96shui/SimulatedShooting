using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using SimulatedShooting.Scene;

namespace SimulatedShooting.Editor
{
    public static class DjiMavic3Installer
    {
        const string Source="Assets/SimulatedShooting/Art/Drones/DJI-Mavic-3/source/DJI-Mavic_3.glb";
        const string MeshDirectory="Assets/SimulatedShooting/Art/Drones/DJI-Mavic-3/Baked";
        public const string PrefabPath="Assets/Resources/Combat/Drone_Realistic.prefab";

        [MenuItem("VR Shooting/Combat/Import and Bind DJI Mavic 3")]
        public static void Install()
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Source);
            if(source==null)throw new InvalidOperationException("Import the DJI GLB using glTFast first.");
            Directory.CreateDirectory(MeshDirectory);AssetDatabase.Refresh();
            var imported=UnityEngine.Object.Instantiate(source);
            var root=new GameObject("Drone_Realistic");
            try
            {
                imported.transform.rotation=Quaternion.Euler(0,180,0); // Camera is on the source model's -Z side.
                var bounds=BoundsOf(imported);
                imported.transform.localScale*=.65f/Mathf.Max(bounds.size.x,bounds.size.z);
                bounds=BoundsOf(imported);
                imported.transform.position=new Vector3(-bounds.center.x,-bounds.min.y-.20f,-bounds.center.z);
                var renderers=imported.GetComponentsInChildren<MeshRenderer>();
                Bake(renderers.Where(r=>!r.name.StartsWith("桨叶",StringComparison.Ordinal)).ToArray(),root.transform,"Body");
                var motors=imported.GetComponentsInChildren<Transform>().Where(t=>t.name.Contains("电机")).ToArray();
                if(motors.Length!=4)throw new InvalidOperationException("Expected four DJI rotor motors.");
                for(var i=0;i<motors.Length;i++)
                {
                    var blades=motors[i].GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("桨叶",StringComparison.Ordinal)).ToArray();
                    if(blades.Length==0)throw new InvalidOperationException("Missing rotor blades: "+motors[i].name);
                    var hub=new GameObject("Rotor").transform;hub.SetParent(root.transform,false);
                    hub.position=new Vector3(motors[i].position.x,blades.Average(r=>r.bounds.center.y),motors[i].position.z);
                    Bake(blades,hub,"Rotor"+i);
                }
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
                var scene=SceneManager.GetSceneByPath(UnityCombatSceneLoader.TrenchScenePath);
                var opened=!scene.isLoaded;
                if(opened)scene=EditorSceneManager.OpenScene(UnityCombatSceneLoader.TrenchScenePath,OpenSceneMode.Additive);
                try
                {
                    var binding=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CombatSceneBindings>(true)).Single();
                    binding.DroneVisualPrefab=prefab;
                    foreach(var renderer in binding.Drone.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
                    var existing=binding.Drone.Find("Drone_RealisticVisual");
                    if(existing!=null)UnityEngine.Object.DestroyImmediate(existing.gameObject);
                    var visual=(GameObject)PrefabUtility.InstantiatePrefab(prefab,binding.Drone);
                    visual.name="Drone_RealisticVisual";visual.transform.localPosition=Vector3.zero;
                    visual.transform.localRotation=Quaternion.identity;
                    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                }
                finally {if(opened)EditorSceneManager.CloseScene(scene,true);}
                AssetDatabase.SaveAssets();Selection.activeObject=prefab;
                Debug.Log("DJI Mavic 3 imported and bound: "+PrefabPath);
            }
            finally {UnityEngine.Object.DestroyImmediate(imported);UnityEngine.Object.DestroyImmediate(root);}
        }

        static Bounds BoundsOf(GameObject model)
        {
            var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
            foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
        static void Bake(MeshRenderer[] sources,Transform destination,string name)
        {
            var materials=sources.SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
            for(var materialIndex=0;materialIndex<materials.Length;materialIndex++)
            {
                var material=materials[materialIndex];
                var instances=sources.SelectMany(renderer=>renderer.sharedMaterials.Select((m,i)=>new {m,i,renderer}))
                    .Where(part=>part.m==material).Select(part=>new CombineInstance {
                        mesh=part.renderer.GetComponent<MeshFilter>().sharedMesh,subMeshIndex=part.i,
                        transform=destination.worldToLocalMatrix*part.renderer.transform.localToWorldMatrix}).ToArray();
                var mesh=new Mesh {name=name+"_"+materialIndex,indexFormat=IndexFormat.UInt32};
                mesh.CombineMeshes(instances,true,true);mesh.RecalculateBounds();
                var path=MeshDirectory+"/"+mesh.name+".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing==null)AssetDatabase.CreateAsset(mesh,path);
                else {EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}
                var part=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(destination,false);
                part.GetComponent<MeshFilter>().sharedMesh=mesh;part.GetComponent<MeshRenderer>().sharedMaterial=material;
            }
        }
    }
}
