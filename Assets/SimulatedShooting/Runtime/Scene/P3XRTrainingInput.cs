using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
using VRShooting.Input;
using VRShooting.Common;

namespace SimulatedShooting.Scene
{
    // P3-only hardware adapter. Desktop grip toggles keep both hands available for WASD/mouse.
    public sealed class P3XRTrainingInput : IXRTrainingInput, IShoulderAwareTrainingInput
    {
        readonly InputSystemXRTrainingInput desktop=new InputSystemXRTrainingInput();
        bool rear,front,oldRear,oldFront,oldTrigger,trigger,grenadeHeld,oldGrenadeHeld,grenadePressed;
        bool reloadHeld,oldReloadHeld,reloadPressed;
        public bool ReloadWithLeftX { get; set; } = true;
        public bool IsVr {get;private set;}
        ShoulderSide shoulder = ShoulderSide.Right;
        public void SetShoulder(ShoulderSide value)
        {
            if (shoulder == value) return;
            shoulder = value;
            desktop.SetShoulder(value);
            oldTrigger = trigger = false;
        }
        public void Sample(bool vr)
        {
            IsVr=vr; oldRear=rear;oldFront=front;oldTrigger=trigger;oldGrenadeHeld=grenadeHeld;oldReloadHeld=reloadHeld;
            if(vr)
            {
                rear=Held(XRController.rightHand,"gripPressed");front=Held(XRController.leftHand,"gripPressed");
                var right=UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
                bool xrPrimary=right.isValid&&right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton,out bool pressed)&&pressed;
                grenadeHeld=Held(XRController.rightHand,"primaryButton")||xrPrimary;
                grenadePressed=Pressed(XRController.rightHand,"primaryButton")||(grenadeHeld&&!oldGrenadeHeld);
                var left=UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
                var reloadUsage=ReloadWithLeftX ? UnityEngine.XR.CommonUsages.primaryButton : UnityEngine.XR.CommonUsages.primary2DAxisClick;
                var legacyReload=left.isValid&&left.TryGetFeatureValue(reloadUsage,out bool reload)&&reload;
                reloadHeld=Held(XRController.leftHand,ReloadWithLeftX?"primaryButton":"primary2DAxisClick")||legacyReload;
                reloadPressed=reloadHeld&&!oldReloadHeld;
            }
            else
            {
                if(Keyboard.current?.eKey.wasPressedThisFrame==true)rear=!rear;
                if(Keyboard.current?.gKey.wasPressedThisFrame==true)front=!front;
                grenadeHeld=false;grenadePressed=Keyboard.current?.hKey.wasPressedThisFrame==true;
                reloadHeld=reloadPressed=false;
            }
            trigger=desktop.TriggerHeld;
        }
        public void Reset() { SetShoulder(ShoulderSide.Right); rear=front=oldRear=oldFront=oldTrigger=trigger=grenadeHeld=oldGrenadeHeld=grenadePressed=reloadHeld=oldReloadHeld=reloadPressed=false; }
        public bool PosturePressed => IsVr ? Pressed(XRController.leftHand,"secondaryButton") : Keyboard.current?.cKey.wasPressedThisFrame==true;
        public bool ConfirmPressed=>IsVr ? !ReloadWithLeftX&&Pressed(XRController.leftHand,"primaryButton") : desktop.ConfirmPressed;
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
        public bool ReloadPressed=>IsVr ? reloadPressed : desktop.ReloadPressed;
        public bool GrenadePressed=>grenadePressed;
        public bool GrenadeHeld=>grenadeHeld;
        public void ClearOpeningToggles()
        {if(!IsVr)rear=front=oldRear=oldFront=false;grenadePressed=false;}
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
