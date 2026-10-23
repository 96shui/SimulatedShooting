using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VRShooting.Unity.Weapons;

namespace SimulatedShooting.Tests.EditMode
{
    // BDD33 / task013: landmarks measured from the actual OBJ, not from AimLinePoint.
    public sealed class VrMechanicalSightCalibrationTests
    {
        static readonly Vector3 Rear=new Vector3(-.0057315f,.0573025f,-.118108f);
        static readonly Vector3 Front=new Vector3(-.005689f,.050240f,.228503f);
        [Test] public void Bdd33_ImportedMeshHasAnOpenRearApertureAndTheMeasuredFrontPost()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimulatedShooting/Prefabs/Weapons/Weapon_training-rifle_Blockout.prefab");
            var root=Object.Instantiate(prefab);root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            try
            {
                var model=root.transform.Find("WeaponRoot_training-rifle/RecoilRoot_training-rifle/Model_QBZ191");
                var colliders=model.GetComponentsInChildren<MeshFilter>().Select(f=>
                {var c=f.gameObject.AddComponent<MeshCollider>();c.sharedMesh=f.sharedMesh;return c;}).ToArray();
                Physics.SyncTransforms();
                bool Hit(Ray ray,float distance)=>colliders.Any(c=>c.Raycast(ray,out _,distance));
                Assert.That(Hit(new Ray(Rear-Vector3.forward*.01f,Vector3.forward),.02f),Is.False,"Measured sight origin must pass through the visible aperture");
                Assert.That(Hit(new Ray(Rear+Vector3.up*.003f-Vector3.forward*.01f,Vector3.forward),.02f),Is.True,"Three millimetres above the aperture must intersect its metal ring");
                var hits=colliders.Select(c=>c.Raycast(new Ray(Front+Vector3.forward*.001f+Vector3.up*.003f,Vector3.down),out var hit,.008f)?hit:default)
                    .Where(h=>h.collider!=null).OrderBy(h=>h.distance).ToArray();
                Assert.That(hits.Length,Is.GreaterThan(0),"Measured front landmark must land on the real post");
                Assert.That(hits[0].point.y,Is.EqualTo(Front.y).Within(.0004f));
            }
            finally{Object.DestroyImmediate(root);}
        }

        [Test] public void Bdd33_PrefabAimLineMatchesVisibleApertureAndFrontPostAtOneHundredMetres()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SimulatedShooting/Prefabs/Weapons/Weapon_training-rifle_Blockout.prefab");
            var root=Object.Instantiate(prefab);root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            try
            {
                var binding=root.GetComponent<WeaponPrefabBinding>();
                var direction=(Front-Rear).normalized;
                var opticalPoint=Rear+direction*((100-Rear.z)/direction.z);
                var aim=binding.AimLinePoint;
                var logicalPoint=aim.position+aim.forward*((100-aim.position.z)/aim.forward.z);
                TestContext.WriteLine("Visible-vs-logical sight error at 100m: "+Vector3.Distance(opticalPoint,logicalPoint)+" metres");
                Assert.That(Vector3.Distance(logicalPoint,opticalPoint),Is.LessThan(.01f),"The VR player aligns real model sights, not a virtual +Z anchor");
                Assert.That(Vector3.Distance(aim.position,Rear),Is.LessThan(.0001f));
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
