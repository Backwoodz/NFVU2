using System;

namespace Rachel350Z {
// Scalar control rules, independent of Unity's physics runtime.
public static class DrivingMath {
    // NFVU2 torque tuning, independent of the acoustic gearbox and NFS physics.
    // Reverse needs its own low-speed reserve for backing up a slope.
    public static float BaseTorque(bool terrain) => terrain?4000f:2750f;
    public static float LaunchTorqueFactor(float speed,float input) => input!=0&&!DirectionBrake(speed,input)
        ?1+(input<0?.7f:.55f)*Clamp(((input<0?5f:7f)-Math.Abs(speed))/3,0,1):1;
    public static float ThrottleResponse(float speed) => Math.Abs(speed)<1.5f?6f:2.4f;
    public static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
    public static float SteeringLimit(float speed, bool terrain) {
        var blend=Clamp(Math.Abs(speed)/25f,0,1);
        return (terrain?34f:32f)*(1-blend)+10f*blend;
    }
    public static float TorqueFactor(float speed, float input, bool terrain) {
        if(input==0 || DirectionBrake(speed,input))return 0;
        float limit=input<0?8f:(terrain?20f:34f);
        return Clamp((limit-Math.Abs(speed))/5f,0,1);
    }
    // Allow drive torque to catch a slow rollback; repeatedly switching into
    // the direction brake near rest otherwise prevents hill starts. Use the
    // requested direction, not analogue trigger pressure, for the threshold.
    public static bool DirectionBrake(float speed,float input) => input!=0 && speed*(input<0?-1:1)<-1.25f;
    // Engine drive is independent of contact reporting: airborne wheels can spin,
    // and an unavailable contact sample must not lock out a parked vehicle.
    public static float WheelWeight(int index,bool terrain) => index<2?(terrain?.2f:0):(terrain?.3f:.5f);
    public static float GripTorqueFactor(float slip) => float.IsNaN(slip)||float.IsInfinity(slip)?1f:1f-.7f*Clamp((Math.Abs(slip)-.6f)/1.4f,0,1);
    public static float WrapAngle(float angle) => ((angle+180f)%360f+360f)%360f-180f;
}
}
