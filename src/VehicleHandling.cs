using UnityEngine;

namespace Rachel350Z {
public sealed class VehicleHandling {
    readonly Rigidbody body;
    readonly WheelCollider[] wheels;
    readonly bool[] grounded=new bool[4];
    readonly WheelHit[] contacts=new WheelHit[4];
    readonly System.Action<string> log;
    float appliedThrottle, appliedSteering;
    float diagnosticElapsed;
    int diagnosticSamples;
    bool terrain;
    public bool BurnoutActive { get; private set; }
    bool burnoutGrip;
    public VehicleHandling(Rigidbody rigidbody,WheelCollider[] wheelColliders,bool terrainMode,System.Action<string> logger=null) {
        body=rigidbody;wheels=wheelColliders;log=logger;
        body.centerOfMass=new Vector3(0,.05f,0);
        body.linearDamping=.08f;body.angularDamping=.8f;
        body.solverIterations=12;body.solverVelocityIterations=4;
        for(int i=0;i<4;i++) {
            wheels[i].mass=24;wheels[i].wheelDampingRate=1.2f;
            wheels[i].forceAppPointDistance=.2f;
        }
        wheels[0].ConfigureVehicleSubsteps(5,12,8);
        SetTerrainMode(terrainMode);
    }
    public void SetTerrainMode(bool enabled) {
        terrain=enabled;
        burnoutGrip=false;
        foreach(var wheel in wheels) {
            wheel.suspensionDistance=terrain?.4f:.24f;
            var spring=wheel.suspensionSpring;
            spring.spring=terrain?42000:48000;spring.damper=terrain?5600:6000;spring.targetPosition=.45f;
            wheel.suspensionSpring=spring;
            var forward=wheel.forwardFriction;forward.extremumSlip=.3f;forward.extremumValue=1;
            forward.asymptoteSlip=.8f;forward.asymptoteValue=.75f;forward.stiffness=terrain?1.65f:1.3f;wheel.forwardFriction=forward;
            var sideways=wheel.sidewaysFriction;sideways.extremumSlip=.25f;sideways.extremumValue=1;
            sideways.asymptoteSlip=.65f;sideways.asymptoteValue=.8f;sideways.stiffness=terrain?1.45f:1.25f;wheel.sidewaysFriction=sideways;
        }
        body.WakeUp();
    }
    public void Step(bool driving,float throttle,float steer,float brake,bool burnoutRequested=false) {
        float dt=Time.fixedDeltaTime;
        if(driving && (throttle!=0 || steer!=0 || burnoutRequested))body.WakeUp();
        float speed=Vector3.Dot(body.linearVelocity,body.transform.forward);
        for(int i=0;i<4;i++)grounded[i]=wheels[i].GetGroundHit(out contacts[i]);
        float totalSpeed=body.linearVelocity.magnitude;
        BurnoutActive=driving&&burnoutRequested&&brake<=0&&totalSpeed<(BurnoutActive?2f:.65f)
            &&(grounded[0]||grounded[1])&&grounded[2]&&grounded[3];
        if(BurnoutActive)throttle=1;
        bool directionBrake=!BurnoutActive&&DrivingMath.DirectionBrake(speed,throttle);
        bool stopping=!driving || brake>0 || directionBrake || (burnoutRequested&&!BurnoutActive);
        appliedThrottle=Mathf.MoveTowards(appliedThrottle,stopping?0:throttle,dt*2.4f);
        // Let go of the previous direction before starting reverse torque.
        if(stopping || appliedThrottle*throttle<0)appliedThrottle=0;
        float maxSteer=DrivingMath.SteeringLimit(speed,terrain);
        appliedSteering=Mathf.MoveTowards(appliedSteering,driving?steer*maxSteer:0,dt*85f);
        float torque=(terrain?3200:2200)*appliedThrottle*DrivingMath.TorqueFactor(speed,throttle,terrain)*DrivingMath.LaunchTorqueFactor(speed,throttle);
        if(burnoutGrip!=BurnoutActive) {
            for(int i=2;i<4;i++) {
                var friction=wheels[i].forwardFriction;
                friction.stiffness=BurnoutActive?.35f:(terrain?1.65f:1.3f);wheels[i].forwardFriction=friction;
            }
            burnoutGrip=BurnoutActive;
        }
        for(int i=0;i<4;i++) {
            var wheel=wheels[i];wheel.steerAngle=i<2?appliedSteering:0;
            var share=DrivingMath.WheelWeight(i,terrain);
            // Contact and slip measurements refine traction only after launch.
            // They must never be prerequisites for applying engine torque.
            float grip=grounded[i]&&Mathf.Abs(speed)>2?DrivingMath.GripTorqueFactor(contacts[i].forwardSlip):1;
            wheel.motorTorque=stopping?0:torque*share*grip;
            // S first brakes forward motion; reverse engages near a standstill.
            wheel.brakeTorque=stopping?3200:(throttle==0&&Mathf.Abs(speed)<.5f?900:0);
            if(BurnoutActive) {
                wheel.motorTorque=i<2?0:1800*appliedThrottle;
                wheel.brakeTorque=i<2?6000:0;
            }
        }
        AntiRoll(0,1);AntiRoll(2,3);
        LogDriveState(driving,throttle,speed,directionBrake);
    }
    void LogDriveState(bool driving,float throttle,float speed,bool directionBrake) {
        if(!driving){diagnosticElapsed=0;diagnosticSamples=0;return;}
        if(log==null || throttle==0 || diagnosticSamples>=8)return;
        diagnosticElapsed+=Time.fixedDeltaTime;
        if(diagnosticElapsed<1)return;
        diagnosticElapsed=0;diagnosticSamples++;
        int groundCount=0;for(int i=0;i<4;i++)if(grounded[i])groundCount++;
        log(string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "350Z drive: input={0:F1}, applied={1:F2}, speed={2:F2}m/s, contacts={3}/4, motor=[{4:F0},{5:F0},{6:F0},{7:F0}]Nm, brake={8:F0}Nm, reverseBrake={9}, sleeping={10}, kinematic={11}, terrain={12}, up={13:F2}, rearRPM={14:F0}",
            throttle,appliedThrottle,speed,groundCount,wheels[0].motorTorque,wheels[1].motorTorque,wheels[2].motorTorque,wheels[3].motorTorque,
            wheels[0].brakeTorque,directionBrake,body.IsSleeping(),body.isKinematic,terrain,body.transform.up.y,wheels[2].rpm));
    }
    void AntiRoll(int left,int right) {
        // Avoid the single-contact kick that can otherwise flip a car on rocks.
        if(!grounded[left]||!grounded[right])return;
        float Travel(int i) => Mathf.Clamp01((-wheels[i].transform.InverseTransformPoint(contacts[i].point).y-wheels[i].radius)/wheels[i].suspensionDistance);
        float force=(Travel(left)-Travel(right))*(terrain?1600:3200);
        body.AddForceAtPosition(-wheels[left].transform.up*force,wheels[left].transform.position);
        body.AddForceAtPosition(wheels[right].transform.up*force,wheels[right].transform.position);
    }
}
}
