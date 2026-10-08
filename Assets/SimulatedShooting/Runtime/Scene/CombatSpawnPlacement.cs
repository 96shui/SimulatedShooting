using UnityEngine;

namespace SimulatedShooting.Scene
{
    /// <summary>Find a nearby standing position above the actual geometry, not the authored anchor height.</summary>
    public static class CombatSpawnPlacement
    {
        public static Vector3 Resolve(Transform geometry,Vector3 anchor,float radius,float height)
        {
            var first=OnGround(geometry,anchor);
            if(Clear(geometry,first,radius,height))return first;
            var best=first;var bestCost=float.PositiveInfinity;
            for(var ring=1;ring<=6;ring++)
                for(var direction=0;direction<16;direction++)
                {
                    var angle=direction*Mathf.PI/8;
                    var point=OnGround(geometry,anchor+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*ring*.35f);
                    if(!Clear(geometry,point,radius,height))continue;
                    // Prefer a nearby ground-level opening over spawning on a trench roof.
                    var cost=Vector2.Distance(new Vector2(point.x,point.z),new Vector2(anchor.x,anchor.z))
                        +Mathf.Abs(point.y-first.y)*2;
                    if(cost<bestCost){bestCost=cost;best=point;}
                }
            return best;
        }
        static Vector3 OnGround(Transform geometry,Vector3 point)
        {
            var ground=point.y;
            foreach(var terrain in geometry.GetComponentsInChildren<Terrain>(true))
            {
                var local=point-terrain.transform.position;var size=terrain.terrainData.size;
                if(local.x<0||local.z<0||local.x>size.x||local.z>size.z)continue;
                ground=terrain.SampleHeight(point)+terrain.transform.position.y;
            }
            foreach(var hit in Physics.RaycastAll(new Vector3(point.x,ground+1.35f,point.z),Vector3.down,3,~0,QueryTriggerInteraction.Ignore))
                if(hit.transform.IsChildOf(geometry)&&hit.normal.y>=.65f)ground=Mathf.Max(ground,hit.point.y);
            // Clearance also covers the downhill side of the capsule on the original sloped terrain.
            point.y=ground+.12f;return point;
        }
        static bool Clear(Transform geometry,Vector3 feet,float radius,float height)
        {
            foreach(var collider in Physics.OverlapCapsule(feet+Vector3.up*radius,feet+Vector3.up*(height-radius),radius,~0,QueryTriggerInteraction.Ignore))
                if(collider.transform.IsChildOf(geometry))return false;
            return true;
        }
    }
}
