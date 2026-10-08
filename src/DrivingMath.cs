using System;

namespace Rachel350Z {
// Scalar control rules, independent of Unity's physics runtime.
public static class DrivingMath {
    public static float LaunchTorqueFactor(float speed,float input) => input>0&&speed>=-.65f
        ?1+.2f*Clamp((6-Math.Abs(speed))/3,0,1):1;
    public static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
    public static float SteeringLimit(float speed, bool terrain) {
        var blend=Clamp(Math.Abs(speed)/25f,0,1);
        return (terrain?34f:32f)*(1-blend)+10f*blend;
    }
    public static float TorqueFactor(float speed, float input, bool terrain) {
        if(input==0 || speed*input < -.65f)return 0;
        float limit=input<0?8f:(terrain?20f:34f);
        return Clamp((limit-Math.Abs(speed))/5f,0,1);
    }
    public static bool DirectionBrake(float speed,float input) => input!=0 && speed*input<-.65f;
    // Engine drive is independent of contact reporting: airborne wheels can spin,
    // and an unavailable contact sample must not lock out a parked vehicle.
    public static float WheelWeight(int index,bool terrain) => index<2?(terrain?.2f:0):(terrain?.3f:.5f);
    public static float GripTorqueFactor(float slip) => float.IsNaN(slip)||float.IsInfinity(slip)?1f:1f-.7f*Clamp((Math.Abs(slip)-.6f)/1.4f,0,1);
    public static float WrapAngle(float angle) => ((angle+180f)%360f+360f)%360f-180f;
}
}
