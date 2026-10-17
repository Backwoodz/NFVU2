using System;
using System.Numerics;
using Rachel350Z;
static class ClimbLegChecks {
    static Vector3 Pole(Vector3 foot) {
        TransformerClimbLeg.PoleOffset(foot.Y,foot.Z,out float up,out float forward);
        return new Vector3(0,up,forward);
    }
    static Vector3 Knee(Vector3 foot,Vector3 pole) {
        const float thigh=.47f,shin=.46f;
        float distance=Math.Max(Math.Abs(thigh-shin)+.001f,Math.Min(thigh+shin-.001f,foot.Length()));
        var axis=Vector3.Normalize(foot);
        var bend=Vector3.Normalize(pole-axis*Vector3.Dot(pole,axis));
        float along=(thigh*thigh-shin*shin+distance*distance)/(2*distance);
        return axis*along+bend*(float)Math.Sqrt(Math.Max(0,thigh*thigh-along*along));
    }
    static float SignedBend(Vector3 foot,Vector3 pole) {
        var knee=Knee(foot,pole);
        return Vector3.Cross(knee,foot-knee).X;
    }
    public static void Run(Action<bool,string> check) {
        var standing=Pole(new Vector3(0,-.85f,0));
        check(standing.Z>.64f&&Math.Abs(standing.Y)<.00001f,"standing climber bends knees forward rather than outward");
        foreach(float side in new[]{-1f,1f}) {
            var foot=new Vector3(side*.04f,-.65f,.30f);
            var oldPole=new Vector3(side*.38f,-.26f,-.10f);
            check(SignedBend(foot,oldPole)<0&&SignedBend(foot,Pole(foot))>0,
                "regression: former sideways/rearward pole reverses the knee hinge on side "+side);
        }
        bool anatomical=true,lengths=true,hinge=true;
        for(int yi=-88;yi<=35;yi+=3)for(int zi=-30;zi<=70;zi+=3)foreach(float side in new[]{-.15f,.15f}) {
            var foot=new Vector3(side,yi*.01f,zi*.01f);
            if(foot.Length()<.15f||foot.Length()>.925f||foot.Y*foot.Y+foot.Z*foot.Z<.01f)continue;
            var pole=Pole(foot);var knee=Knee(foot,pole);
            anatomical&=SignedBend(foot,pole)>0;
            lengths&=Math.Abs(knee.Length()-.47f)<.00001f&&Math.Abs((foot-knee).Length()-.46f)<.00001f;
            hinge&=Math.Abs(Vector3.Dot(pole,foot))<.00001f&&Math.Abs(pole.Length()-.65f)<.00001f;
        }
        check(anatomical,"both legs retain human hinge direction across reachable low, raised, forward and trailing boot targets");
        check(lengths,"corrected climbing knees preserve thigh/shin lengths and planted boot endpoints");
        check(hinge,"knee pole stays perpendicular to the actual leg without a sideways bend offset");
        bool continuous=true;var previous=Pole(new Vector3(0,-.4f,.45f));
        for(int i=1;i<=800;i++) {
            var current=Pole(new Vector3(0,-.4f+i*.001f,.45f));
            continuous&=Vector3.Distance(previous,current)<.0015f;previous=current;
        }
        check(continuous,"raising a boot through hip height does not flip the knee pole");
        foreach(float invalid in new[]{0f,float.NaN,float.PositiveInfinity}) {
            TransformerClimbLeg.PoleOffset(invalid,0,out float up,out float forward);
            check(up==0&&forward==.65f,"degenerate or invalid leg direction has a finite forward fallback");
        }
    }
}
