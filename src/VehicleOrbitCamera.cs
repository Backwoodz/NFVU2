using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Rachel350Z {
public sealed class VehicleOrbitCamera {
    readonly Func<Vector2> mouseDelta;
    Transform vehicle;
    Camera camera;
    float yaw,pitch=18,distance=6,smoothedDistance=6;
    Vector3 smoothedPivot;
    readonly ChaseCameraState chase=new ChaseCameraState();
    bool dynamicMode=true;
    bool active,hadInput;
    CursorLockMode oldLock;
    bool oldVisible;
    public VehicleOrbitCamera(Action<string> log) {
        var zinput=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ZInput")).FirstOrDefault(t=>t!=null);
        var method=zinput?.GetMethod("GetMouseDelta",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static,null,Type.EmptyTypes,null);
        if(method!=null && method.ReturnType==typeof(Vector2)) {
            mouseDelta=(Func<Vector2>)Delegate.CreateDelegate(typeof(Func<Vector2>),method);
            log("350Z orbit camera uses Valheim ZInput.GetMouseDelta.");
        } else {
            mouseDelta=()=>new Vector2(Input.GetAxisRaw("Mouse X")*10,Input.GetAxisRaw("Mouse Y")*10);
            log("350Z orbit camera uses legacy mouse input fallback.");
        }
    }
    public void Begin(Transform target) {
        vehicle=target;camera=Camera.main;
        oldLock=Cursor.lockState;oldVisible=Cursor.visible;active=true;hadInput=false;
        yaw=0;pitch=18;distance=6;smoothedDistance=distance;
        chase.Reset(vehicle.eulerAngles.y);
        smoothedPivot=vehicle.position+Vector3.up*1.05f;
    }
    public void InputFrame(bool enabled,float sensitivity,bool invert) {
        if(!active)return;
        bool capture=enabled && Application.isFocused;
        Cursor.lockState=capture?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!capture;
        if(!capture){hadInput=false;return;}
        // Ignore accumulated movement when returning from a menu or Alt-Tab.
        if(hadInput) {
            var delta=mouseDelta();
            yaw=DrivingMath.WrapAngle(yaw+delta.x*sensitivity);
            chase.Orbit(delta.x*sensitivity);
            if(Mathf.Abs(delta.y)>.05f)chase.Hold();
            pitch=DrivingMath.Clamp(pitch-delta.y*sensitivity*(invert?-1:1),-10,70);
            distance=DrivingMath.Clamp(distance-Input.mouseScrollDelta.y*.65f,3,10);
        }
        hadInput=true;
        if(Input.GetMouseButtonDown(2)){yaw=0;pitch=18;distance=6;chase.Reset(vehicle.eulerAngles.y);}
    }
    public void LateFrame(Vector3 velocity,bool dynamicFollow,float followRate,bool blocked) {
        if(!active||vehicle==null)return;
        if(camera==null)camera=Camera.main;
        if(camera==null)return;
        if(dynamicMode!=dynamicFollow){dynamicMode=dynamicFollow;yaw=0;chase.Reset(vehicle.eulerAngles.y);}
        float speed=velocity.magnitude;
        if(dynamicMode&&!blocked)chase.Step(Time.deltaTime,vehicle.eulerAngles.y,
            Mathf.Atan2(velocity.x,velocity.z)*Mathf.Rad2Deg,speed,Vector3.Dot(velocity,vehicle.forward),followRate);
        float blend=1-Mathf.Exp(-14*Time.deltaTime);
        var lookAhead=dynamicMode?Vector3.ClampMagnitude(new Vector3(velocity.x,0,velocity.z)*.08f,1.25f):Vector3.zero;
        smoothedPivot=Vector3.Lerp(smoothedPivot,vehicle.position+Vector3.up*1.05f+lookAhead,blend);
        var rotation=Quaternion.Euler(pitch,dynamicMode?chase.Yaw:vehicle.eulerAngles.y+yaw,0);
        var direction=rotation*Vector3.back;
        float clear=distance;
        foreach(var hit in Physics.SphereCastAll(smoothedPivot,.22f,direction,distance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)) {
            if(hit.collider==null || hit.collider.transform.IsChildOf(vehicle))continue;
            clear=Mathf.Min(clear,Mathf.Max(.45f,hit.distance-.12f));
        }
        // Retract immediately at obstacles, ease back out when the view clears.
        smoothedDistance=clear<smoothedDistance?clear:Mathf.Lerp(smoothedDistance,clear,blend);
        camera.transform.SetPositionAndRotation(smoothedPivot+direction*smoothedDistance,rotation);
    }
    public void End() {
        if(!active)return;
        Cursor.lockState=oldLock;Cursor.visible=oldVisible;active=false;hadInput=false;vehicle=null;camera=null;
    }
}
}
