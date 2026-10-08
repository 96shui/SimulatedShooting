using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using SimulatedShooting.Scene;

namespace SimulatedShooting.Editor
{
    public static class Mode3FullSurveyInstaller
    {
        [MenuItem("VR Shooting/Combat/Configure Brief Trench Flyover")]
        public static void Install()
        {
            var scene=SceneManager.GetSceneByPath(UnityCombatSceneLoader.TrenchScenePath);
            var opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(UnityCombatSceneLoader.TrenchScenePath,OpenSceneMode.Additive);
            var binding=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CombatSceneBindings>(true)).Single();
            var navigation=NavMesh.AddNavMeshData(binding.NavigationData);
            try
            {
                MoveEnemy(binding,"trench-spawn-1",new Vector3(306,2.1f,275));
                MoveEnemy(binding,"trench-spawn-2",new Vector3(308,2.1f,275));
                var parent=binding.transform.Find("Drone_BriefFlyoverObservations");
                if(parent==null){parent=new GameObject("Drone_BriefFlyoverObservations").transform;parent.SetParent(binding.transform,false);}
                var terrain=binding.GeometryRoot.GetComponentInChildren<Terrain>();
                var observations=new List<Transform>();
                var points=new[]{new Vector3(299,0,282),new Vector3(304,0,281),new Vector3(306,0,280)};
                for(var i=0;i<points.Length;i++)
                {
                    var point=points[i];point.y=terrain.SampleHeight(point)+terrain.transform.position.y;
                    var name="Flyover_"+i.ToString("00");var anchor=parent.Find(name);
                    if(anchor==null){anchor=new GameObject(name).transform;anchor.SetParent(parent,false);}
                    anchor.position=point;observations.Add(anchor);
                }
                var old=binding.transform.Find("Drone_FullTrenchObservations");
                if(old!=null)old.gameObject.SetActive(false);
                binding.DroneReconWaypoints=observations.ToArray();
                EditorUtility.SetDirty(binding);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Debug.Log("Brief flyover saved: "+observations.Count+" overview points; enemy cover positions retained.");
            }
            finally {navigation.Remove();if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        static void MoveEnemy(CombatSceneBindings binding,string id,Vector3 destination)
        {
            if(!NavMesh.SamplePosition(destination,out var hit,.75f,NavMesh.AllAreas))throw new InvalidOperationException("Enemy cover position is not navigable: "+id);
            var point=binding.Points.Single(p=>p.Id==id);
            var delta=hit.position-point.transform.position;
            point.transform.position=hit.position;
            if(point.EstimateAnchor!=null&&!point.EstimateAnchor.IsChildOf(point.transform))point.EstimateAnchor.position+=delta;
            var estimate=binding.Points.FirstOrDefault(p=>p.Id==id.Replace("spawn","estimate"));
            if(estimate!=null&&estimate.transform!=point.EstimateAnchor&&!estimate.transform.IsChildOf(point.transform))estimate.transform.position+=delta;
        }
    }
}


