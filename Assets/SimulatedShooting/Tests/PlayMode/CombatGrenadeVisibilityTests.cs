using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using SimulatedShooting.Scene;
using VRShooting.Common;

public sealed class CombatGrenadeVisibilityTests
{
    [Test]
    public void Bdd27_ImportedModelNormalizesBeforeFirstRenderedFrame()
    {
        var parent=new GameObject("GrenadeWorldRoot");
        try
        {
            var prefab=Resources.Load<GameObject>("Combat/m67_low");
            Assert.That(prefab,Is.Not.Null);
            var model=Object.Instantiate(prefab,parent.transform);
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
            { renderer.enabled=false;renderer.forceRenderingOff=true; }
            model.SetActive(false);
            Assert.That(CombatGrenadeProjectile.NormalizeModel(model,.22f),Is.True);
            var bounds=CombatGrenadeProjectile.GetModelBounds(model);
            Assert.That(bounds.size.y,Is.EqualTo(.22f).Within(.001f));
            Assert.That(Vector3.Distance(bounds.center,parent.transform.position),Is.LessThan(.001f));
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
            { Assert.That(renderer.enabled&&renderer.gameObject.activeInHierarchy,Is.True);Assert.That(renderer.forceRenderingOff,Is.False); }
            Assert.That(parent.transform.lossyScale,Is.EqualTo(Vector3.one));
        }
        finally { Object.DestroyImmediate(parent); }
    }
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
