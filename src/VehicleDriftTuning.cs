using UnityEngine;
namespace Rachel350Z {
// Optional rear-drive setup. Baseline terrain/street values live in the
// confirmed controller and are restored through SetTerrainMode.
public static class VehicleDriftTuning {
    public static float SteeringLimit(float speed)=>45-23*DrivingMath.Clamp(Mathf.Abs(speed)/32,0,1);
    public static void Apply(WheelCollider[] wheels) {
        for(int i=0;i<wheels.Length;i++) {
            var lateral=wheels[i].sidewaysFriction;
            lateral.extremumSlip=i<2?.25f:.38f;lateral.asymptoteSlip=i<2?.65f:1.0f;
            lateral.asymptoteValue=i<2?.8f:.65f;lateral.stiffness=i<2?1.4f:.72f;
            wheels[i].sidewaysFriction=lateral;
            var forward=wheels[i].forwardFriction;forward.stiffness=i<2?1.3f:1.05f;wheels[i].forwardFriction=forward;
        }
    }
}
}
