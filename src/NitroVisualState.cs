using System;

namespace Rachel350Z {
// NFVU2 timing, not recovered NFS constants. No authority over fuel or handling.
public sealed class NitroVisualState {
    public float Amount { get; private set; }
    public void Reset() { Amount=0; }
    public void Step(float dt,bool boost,bool allowed,float strength) {
        if(!allowed){Reset();return;}
        strength=Finite(strength)?Clamp(strength,0,1):0;
        if(strength==0){Reset();return;}
        if(!Finite(dt)||dt<=0)return;
        float target=boost?strength:0;
        Amount+=(target-Amount)*(1-(float)Math.Exp(-Math.Min(dt,.1f)*(boost?12:7)));
        if(!boost&&Amount<.002f)Amount=0;
    }
    public static float EdgeMask(float x,float y,float left,float bottom,float right,float top) {
        // Sharp central view plus a feathered rectangle around the projected car.
        float dx=(x-.5f)*2,dy=(y-.53f)*2;
        float edge=Smooth(((float)Math.Sqrt(dx*dx+dy*dy)-.18f)/.65f);
        float outside=Math.Max(Math.Max(left-x,x-right),Math.Max(bottom-y,y-top));
        return edge*Smooth(outside/.08f);
    }
    static float Smooth(float x){x=Clamp(x,0,1);return x*x*(3-2*x);}
    static float Clamp(float x,float min,float max)=>Math.Max(min,Math.Min(max,x));
    static bool Finite(float x)=>!float.IsNaN(x)&&!float.IsInfinity(x);
}
}
