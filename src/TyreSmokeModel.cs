using System;

namespace Rachel350Z {
// Visual friction/heat proxy only. This never changes tyre grip or drivetrain state.
public sealed class TyreSmokeModel {
    public float Heat { get; private set; }
    public float Density { get; private set; }
    public void Reset(){Heat=Density=0;}
    public void Step(float dt,bool grounded,float slipSpeed,float load) {
        if(!Finite(dt)||dt<0||dt>.25f){Reset();return;}
        if(dt==0)return;
        if(!Finite(slipSpeed)||!Finite(load)){Reset();return;}
        float friction=grounded?Clamp((Math.Abs(slipSpeed)-.8f)*Math.Max(0,load)/42000f):0;
        float heatTarget=friction;
        Heat=Approach(Heat,heatTarget,dt,heatTarget>Heat?1.8f:5.5f);
        float target=friction*(.10f+.90f*Heat*Heat);
        Density=Approach(Density,target,dt,target>Density?.16f:.32f);
        // Warm tyres that regain grip/leave the road stop creating new smoke;
        // existing world-space smoke has its own independent lifetime.
        if(friction==0)Density=0;
    }
    public float CloudRate=>Density>0?55f*(float)Math.Sqrt(Density)+155f*Density:0;
    public float WispRate=>Density>0?100f*(float)Math.Sqrt(Density)+160f*Density:0;
    public static float Entrainment(float rpm)=>Math.Sign(rpm)*Math.Min(13f,Math.Abs(rpm)*(float)(Math.PI/30)*.16f);
    static float Approach(float a,float b,float dt,float tau)=>b+(a-b)*(float)Math.Exp(-dt/tau);
    static float Clamp(float v)=>Math.Max(0,Math.Min(1,v));
    static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
}
}
