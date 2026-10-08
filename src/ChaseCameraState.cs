using System;
namespace Rachel350Z {
// World-space heading: steering the car does not instantly rotate the camera.
public sealed class ChaseCameraState {
    public float Yaw {get;private set;}
    float manualHold;
    public void Reset(float heading){Yaw=DrivingMath.WrapAngle(heading);manualHold=0;}
    public void Orbit(float degrees){if(Math.Abs(degrees)<.001f)return;Yaw=DrivingMath.WrapAngle(Yaw+degrees);manualHold=2.2f;}
    public void Hold(){manualHold=2.2f;}
    public void Step(float dt,float heading,float travelHeading,float speed,float forwardSpeed,float followRate) {
        if(float.IsNaN(dt)||float.IsInfinity(dt))return;
        dt=DrivingMath.Clamp(dt,0,.1f);manualHold=Math.Max(0,manualHold-dt);
        if(manualHold>0||speed<1)return;
        // Look partly along the path through a corner/drift. Reverse stays
        // behind the chassis instead of whipping around by 180 degrees.
        float slip=forwardSpeed>1?DrivingMath.Clamp(DrivingMath.WrapAngle(travelHeading-heading),-55,55):0;
        float target=heading+slip*.6f*DrivingMath.Clamp((speed-1)/7,0,1);
        float blend=1-(float)Math.Exp(-DrivingMath.Clamp(followRate,1,6)*dt);
        Yaw=DrivingMath.WrapAngle(Yaw+DrivingMath.WrapAngle(target-Yaw)*blend);
    }
}
}
