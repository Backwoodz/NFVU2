using System;

namespace Rachel350Z {
// Shared acoustic/HUD load model, not NFS's recovered boost physics.
// ChargeTime=.5 and sample gain ratios come from the 350Z's TurboDataSet 08.
// Pressure scale, dynamic gain and vacuum/flutter timing are NFVU2 adaptations.
public sealed class TurboSoundModel {
    public float Charge {get;private set;}
    public float Pressure {get;private set;}
    public float ChargeGain => ChargeSampleGain*(float)Math.Pow(Charge,1.5);
    public float ChargePitch => .85f+.35f*Charge;
    public float BlowOffGain {get;private set;}
    public float ReleasedCharge {get;private set;}
    // /32768 is our Unity gain conversion, not a recovered NFS mixer unit.
    public const float ChargeSampleGain=8000f/32768f;
    public const float LowSampleGain=24500f/32768f;
    public const float HighSampleGain=16500f/32768f;
    // Our mix trim preserves the sample-bank ratios while keeping releases
    // below the engine. Shifts vent much less audibly than a complete lift.
    public const float ReleaseMixGain=.4f;
    public const float ShiftReleaseGain=.35f;
    public const float ReleaseDuration=.55f;
    public static float ReleaseEnvelope(float age) {
        if(float.IsNaN(age)||float.IsInfinity(age)||age<0||age>=ReleaseDuration)return 0;
        float remaining=1-DrivingMath.Clamp((age-.065f)/(ReleaseDuration-.065f),0,1);
        return remaining*remaining;
    }
    // A one-frame event: 0 = none, 1 = low boost, 2 = high boost.
    public int BlowOff {get;private set;}
    float releaseReference,cooldown,flutterLeft,flutterStrength;
    bool wasShifting;
    static float Safe(float value)=>float.IsNaN(value)||float.IsInfinity(value)?0:value;
    static float Move(float value,float target,float step)=>Math.Abs(target-value)<=step?target:value+Math.Sign(target-value)*step;
    public void Reset() {
        Charge=Pressure=BlowOffGain=ReleasedCharge=releaseReference=cooldown=flutterLeft=flutterStrength=0;
        BlowOff=0;wasShifting=false;
    }
    public void Step(float dt,bool driving,float rpm,float throttle,bool shifting,bool reverse,float load=-1) {
        dt=DrivingMath.Clamp(Safe(dt),0,.1f);
        throttle=DrivingMath.Clamp(Safe(throttle),0,1);rpm=Safe(rpm);
        load=load<0?throttle:DrivingMath.Clamp(Safe(load),0,1);
        BlowOff=0;BlowOffGain=ReleasedCharge=0;
        if(!driving||reverse){Reset();return;}
        if(dt<=0)return;
        cooldown=Math.Max(0,cooldown-dt);
        // A decaying recent throttle peak detects a normal controller lift
        // across many frames; single-frame deltas missed analog releases.
        releaseReference=Math.Max(throttle,releaseReference-dt*.8f);
        // Feathering the accelerator at high load must not fire a full vent.
        // Keep the recent-peak detector for smooth analog trigger releases.
        bool released=throttle<.35f&&releaseReference>.2f&&releaseReference-throttle>.18f;
        bool shift=shifting&&!wasShifting;
        if((released||shift)&&Charge>.12f&&cooldown==0) {
            // Capture pressure before dumping it; the voice keeps this gain
            // for its complete sample, independent of next frame's charge.
            ReleasedCharge=Charge;
            BlowOff=ReleasedCharge>.6f?2:1;
            BlowOffGain=ReleaseMixGain*(BlowOff==2?HighSampleGain:LowSampleGain)*(float)Math.Pow(ReleasedCharge,1.2)*(shift&&!released?ShiftReleaseGain:1);
            flutterLeft=.42f;flutterStrength=ReleasedCharge;
            Charge*=.12f;cooldown=.4f;
            releaseReference=throttle;
        }
        float revs=DrivingMath.Clamp((rpm-1900)/3600,0,1);
        float target=shifting?0:Math.Min(throttle,load)*revs;
        Charge=Move(Charge,target,dt/(target>Charge?.5f:.18f));
        float vacuum=-18*revs*(1-throttle);
        float pressure=30*Charge+(1-Charge)*vacuum;
        if(flutterLeft>0) {
            float age=.42f-flutterLeft;
            pressure+=8*flutterStrength*(flutterLeft/.42f)*(float)Math.Sin(age*2*Math.PI*13);
            flutterLeft=Math.Max(0,flutterLeft-dt);
        }
        Pressure=DrivingMath.Clamp(pressure,-30,30);
        wasShifting=shifting;
    }
}
}
