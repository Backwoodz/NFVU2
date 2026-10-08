using UnityEngine;

namespace Rachel350Z {
// Additional nitrous acceleration is deliberately outside the proven controller.
public sealed class VehicleNitro {
    readonly Rigidbody body;
    readonly WheelCollider[] wheels;
    public NitroState State { get; } = new NitroState();
    public VehicleNitro(Rigidbody body,WheelCollider[] wheels){this.body=body;this.wheels=wheels;}
    public void Step(bool driving,bool blocked,bool held,float throttle,bool braking,bool terrain) {
        int contacts=0;
        foreach(var wheel in wheels)if(wheel.GetGroundHit(out var hit))contacts++;
        float forward=Vector3.Dot(body.linearVelocity,body.transform.forward),speed=body.linearVelocity.magnitude;
        bool grounded=contacts>=2;
        State.Step(Time.fixedDeltaTime,driving,blocked,held,forward,speed,throttle,braking,grounded,terrain);
        float acceleration=NitroState.Acceleration(State.Boosting,forward,speed,grounded,terrain);
        if(acceleration>0)body.AddForce(body.transform.forward*acceleration,ForceMode.Acceleration);
    }
}
}
