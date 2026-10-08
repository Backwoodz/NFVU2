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
            Check(Math.Abs(wheels.Sum(w=>w.motorTorque)-(terrain?3840:2640))<.1f,"20 percent more launch torque after contact returns, terrain="+terrain);
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
            body.linearVelocity=new Vector3();
            for(int i=0;i<30;i++)controller.Step(true,0,0,0,true);
            Check(controller.BurnoutActive&&wheels.Take(2).All(w=>w.motorTorque==0&&w.brakeTorque==6000),"burnout locks front axle without drive torque, terrain="+terrain);
            Check(wheels.Skip(2).All(w=>w.motorTorque>1500&&w.brakeTorque==0&&w.forwardFriction.stiffness<.5f),"burnout drives and releases both rear tyres, terrain="+terrain);
            controller.Step(true,1,0,0,false);
            Check(!controller.BurnoutActive&&wheels.All(w=>w.brakeTorque==0)&&Math.Abs(wheels[2].forwardFriction.stiffness-(terrain?1.65f:1.3f))<.001f,"release restores normal grip and driving, terrain="+terrain);
            controller.Step(true,0,0,0,true);controller.Step(true,0,0,1,true);
            Check(!controller.BurnoutActive&&wheels.All(w=>w.motorTorque==0&&w.brakeTorque>0),"space brake cancels burnout, terrain="+terrain);
            body.linearVelocity=new Vector3(0,0,5);controller.Step(true,0,0,0,true);
            Check(!controller.BurnoutActive&&wheels.All(w=>w.motorTorque==0&&w.brakeTorque>0),"W plus S while moving brakes before burnout, terrain="+terrain);
            body.linearVelocity=new Vector3();controller.Step(true,0,0,0,true);
            body.linearVelocity=new Vector3(3,0,0);controller.Step(true,0,0,0,true);
            Check(!controller.BurnoutActive,"sideways slide cancels burnout, terrain="+terrain);
            body.linearVelocity=new Vector3();controller.Step(true,0,0,0,true);wheels[3].contact=false;controller.Step(true,0,0,0,true);
            Check(!controller.BurnoutActive&&wheels.All(w=>w.motorTorque==0),"loss of rear contact cancels burnout, terrain="+terrain);
            wheels[3].contact=true;controller.Step(true,0,0,0,true);controller.Step(false,0,0,0,true);
            Check(!controller.BurnoutActive&&wheels.All(w=>w.motorTorque==0&&w.brakeTorque>0),"exit cancels burnout and parks all wheels, terrain="+terrain);
        }
        Check(DrivingMath.LaunchTorqueFactor(0,1)==1.2f&&DrivingMath.LaunchTorqueFactor(4.5f,1)>1&&DrivingMath.LaunchTorqueFactor(6,1)==1,"launch assistance tapers out at six metres per second");
        Check(DrivingMath.LaunchTorqueFactor(-2,-1)==1&&DrivingMath.LaunchTorqueFactor(0,0)==1,"reverse and coasting receive no launch assistance");
        {
            var body=new Rigidbody();var wheels=Wheels(true);var controller=new VehicleHandling(body,wheels,true);
            controller.SetDriftMode();
            for(int i=0;i<50;i++)controller.Step(true,1,1,0);
            Check(controller.DriftMode&&wheels.Take(2).All(w=>w.motorTorque==0)&&wheels.Skip(2).All(w=>w.motorTorque>0),"drift setup uses rear drive even after terrain mode");
            Check(wheels[2].sidewaysFriction.stiffness<wheels[0].sidewaysFriction.stiffness&&wheels[2].sidewaysFriction.asymptoteValue<wheels[0].sidewaysFriction.asymptoteValue,"drift rear axle breaks away before front grip");
            Check(wheels[0].steerAngle>DrivingMath.SteeringLimit(0,false),"drift setup allows more steering lock");
            body.linearVelocity=new Vector3(0,0,8);foreach(var w in wheels)w.slip=2;
            controller.Step(true,1,0,0);
            Check(wheels[2].motorTorque>1000,"drift retains drive torque through wheelspin");
            controller.Step(true,1,0,0,false,true);
            Check(wheels.Take(2).All(w=>w.brakeTorque==0)&&wheels.Skip(2).All(w=>w.brakeTorque>0)&&wheels.All(w=>w.motorTorque==0),"drift handbrake locks rear axle and cuts drive only while held");
            controller.Step(true,1,0,0);
            Check(wheels.All(w=>w.brakeTorque==0)&&wheels[2].motorTorque>0,"releasing drift handbrake restores engine drive");
            controller.Step(true,-1,0,0,false,true);
            Check(wheels.All(w=>w.brakeTorque>0&&w.motorTorque==0),"reverse direction braking overrides rear handbrake");
            controller.Step(true,1,0,1,false,true);
            Check(wheels.All(w=>w.brakeTorque>0&&w.motorTorque==0),"menu brake stops all wheels in drift mode");
            controller.Step(false,1,0,0,false,true);
            Check(wheels.All(w=>w.brakeTorque>0&&w.motorTorque==0),"exit parks all four wheels in drift mode");
            body.linearVelocity=new Vector3();controller.Step(true,0,0,0,true);
            Check(controller.BurnoutActive,"drift retains stationary burnout");
            controller.Step(true,1,0,0);
            Check(Math.Abs(wheels[2].forwardFriction.stiffness-1.05f)<.001f,"burnout release restores drift longitudinal grip");
            foreach(bool terrain in new[]{false,true}) {
                controller.SetDriftMode();controller.SetTerrainMode(terrain);
                Check(!controller.DriftMode&&wheels.All(w=>Math.Abs(w.sidewaysFriction.stiffness-(terrain?1.45f:1.25f))<.001f&&Math.Abs(w.forwardFriction.stiffness-(terrain?1.65f:1.3f))<.001f)&&wheels.All(w=>w.sidewaysFriction.extremumSlip==.25f),"leaving drift fully restores original grip, terrain="+terrain);
            }
        }
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity}){
            var body=new Rigidbody{linearVelocity=new Vector3(0,0,3)};var wheels=Wheels(true);
            foreach(var wheel in wheels)wheel.slip=invalid;
            var controller=new VehicleHandling(body,wheels,true);controller.Step(true,1,0,0);
            Check(wheels.All(w=>!float.IsNaN(w.motorTorque)&&!float.IsInfinity(w.motorTorque)&&w.motorTorque>0),"nonfinite slip cannot poison motor torque: "+invalid);
        }
        foreach(bool terrain in new[]{false,true}) {
            var body=new Rigidbody{mass=2900};var wheels=Wheels(true);
            var controller=new VehicleHandling(body,wheels,terrain);HummerTuning.Configure(body,wheels);
            Check(wheels.All(w=>w.suspensionSpring.spring==(terrain?84000:96000))&&body.centerOfMass.y==.2f,"H2 supports its heavier body without altering 350Z defaults, terrain="+terrain);
            for(int i=0;i<100;i++){controller.Step(true,1,0,0);HummerTuning.AfterStep(wheels);}
            float torque=wheels.Sum(w=>w.motorTorque);
            Check(Math.Abs(torque-(terrain?3840:2640)*2.35f)<1,"H2 torque scaling does not accumulate between ticks, terrain="+terrain);
            controller.Step(false,0,0,0);HummerTuning.AfterStep(wheels);
            Check(wheels.All(w=>w.motorTorque==0&&w.brakeTorque==6400),"parked H2 holds all wheels, terrain="+terrain);
            controller.SetTerrainMode(terrain);HummerTuning.Configure(body,wheels);
            Check(wheels.All(w=>w.suspensionSpring.spring==(terrain?84000:96000)),"H2 mode reapplication keeps spring values stable, terrain="+terrain);
        }
        Console.WriteLine(checks+" controller checks; "+failures+" failed. These checks do not simulate Unity physics.");
        Environment.ExitCode=failures==0?0:1;
    }
}
