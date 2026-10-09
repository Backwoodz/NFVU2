using System;

namespace Rachel350Z {
// Valheim gameplay tuning, not a reproduction of NFS's nitrous physics/refill.
public sealed class NitroState {
    public float Charge { get; private set; } = 1;
    public bool Boosting { get; private set; }
    public bool Purging { get; private set; }
    bool exhausted;
    float refillDelay;
    public void Step(float dt,bool driving,bool blocked,bool held,float forwardSpeed,float speed,
                     float throttle,bool braking,bool grounded,bool terrain) {
        Boosting=Purging=false;
        if(!Finite(dt)||!Finite(forwardSpeed)||!Finite(speed)||!Finite(throttle))return;
        dt=DrivingMath.Clamp(dt,0,.1f);
        if(!driving||blocked)return;
        if(!held)exhausted=false;
        // Refill after release during ordinary driving; no menu/exit refill exploit.
        if(!held) {
            refillDelay=Math.Max(0,refillDelay-dt);
            if(refillDelay==0&&forwardSpeed>2&&grounded)Charge=Math.Min(1,Charge+dt/10);
        }
        if(!held||throttle<0)return;
        if(speed<.65f){Purging=grounded;return;}
        float limit=terrain?27:42;
        if(exhausted||Charge<=0||forwardSpeed<1.5f||speed>=limit||throttle<=0||braking||!grounded)return;
        Boosting=true;Charge=Math.Max(0,Charge-dt/6);refillDelay=2;
        if(Charge<=0){Boosting=false;exhausted=true;}
    }
    public void StepFlight(float dt,bool allowed,bool held,float throttle,float speed) {
        Boosting=Purging=false;
        if(!allowed||!Finite(dt)||!Finite(throttle)||!Finite(speed))return;
        dt=DrivingMath.Clamp(dt,0,.1f);
        if(!held) {
            exhausted=false;refillDelay=Math.Max(0,refillDelay-dt);
            if(refillDelay==0&&speed>2)Charge=Math.Min(1,Charge+dt/10);
        }
        if(!held||throttle<=0||speed>=48||exhausted||Charge<=0)return;
        Boosting=true;Charge=Math.Max(0,Charge-dt/6);refillDelay=2;
        if(Charge<=0){Boosting=false;exhausted=true;}
    }
    public void Stop(){Boosting=Purging=false;}
    public static float Acceleration(bool boosting,float forwardSpeed,float speed,bool grounded,bool terrain) {
        if(!boosting||!grounded||!Finite(forwardSpeed)||!Finite(speed)||forwardSpeed<1.5f)return 0;
        float limit=terrain?27:42;
        return 7*DrivingMath.Clamp((limit-speed)/3,0,1);
    }
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
}
public sealed class NitroInputGate {
    bool needsRelease=true;
    public bool NeedsRelease=>needsRelease;
    public bool ReadSources(bool nativeMouse,bool legacyMouse,bool keyboard,bool allowed)=>Read(nativeMouse||legacyMouse||keyboard,allowed);
    public bool Read(bool held,bool allowed) {
        if(!allowed){needsRelease=true;return false;}
        if(!held)needsRelease=false;
        return held&&!needsRelease;
    }
    public void Reset(){needsRelease=true;}
}
}
