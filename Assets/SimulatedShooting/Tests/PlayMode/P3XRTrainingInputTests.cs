using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using SimulatedShooting.Scene;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

using UnityEngine.InputSystem.XR;

namespace SimulatedShooting.Tests.PlayMode
{
    // BDD23 posture/input; interface18 physical P3 controller bindings.
    public sealed class P3XRTrainingInputTests
    {
        XRController left,right;
        [SetUp] public void Setup()
        {
            InputSystem.RegisterLayout(@"{""name"":""P3BindingTestController"",""extend"":""XRController"",""controls"":[
                {""name"":""thumbstick"",""layout"":""Stick""},
                {""name"":""primaryButton"",""layout"":""Button"",""format"":""FLT"",""sizeInBits"":32},
                {""name"":""secondaryButton"",""layout"":""Button"",""format"":""FLT"",""sizeInBits"":32},
                {""name"":""gripPressed"",""layout"":""Button"",""format"":""FLT"",""sizeInBits"":32},
                {""name"":""trigger"",""layout"":""Axis""}]} ");
            left=(XRController)InputSystem.AddDevice("P3BindingTestController");InputSystem.SetDeviceUsage(left,UnityEngine.InputSystem.CommonUsages.LeftHand);
            right=(XRController)InputSystem.AddDevice("P3BindingTestController");InputSystem.SetDeviceUsage(right,UnityEngine.InputSystem.CommonUsages.RightHand);
        }
        [TearDown] public void Cleanup()
        {
            if(left!=null&&left.added)InputSystem.RemoveDevice(left);
            if(right!=null&&right.added)InputSystem.RemoveDevice(right);
            InputSystem.RemoveLayout("P3BindingTestController");
        }
        [UnityTest] public IEnumerator Screen23_VrSticksAndFaceButtonsReachP3Commands()
        {
            yield return null;
            var input=new P3XRTrainingInput();input.Sample(true);
            Assert.That(input.ReloadPressed||input.ConfirmPressed||input.SwitchShoulderPressed||input.PosturePressed,Is.False);
            InputSystem.QueueDeltaStateEvent(left.GetChildControl<StickControl>("thumbstick"),new Vector2(.3f,.8f));
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<StickControl>("thumbstick"),new Vector2(.9f,0));
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<ButtonControl>("primaryButton"),1f);
            InputSystem.QueueDeltaStateEvent(left.GetChildControl<ButtonControl>("primaryButton"),1f);
            yield return null;
            input.Sample(true);
            Assert.That(input.MoveAxis.y,Is.GreaterThan(.5f));Assert.That(input.TurnAxis.x,Is.GreaterThan(.7f));
            Assert.That(input.ReloadPressed,Is.True,"right primary reload; value="+right.GetChildControl<ButtonControl>("primaryButton").ReadValue()+" updated="+right.wasUpdatedThisFrame);Assert.That(input.ConfirmPressed,Is.True,"left primary confirm");
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<ButtonControl>("secondaryButton"),1f);
            InputSystem.QueueDeltaStateEvent(left.GetChildControl<ButtonControl>("secondaryButton"),1f);
            yield return null;input.Sample(true);
            Assert.That(input.SwitchShoulderPressed,Is.True,"right secondary shoulder");Assert.That(input.PosturePressed,Is.True,"left secondary posture");
        }
        [UnityTest] public IEnumerator Screen23_VrGripsRetainIndependentPressHoldReleaseEdges()
        {
            yield return null;
            var input=new P3XRTrainingInput();input.Sample(true);
            InputSystem.QueueDeltaStateEvent(right.GetChildControl<ButtonControl>("gripPressed"),1f);
            InputSystem.QueueDeltaStateEvent(left.GetChildControl<ButtonControl>("gripPressed"),1f);
            yield return null;input.Sample(true);
            Assert.That(input.RightGripPressed&&input.LeftGripPressed,Is.True);
            input.Sample(true);Assert.That(input.RightGripHeld&&input.LeftGripHeld,Is.True);Assert.That(input.RightGripPressed,Is.False);
            InputSystem.QueueDeltaStateEvent(left.GetChildControl<ButtonControl>("gripPressed"),0f);yield return null;input.Sample(true);
            Assert.That(input.LeftGripReleased,Is.True);Assert.That(input.RightGripHeld,Is.True);
        }
    }
}
