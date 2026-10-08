using UnityEngine;
namespace Rachel350Z {
// H2-only adaptation; the proven 350Z controller retains its existing numbers.
public static class HummerTuning {
    public static void Configure(Rigidbody body,WheelCollider[] wheels) {
        body.centerOfMass=new Vector3(0,.2f,0);
        foreach(var wheel in wheels) {
            var spring=wheel.suspensionSpring;spring.spring*=2;spring.damper*=1.6f;wheel.suspensionSpring=spring;
            wheel.mass=40;wheel.forceAppPointDistance=.3f;
        }
    }
    public static void AfterStep(WheelCollider[] wheels) {
        foreach(var wheel in wheels){wheel.motorTorque*=2.35f;wheel.brakeTorque*=2;}
    }
}
}
