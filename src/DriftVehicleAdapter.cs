using System;
using NFVU2.DriftPhysics;
using UnityEngine;

namespace Rachel350Z {
// NFVU2's own four-contact vehicle. Ground geometry and rigid-body integration
// are supplied by Unity; the portable library computes drivetrain/tyre forces.
internal sealed class DriftVehicleAdapter : IDisposable {
    readonly Rigidbody body;
    readonly WheelCollider[] wheels;
    readonly Action<string> log;
    readonly DriftPowertrain powertrain=new DriftPowertrain();
    readonly DriftTyre[] tyres={new DriftTyre(),new DriftTyre(),new DriftTyre(),new DriftTyre()};
    readonly Vector3[] anchors=new Vector3[4],contacts=new Vector3[4],normals=new Vector3[4],hubs=new Vector3[4],previousHubs=new Vector3[4],forces=new Vector3[4];
    readonly float[] loads=new float[4],spinAngles=new float[4],spinAdvances=new float[4],slip=new float[4],sideSlip=new float[4],skid=new float[4];
    readonly bool[] grounded=new bool[4],previousEnabled=new bool[4];
    bool disposed,gravity;
    float linearDamping,angularDamping,angularLimit,steerAngle;
    float diagnosticElapsed;
    float previousSteerAngle,visualStepDt;
    int diagnosticSamples;
    public bool Active=>!disposed;
    public float Rpm=>powertrain.Rpm;
    public int Gear=>powertrain.Gear;
    public bool BurnoutActive {get;private set;}

