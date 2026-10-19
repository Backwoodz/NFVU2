using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Rachel350Z {
public sealed partial class VehicleOrbitCamera {
    readonly Func<Vector2> mouseDelta;
    Transform vehicle;
    Camera camera;
    float yaw,pitch=18,distance=6,smoothedDistance=6;
    Vector3 smoothedPivot;
    readonly ChaseCameraState chase=new ChaseCameraState();
    readonly TransformerStepShake footsteps=new TransformerStepShake();
    bool dynamicMode=true;
    bool active,hadInput;
    bool suv;
    public float RobotBlend {get;set;}
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
    public void Begin(Transform target,bool hummer=false) {
        ResetTransformationShot();
        footsteps.Reset();
        suv=hummer;RobotBlend=0;
        vehicle=target;camera=Camera.main;
        oldLock=Cursor.lockState;oldVisible=Cursor.visible;active=true;hadInput=false;
        yaw=0;pitch=18;distance=suv?7.5f:6;smoothedDistance=distance;
        chase.Reset(vehicle.eulerAngles.y);
        smoothedPivot=vehicle.position+Vector3.up*(suv?1.5f:1.05f);
    }
    public void InputFrame(bool enabled,float sensitivity,bool invert) {
        if(!active)return;
        bool capture=enabled && Application.isFocused;
        Cursor.lockState=capture?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!capture;
        if(!capture){hadInput=false;return;}
        // Ignore accumulated movement when returning from a menu or Alt-Tab.
        if(hadInput) {
            var delta=mouseDelta();
            if(deploymentShot.Active) {
                if(delta.sqrMagnitude*sensitivity*sensitivity>.0064f||Input.mouseScrollDelta.y!=0||Input.GetMouseButtonDown(2))InterruptTransformationShot();
                else return;
            }
            yaw=DrivingMath.WrapAngle(yaw+delta.x*sensitivity);
            chase.Orbit(delta.x*sensitivity);
            if(Mathf.Abs(delta.y)>.05f)chase.Hold();
            pitch=DrivingMath.Clamp(pitch-delta.y*sensitivity*(invert?-1:1),-10,70);
            distance=DrivingMath.Clamp(distance-Input.mouseScrollDelta.y*.65f,3,10);
        }
        hadInput=true;
        if(Input.GetMouseButtonDown(2)){yaw=0;pitch=18;distance=6;chase.Reset(vehicle.eulerAngles.y);}
    }
    public Vector2 ReadMouseDelta()=>mouseDelta();
    public void RobotFootsteps(float dt,bool enabled,uint left,uint right,float runBlend)=>footsteps.Step(dt,enabled,left,right,runBlend);
    // Menu look owns neither vehicle controls nor the menu cursor.
    public void MenuFrame(Vector2 degrees,float scroll) {
        hadInput=false;
        if(!active||!Application.isFocused)return;
        if(degrees.sqrMagnitude>.0001f||scroll!=0)InterruptTransformationShot();
        chase.Orbit(degrees.x);
        yaw=DrivingMath.WrapAngle(yaw+degrees.x);
        if(Mathf.Abs(degrees.y)>.01f)chase.Hold();
        pitch=DrivingMath.Clamp(pitch-degrees.y,-10,70);
        distance=DrivingMath.Clamp(distance-scroll*.65f,3,10);
    }
    public void ControllerFrame(Vector2 look,float degreesPerSecond,bool invert) {
        if(!active)return;
        Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
        hadInput=false;
        if(deploymentShot.Active) {
            if(look.sqrMagnitude>.01f)InterruptTransformationShot();else return;
        }
        float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
        chase.Orbit(look.x*degreesPerSecond*dt);
        yaw=DrivingMath.WrapAngle(yaw+look.x*degreesPerSecond*dt);
        if(Mathf.Abs(look.y)>.01f)chase.Hold();
        pitch=DrivingMath.Clamp(pitch-look.y*degreesPerSecond*dt*(invert?-1:1),-10,70);
        hadInput=false;
    }
    public void LateFrame(Vector3 velocity,bool dynamicFollow,float followRate,bool blocked,bool turretAim=false,bool driftFollow=false) {
        if(!active||vehicle==null)return;
        if(camera==null)camera=Camera.main;
        if(camera==null)return;
        // Drift has a separately selectable chase view even if ordinary road
        // driving uses the chassis-attached camera.
        dynamicFollow|=driftFollow;
        if(dynamicMode!=dynamicFollow) {
            dynamicMode=dynamicFollow;
            if(deploymentShot.Active||deploymentShot.Releasing)InterruptTransformationShot();
            else {yaw=0;chase.Reset(vehicle.eulerAngles.y);}
        }
        float speed=velocity.magnitude;
        if(dynamicMode&&!blocked)chase.Step(Time.deltaTime,vehicle.eulerAngles.y,
            Mathf.Atan2(velocity.x,velocity.z)*Mathf.Rad2Deg,speed,Vector3.Dot(velocity,vehicle.forward),followRate,driftFollow);
        float blend=blocked&&(deploymentShot.Active||deploymentShot.Releasing)?0:1-Mathf.Exp(-14*Time.deltaTime);
        var lookAhead=dynamicMode?Vector3.ClampMagnitude(new Vector3(velocity.x,0,velocity.z)*(driftFollow?.12f:.08f),driftFollow?1.8f:1.25f):Vector3.zero;
        float robotPivot=turretAim?TransformerAimSettings.PivotHeight(pitch):3.2f;
        var pivot=vehicle.position+Vector3.up*Mathf.Lerp(turretAim?TurretAimSettings.PivotHeight(pitch):suv?1.5f:1.05f,robotPivot,RobotBlend)+lookAhead;
        float viewYaw=dynamicMode?chase.Yaw:vehicle.eulerAngles.y+yaw,viewPitch=pitch;
        float aimDistance=Mathf.Lerp(TurretAimSettings.CameraMinDistance,TransformerAimSettings.CameraMinDistance,RobotBlend);
        float clear=(turretAim?Mathf.Max(distance,aimDistance):distance)+RobotBlend*3.5f;
        ApplyTransformationShot(ref pivot,ref viewYaw,ref viewPitch,ref clear,out float impactPulse);
        smoothedPivot=Vector3.Lerp(smoothedPivot,pivot,blend);
        var rotation=Quaternion.Euler(viewPitch,viewYaw,0);
        var direction=rotation*Vector3.back;
        foreach(var hit in Physics.SphereCastAll(smoothedPivot,.22f,direction,clear,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)) {
            if(hit.collider==null || hit.collider.transform.IsChildOf(vehicle))continue;
            clear=Mathf.Min(clear,Mathf.Max(.45f,hit.distance-.12f));
        }
        // Retract immediately at obstacles, ease back out when the view clears.
        smoothedDistance=clear<smoothedDistance?clear:Mathf.Lerp(smoothedDistance,clear,blend);
        if(blocked)footsteps.Stop();
        if(RobotBlend<=0)footsteps.Reset();
        var shake=Quaternion.Euler(footsteps.Pitch+impactPulse,0,footsteps.Roll+impactPulse*.35f);
        camera.transform.SetPositionAndRotation(smoothedPivot+direction*smoothedDistance,rotation*shake);
    }
    public void End() {
        ResetTransformationShot();
        footsteps.Reset();
        if(!active)return;
        Cursor.lockState=oldLock;Cursor.visible=oldVisible;active=false;hadInput=false;vehicle=null;camera=null;
    }
}
}
