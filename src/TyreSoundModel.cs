using System;

namespace Rachel350Z {
// Acoustic response only. No tyre forces, grip or steering are changed.
public sealed class TyreSoundModel {
    public float Level {get;private set;}
    public float HardMix {get;private set;}
    public float Pitch {get;private set;}=1;
    public float SoftGain=>Level*(float)Math.Sqrt(1-HardMix);
    public float HardGain=>Level*(float)Math.Sqrt(HardMix);
    public void Reset(){Level=HardMix=0;Pitch=1;}
    public void Step(float dt,bool active,float speed,float sideways,float spin,float activity,int contacts,bool burnout) {
        if(!Finite(dt)||dt<=0){Reset();return;}dt=Math.Min(.1f,dt);
        speed=Safe(speed);sideways=Safe(sideways);spin=Safe(spin);activity=Clamp(Safe(activity));contacts=Math.Max(0,Math.Min(4,contacts));
        float excitation=Math.Max(0,sideways-.65f)*.10f+Math.Max(0,spin-1)*.055f;
        // The normalized physics skid channel is deliberately a small part:
        // it saturates at 1 during a drift, while slip velocities still vary.
        float target=Clamp((float)Math.Sqrt(excitation)*.85f+activity*.15f);
        target*=burnout?Clamp(spin/4):(Clamp((speed-2)/7));
        target*=(float)Math.Sqrt(contacts/4f);
        if(!active||contacts==0)target=0;
        Level=Approach(Level,target,dt, target>Level?.075f:.14f);
        float mix=Clamp((sideways-2)/10*.7f+spin/24*.3f);
        HardMix=Approach(HardMix,mix,dt,.18f);
        float tone=.83f+.29f*Clamp(speed/30)+.15f*Clamp(sideways/14)+.06f*Clamp(spin/25);
        Pitch=Approach(Pitch,tone,dt,.12f);
        if(Level<.0001f)Level=0;
    }
    static float Approach(float current,float target,float dt,float time)=>current+(target-current)*(1-(float)Math.Exp(-dt/time));
    static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
    static float Safe(float n)=>Finite(n)?Math.Max(0,n):0;
    static float Clamp(float n)=>Math.Max(0,Math.Min(1,n));
}
}
