using System;
namespace Rachel350Z {
// World-space heading: steering the car does not instantly rotate the camera.
public sealed class ChaseCameraState {
    public float Yaw {get;private set;}
    float manualHold;
    public void Reset(float heading){Yaw=DrivingMath.WrapAngle(heading);manualHold=0;}
    public void Orbit(float degrees){if(Math.Abs(degrees)<.001f)return;Yaw=DrivingMath.WrapAngle(Yaw+degrees);manualHold=2.2f;}
    public void Hold(){manualHold=2.2f;}
    public void Step(float dt,float heading,float travelHeading,float speed,float forwardSpeed,float followRate,bool drift=false) {
        if(float.IsNaN(dt)||float.IsInfinity(dt))return;
        dt=DrivingMath.Clamp(dt,0,.1f);manualHold=Math.Max(0,manualHold-dt);
        if(manualHold>0||speed<1)return;
        // Look partly along the path through a corner/drift. Reverse stays
        // behind the chassis instead of whipping around by 180 degrees.
        float slipLimit=drift?80:55;
        float slip=forwardSpeed>1?DrivingMath.Clamp(DrivingMath.WrapAngle(travelHeading-heading),-slipLimit,slipLimit):0;
        // In drift mode, almost follow the velocity heading: the car can yaw
        // across the view while the road remains ahead. The slower response
        // carries the camera through transitions instead of locking to the tail.
        float target=heading+slip*(drift?.95f:.6f)*DrivingMath.Clamp((speed-1)/7,0,1);
        float blend=1-(float)Math.Exp(-DrivingMath.Clamp(followRate,1,6)*(drift?.65f:1)*dt);
        Yaw=DrivingMath.WrapAngle(Yaw+DrivingMath.WrapAngle(target-Yaw)*blend);
    }
}
}
