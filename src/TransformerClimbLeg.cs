using System;
namespace Rachel350Z {
// Sagittal knee hinge in the climber's body frame (+Y up, +Z facing).
// Rotate the hip-to-boot direction by a quarter turn, rather than pointing
// knees sideways/backwards to work around the robot's panels.
public static class TransformerClimbLeg {
    public static void PoleOffset(float footUp,float footForward,out float up,out float forward) {
        double length=Math.Sqrt((double)footUp*footUp+(double)footForward*footForward);
        if(double.IsNaN(length)||double.IsInfinity(length)||length<.00001) {
            up=0;forward=.65f;return;
        }
        up=(float)(.65*footForward/length);
        forward=(float)(-.65*footUp/length);
    }
}
}
