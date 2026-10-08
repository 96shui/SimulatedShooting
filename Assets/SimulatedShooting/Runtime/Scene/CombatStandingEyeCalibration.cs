using System;
using UnityEngine;
using UnityEngine.XR;

namespace SimulatedShooting.Scene
{
    /// <summary>One opening calibration, with tracked camera/controllers kept under the same offset.</summary>
    public sealed class CombatStandingEyeCalibration : MonoBehaviour
    {
        Camera eye;
        Transform correction;
        float standingEyeHeight;
        bool waitingForTracking;
        int trackedFrames;
        Action onAligned;

        public void Configure(Camera camera,Transform floorOffset,float height,Action aligned=null)
        {
            eye=camera;standingEyeHeight=height;onAligned=aligned;
            correction=floorOffset.Find("Combat_StandingCalibration");
            if(correction==null)
            {
                correction=new GameObject("Combat_StandingCalibration").transform;
                correction.SetParent(floorOffset,false);
                for(var i=floorOffset.childCount-1;i>=0;i--)
                {
                    var child=floorOffset.GetChild(i);
                    if(child!=correction)child.SetParent(correction,false);
                }
            }
            correction.localPosition=Vector3.zero;
            Align();waitingForTracking=true;trackedFrames=0;
        }
        void LateUpdate()
        {
            if(!waitingForTracking||eye==null)return;
            var head=InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if(!head.isValid||!head.TryGetFeatureValue(CommonUsages.isTracked,out var tracked)||!tracked
                ||!head.TryGetFeatureValue(CommonUsages.devicePosition,out var position)||position.y<.2f){trackedFrames=0;return;}
            if(++trackedFrames<3)return;
            Align();waitingForTracking=false;onAligned?.Invoke();
        }
        void Align()
        {
            // Offset the shared tracking parent; never overwrite the hardware camera pose.
            correction.position+=Vector3.up*(standingEyeHeight-(eye.transform.position.y-transform.position.y));
        }
    }
}
