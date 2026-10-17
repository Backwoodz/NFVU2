using System;
using System.IO;
namespace Rachel350Z {
public struct TransformerMinigunMotorLevels {
    public float Pitched,Unpitched,Action;
}
// Motor-only correction. Native shot, mechanism, bass and reload PCM/gains
// remain untouched, and worker-cached source samples are never edited in place.
public static class TransformerMinigunSoundLevels {
    public const float MotorBoost=3.9810717f,PeakLimit=.97f; // +12 dB
    public static bool IsMotor(string name)=>name=="spin_start"||name=="spin_loop"||name=="spin_unpitched_loop"||name=="spin_release"||name=="spin_stop";
    public static float[] Prepare(string name,float[] source,out float peak) {
        if(source==null||source.Length==0)throw new InvalidDataException("Empty minigun PCM");
        peak=0;
        foreach(float sample in source) {
            if(!Finite(sample)||Math.Abs(sample)>1)throw new InvalidDataException("Invalid minigun PCM sample");
            peak=Math.Max(peak,Math.Abs(sample));
        }
        if(!IsMotor(name))return source;
        float gain=peak>0?(float)Math.Min(MotorBoost,(double)PeakLimit/peak):MotorBoost;
        if(peak*gain>PeakLimit)gain*=.999999f;
        var prepared=new float[source.Length];float preparedPeak=0;
        for(int i=0;i<source.Length;i++){prepared[i]=source[i]*gain;preparedPeak=Math.Max(preparedPeak,Math.Abs(prepared[i]));}
        peak=preparedPeak;return prepared;
    }
    public static float Master(float value)=>Finite(value)?Math.Max(0,Math.Min(1,value)):0;
    public static TransformerMinigunMotorLevels Motor(float spin,float volume,float pitchedPeak,float unpitchedPeak,float actionPeak) {
        float q=Master(spin),master=Master(volume);
        float pitched=q>0?.42f+.18f*(float)Math.Sqrt(q):0;
        // Rust has a separate fixed-pitch motor texture. It reaches most of
        // its presence early instead of being inaudible until bullets start.
        float unpitched=q>0?.36f+.24f*Math.Min(1,q/.4f):0;
        float budget=pitched*Peak(pitchedPeak)+unpitched*Peak(unpitchedPeak)+Peak(actionPeak);
        float scale=budget>PeakLimit?PeakLimit/budget:1;
        return new TransformerMinigunMotorLevels{Pitched=master*pitched*scale,Unpitched=master*unpitched*scale,Action=master*scale};
    }
    static float Peak(float value)=>Finite(value)?Math.Max(0,Math.Min(1,value)):1;
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
}
}
