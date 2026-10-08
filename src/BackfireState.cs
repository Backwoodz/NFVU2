using System;
namespace Rachel350Z {
// Optional lift-off interpretation using original effect textures, not recovered NFS rules.
public sealed class BackfireState {
    float loadedTime,cooldown;
    bool armed;
    public bool Step(float dt,bool active,float rpm,float throttle,float forwardSpeed,bool enabled) {
        if(!active||!enabled||!Finite(rpm)||!Finite(throttle)||!Finite(forwardSpeed)||!Finite(dt)) {
            loadedTime=cooldown=0;armed=false;return false;
        }
        dt=DrivingMath.Clamp(dt,0,.1f);cooldown=Math.Max(0,cooldown-dt);
        if(forwardSpeed<3){loadedTime=0;armed=false;return false;}
        if(throttle>.65f&&rpm>4200) {
            loadedTime+=dt;
            if(loadedTime>=.3f)armed=true;
        } else if(throttle<.1f) {
            bool fire=armed&&cooldown==0&&rpm>3000;
            armed=false;loadedTime=0;
            if(fire)cooldown=.8f;
            return fire;
        } else loadedTime=0;
        return false;
    }
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
}
}
