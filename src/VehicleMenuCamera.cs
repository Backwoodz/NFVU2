using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Rachel350Z {
public partial class Plugin {
    delegate void GroundCameraPosition(float dt,out Vector3 position,out Quaternion rotation);
    GroundCameraPosition menuGroundCamera;
    bool menuMouseLookHeld;

    void BeginMenuCamera() {
        menuMouseLookHeld=false;menuGroundCamera=null;
        if(menuCamera==null)return;
        // Keep the native camera disabled (its LateUpdate captures the cursor).
        // Reuse only its collision-aware positioning, without native zoom/input.
        var method=menuCamera.GetType().GetMethod("GetCameraPosition",BindingFlags.Instance|BindingFlags.NonPublic);
        if(method!=null)menuGroundCamera=(GroundCameraPosition)Delegate.CreateDelegate(typeof(GroundCameraPosition),menuCamera,method);
    }
    void EndMenuCamera(){menuMouseLookHeld=false;menuGroundCamera=null;}
    void TickMenuCamera() {
        bool mouseLook=Input.GetMouseButton(1)&&!Input.GetMouseButton(0)&&!menuPlacement.Dragging;
        Vector2 degrees=Vector2.zero;
        float scroll=0;
        if(mouseLook) {
            // Ignore cursor recentering on the first captured frame.
            if(menuMouseLookHeld) {
                degrees=orbitCamera.ReadMouseDelta()*mouseSensitivity.Value;
                if(invertMouseY.Value)degrees.y=-degrees.y;
                scroll=Input.mouseScrollDelta.y;
            }
        }else if(pad!=null) {
            var look=pad.rightStick.ReadUnprocessedValue();
            degrees=new Vector2(ControlRules.Axis(look.x,stickDeadzone.Value),ControlRules.Axis(look.y,stickDeadzone.Value))*controllerSensitivity.Value*Mathf.Min(Time.unscaledDeltaTime,.1f);
            if(controllerInvertY.Value)degrees.y=-degrees.y;
        }
        menuMouseLookHeld=mouseLook;
        Cursor.lockState=mouseLook?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!mouseLook;
        if(driving)orbitCamera.MenuFrame(degrees,scroll);
        else if(menuPlayer is Player local)local.SetMouseLook(degrees);
    }
    void LateMenuCamera() {
        if(!menuOpen||driving||audioBlocked||!Application.isFocused||menuCamera==null||menuGroundCamera==null)return;
        menuGroundCamera(Time.unscaledDeltaTime,out var position,out var rotation);
        menuCamera.transform.SetPositionAndRotation(position,rotation);
    }
    internal bool CaptureMenuLook(PlayerController controller)=>ready&&menuOpen&&controller.GetComponent<Player>()==LocalPlayer()&&!Multiplayer();
}

// PlayerController's LateUpdate independently reads look input. Suppress it
// while our menu handles look, avoiding double rotation and mouse hover drift.
[HarmonyPatch(typeof(PlayerController),"LateUpdate")]
static class NativeMenuLookCapture {
    static bool Prefix(PlayerController __instance)=>Plugin.ControlsOwner==null||!Plugin.ControlsOwner.CaptureMenuLook(__instance);
}
}
