using NUnit.Framework;
using UnityEngine;
using SimulatedShooting.Scene;

public sealed class CombatStandingEyeCalibrationTests
{
    // BDD28: seated/floor-origin input is calibrated to the soldier's standing head without freezing tracking.
    [Test]
    public void InitialHeadHeightIsCalibratedAndSubsequentPhysicalMovementIsPreserved()
    {
        var rig=new GameObject("StandingRig");
        try
        {
            rig.transform.position=new Vector3(0,2,0);
            var floor=new GameObject("FloorOffset").transform;floor.SetParent(rig.transform,false);
            var camera=new GameObject("TrackedEye",typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetParent(floor,false);camera.transform.localPosition=new Vector3(.2f,.7f,.1f);
            camera.transform.localRotation=Quaternion.Euler(10,20,0);var rotation=camera.transform.rotation;
            var calibration=rig.AddComponent<CombatStandingEyeCalibration>();calibration.Configure(camera,floor,1.85f);
            Assert.That(camera.transform.position.y-rig.transform.position.y,Is.EqualTo(1.85f).Within(.001f));
            Assert.That(camera.transform.localPosition.y,Is.EqualTo(.7f));
            Assert.That(Quaternion.Angle(rotation,camera.transform.rotation),Is.LessThan(.001f));
            camera.transform.localPosition+=Vector3.up*.2f;
            Assert.That(camera.transform.position.y-rig.transform.position.y,Is.EqualTo(2.05f).Within(.001f));
            calibration.Configure(camera,floor,1.85f);
            Assert.That(camera.transform.position.y-rig.transform.position.y,Is.EqualTo(1.85f).Within(.001f));
            Assert.That(floor.childCount,Is.EqualTo(1),"Recalibration must reuse its tracking parent.");
        }
        finally{Object.DestroyImmediate(rig);}
    }
}
