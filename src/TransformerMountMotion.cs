using System;
namespace Rachel350Z {
// Frame-rate independent timing only. Unity/player ownership stays with the cabin driver.
public struct TransformerMountSample {
    public float Progress,Travel,Lift,SeatBlend,PitchDegrees,Tuck,Crouch;
}
public struct TransformerClimbSample {
    public int FromLevel,ToLevel;
    public float LeftHand,RightHand,LeftFoot,RightFoot,Pull;
    public bool Pulling=>Pull>0&&Pull<1;
}
public sealed class TransformerMountMotion {
    public const float EntryDuration=8f,ExitDuration=2.15f;
    public const float ClimbApproachEnd=.22f,ClimbSeatStart=.90f;
    public const float DismountTakeoff=.30f,DismountLanding=.90f;
    public bool Started {get;private set;}
    public bool Entering {get;private set;}
    public bool Complete {get;private set;}
    public bool Active=>Started&&!Complete;
    public float Progress {get;private set;}
    public TransformerMountSample Pose=>Sample(Progress,Entering);
    // Seat translation is separate from pose blending: retract behind the
    // helmet before rising, and lower before moving forward when sitting.
    public static float SeatForward(float seatBlend)=>Smooth((seatBlend-.50f)/.50f);
    public static float SeatLower(float seatBlend)=>Smooth(seatBlend/.75f);
    public bool Begin(bool entering) {
        if(Active)return false;
        Started=true;Entering=entering;Complete=false;Progress=0;return true;
    }
    public void Step(float dt,bool blocked) {
        if(!Active||blocked||!Finite(dt)||dt<=0)return;
        Progress=Math.Min(1,Progress+Math.Min(dt,.1f)/(Entering?EntryDuration:ExitDuration));
        Complete=Progress>=1;
    }
    public static TransformerMountSample Sample(float progress,bool entering) {
        float p=Finite(progress)?Clamp(progress):0;
        var s=new TransformerMountSample{Progress=p};
        if(entering) {
            s.Travel=Clamp((p-ClimbApproachEnd)/(ClimbSeatStart-ClimbApproachEnd));
            s.SeatBlend=Smooth((p-ClimbSeatStart)/(1-ClimbSeatStart));
        } else {
            s.Travel=Clamp((p-DismountTakeoff)/(DismountLanding-DismountTakeoff));
            s.SeatBlend=1-Smooth(p/.20f);
            // This flat-height preview is informational. The actor samples the
            // same arc with the actual platform/ground heights, rather than
            // adding a sine on top of a diagonal four-metre descent.
            s.Lift=TransformerDismountArc.Height(s.Travel,0,0);
            // With +Z facing, -90 X turns the head (local up) rearward toward -Z.
            // Keep an unwrapped angle: quaternion interpolation would choose the short path.
            float flip=Smoother((p-.34f)/.52f);
            s.PitchDegrees=-360*flip;
            // Load, close the shape decisively, hold through the fast middle
            // rotation, then visibly extend before the feet meet the ground.
            s.Tuck=Smooth((p-.38f)/.12f)*(1-Smooth((p-.66f)/.16f));
            s.Crouch=p>=.20f&&p<.30f?Smooth((p-.20f)/.10f):p>=.30f&&p<.40f?1-Smooth((p-.30f)/.10f):p>.90f?Arc((p-.90f)/.10f):0;
        }
        return s;
    }
    // Each ratchet moves one extremity at a time. Once both hands have found
    // their grips, the torso rises through the steps instead of snapping up
    // during the last third of each cycle. Two hands plus one boot stay fixed.
    public static TransformerClimbSample Climb(float progress,int levels) {
        levels=Math.Max(1,levels);
        float q=Clamp((Clamp(progress)-ClimbApproachEnd)/(ClimbSeatStart-ClimbApproachEnd))*levels;
        int to=Math.Min(levels-1,(int)q);float cycle=q>=levels?1:q-to;
        return new TransformerClimbSample {
            FromLevel=to-1,ToLevel=to,
            LeftHand=Smooth(cycle/.16f),RightHand=Smooth((cycle-.16f)/.16f),
            LeftFoot=Smooth((cycle-.32f)/.16f),RightFoot=Smooth((cycle-.48f)/.16f),
            Pull=Smooth((cycle-.32f)/.68f)
        };
    }
    static float Arc(float p)=>p<=0||p>=1?0:(float)Math.Sin(Math.PI*p);
    static float Smooth(float p){p=Clamp(p);return p*p*(3-2*p);}
    static float Smoother(float p){double t=Clamp(p);return (float)(t*t*t*(t*(t*6-15)+10));}
    static float Clamp(float p)=>Math.Max(0,Math.Min(1,p));
    static bool Finite(float p)=>!float.IsNaN(p)&&!float.IsInfinity(p);
}
}