    DriftVehicleAdapter(Rigidbody chassis,WheelCollider[] colliders,Action<string> logger) {
        body=chassis;wheels=colliders;log=logger;
        powertrain.WheelRadius=(wheels[2].radius+wheels[3].radius)*.5f;
        powertrain.DrivenWheelInertia=(tyres[2].WheelInertia+tyres[3].WheelInertia)*.5f;
        gravity=body.useGravity;linearDamping=body.linearDamping;angularDamping=body.angularDamping;angularLimit=body.maxAngularVelocity;
        for(int i=0;i<4;i++) {
            anchors[i]=body.transform.InverseTransformPoint(wheels[i].transform.position);
            hubs[i]=previousHubs[i]=anchors[i];previousEnabled[i]=wheels[i].enabled;
            tyres[i].AngularSpeed=Vector3.Dot(body.GetPointVelocity(wheels[i].transform.position),body.rotation*Vector3.forward)/wheels[i].radius;
        }
        for(int i=0;i<4;i++){wheels[i].motorTorque=0;wheels[i].brakeTorque=0;wheels[i].enabled=false;}
        body.useGravity=false;body.linearDamping=0;body.angularDamping=.12f;body.maxAngularVelocity=20;
        body.WakeUp();
    }
    public static DriftVehicleAdapter Create(Rigidbody body,WheelCollider[] wheels,Action<string> log=null) {
        if(body==null||body.isKinematic||wheels==null||wheels.Length!=4)throw new ArgumentException("Drift physics requires a dynamic chassis and four wheels.");
        for(int i=0;i<4;i++)if(wheels[i]==null||!wheels[i].enabled)throw new ArgumentException("Drift physics requires available wheels.");
        return new DriftVehicleAdapter(body,wheels,log);
    }
    public bool Step(bool driving,float throttle,float steer,float brake,bool burnout=false,bool handbrake=false) {
        if(!Active)return false;
        try {
            float dt=Time.fixedDeltaTime;
            if(body==null||body.isKinematic||!Finite(dt)||dt<=0||dt>.1f||!Finite(throttle)||!Finite(steer)||!Finite(brake)||!Valid(body.linearVelocity)||!Valid(body.angularVelocity))
                throw new InvalidOperationException("Invalid drift chassis state or input.");
            throttle=driving?Mathf.Clamp(throttle,-1,1):0;
            steer=Mathf.Clamp(steer,-1,1);
            handbrake=driving&&handbrake;
            brake=!driving?1:handbrake?0:Mathf.Clamp01(brake);
            previousSteerAngle=steerAngle;
            for(int i=0;i<4;i++)previousHubs[i]=hubs[i];
            if(driving)UpdateSteering(steer,dt);
            Vector3 up=body.rotation*Vector3.up,forward=body.rotation*Vector3.forward;
            int contactCount=0;
            for(int i=0;i<4;i++) {
                grounded[i]=false;loads[i]=0;contacts[i]=normals[i]=Vector3.zero;
                Vector3 origin=body.position+body.rotation*anchors[i];
                float radius=wheels[i].radius,travel=wheels[i].suspensionDistance;
                float nearest=float.PositiveInfinity;RaycastHit selected=new RaycastHit();
                foreach(var hit in Physics.RaycastAll(origin,-up,radius+travel,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)) {
                    if(hit.collider==null||hit.collider.attachedRigidbody==body||hit.collider.transform.IsChildOf(body.transform)||hit.collider.GetComponentInParent<Character>()!=null)continue;
                    if(Vector3.Dot(hit.normal,up)<.2f||hit.distance>=nearest)continue;
                    nearest=hit.distance;selected=hit;
                }
                float extension=travel;
                if(nearest<float.PositiveInfinity) {
                    extension=Mathf.Clamp(nearest-radius,0,travel);
                    Vector3 normal=selected.normal.normalized;
                    float compression=travel-extension;
                    float rate=Vector3.Dot(body.GetPointVelocity(selected.point),normal);
                    var spring=wheels[i].suspensionSpring;
                    float load=Mathf.Clamp(spring.spring*compression-spring.damper*rate,0,body.mass*9.81f*1.25f);
                    grounded[i]=load>1;loads[i]=load;
                    if(grounded[i]){contacts[i]=selected.point;normals[i]=normal;contactCount++;}
                }
                hubs[i]=anchors[i]-Vector3.up*extension;
            }
            float signedSpeed=Vector3.Dot(body.linearVelocity,forward);
            BurnoutActive=driving&&burnout&&!handbrake&&brake<=0&&body.linearVelocity.magnitude<1;
            var drive=powertrain.Step(dt,throttle,signedSpeed,(tyres[2].AngularSpeed+tyres[3].AngularSpeed)*.5f,driving,handbrake,brake>0,BurnoutActive);
            float pedal=drive.DirectionBrake?1:brake;
            for(int i=0;i<4;i++) {
                Vector3 normal=grounded[i]?normals[i]:up;
                Vector3 direction=Quaternion.AngleAxis(i<2?steerAngle:0,up)*forward;
                direction=Vector3.ProjectOnPlane(direction,normal).normalized;
                Vector3 side=Vector3.Cross(normal,direction).normalized;
                Vector3 velocity=grounded[i]?body.GetPointVelocity(contacts[i]):body.linearVelocity;
                float brakeTorque=pedal*4000;
                if(handbrake&&i>=2)brakeTorque=4200;
                if(BurnoutActive)brakeTorque=i<2?6000:0;
                var tyre=tyres[i].Step(dt,Vector3.Dot(velocity,direction),Vector3.Dot(velocity,side),loads[i],wheels[i].radius,
                    i>=2?drive.RearWheelTorque:0,brakeTorque,i<2);
                slip[i]=tyre.SlipRatio;sideSlip[i]=tyre.SlipAngle;skid[i]=tyre.Skid;
                spinAdvances[i]=tyres[i].AngularSpeed*dt*Mathf.Rad2Deg;
                spinAngles[i]=(spinAngles[i]+spinAdvances[i])%360;
                forces[i]=normal*loads[i]+direction*tyre.LongitudinalForce+side*tyre.SideForce;
                if(!Valid(forces[i])||!Finite(tyres[i].AngularSpeed)||!Finite(slip[i])||!Finite(sideSlip[i])||!Finite(skid[i]))throw new InvalidOperationException("Invalid calculated tyre force.");
            }
            // Validate every result before applying any impulses, so fallback
            // cannot accidentally drive the same frame a second time.
            Vector3 drag=-body.linearVelocity*(.4f*body.linearVelocity.magnitude);
            body.AddForce((new Vector3(0,-9.81f*body.mass,0)+drag)*dt,ForceMode.Impulse);
            for(int i=0;i<4;i++)if(grounded[i])body.AddForceAtPosition(forces[i]*dt,contacts[i],ForceMode.Impulse);
            visualStepDt=dt;
            // Diagnostics must never convert an already applied physics step
            // into a fallback step if an external logger fails.
            try { Trace(driving,throttle,steer,handbrake,contactCount,dt); } catch { }
            return true;
        } catch(Exception error) {
            Dispose();try { log?.Invoke("NFVU2 drift physics stopped; ordinary wheel handling restored: "+error.Message); } catch { } return false;
        }
    }
    void UpdateSteering(float input,float dt) {
        var inverse=Quaternion.Inverse(body.rotation);
        Vector3 velocity=inverse*body.linearVelocity,spin=inverse*body.angularVelocity;
        float speed=(float)Math.Sqrt(velocity.x*velocity.x+velocity.z*velocity.z);
        float target=input*45,rate=180;
        if(speed>=3&&velocity.z>0) {
            float beta=Mathf.Clamp((float)Math.Atan2(velocity.x,velocity.z),-70*Mathf.Deg2Rad,70*Mathf.Deg2Rad);
            float driftDemand=input/(.4f+.6f*Mathf.Abs(input));
            float desiredYaw=Mathf.Clamp(input*speed/28+(beta+driftDemand*40*Mathf.Deg2Rad)*4,-2.1f,2.1f);
            float lever=(anchors[0].z+anchors[1].z)*.5f-body.centerOfMass.z;
            float frontTravel=(float)Math.Atan2(velocity.x+spin.y*lever,Math.Max(3,velocity.z));
            float correction=Mathf.Clamp(.55f*(desiredYaw-spin.y),-22*Mathf.Deg2Rad,22*Mathf.Deg2Rad);
            target=(frontTravel+correction)*Mathf.Rad2Deg;
            if(input*beta>0&&Mathf.Abs(beta)>8*Mathf.Deg2Rad&&Mathf.Abs(input)>.1f)rate=350;
        }
        steerAngle+=Mathf.Clamp(Mathf.Clamp(target,-60,60)-steerAngle,-rate*dt,rate*dt);
    }
    void Trace(bool driving,float throttle,float steer,bool handbrake,int count,float dt) {
        if(log==null||diagnosticSamples>=36)return;
        diagnosticElapsed+=dt;
        if(!driving||body.linearVelocity.magnitude<2||Mathf.Abs(steer)<.1f&&!handbrake||diagnosticElapsed<1)return;
        diagnosticElapsed=0;diagnosticSamples++;
        Vector3 local=Quaternion.Inverse(body.rotation)*body.linearVelocity;
        log(string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "NFVU2 drift: gas={0:F2}, input={1:F2}, rack={2:F1}deg, speed={3:F1}m/s, beta={4:F1}deg, gear={5}, rpm={6:F0}, handbrake={7}, contacts={8}/4",
            throttle,steer,steerAngle,body.linearVelocity.magnitude,Math.Atan2(local.x,local.z)*Mathf.Rad2Deg,Gear,Rpm,handbrake,count));
    }
    public bool TryGetWheelPose(int index,out Vector3 position,out Quaternion rotation) {
        position=Vector3.zero;rotation=Quaternion.identity;if(!Active||index<0||index>=4)return false;
        // Rigidbody interpolation owns the rendered chassis transform. Mixing
        // it with the newer raw physics pose made all four child wheels jitter.
        float fraction=VisualFraction;
        Vector3 hub=previousHubs[index]+(hubs[index]-previousHubs[index])*fraction;
        float rack=previousSteerAngle+(steerAngle-previousSteerAngle)*fraction;
        // Preserve the signed full advance, including multiple revolutions.
        // Quaternion shortest-path interpolation would reverse fast wheels.
        float spin=spinAngles[index]-spinAdvances[index]*(1-fraction);
        position=body.transform.TransformPoint(hub);
        rotation=body.transform.rotation*Quaternion.Euler(0,index<2?rack:0,0)*Quaternion.Euler(spin,0,0);
        return true;
    }
    float VisualFraction=>body.interpolation==RigidbodyInterpolation.Interpolate&&visualStepDt>0
        ?Mathf.Clamp01((float)((Time.timeAsDouble-Time.fixedTimeAsDouble)/visualStepDt)):1;
    public float VisualSteeringAngle(int index)=>Active&&index>=0&&index<2
        ?previousSteerAngle+(steerAngle-previousSteerAngle)*VisualFraction:0;
    public float SteeringAngle(int index)=>Active&&index>=0&&index<2?steerAngle:0;
    public float WheelRpm(int index)=>tyres[index].AngularSpeed*60/(2*Mathf.PI);
    public bool Grounded(int index)=>grounded[index];
    public Vector3 ContactPoint(int index)=>contacts[index];
    public Vector3 ContactNormal(int index)=>normals[index];
    public float SidewaysSlip(int index)=>sideSlip[index];
    public float ForwardSlip(int index)=>slip[index];
    public float SkidVolume(int index)=>skid[index];
    public void Dispose() {
        if(disposed)return;disposed=true;
        if(body!=null){body.useGravity=gravity;body.linearDamping=linearDamping;body.angularDamping=angularDamping;body.maxAngularVelocity=angularLimit;}
        for(int i=0;i<4;i++)if(wheels[i]!=null){wheels[i].motorTorque=0;wheels[i].brakeTorque=0;wheels[i].enabled=previousEnabled[i];}
    }
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    static bool Valid(Vector3 value)=>Finite(value.x)&&Finite(value.y)&&Finite(value.z);
}
}
