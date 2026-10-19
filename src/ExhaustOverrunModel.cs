using System;

namespace Rachel350Z {
// NFVU2 timing and gain model; only the optional recordings come from local media.
public sealed class ExhaustOverrunModel {
    float loaded,cooldown,untilPop,strength,bedDelay;
    int remaining,sequence;
    bool armed,coasting;
    public bool Pop {get;private set;}
    public int Variant {get;private set;}
    public float Gain {get;private set;}
    public float Pitch {get;private set;}=1;
    public float Level {get;private set;}
    public float LoopPitch {get;private set;}=1;
    public void Reset() {loaded=cooldown=untilPop=strength=bedDelay=0;remaining=sequence=0;armed=coasting=Pop=false;Gain=Level=0;Pitch=LoopPitch=1;}
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    public void Step(float dt,bool active,float rpm,float throttle,float forwardSpeed,bool flame) {
        Pop=false;Gain=0;
        if(!active||!Finite(dt)||dt<=0||!Finite(rpm)||!Finite(throttle)||!Finite(forwardSpeed)) {Reset();return;}
        dt=Math.Min(dt,.1f);cooldown=Math.Max(0,cooldown-dt);
        if(forwardSpeed<2||rpm<1500) {Reset();return;}
        if(throttle>=.18f)coasting=false;
        if(throttle>.55f&&rpm>3200) {
            remaining=0;loaded+=dt;
            if(loaded>=.18f&&cooldown==0)armed=true;
        } else if(throttle<.18f) {
            if(armed&&cooldown==0) {
                strength=Math.Max(.25f,Math.Min(1,(rpm-2400)/3600));
                // Leave room for the flame's stronger bang instead of stacking it.
                remaining=flame?2:3;sequence=0;untilPop=flame?.13f:.035f;cooldown=.7f;
                coasting=true;bedDelay=flame?.14f:.03f;
            }
            armed=false;loaded=0;
        } else {loaded=0;remaining=0;}
        bedDelay=Math.Max(0,bedDelay-dt);
        float target=coasting&&bedDelay==0?(.22f+.40f*Math.Max(0,Math.Min(1,(rpm-1500)/3500))):0;
        Level=Math.Max(0,Math.Min(1,Level+Math.Max(-dt*6,Math.Min(dt*3,target-Level))));
        LoopPitch=Math.Max(.7f,Math.Min(1.25f,rpm/4500));
        if(rpm<2400)remaining=0;
        if(remaining==0)return;
        untilPop-=dt;
        if(untilPop>0)return;
        Pop=true;Variant=sequence%2;
        Gain=(.16f+.18f*strength)*(float)Math.Pow(.72,sequence);
        Pitch=.86f+.17f*strength-sequence*.025f;
        sequence++;remaining--;untilPop+=.105f+sequence*.045f;
    }
}
}
