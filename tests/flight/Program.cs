using System;
using System.Linq;
using Rachel350Z;
using UnityEngine;
class FlightTests {
    static int checks;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Console.WriteLine("PASS "+message);}
    static bool Near(float a,float b,float tolerance=.02f)=>Math.Abs(a-b)<tolerance;
    static void Main() {
        var s=new FlightState();Check(!s.Active,"starts grounded");
        s.Fly();s.Step(.1f,false);Check(s.Active&&s.Fold>0&&s.Fold<1&&!s.CanPilot,"folding animates before piloting");
        float before=s.Fold;s.Step(float.NaN,false);s.Step(float.PositiveInfinity,false);s.Step(-1,false);Check(s.Fold==before,"bad frame times do not poison folding");
        for(int i=0;i<70;i++)s.Step(.02f,false);
        Check(s.Phase==FlightPhase.Flying&&s.Fold==1&&s.CanPilot,"fold completes in flight");
        s.Land();for(int i=0;i<200;i++)s.Step(.02f,false);
        Check(s.Landing&&s.Active&&s.Fold==1,"ground request retains folded wheels and lift until near ground");
        s.Step(.02f,true);s.Step(.1f,true);Check(s.Phase==FlightPhase.Unfolding&&s.Fold<1,"unfolding only at a suitable landing surface");
        s.Step(.02f,false);Check(s.Phase==FlightPhase.Landing&&s.Active,"lost landing surface resumes powered descent");
        s.Fly();for(int i=0;i<70;i++)s.Step(.02f,false);Check(!s.Landing&&s.CanPilot,"flight selection cancels landing");
        s.Land();for(int i=0;i<100;i++)s.Step(.02f,true);Check(!s.Active&&s.Fold==0,"ground ownership resumes after full unfold");
        Check(FlightState.Axis(true,false,true)==1&&FlightState.Axis(false,true,true)==-1,"Shift climbs and Space descends");
        Check(FlightState.Axis(true,true,true)==0&&FlightState.Axis(true,false,false)==0,"opposing and blocked vertical keys cancel");
        Check(FlightState.Acceleration(100,0,4,12)==12&&FlightState.Acceleration(-100,0,4,12)==-12,"flight acceleration bounded in both directions");
        Check(FlightState.Acceleration(float.NaN,0,4,12)==0,"invalid acceleration rejected");
        for(int i=0;i<4;i++) {
            var fold=Quaternion.Euler(0,0,FlightState.WheelTilt(i,-8,1));
            var mirror=Quaternion.Euler(0,i%2==0?180:0,0);
            var face=fold*mirror*new Vector3(1,0,0);
            Check(face.y>.999f,"wheel "+i+" finished outer face points up after folding");
        }
        var body=new Rigidbody{position=new Vector3(0,.45f,0)};
        var wheels=Enumerable.Range(0,4).Select(i=>new WheelCollider{transform=new Transform{localPosition=new Vector3(i%2==0?-.8f:.8f,.34f,i<2?1.3f:-1.35f)}}).ToArray();
        var flight=new VehicleFlight(body,wheels);flight.Fly();
        Check(!body.useGravity&&wheels.All(w=>!w.enabled&&w.motorTorque==0&&w.brakeTorque==0),"takeoff releases all wheel forces and owns gravity");
        flight.WheelPose(0,out var p,out var r);Check(!float.IsNaN(p.x),"folded wheels use captured poses without querying disabled colliders");
        for(int i=0;i<500;i++){flight.Step(false,1,1,-1);body.Integrate();}
        Check(Near(body.position.y,1.85f)&&Near(body.position.z,0),"takeoff settles into hover even with stale blocked inputs");
        float hover=body.position.y;
        for(int i=0;i<100;i++){flight.Step(true,0,0,1);body.Integrate();}
        Check(body.position.y>hover+16&&body.linearVelocity.y>11,"Shift climbs at twelve metres per second");
        float high=body.position.y;
        for(int i=0;i<100;i++){flight.Step(true,0,0,-1);body.Integrate();}
        Check(body.position.y<high-12,"Space descends much faster than the previous controller");
        for(int i=0;i<200;i++){flight.Step(true,0,0,-1);body.Integrate();}
        Check(body.position.y>.65f,"fast descent brakes before the ground");
        for(int i=0;i<150;i++){flight.Step(true,1,0,0);body.Integrate();}
        Check(body.position.z>10&&body.linearVelocity.z<=18.1f,"W accelerates forward with bounded cruise speed");
        var jet=Quaternion.Euler(flight.WheelPitch,0,flight.WheelRoll)*Vector3.down;
        Check(jet.z<-.2f&&jet.y<-.8f,"forward flight vectors exhaust backwards");
        var mouseGate=new NitroInputGate();mouseGate.ReadSources(false,false,false,true);
        for(int i=0;i<100;i++){flight.Step(true,1,0,0,mouseGate.ReadSources(true,false,false,true));body.Integrate();}
        Check(flight.Nitro.Boosting&&flight.Nitro.Charge<.8f&&body.linearVelocity.z>35,"air nitro consumes fuel and accelerates beyond ordinary cruise");
        for(int i=0;i<50;i++){flight.Step(true,-1,-1,0);body.Integrate();}
        jet=Quaternion.Euler(flight.WheelPitch,0,flight.WheelRoll)*Vector3.down;
        Check(jet.z>.2f&&jet.x>.2f,"reverse and left input vector exhaust forward and right");
        // This fixture records torque without integrating rotation; return its
        // requested heading to zero before checking steering torque signs.
        for(int i=0;i<50;i++){flight.Step(true,0,1,0);body.Integrate();}
        for(int i=0;i<400;i++){flight.Step(false,1,1,1);body.Integrate();}
        Check(body.linearVelocity.magnitude<.02f,"menu or focus loss brakes to a position hold");
        flight.Step(true,0,1,0);Check(body.torque.y>0,"D turns right");body.force=body.torque=Vector3.zero;
        for(int i=0;i<20;i++)flight.Step(true,0,-1,0);
        Check(body.torque.y<0,"A turns left");body.force=body.torque=Vector3.zero;
        body.rotation=Quaternion.identity;
        flight.Land();
        for(int i=0;i<1000&&flight.Active;i++){flight.Step(false,0,0,0);if(flight.Active)body.Integrate();}
        Check(!flight.Active&&body.useGravity&&wheels.All(w=>w.enabled),"automatic landing restores gravity and all wheel colliders");
        Check(Near(body.linearDamping,.08f)&&Near(body.angularDamping,.8f),"landing restores original damping");
        flight.Fly();flight.Dispose();flight.Dispose();Check(body.useGravity&&wheels.All(w=>w.enabled),"cleanup while flying restores physics idempotently");
        var highBody=new Rigidbody{position=new Vector3(0,230,0)};
        var highFlight=new VehicleFlight(highBody,Enumerable.Range(0,4).Select(i=>new WheelCollider()).ToArray());
        highFlight.Fly();
        for(int i=0;i<100;i++){highFlight.Step(false,0,0,0);highBody.Integrate();}
        for(int i=0;i<100;i++){highFlight.Step(true,0,0,-1);highBody.Integrate();}
        Check(highBody.linearVelocity.y<-15.5f,"manual descent reaches sixteen metres per second away from terrain");
        highFlight.Land();
        for(int i=0;i<6000&&highFlight.Active;i++){highFlight.Step(false,0,0,0);if(highFlight.Active)highBody.Integrate();}
        Check(!highFlight.Active&&Near(highBody.position.y,.45f,.2f),"landing beyond probe range descends until terrain is found");
        // Two cars own independent state and original physics settings.
        var secondBody=new Rigidbody{useGravity=false,linearDamping=.4f};
        var secondWheels=Enumerable.Range(0,4).Select(i=>new WheelCollider()).ToArray();
        secondWheels[3].enabled=false;
        var second=new VehicleFlight(secondBody,secondWheels);second.Fly();second.Dispose();
        Check(!secondBody.useGravity&&Near(secondBody.linearDamping,.4f),"separate car restores its own gravity and damping");
        Check(!secondWheels[3].enabled&&secondWheels.Take(3).All(w=>w.enabled),"cleanup preserves originally disabled wheels");
        Console.WriteLine(checks+" flight checks passed; no Unity physics/rendering executed.");
    }
}
