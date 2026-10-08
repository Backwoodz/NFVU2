using System;

namespace Rachel350Z {
// An acoustic approximation, not NFS's recovered boost or physics simulation.
public sealed class TurboSoundModel {
    public float Charge {get;private set;}
    // A one-frame event: 0 = none, 1 = low boost, 2 = high boost.
    public int BlowOff {get;private set;}
    float previousThrottle,cooldown;
    bool wasShifting;
    static float Safe(float value)=>float.IsNaN(value)||float.IsInfinity(value)?0:value;
    public void Step(float dt,bool driving,float rpm,float throttle,bool shifting,bool reverse) {
        dt=DrivingMath.Clamp(Safe(dt),0,.1f);
        throttle=DrivingMath.Clamp(Safe(throttle),0,1);rpm=Safe(rpm);
        BlowOff=0;cooldown=Math.Max(0,cooldown-dt);
        if(!driving||reverse){Charge=0;previousThrottle=0;wasShifting=false;cooldown=0;return;}
        bool released=previousThrottle>.3f&&throttle<previousThrottle-.25f;
        if((released||(shifting&&!wasShifting))&&Charge>.18f&&cooldown==0) {
            BlowOff=Charge>.6f?2:1;Charge*=.12f;cooldown=.25f;
        }
        float target=shifting?0:throttle*DrivingMath.Clamp((rpm-1900)/3600,0,1);
        // The recovered 350Z turbo datasets use ChargeTime=0.5. The envelope
        // and event thresholds here are deliberately our own approximation.
        float step=dt/(target>Charge?.5f:.18f);
        Charge=Math.Abs(target-Charge)<=step?target:Charge+Math.Sign(target-Charge)*step;
        previousThrottle=throttle;wasShifting=shifting;
    }
}
}
