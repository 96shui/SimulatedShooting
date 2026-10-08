using System.Collections;
using System.Linq;
using NUnit.Framework;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.TestTools;

namespace SimulatedShooting.Tests.PlayMode
{
    public sealed class VRHandPresentationTests
    {
        [UnityTest]
        public IEnumerator Bdd23_Task019_HideHardwareModelsKeepHandsAndUiRaysIncludingLateModels()
        {
            var rig = new GameObject("Rig");
            try
            {
                var controller = new GameObject("Right Controller"); controller.transform.SetParent(rig.transform);
                var hand = new GameObject("VirtualHand_Right"); hand.transform.SetParent(controller.transform);
                var model = GameObject.CreatePrimitive(PrimitiveType.Cube); model.transform.SetParent(hand.transform);
                var visual = hand.AddComponent<VRControllerHandVisual>();
                visual.Configure(VirtualHandSide.Right, model.transform, null, null, Vector3.zero, Vector3.zero);
                var hardware = GameObject.CreatePrimitive(PrimitiveType.Cube); hardware.transform.SetParent(controller.transform);
                var line = controller.AddComponent<LineRenderer>();
                rig.SetActive(false);
                VRHandPresentation.EnsureOnRig(rig);
                Assert.That(model.GetComponent<Renderer>().enabled, Is.True,"Installing on an inactive rig must not classify the hand as hardware");
                rig.SetActive(true);
                yield return null;
                Assert.That(model.GetComponent<Renderer>().enabled, Is.True);
                Assert.That(hardware.GetComponent<Renderer>().enabled, Is.False);
                Assert.That(line.enabled, Is.True);
                var lateModel = GameObject.CreatePrimitive(PrimitiveType.Cube); lateModel.transform.SetParent(controller.transform);
                yield return new WaitForSeconds(.6f);
                Assert.That(lateModel.GetComponent<Renderer>().enabled, Is.False);
                Assert.That(visual.HasRenderableHand, Is.True);
            }
            finally { Object.Destroy(rig); }
        }
    }
}
