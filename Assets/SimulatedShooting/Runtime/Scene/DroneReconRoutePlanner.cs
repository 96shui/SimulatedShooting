using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace SimulatedShooting.Scene
{
    public static class DroneReconRoutePlanner
    {
        public static List<Vector3> BuildFlyover(Vector3 launch,IEnumerable<Vector3> observations,Func<Vector3,float> groundHeight,float cruiseHeight)
        {
            var result=new List<Vector3>();var previous=launch;
            foreach(var destination in observations)
            {
                var samples=Mathf.Max(1,Mathf.CeilToInt(HorizontalDistance(previous,destination)/1.5f));
                for(var i=1;i<=samples;i++)
                {
                    var point=Vector3.Lerp(previous,destination,i/(float)samples);point.y=groundHeight(point)+cruiseHeight;
                    result.Add(point);
                }
                previous=destination;
            }
            return result;
        }
        // Visit every observation once. Connecting/reversing branches is allowed; no tour is replayed.
        public static List<Vector3> Build(Vector3 launch,IEnumerable<Vector3> observations,Func<Vector3,float> groundHeight,float cruiseHeight)
        {
            var remaining=new List<Vector3>();
            foreach(var point in observations)
                if(!remaining.Any(p=>HorizontalDistance(p,point)<.5f))remaining.Add(point);
            var route=new List<Vector3>();var current=launch;
            while(remaining.Count>0)
            {
                var next=remaining.OrderBy(p=>HorizontalDistance(current,p)).First();remaining.Remove(next);
                var path=new NavMeshPath();Vector3[] corners;
                if(NavMesh.SamplePosition(current,out var from,4,NavMesh.AllAreas)
                    && NavMesh.SamplePosition(next,out var to,4,NavMesh.AllAreas)
                    && NavMesh.CalculatePath(from.position,to.position,NavMesh.AllAreas,path)
                    && path.status==NavMeshPathStatus.PathComplete)
                    corners=new[]{current}.Concat(path.corners).Concat(new[]{next}).ToArray();
                else corners=new[]{current,next}; // An aerial survey can cross terrain without a ground-agent path.
                for(var i=1;i<corners.Length;i++)
                {
                    var samples=Mathf.Max(1,Mathf.CeilToInt(HorizontalDistance(corners[i-1],corners[i])/1.5f));
                    for(var sample=1;sample<=samples;sample++)
                    {
                        var point=Vector3.Lerp(corners[i-1],corners[i],sample/(float)samples);
                        point.y=groundHeight(point)+cruiseHeight;
                        if(route.Count==0||Vector3.Distance(route[route.Count-1],point)>.05f)route.Add(point);
                    }
                }
                current=next;
            }
            return route;
        }
        static float HorizontalDistance(Vector3 a,Vector3 b)=>Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z));
    }
}
