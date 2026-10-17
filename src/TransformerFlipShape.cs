using System;
namespace Rachel350Z {
// Joint-space shape, separate from the backward rotation of the whole body.
public struct TransformerFlipShape {
    public float HipDegrees,KneeDegrees,SpineDegrees,HeadDegrees,ArmRaise;
    public static TransformerFlipShape Sample(TransformerMountSample sample) {
        float tuck=sample.Tuck,crouch=sample.Crouch;
        float raise=Smooth((sample.Progress-.20f)/.14f)*(1-Smooth((sample.Progress-.47f)/.12f));
        return new TransformerFlipShape {
            HipDegrees=6+106*tuck+42*crouch,
            KneeDegrees=12+137*tuck+67*crouch,
            SpineDegrees=28*tuck+15*crouch,
            HeadDegrees=16*tuck,ArmRaise=raise
        };
    }
    public void Leg(float thigh,float shin,out float kneeUp,out float kneeForward,out float footUp,out float footForward) {
        double hip=HipDegrees*Math.PI/180,lower=(HipDegrees-KneeDegrees)*Math.PI/180;
        kneeUp=-(float)Math.Cos(hip)*thigh;kneeForward=(float)Math.Sin(hip)*thigh;
        footUp=kneeUp-(float)Math.Cos(lower)*shin;footForward=kneeForward+(float)Math.Sin(lower)*shin;
    }
    static float Smooth(float p){p=Math.Max(0,Math.Min(1,p));return p*p*(3-2*p);}
}
}
