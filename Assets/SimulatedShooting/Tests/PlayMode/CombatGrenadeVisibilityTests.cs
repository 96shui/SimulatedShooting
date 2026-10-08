using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SimulatedShooting.Scene;
using VRShooting.Common;

public sealed class CombatGrenadeVisibilityTests
{
    // BDD27: the visible grenade remains at the authoritative location until explosion/cancellation.
    [Test]
    public void ServiceDrivenGrenadeDoesNotExpireFromPresentationTime()
    {
        var root=new GameObject("GrenadeVisibility");
        try
        {
            var projectile=root.AddComponent<CombatGrenadeProjectile>();
            var expired=false;
            var position=new Vector3(2,3,4);
            projectile.Launch(new GrenadeThrowPlanDto
            { Origin=Vector3.zero,Target=Vector3.forward*10,ThrowTime=0,ExplosionTime=2 },
                ()=>expired=true,()=>position);
            typeof(CombatGrenadeProjectile).GetField("elapsed",BindingFlags.Instance|BindingFlags.NonPublic)
                .SetValue(projectile,10f);
            typeof(CombatGrenadeProjectile).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(projectile,null);
            Assert.That(expired,Is.False);
            Assert.That(projectile.transform.position,Is.EqualTo(position));
            projectile.transform.rotation=Quaternion.Euler(55,20,40);
            typeof(CombatGrenadeProjectile).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(projectile,null);
            Assert.That(Quaternion.Angle(projectile.transform.rotation,Quaternion.identity),Is.LessThan(.01f),
                "A stationary grenade must settle upright instead of retaining a buried tumble pose.");
        }
        finally { Object.DestroyImmediate(root); }
    }
}
