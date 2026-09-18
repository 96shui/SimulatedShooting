using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
using VRShooting.Input;

namespace SimulatedShooting.Scene
{
    // P3-only hardware adapter. Desktop grip toggles keep both hands available for WASD/mouse.
    public sealed class P3XRTrainingInput : IXRTrainingInput
    {
        readonly InputSystemXRTrainingInput desktop=new InputSystemXRTrainingInput();
        bool rear,front,oldRear,oldFront,oldTrigger,trigger;
        public bool IsVr {get;private set;}
        public void Sample(bool vr)
        {
            IsVr=vr; oldRear=rear;oldFront=front;oldTrigger=trigger;
            if(vr) { rear=Held(XRController.rightHand,"gripPressed");front=Held(XRController.leftHand,"gripPressed"); }
            else
            {
                if(Keyboard.current?.eKey.wasPressedThisFrame==true)rear=!rear;
                if(Keyboard.current?.gKey.wasPressedThisFrame==true)front=!front;
            }
            trigger=desktop.TriggerHeld;
        }
        public void Reset() { rear=front=oldRear=oldFront=oldTrigger=trigger=false; }
        public bool PosturePressed => IsVr ? Pressed(XRController.leftHand,"secondaryButton") : Keyboard.current?.cKey.wasPressedThisFrame==true;
        public bool ConfirmPressed=>IsVr ? Pressed(XRController.leftHand,"primaryButton") : desktop.ConfirmPressed;
        public bool BackPressed=>desktop.BackPressed;
        public bool TriggerPressed=>trigger&&!oldTrigger;
        public bool TriggerHeld=>trigger;
        public bool TriggerReleased=>!trigger&&oldTrigger;
        public float RightTriggerValue=>desktop.RightTriggerValue;
        public bool RightGripPressed=>rear&&!oldRear;
        public bool RightGripHeld=>rear;
        public bool RightGripReleased=>!rear&&oldRear;
        public bool LeftGripPressed=>front&&!oldFront;
        public bool LeftGripHeld=>front;
        public bool LeftGripReleased=>!front&&oldFront;
        public bool ReloadPressed=>IsVr ? Pressed(XRController.rightHand,"primaryButton") : desktop.ReloadPressed;
        public bool SwitchShoulderPressed=>IsVr ? Pressed(XRController.rightHand,"secondaryButton") : desktop.SwitchShoulderPressed;
        public bool AimPressed=>desktop.AimPressed;
        public bool AimHeld=>desktop.AimHeld;
        public bool CommandMenuHeld=>false;
        public Vector2 MoveAxis=>IsVr ? Axis(XRController.leftHand) : desktop.MoveAxis;
        public Vector2 TurnAxis=>IsVr ? Axis(XRController.rightHand) : new Vector2((Keyboard.current?.rightArrowKey.isPressed==true?1:0)-(Keyboard.current?.leftArrowKey.isPressed==true?1:0),0);
        static bool Pressed(XRController c,string name)=>c?.TryGetChildControl<ButtonControl>(name)?.wasPressedThisFrame==true;
        static bool Held(XRController c,string name)=>c?.TryGetChildControl<ButtonControl>(name)?.isPressed==true;
        static Vector2 Axis(XRController c)=>c?.TryGetChildControl<Vector2Control>("thumbstick")?.ReadValue() ?? c?.TryGetChildControl<Vector2Control>("primary2DAxis")?.ReadValue() ?? Vector2.zero;
    }
}
