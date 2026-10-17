using System;
namespace Rachel350Z {
public struct TransformerCinematicPose {
    public float Pullback,Dolly,Rise,YawBlend,Orbit,PitchBlend,Zoom,Impulse;
}
// A shot follows authoritative deployment progress, never a second animation
// clock. Only the short handoff after completion/manual takeover uses time.
public sealed class TransformerCinematicCamera {
    public const float ReleaseSeconds=.85f;
    float progress,releaseAge;
    public bool Active {get;private set;}
    public bool Releasing {get;private set;}
    public float Weight=>Active?1:Releasing?1-Smooth(releaseAge/ReleaseSeconds):0;
    public TransformerCinematicPose Pose=>Sample(progress);
    public void Begin(){Reset();Active=true;}
    public void Cancel(){if(!Active)return;Active=false;Releasing=true;releaseAge=0;}
    public void Reset(){Active=Releasing=false;progress=releaseAge=0;}
    public void Step(float dt,bool deployed,bool returning,float phase,bool blocked) {
        if(!Active&&!Releasing)return;
        if(Active&&(!deployed||returning||!Finite(phase)||phase<progress-.0001f||phase<0||phase>1)){Cancel();return;}
        if(blocked||!Finite(dt)||dt<=0)return;
        if(Active) {
            progress=phase;
            if(progress>=1)Cancel();
        } else {
            releaseAge=Math.Min(ReleaseSeconds,releaseAge+Math.Min(dt,.1f));
            if(releaseAge>=ReleaseSeconds)Releasing=false;
        }
    }
    public static TransformerCinematicPose Sample(float phase) {
        float p=Finite(phase)?Clamp(phase):0;
        // The existing late sub-drop enters at 3.75 s of the five-second clip.
        float impact=Clamp((p-.75f)/.08f);
        return new TransformerCinematicPose {
            Pullback=Smooth(p/.22f),Dolly=Smooth((p-.32f)/.68f),
            Rise=Smooth((p-.04f)/.80f),YawBlend=Smooth(p/.65f),
            Orbit=45*Smooth((p-.25f)/.75f),PitchBlend=Smooth(p/.85f),
            Zoom=5*Smooth((p-.32f)/.68f),
            Impulse=impact<=0||impact>=1?0:.20f*(float)Math.Sin(impact*4*Math.PI)*(1-impact)*(1-impact)
        };
    }
    static float Smooth(float x){double t=Clamp(x);return (float)Math.Max(0,Math.Min(1,t*t*t*(10+t*(-15+6*t))));}
    static float Clamp(float x)=>Math.Max(0,Math.Min(1,x));
    static bool Finite(float x)=>!float.IsNaN(x)&&!float.IsInfinity(x);
}
}
