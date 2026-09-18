using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SimulatedShooting.Editor
{
    public static class P3ProductionInstaller
    {
        [MenuItem("Tools/Simulated Shooting/P3/Install Production Bindings")]
        public static void Install()
        {
            var scene=EditorSceneManager.OpenScene(UnityCombatSceneLoader.ScenePath,OpenSceneMode.Additive);
            try
            {
                var b=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CombatSceneBindings>(true)).Single();
                b.TrainingRiflePrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimulatedShooting/Prefabs/Weapons/Weapon_training-rifle_Blockout.prefab");
                b.RifleShotClip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SimulatedShooting/Audio/Weapons/rifle-sks-single-shot.wav");
                if(b.TrainingRiflePrefab==null)throw new System.InvalidOperationException("Training rifle prefab missing");
                foreach(var collider in b.WeaponAnchor.GetComponentsInChildren<Collider>(true))collider.enabled=false;
                foreach(var point in b.Points.Where(p=>p.Kind==CombatPointKind.EnemySpawn))
                {
                    if(point.EstimateAnchor!=null)
                    {
                        // Generated estimates follow authored spawn changes when bindings are reinstalled.
                        if(point.EstimateAnchor.name=="Estimate_"+point.Id)
                        {
                            point.EstimateAnchor.position=point.transform.position+new Vector3(-.5f,0,-.5f);
                            EditorUtility.SetDirty(point.EstimateAnchor);
                        }
                        continue;
                    }
                    var estimate=b.Points.Where(p=>p.Kind==CombatPointKind.EstimateArea&&p.RegionId==point.RegionId&&p.FloorId==point.FloorId)
                        .OrderBy(p=>Vector3.Distance(p.transform.position,point.transform.position)).FirstOrDefault();
                    if(estimate!=null&&Vector3.Distance(estimate.transform.position,point.transform.position)<=2f)
                        point.EstimateAnchor=estimate.transform;
                    else
                    {
                        var anchor=new GameObject("Estimate_"+point.Id).transform;
                        anchor.SetParent(b.transform,false);
                        anchor.position=point.transform.position+new Vector3(-.5f,0,-.5f);
                        point.EstimateAnchor=anchor;
                    }
                    EditorUtility.SetDirty(point);
                }
                EditorUtility.SetDirty(b);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                var scenes=EditorBuildSettings.scenes.ToList();
                if(!scenes.Any(s=>s.path==UnityCombatSceneLoader.ScenePath))scenes.Add(new EditorBuildSettingsScene(UnityCombatSceneLoader.ScenePath,true));
                EditorBuildSettings.scenes=scenes.ToArray();
                AssetDatabase.SaveAssets();
                Debug.Log("P3 production rifle binding and Build Settings installed.");
            }
            finally{EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
