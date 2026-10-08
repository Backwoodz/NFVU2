using System;

namespace Rachel350Z {
// Read-only DSP analysis; independent channels avoid cancelling opposite-phase stereo bass.
public sealed class BassSignal {
    readonly double[] low=new double[8],high=new double[8];
    public void Reset(){Array.Clear(low,0,low.Length);Array.Clear(high,0,high.Length);}
    public float Read(float[] data,int channels,int rate) {
        if(data==null||channels<1||channels>8||rate<8000||data.Length<channels)return 0;
        double a=1-Math.Exp(-2*Math.PI*180/rate),b=1-Math.Exp(-2*Math.PI*30/rate),power=0;
        int count=data.Length-data.Length%channels;
        for(int i=0;i<count;i++) {
            int c=i%channels;double v=data[i];if(double.IsNaN(v)||double.IsInfinity(v))v=0;
            low[c]+=a*(v-low[c]);high[c]+=b*(v-high[c]);
            double band=low[c]-high[c];power+=band*band;
        }
        return (float)Math.Sqrt(power/count);
    }
}
public sealed class BassMotion {
    float envelope,phase;
    public float Offset {get;private set;}
    public void Reset(){envelope=phase=Offset=0;}
    public float Step(float dt,float energy,bool active) {
        if(float.IsNaN(dt)||float.IsInfinity(dt))dt=0;dt=Math.Max(0,Math.Min(dt,.1f));
        float target=active&&!float.IsNaN(energy)&&!float.IsInfinity(energy)?Math.Max(0,Math.Min(1,(energy-.002f)*12)):0;
        envelope+=(target-envelope)*(1-(float)Math.Exp(-dt*(target>envelope?35:14)));
        if(envelope<.0001f){envelope=Offset=0;return 0;}
        phase=(phase+dt*8*(float)Math.PI*2)%((float)Math.PI*2);
        Offset=.014f*envelope*(float)Math.Sin(phase);return Offset;
    }
    public static float ConeWeight(float x,float y,float normalZ,float radius) {
        if(radius<=0||normalZ<=.05f)return 0;
        float r=(float)Math.Sqrt(x*x+y*y)/radius;
        float t=Math.Max(0,Math.Min(1,(.88f-r)/.23f));
        return t*t*(3-2*t);
    }
}
}
