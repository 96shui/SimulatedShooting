using NUnit.Framework;
using UnityEngine;
using SimulatedShooting.Scene;

public sealed class CombatSpawnPlacementTests
{
    // BDD28: opening must start above geometry with a clear player capsule, without VR hardware.
    [Test]
    public void EmbeddedAnchorResolvesAboveFloorAndOutsideWall()
    {
        var root=new GameObject("SpawnGeometry");
        try
        {
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(root.transform);
            floor.transform.position=new Vector3(0,1,0);floor.transform.localScale=new Vector3(10,.2f,10);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(root.transform);
            wall.transform.position=new Vector3(0,2,0);wall.transform.localScale=new Vector3(.3f,2,.3f);
            Physics.SyncTransforms();
            var point=CombatSpawnPlacement.Resolve(root.transform,new Vector3(0,.5f,0),.22f,1.7f);
            Assert.That(point.y,Is.GreaterThanOrEqualTo(1.15f));
            foreach(var collider in Physics.OverlapCapsule(point+Vector3.up*.22f,point+Vector3.up*1.48f,.22f))
                Assert.That(collider.transform.IsChildOf(root.transform),Is.False);
        }
        finally{Object.DestroyImmediate(root);}
    }
}
