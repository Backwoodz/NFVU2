using System;
namespace Rachel350Z {
// One ballistic-shaped arc from the shoulder to the actual landing height.
// Horizontal travel uses the same t, so takeoff moves backward and up together.
public static class TransformerDismountArc {
    public const float Rise=.78f;
    public static float Height(float travel,float takeoffY,float landingY,float rise=Rise) {
        double t=float.IsNaN(travel)?0:Math.Max(0,Math.Min(1,travel));
        if(t<=0)return takeoffY;if(t>=1)return landingY;
        double drop=(double)takeoffY-landingY;
        double height=Math.Max(.01,rise)+Math.Max(0,-drop);
        double launch=2*height+2*Math.Sqrt(height*(height+drop));
        return (float)(takeoffY+launch*t-(launch+drop)*t*t);
    }
}
}
