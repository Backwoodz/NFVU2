using System;
using UnityEngine;
namespace Rachel350Z {
// Separate flight forces: never run the wheel controller while it owns the body.
public sealed class VehicleFlight : IDisposable {
    readonly Rigidbody body;
    readonly WheelCollider[] wheels;
    readonly Vector3[] wheelPositions=new Vector3[4];
    readonly Quaternion[] wheelRotations=new Quaternion[4];
    readonly bool[] wheelEnabled=new bool[4];
    readonly FlightState state=new FlightState();
    bool ownsBody,gravity;
    float linearDamping,angularDamping,targetHeight,yaw;
    Vector3 holdPosition;
    bool moving,verticalMoving;
    public NitroState Nitro {get;}
    public float WheelPitch {get;private set;}
    public float WheelRoll {get;private set;}
    public FlightState State=>state;
    public bool Active=>state.Active;
    public float Thrust {get;private set;}
    public VehicleFlight(Rigidbody body,WheelCollider[] wheels,NitroState nitro=null){this.body=body;this.wheels=wheels;Nitro=nitro??new NitroState();}
    public void Fly() {
        if(!ownsBody) {
            gravity=body.useGravity;linearDamping=body.linearDamping;angularDamping=body.angularDamping;
            for(int i=0;i<4;i++) {
                wheelEnabled[i]=wheels[i].enabled;
                var p=wheels[i].transform.position;var r=wheels[i].transform.rotation;
                if(wheelEnabled[i])wheels[i].GetWorldPose(out p,out r);
                wheelPositions[i]=body.transform.InverseTransformPoint(p);
                wheelRotations[i]=Quaternion.Inverse(body.rotation)*r;
                wheels[i].motorTorque=wheels[i].brakeTorque=0;wheels[i].enabled=false;
            }
            ownsBody=true;body.useGravity=false;body.linearDamping=0;body.angularDamping=0;
            holdPosition=body.position;targetHeight=body.position.y+1.4f;yaw=body.rotation.eulerAngles.y;moving=verticalMoving=false;
        } else if(state.Landing)targetHeight=body.position.y+1;
        state.Fly();body.WakeUp();
    }
    public void Land(){state.Land();Nitro.Stop();}
    public void Step(bool inputAllowed,float forward,float turn,float vertical,bool nitroHeld=false) {
        if(!Active)return;
        float dt=Time.fixedDeltaTime;
        bool landing=state.Landing;
        bool ground=GroundBelow(out float groundY);
        float landingHeight=groundY+.45f;
        bool nearGround=ground&&Mathf.Abs(body.position.y-landingHeight)<.18f&&Mathf.Abs(body.linearVelocity.y)<.65f
            &&Vector3.ProjectOnPlane(body.linearVelocity,Vector3.up).magnitude<1&&body.transform.up.y>.95f;
        state.Step(dt,nearGround);
        if(!Active){Restore();return;}
        bool pilot=inputAllowed&&state.CanPilot;
        forward=pilot?Mathf.Clamp(forward,-1,1):0;turn=pilot?Mathf.Clamp(turn,-1,1):0;
        vertical=pilot&&!landing?Mathf.Clamp(vertical,-1,1):0;
        Nitro.StepFlight(dt,pilot&&!landing,nitroHeld,forward,body.linearVelocity.magnitude);
        WheelPitch+=(forward*(Nitro.Boosting?26:18)-WheelPitch)*Math.Min(1,dt*7);
        WheelRoll+=(-turn*18-WheelRoll)*Math.Min(1,dt*7);
        yaw+=turn*65*dt;
        if(landing) {
            // Descend at a bounded speed; no ground within the probe range means
            // continue descending until a surface appears. Chassis collisions stay on.
            targetHeight=ground?landingHeight:body.position.y-6;
        } else {
            if(vertical!=0){verticalMoving=true;targetHeight=body.position.y;}
            else if(verticalMoving){targetHeight=body.position.y;if(Mathf.Abs(body.linearVelocity.y)<.15f)verticalMoving=false;}
            // Avoid integrating an unreachable target into terrain or a ceiling.
            targetHeight=Mathf.Clamp(targetHeight,body.position.y-2,body.position.y+2);
            if(ground)targetHeight=Mathf.Max(targetHeight,groundY+.85f);
        }
        bool move=Mathf.Abs(forward)>.01f;
        // Brake where the car is headed, then lock the settled position. Locking
        // the release point immediately would make it fly backwards after braking.
        if(move){holdPosition=body.position;moving=true;}
        else if(moving){holdPosition=body.position;if(Vector3.ProjectOnPlane(body.linearVelocity,Vector3.up).magnitude<.15f)moving=false;}
        var heading=Quaternion.Euler(0,yaw,0)*Vector3.forward;
        var desired=move?heading*(forward>0?forward*(Nitro.Boosting?44:18):forward*8):Vector3.ClampMagnitude(Vector3.ProjectOnPlane(holdPosition-body.position,Vector3.up)*1.8f,4);
        var horizontal=Vector3.ClampMagnitude((desired-Vector3.ProjectOnPlane(body.linearVelocity,Vector3.up))*2.5f,Nitro.Boosting?20:12);
        float climb=vertical!=0?vertical*(vertical>0?12:16):Mathf.Clamp((targetHeight-body.position.y)*2,landing?-10:-12,landing?2:12);
        // Brake before the hover/landing clearance; v^2=2*a*d limits descent.
        if(ground&&climb<0) {
            float clearance=Mathf.Max(0,body.position.y-(groundY+(landing?.45f:.85f)));
            // Leave braking margin for the velocity servo's response time, then
            // switch to a proportional approach for the last few metres.
            climb=Mathf.Max(climb,-Math.Min((float)Math.Sqrt(2*8*clearance),clearance*2));
            climb=Mathf.Max(climb,(groundY+(landing?.45f:.85f)-body.position.y)*2);
        }
        float lift=FlightState.Acceleration(climb,body.linearVelocity.y,5,24);
        body.AddForce(horizontal+Vector3.up*lift,ForceMode.Acceleration);
        var target=Quaternion.Euler(forward*4,yaw,-turn*4);
        var error=target*Quaternion.Inverse(body.rotation);error.ToAngleAxis(out float angle,out var axis);
        if(angle>180)angle-=360;
        if(axis.sqrMagnitude>.001f&&!float.IsNaN(axis.x))
            body.AddTorque(Vector3.ClampMagnitude(axis.normalized*(angle*Mathf.Deg2Rad*12)-body.angularVelocity*6,18),ForceMode.Acceleration);
        Thrust=Mathf.Clamp01(.5f+Mathf.Max(0,vertical)*.35f+Mathf.Abs(forward)*.12f+lift*.015f+(Nitro.Boosting?.35f:0));
    }
    bool GroundBelow(out float y) {
        y=float.NegativeInfinity;
        // Centre and four wheel locations catch ledges; exclude our hull and driver.
        for(int i=-1;i<4;i++) {
            var p=i<0?body.position:body.transform.TransformPoint(wheels[i].transform.localPosition);
            p.y=body.position.y+.35f;
            foreach(var hit in Physics.RaycastAll(p,Vector3.down,200,~0,QueryTriggerInteraction.Ignore)) {
                if(hit.collider.transform.IsChildOf(body.transform)||hit.collider.GetComponentInParent<Player>()!=null||hit.normal.y<.7f)continue;
                y=Mathf.Max(y,hit.point.y);
            }
        }
        return !float.IsNegativeInfinity(y);
    }
    public void WheelPose(int index,out Vector3 position,out Quaternion rotation) {
        position=body.transform.TransformPoint(wheelPositions[index]);rotation=body.rotation*wheelRotations[index];
    }
    void Restore() {
        if(!ownsBody)return;
        for(int i=0;i<4;i++)if(wheels[i]!=null){wheels[i].motorTorque=wheels[i].brakeTorque=0;wheels[i].enabled=wheelEnabled[i];}
        if(body!=null){body.useGravity=gravity;body.linearDamping=linearDamping;body.angularDamping=angularDamping;body.WakeUp();}
        ownsBody=false;Thrust=WheelPitch=WheelRoll=0;Nitro.Stop();
    }
    public void Dispose()=>Restore();
}
}
