using NUnit.Framework;
using UnityEngine;
using SimulatedShooting.Scene;

public sealed class DroneReconRoutePlannerTests
{
    [Test]
    public void BriefFlyoverFollowsAuthoredOrderWithoutTouringGroundBranches()
    {
        var points=new[]{new Vector3(9,0,0),new Vector3(14,0,-1),new Vector3(16,0,-2)};
        var route=DroneReconRoutePlanner.BuildFlyover(Vector3.zero,points,_=>0,3.2f);
        var last=-1;
        foreach(var point in points)
        {
            var index=route.FindIndex(p=>Vector2.Distance(new Vector2(p.x,p.z),new Vector2(point.x,point.z))<.05f);
            Assert.That(index,Is.GreaterThan(last));last=index;
        }
        Assert.That(route.Count,Is.LessThan(20));
    }
    // BDD28: both branches and every observation must be visited before returning.
    [Test]
    public void SurveyIncludesAllBranchesAndFinishesAtAnObservation()
    {
        var points=new[]{new Vector3(10,0,0),new Vector3(20,0,0),new Vector3(10,0,10),new Vector3(10,0,-10)};
        var route=DroneReconRoutePlanner.Build(Vector3.zero,points,_=>0,3.2f);
        foreach(var point in points)
            Assert.That(route.Exists(p=>Vector2.Distance(new Vector2(p.x,p.z),new Vector2(point.x,point.z))<.05f),Is.True);
        Assert.That(route.TrueForAll(p=>Mathf.Abs(p.y-3.2f)<.001f),Is.True);
        Assert.That(route.Count,Is.GreaterThan(points.Length),"Long legs are sampled to follow terrain clearance.");
    }
}
