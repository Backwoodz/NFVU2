using System;

namespace Rachel350Z {
// This layer follows the existing five-second transformation clock; it never
// advances the animation or schedules delayed mechanical events of its own.
public sealed class TransformerDeploySoundModel {
    public const float TransformSeconds=5f;
    readonly float tailLength;
    bool live,haveProgress,finished;
    float previousProgress,tailAge;
    public bool Playing {get;private set;}
    public bool Paused {get;private set;}
    public float Position {get;private set;}
    public float Gain {get;private set;}
    public TransformerDeploySoundModel(float clipSeconds) {
        if(!Finite(clipSeconds)||clipSeconds<=0||clipSeconds>6)throw new ArgumentOutOfRangeException(nameof(clipSeconds));
        tailLength=Math.Max(0,Math.Min(.3f,clipSeconds-TransformSeconds));
    }
    public void Step(float dt,bool active,bool returning,float progress,bool blocked,float volume) {
        Playing=Paused=false;Gain=Finite(volume)?Math.Max(0,Math.Min(1,volume)):0;
        if(!active||returning||!Finite(progress)||progress<0||progress>1){Reset();return;}
        bool frozen=blocked||!Finite(dt)||dt<=0;
        if(progress<1) {
            // A saved/parked robot is first observed at one, so it cannot start
            // a soundtrack. Any real forward deployment is observed below one.
            if(!live){live=true;finished=false;tailAge=0;}
            bool stalled=haveProgress&&progress==previousProgress;
            Position=progress*TransformSeconds;
            Playing=true;Paused=frozen||stalled;
        } else if(live) {
            if(!finished&&frozen){live=false;haveProgress=true;previousProgress=progress;Position=0;return;}
            if(!finished) {
                finished=true;
                // The animation clamps its own frame step. Raw wall/frame dt
                // cannot tell how far past the end pose it advanced, so begin
                // the recorded ring at the observed authoritative end pose.
                tailAge=0;
            } else if(!frozen)tailAge+=dt;
            if(tailLength<=0||tailAge>=tailLength){live=false;Position=0;}
            else {Position=TransformSeconds+tailAge;Playing=true;Paused=frozen;}
        }
        haveProgress=true;previousProgress=progress;
    }
    public void Reset(){live=haveProgress=finished=false;tailAge=previousProgress=Position=Gain=0;Playing=Paused=false;}
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
}
}
