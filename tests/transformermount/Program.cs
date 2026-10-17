using System;
using Rachel350Z;
class Program {
    static int checks;
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
    static bool Near(float a,float b)=>Math.Abs(a-b)<.00001f;
    static void Main() {
        ClimbLegChecks.Run(Check);
        Check(Near(TransformerMountMotion.SeatForward(0),0)&&Near(TransformerMountMotion.SeatLower(0),0)
            &&Near(TransformerMountMotion.SeatForward(1),1)&&Near(TransformerMountMotion.SeatLower(1),1),"seat transfer preserves standing and seated endpoints");
        Check(Near(TransformerMountMotion.SeatForward(.5f),0)&&TransformerMountMotion.SeatLower(.5f)>.7f,
            "rider lowers behind helmet before moving forward into seat");
        float lastForward=0,lastLower=0;
        for(int i=1;i<=1000;i++) {
            float blend=i/1000f,forward=TransformerMountMotion.SeatForward(blend),lower=TransformerMountMotion.SeatLower(blend);
            if(forward<lastForward||lower<lastLower||forward-lastForward>.004f||lower-lastLower>.004f)
                throw new Exception("seat transfer jumps or reverses direction");
            lastForward=forward;lastLower=lower;
        }
        Check(true,"standing transfer stays continuous in both directions");
        foreach(bool entry in new[]{true,false}) {
            var m=new TransformerMountMotion();Check(m.Begin(entry)&&m.Active,"starts "+(entry?"climb":"exit"));
            Check(!m.Begin(!entry)&&m.Entering==entry,"active motion cannot be overwritten");
            foreach(float dt in new[]{0,-1,float.NaN,float.PositiveInfinity})m.Step(dt,false);
            m.Step(.05f,true);Check(m.Progress==0,"invalid time and pause hold the current pose");
            float previous=0,largestLift=0,largestTuck=0;
            for(int i=0;i<1000;i++) {
                m.Step(.01f,false);var p=m.Pose;
                if(p.Travel<previous||p.Travel<0||p.Travel>1||p.SeatBlend<0||p.SeatBlend>1)throw new Exception("trajectory escaped monotonic bounds");
                if(float.IsNaN(p.PitchDegrees)||p.Lift<0||p.Crouch<-.00001f)throw new Exception("invalid pose");
                largestLift=Math.Max(largestLift,p.Lift);largestTuck=Math.Max(largestTuck,p.Tuck);previous=p.Travel;
            }
            Check(m.Complete&&!m.Active&&m.Progress==1,"transition finishes exactly");
            Check(entry?largestLift==0&&largestTuck==0:largestLift>.59f&&largestTuck>.99f,entry?"climb has no airborne loft or jump tuck":"exit has upward takeoff and full tuck");
            Check(Near(m.Pose.Travel,1)&&Near(m.Pose.SeatBlend,entry?1:0),"final pose reaches correct seat or ground endpoint");
            Check(Near(m.Pose.Lift,0)&&Near(m.Pose.Crouch,0),"final pose has no residual height or squat");
            m.Step(.1f,false);Check(m.Progress==1,"completed motion does not repeat");
        }
        var before=TransformerMountMotion.Sample(.20f,false);var takeoff=TransformerMountMotion.Sample(.301f,false);var after=TransformerMountMotion.Sample(.90f,false);
        Check(Near(before.SeatBlend,0)&&Near(before.Travel,0)&&Near(before.PitchDegrees,0),"dismount stands fully before squat");
        Check(TransformerMountMotion.Sample(.30f,false).Crouch>.99f,"takeoff starts from a visible loaded squat");
        Check(takeoff.Travel>0&&TransformerDismountArc.Height(takeoff.Travel,4.6f,.92f)>4.6f,"first airborne sample moves backward and upward from the same takeoff point");
        foreach(float drop in new[]{0f,1f,3.65f,5f,-.5f}) {
            const float startY=4.6f;float endY=startY-drop,peak=startY,previous=startY,previousSlope=float.PositiveInfinity;
            Check(Near(TransformerDismountArc.Height(0,startY,endY),startY)&&Near(TransformerDismountArc.Height(1,startY,endY),endY),"arc has exact takeoff and landing endpoints for drop "+drop);
            for(int tick=1;tick<=1000;tick++) {
                float t=tick/1000f,y=TransformerDismountArc.Height(t,startY,endY),slope=(y-previous)*1000;
                if(slope>previousSlope+.004f)throw new Exception("arc descent becomes shallower or changes segment");
                peak=Math.Max(peak,y);previous=y;previousSlope=slope;
            }
            Check(Math.Abs(peak-Math.Max(startY,endY)-TransformerDismountArc.Rise)<.001f,"arc peaks at requested rise and descends progressively steeper for drop "+drop);
        }
        Check(Near(after.Travel,1)&&Near(after.PitchDegrees,-360),"exit completes a full backward rotation before landing");
        Check(TransformerMountMotion.Sample(.55f,false).PitchDegrees<0,"flip rotates backward toward vehicle rear");
        Check(TransformerMountMotion.Sample(.95f,false).Crouch>.99f,"landing briefly absorbs impact");
        Check(Near(TransformerMountMotion.Sample(.90f,true).Travel,1)&&Near(TransformerMountMotion.Sample(.90f,true).SeatBlend,0),"climb reaches platform before sitting");
        var folded=TransformerFlipShape.Sample(TransformerMountMotion.Sample(.62f,false));
        Check(folded.HipDegrees>110&&folded.KneeDegrees>145&&folded.SpineDegrees>25,"midair shape folds thighs toward chest and heels toward hips");
        folded.Leg(.47f,.46f,out float kneeUp,out float kneeForward,out float footUp,out float footForward);
        Check(kneeUp>.15f&&kneeForward>.40f&&Math.Abs(footForward)<.20f&&Math.Abs(footUp)<.25f,"full tuck lifts knees while folded boots return close to the hips");
        Check(Math.Abs(Math.Sqrt(kneeUp*kneeUp+kneeForward*kneeForward)-.47)<1e-6&&Math.Abs(Math.Sqrt((footUp-kneeUp)*(footUp-kneeUp)+(footForward-kneeForward)*(footForward-kneeForward))-.46)<1e-6,"tucked targets preserve measured thigh and shin lengths");
        var opened=TransformerFlipShape.Sample(TransformerMountMotion.Sample(.87f,false));
        Check(opened.HipDegrees<10&&opened.KneeDegrees<15&&opened.SpineDegrees==0,"legs and spine reopen before the landing endpoint");
        var raised=TransformerFlipShape.Sample(TransformerMountMotion.Sample(.39f,false));
        Check(raised.ArmRaise>.99f&&folded.ArmRaise<.01f,"hands sweep overhead on takeoff before drawing in to the tucked knees");
        Func<float,float> angle=p=>TransformerMountMotion.Sample(p,false).PitchDegrees;
        Check(Math.Abs(angle(.61f)-angle(.59f))>10*Math.Abs(angle(.36f)-angle(.34f)),"backward rotation accelerates through the compact shape");
        for(int tick=0;tick<=100;tick++) {
            var pose=TransformerFlipShape.Sample(TransformerMountMotion.Sample(tick/100f,false));
            pose.Leg(.47f,.46f,out float ky,out float kz,out float fy,out float fz);
            if(Math.Abs(Math.Sqrt(ky*ky+kz*kz)-.47)>1e-6||Math.Abs(Math.Sqrt((fy-ky)*(fy-ky)+(fz-kz)*(fz-kz))-.46)>1e-6)throw new Exception("flip changes actual leg segment lengths");
        }
        Check(true,"every flip phase has achievable targets with fixed bone lengths");
        const int levels=10;
        for(int level=0;level<levels;level++) {
            float span=TransformerMountMotion.ClimbSeatStart-TransformerMountMotion.ClimbApproachEnd;
            float start=TransformerMountMotion.ClimbApproachEnd+span*level/levels;
            Func<float,TransformerClimbSample> at=phase=>TransformerMountMotion.Climb(start+span*phase/levels,levels);
            var left=at(.08f);var right=at(.24f);var leftBoot=at(.40f);var rightBoot=at(.56f);var pull1=at(.70f);var pull2=at(.94f);
            if(!(left.LeftHand>0&&left.LeftHand<1&&Near(left.RightHand,0)&&Near(left.LeftFoot,0)&&Near(left.RightFoot,0)))throw new Exception("left hand does not move alone");
            if(!(Near(right.LeftHand,1)&&right.RightHand>0&&right.RightHand<1&&Near(right.LeftFoot,0)))throw new Exception("right hand not anchored after left");
            if(!(Near(leftBoot.LeftHand,1)&&Near(leftBoot.RightHand,1)&&leftBoot.LeftFoot>0&&leftBoot.LeftFoot<1&&Near(leftBoot.RightFoot,0)))throw new Exception("left boot moves without two fixed grips");
            if(!(Near(rightBoot.LeftFoot,1)&&rightBoot.RightFoot>0&&rightBoot.RightFoot<1&&rightBoot.Pull>leftBoot.Pull&&leftBoot.Pull>0))throw new Exception("body does not rise continuously while two grips and opposite boot support stepping");
            if(!(Near(pull1.LeftHand,1)&&Near(pull1.RightHand,1)&&Near(pull1.LeftFoot,1)&&Near(pull1.RightFoot,1)&&pull2.Pull>pull1.Pull))throw new Exception("body rises without four fixed contacts");
            if(!Near(pull1.LeftHand,pull2.LeftHand)||!Near(pull1.RightHand,pull2.RightHand)||!Near(pull1.LeftFoot,pull2.LeftFoot)||!Near(pull1.RightFoot,pull2.RightFoot))throw new Exception("contact slides during pull");
        }
        Check(true,"every cycle reaches both hands then rises smoothly while boots step separately");
        Check(true,"all four final contact positions remain fixed during the last pull phase");
        foreach(bool entry in new[]{true,false}) {
            var fast=new TransformerMountMotion();var slow=new TransformerMountMotion();fast.Begin(entry);slow.Begin(entry);
            for(int i=0;i<60;i++)fast.Step(1f/60,false);for(int i=0;i<120;i++)slow.Step(1f/120,false);
            Check(Math.Abs(fast.Progress-slow.Progress)<.00001f,"ordinary frame rates give equal timing");
            var stalled=new TransformerMountMotion();stalled.Begin(entry);stalled.Step(10,false);
            Check(stalled.Progress<.05f,"long frame cannot skip the animation");
        }
        Console.WriteLine(checks+" Transformer contact-climb and takeoff checks passed; character pose needs game testing.");
    }
}
