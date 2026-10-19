using System;
using System.Linq;
using Rachel350Z;
using UnityEngine;

static class DriftAdapterChecks {
    static int checks;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    static void Near(float actual,float expected,float tolerance,string message)=>Check(Math.Abs(actual-expected)<=tolerance,message+": "+actual);
    sealed class Fixture {
        public readonly Rigidbody Body=new Rigidbody();
        public readonly WheelCollider[] Wheels=new WheelCollider[4];
        public Fixture(){
            Body.position=new Vector3(0,.1f,0);
            for(int i=0;i<4;i++){
                var w=Wheels[i]=new WheelCollider{motorTorque=30,brakeTorque=20};
                w.transform.parent=Body.transform;w.transform.position=Body.transform.TransformPoint(new Vector3(i%2==0?-.8f:.8f,.34f,i<2?1.3f:-1.35f));
            }
        }
        public DriftVehicleAdapter Create(Action<string> log=null)=>DriftVehicleAdapter.Create(Body,Wheels,log);
    }
    static void Reset(){Physics.Override=null;Physics.queries=0;Time.fixedDeltaTime=.02f;Time.timeAsDouble=Time.fixedTimeAsDouble=0;}
    static void Main(){
        Lifecycle();InvalidCreation();InvalidFrames();RayFiltering();Gravity();Logging();WheelPose();RenderedWheels();
        Console.WriteLine(checks+" independent drift adapter checks passed (Unity boundary doubles; no Unity runtime simulation)");
    }
    static void Lifecycle(){
        Reset();var f=new Fixture();f.Body.linearDamping=.11f;f.Body.angularDamping=.44f;f.Body.maxAngularVelocity=9;
        var adapter=f.Create();
        Check(adapter.Active&&!f.Body.useGravity&&!f.Body.isKinematic,"adapter acquires a dynamic chassis without making it kinematic");
        Check(f.Wheels.All(w=>!w.enabled&&w.motorTorque==0&&w.brakeTorque==0),"ordinary wheel force path is disabled before drift ownership");
        Check(f.Body.wakeCalls==1,"acquiring drift wakes the chassis");
        f.Body.isKinematic=true; // Streaming may freeze it independently of this controller.
        adapter.Dispose();adapter.Dispose();
        Check(!adapter.Active&&f.Body.isKinematic,"disposing twice preserves an external streaming freeze");
        Check(f.Body.useGravity&&f.Wheels.All(w=>w.enabled),"dispose restores gravity and available wheel colliders");
        Near(f.Body.linearDamping,.11f,0,"restore original linear damping");Near(f.Body.angularDamping,.44f,0,"restore original angular damping");Near(f.Body.maxAngularVelocity,9,0,"restore original angular limit");
        int applied=f.Body.forceCalls;Check(!adapter.Step(true,1,1,0)&&f.Body.forceCalls==applied,"disposed adapter never applies another force");
        f=new Fixture();f.Body.useGravity=false;adapter=f.Create();adapter.Dispose();Check(!f.Body.useGravity,"pre-existing disabled gravity is restored faithfully");
    }
    static void InvalidCreation(){
        Reset();foreach(int variant in new[]{0,1,2,3}){
            var f=new Fixture();if(variant==0)f.Body.isKinematic=true;if(variant==1)f.Wheels[2].enabled=false;if(variant==2)f.Wheels[3]=null;
            bool rejected=false;try{DriftVehicleAdapter.Create(f.Body,variant==3?new WheelCollider[3]:f.Wheels);}catch(ArgumentException){rejected=true;}
            Check(rejected&&f.Body.useGravity&&f.Body.forceCalls==0,"invalid creation rejects before taking chassis ownership, variant "+variant);
            Check(f.Wheels.Where(w=>w!=null).All(w=>w.motorTorque==30&&w.brakeTorque==20),"invalid creation does not overwrite wheel forces, variant "+variant);
        }
    }
    static void InvalidFrames(){
        Reset();for(int variant=0;variant<9;variant++){
            var f=new Fixture();var adapter=f.Create();float gas=1,steer=0,brake=0;
            if(variant==0)gas=float.NaN;if(variant==1)steer=float.PositiveInfinity;if(variant==2)brake=float.NaN;
            if(variant==3)Time.fixedDeltaTime=0;if(variant==4)Time.fixedDeltaTime=.11f;
            if(variant==5)f.Body.linearVelocity=new Vector3(float.NaN,0,0);if(variant==6)f.Body.angularVelocity=new Vector3(0,float.NegativeInfinity,0);
            if(variant==7)f.Body.isKinematic=true;
            if(variant==8)f.Wheels[3].radius=.01f; // Failure after the first three tyre calculations.
            Check(!adapter.Step(true,gas,steer,brake),"invalid frame returns fallback, variant "+variant);
            Check(!adapter.Active&&f.Wheels.All(w=>w.enabled)&&f.Body.useGravity,"invalid frame releases drift ownership, variant "+variant);
            Check(f.Body.forceCalls==0,"invalid frame applies no partial drift force before ordinary fallback, variant "+variant);
            if(variant==7)Check(f.Body.isKinematic,"fallback preserves the external kinematic state");
            Reset();
        }
    }
    static RaycastHit Hit(Collider collider,Vector3 origin,Vector3 direction,float distance,Vector3 normal)=>new RaycastHit{collider=collider,distance=distance,point=origin+direction*distance,normal=normal};
    static void RayFiltering(){
        Reset();var f=new Fixture();var own=new Collider{attachedRigidbody=f.Body};var child=new Collider();child.transform.parent=f.Body.transform;
        var character=new Collider{character=true};var adapter=f.Create();
        Physics.Override=(o,d,dist)=>new[]{
            Hit(Physics.Floor,o,d,.5f,Vector3.up),Hit(own,o,d,.01f,Vector3.up),Hit(child,o,d,.015f,Vector3.up),
            Hit(character,o,d,.02f,Vector3.up),Hit(Physics.Floor,o,d,.025f,new Vector3(1,0,0)),Hit(Physics.Floor,o,d,.4f,Vector3.up)
        };
        Check(adapter.Step(true,0,0,0),"valid world contact frame succeeds");
        Check(Physics.queries==4&&f.Wheels.All(w=>w.groundSamples==0),"four own rays replace disabled WheelCollider contact sampling");
        Check(f.Body.pointCalls==4&&Enumerable.Range(0,4).All(adapter.Grounded),"four selected solid surfaces receive contact impulses");
        for(int i=0;i<4;i++){
            Near(adapter.ContactPoint(i).y,.04f,.0001f,"nearest eligible road contact wins over self, children, characters and walls");
            Near(adapter.ContactNormal(i).y,1,.0001f,"contact normal is an upward unit vector");
        }
        int points=f.Body.pointCalls;Physics.Override=(o,d,dist)=>new RaycastHit[0];
        Check(adapter.Step(true,0,0,0)&&f.Body.pointCalls==points,"missing road applies no stale contact impulses");
        Check(Enumerable.Range(0,4).All(i=>!adapter.Grounded(i)&&adapter.ContactPoint(i).sqrMagnitude==0&&adapter.ContactNormal(i).sqrMagnitude==0),"missing road clears all preceding contact telemetry");
        adapter.Dispose();
    }
    static void Gravity(){
        foreach(float dt in new[]{.005f,.02f,.05f}){
            Reset();Time.fixedDeltaTime=dt;var f=new Fixture();
            using(var adapter=f.Create()){
                Check(adapter.Step(true,0,0,0),"airborne own simulation succeeds");
                Check(!f.Body.useGravity&&f.Body.directCalls==1&&f.Body.pointCalls==0,"airborne frame owns gravity exactly once with no fictitious tyre support");
                Near(f.Body.impulse.y,-f.Body.mass*9.81f*dt,.001f,"own Earth-gravity impulse is time-scaled once, independent of Valheim world gravity");
                Near(f.Body.impulse.x,0,.0001f,"zero-input airborne chassis has no sideways thrust");Near(f.Body.impulse.z,0,.0001f,"zero-input airborne chassis has no forward thrust");
            }
        }
        Reset();
    }
    static void Logging(){
        Reset();var f=new Fixture();f.Body.linearVelocity=new Vector3(0,0,8);int logged=0;
        using(var adapter=f.Create(message=>{logged++;throw new Exception("external logger failed");})){
            bool advanced=true;for(int i=0;i<80;i++)advanced&=adapter.Step(true,.5f,.3f,0);
            Check(advanced,"external logging failure cannot abort any of eighty valid force frames");
            Check(logged>0&&adapter.Active&&f.Wheels.All(w=>!w.enabled),"throwing diagnostics retain drift ownership");
            Check(f.Body.forceCalls==80,"logging exception never adds a fallback or duplicate physics impulse");
        }
        f=new Fixture();var failed=f.Create(message=>{throw new Exception("error logger failed");});
        Check(!failed.Step(true,float.NaN,0,0)&&!failed.Active&&f.Body.forceCalls==0,"a throwing error logger cannot interrupt ownership release");
    }
    static void WheelPose(){
        Reset();var f=new Fixture();using(var adapter=f.Create()){
            Check(adapter.Step(true,0,.4f,0),"wheel pose fixture advances");
            Check(adapter.TryGetWheelPose(0,out var before,out var rotation),"valid wheel exposes a pose");
            f.Body.position+=new Vector3(10,2,30);
            Check(adapter.TryGetWheelPose(0,out var after,out rotation),"cached suspension follows a moved chassis");
            Near((after-before).x,10,.0001f,"wheel pose follows translation X");Near((after-before).y,2,.0001f,"wheel pose follows translation Y");
            Check(!adapter.TryGetWheelPose(-1,out after,out rotation)&&!adapter.TryGetWheelPose(4,out after,out rotation),"invalid wheel pose index is rejected");
            adapter.Dispose();Check(!adapter.TryGetWheelPose(0,out after,out rotation),"disposed adapter cannot provide a live wheel pose");
        }
    }
    static void RenderedWheels(){
        Reset();var f=new Fixture();f.Body.interpolation=RigidbodyInterpolation.Interpolate;
        using(var adapter=f.Create()){
            Check(adapter.Step(true,0,1,0),"render fixture advances without changing its force contract");
            f.Body.transform.position=new Vector3(10,2,30);f.Body.transform.rotation=Quaternion.Euler(0,35,0);
            f.Body.SetPhysicsPose(new Vector3(11,2,31),Quaternion.Euler(0,40,0));
            for(int sample=0;sample<=4;sample++){
                float fraction=sample*.25f;Time.timeAsDouble=fraction*Time.fixedDeltaTime;
                for(int i=0;i<4;i++){
                    Check(adapter.TryGetWheelPose(i,out var point,out var rotation),"all four render poses remain available between physics ticks");
                    var local=f.Body.transform.InverseTransformPoint(point);
                    Near(local.x,i%2==0?-.8f:.8f,.0001f,"wheel stays attached to rendered chassis rather than newer raw physics position");
                    Near(local.z,i<2?1.3f:-1.35f,.0001f,"rendered hub follows chassis yaw without physics/render oscillation");
                    Near(local.y,.34f-.24f*fraction,.0001f,"suspension blends continuously between physics samples");
                }
                Near(adapter.VisualSteeringAngle(0),adapter.SteeringAngle(0)*fraction,.0001f,"caliper/camber steering follows the same interpolation as the wheel");
                Near(adapter.VisualSteeringAngle(2),0,0,"rear wheel has no visual steering");
            }
            f.Body.interpolation=RigidbodyInterpolation.None;Time.timeAsDouble=0;
            adapter.TryGetWheelPose(0,out var end,out var endRotation);
            Near(f.Body.transform.InverseTransformPoint(end).y,.1f,.0001f,"non-interpolated chassis uses latest suspension without an extra render delay");
        }
        foreach(float speed in new[]{250f,-250f}){
            Reset();f=new Fixture();f.Body.interpolation=RigidbodyInterpolation.Interpolate;f.Body.linearVelocity=new Vector3(0,0,speed);
            using(var adapter=f.Create()){
                Check(adapter.Step(true,0,0,0),"fast wheel fixture advances");
                for(int wheel=0;wheel<4;wheel++){
                    Vector3 previous=Vector3.zero;float total=0;
                    for(int sample=0;sample<=64;sample++){
                        Time.timeAsDouble=Time.fixedDeltaTime*sample/64.0;
                        adapter.TryGetWheelPose(wheel,out var position,out var rotation);
                        Vector3 axis=rotation*Vector3.up;
                        if(sample>0)total+=(float)Math.Atan2(Vector3.Dot(new Vector3(1,0,0),Vector3.Cross(previous,axis)),Vector3.Dot(previous,axis))*Mathf.Rad2Deg;
                        previous=axis;
                    }
                    Near(total,adapter.WheelRpm(wheel)*6*Time.fixedDeltaTime,.02f,"signed wheelspin preserves every revolution and does not take a reverse shortest path");
                }
            }
        }
        Reset();
    }
}
