using System;
namespace Rachel350Z {
// Procedural straight-cut-gear interpretation; this is not an NFS recording.
public static class ReverseGearSound {
    public static WavData Create() {
        const int rate=22050;var samples=new float[rate];
        for(int i=0;i<samples.Length;i++) {
            double t=(double)i/rate,phase=2*Math.PI*320*t;
            samples[i]=(float)((Math.Sin(phase)+.32*Math.Sin(2*phase)+.12*Math.Sin(3*phase)+.06*Math.Sin(5*phase))
                *(.43+.025*Math.Sin(2*Math.PI*24*t)));
        }
        return new WavData{Channels=1,SampleRate=rate,Samples=samples};
    }
    public static float Level(bool driving,bool reverse,float speed) =>
        driving&&reverse&&!float.IsNaN(speed)&&!float.IsInfinity(speed)
        ?DrivingMath.Clamp((-speed-.15f)/3,0,1):0;
    public static float Pitch(float speed) => float.IsNaN(speed)||float.IsInfinity(speed)?1:
        .65f+1.35f*DrivingMath.Clamp(-speed/8,0,1);
}
}
