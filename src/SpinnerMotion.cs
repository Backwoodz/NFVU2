using System;
namespace Rachel350Z {
// Separate free-spinning insert: follow acceleration slowly, coast after tyre stops.
public sealed class SpinnerMotion {
    public float Rpm {get;private set;}
    public float Angle {get;private set;}
    public void Step(float dt,float wheelRpm) {
        if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0)return;
        dt=Math.Min(dt,.1f);
        if(float.IsNaN(wheelRpm)||float.IsInfinity(wheelRpm))wheelRpm=0;
        wheelRpm=Math.Max(-400,Math.Min(400,wheelRpm));
        if(Math.Abs(wheelRpm)>Math.Abs(Rpm)||wheelRpm*Rpm<0)
            Rpm+=(wheelRpm-Rpm)*(1-(float)Math.Exp(-dt*1.3f));
        else Rpm*= (float)Math.Exp(-dt*.22f);
        if(Math.Abs(Rpm)<.15f)Rpm=0;
        Angle=(Angle+Rpm*6*dt)%360;
    }
}
}
