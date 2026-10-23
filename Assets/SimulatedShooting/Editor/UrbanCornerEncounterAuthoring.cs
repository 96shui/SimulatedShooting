using System;
using System.Linq;
using SimulatedShooting.Scene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace SimulatedShooting.Editor
{
    public static class UrbanCornerEncounterAuthoring
    {
        [MenuItem("Tools/Simulated Shooting/Apply Urban Corner Encounter")]
        public static void Apply()
        {
            var active=EditorSceneManager.GetActiveScene();
            if(active.IsValid()&&active.isDirty)throw new InvalidOperationException("Save scene edits before applying corner positions.");
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene=EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity",OpenSceneMode.Single);
                var binding=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CombatSceneBindings>(true)).Single();
                var navigation=NavMesh.AddNavMeshData(binding.NavigationData);
                try{Patch(binding);}finally{navigation.Remove();}
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }

        public static void Patch(CombatSceneBindings binding)
        {
            var positions=new[]{new Vector3(38.8f,0,65.2f),new Vector3(23.2f,0,86.8f),
                new Vector3(38.8f,3.6f,65.2f),new Vector3(23.2f,3.6f,86.8f),
                new Vector3(38.8f,7.2f,65.2f),new Vector3(23.2f,7.2f,86.8f)};
            var rooms=new[]{"room-11","room-12","room-21","room-22","room-31","room-31"};
            for(var i=0;i<positions.Length;i++)
            {
                var point=binding.Points.Single(p=>p.Id=="building-spawn-"+(i+1));
                if(!NavMesh.SamplePosition(positions[i],out var hit,.7f,NavMesh.AllAreas)||Mathf.Abs(hit.position.y-positions[i].y)>.5f)
                    throw new InvalidOperationException("Corner is not on the expected navigable floor: "+point.Id+" nearest="+hit.position);
                var direction=Vector3.ProjectOnPlane(binding.UrbanEntry.position-hit.position,Vector3.up).normalized;
                point.transform.SetPositionAndRotation(hit.position,Quaternion.LookRotation(direction,Vector3.up));
                point.RoomId=rooms[i];
                // Preserve bounded, inexact map estimates when moving an existing candidate.
                var inward=Vector3.ProjectOnPlane(new Vector3(31,hit.position.y,76)-hit.position,Vector3.up).normalized;
                point.EstimateAnchor.position=hit.position+inward*1.5f;
                EditorUtility.SetDirty(point);
            }
            Debug.Log("[BDD31] Positioned six existing building candidates at room corners; no geometry or actors added.");
        }
    }
}
