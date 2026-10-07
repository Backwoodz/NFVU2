using System;
using System.Linq;
using UnityEngine;
using Rachel350Z;
class HandlingTests {
    static int checks,failures;
    static void Check(bool ok,string name){checks++;if(!ok)failures++;Console.WriteLine((ok?"PASS ":"FAIL ")+name);}
    static WheelCollider[] Wheels(bool contact)=>Enumerable.Range(0,4).Select(_=>new WheelCollider{contact=contact}).ToArray();
    static void Main(){
        foreach(bool terrain in new[]{false,true}) {
            var body=new Rigidbody();var wheels=Wheels(false);
            var controller=new VehicleHandling(body,wheels,terrain);
            controller.Step(false,0,0,0);body.sleeping=true;
            controller.Step(true,1,0,0);
            Check(wheels[2].motorTorque>0 && wheels[3].motorTorque>0,"can start with unavailable wheel contacts, terrain="+terrain);
            Check(!body.sleeping && body.wakeCalls>0,"gas wakes parked body, terrain="+terrain);
            Check(wheels.All(w=>w.brakeTorque==0),"gas fully releases parking brake, terrain="+terrain);
            Check(terrain?wheels[0].motorTorque>0:wheels[0].motorTorque==0,"front-wheel mode, terrain="+terrain);
            for(int i=0;i<50;i++){foreach(var w in wheels)w.contact=true;controller.Step(true,1,0,0);}
            Check(Math.Abs(wheels.Sum(w=>w.motorTorque)-(terrain?3200:2200))<.1f,"full drive torque after contact returns, terrain="+terrain);
            controller.Step(true,1,0,1);
            Check(wheels.All(w=>w.motorTorque==0&&w.brakeTorque>0),"brake overrides throttle, terrain="+terrain);
            controller.Step(true,1,0,0);
            Check(wheels[2].motorTorque>0&&wheels[2].brakeTorque==0,"restarts after braking, terrain="+terrain);
            body.linearVelocity=new Vector3(0,0,4);controller.Step(true,-1,0,0);
            Check(wheels.All(w=>w.motorTorque==0&&w.brakeTorque>0),"reverse request brakes forward motion, terrain="+terrain);
            body.linearVelocity=new Vector3();controller.Step(true,-1,0,0);
            Check(wheels[2].motorTorque<0&&wheels[2].brakeTorque==0,"reverse starts at rest, terrain="+terrain);
            controller.Step(false,1,1,0);
            Check(wheels.All(w=>w.motorTorque==0&&w.brakeTorque>0),"exit parks vehicle, terrain="+terrain);
        }
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity}){
            var body=new Rigidbody{linearVelocity=new Vector3(0,0,3)};var wheels=Wheels(true);
            foreach(var wheel in wheels)wheel.slip=invalid;
            var controller=new VehicleHandling(body,wheels,true);controller.Step(true,1,0,0);
            Check(wheels.All(w=>!float.IsNaN(w.motorTorque)&&!float.IsInfinity(w.motorTorque)&&w.motorTorque>0),"nonfinite slip cannot poison motor torque: "+invalid);
        }
        Console.WriteLine(checks+" controller checks; "+failures+" failed. These checks do not simulate Unity physics.");
        Environment.ExitCode=failures==0?0:1;
    }
}
