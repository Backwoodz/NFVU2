using System;

namespace Rachel350Z {
// The reverse layer has its own clock: progress runs from one (robot) to zero
// (car). ReturnRequested is deliberately insufficient to start this layer.
public sealed class TransformerReturnSoundModel {
    public const float TransformSeconds=5f;
    readonly float clipLength,tailLength;
    bool haveProgress,live,finished,completing,firstTailFrame;
    float previousProgress,tailAge;
    public bool Playing {get;private set;}
    public bool Paused {get;private set;}
    public float Position {get;private set;}
    public float Gain {get;private set;}
    public TransformerReturnSoundModel(float clipSeconds) {
        if(!Finite(clipSeconds)||clipSeconds<=0||clipSeconds>6)throw new ArgumentOutOfRangeException(nameof(clipSeconds));
        clipLength=clipSeconds;
        tailLength=Math.Max(0,Math.Min(.3f,clipSeconds-TransformSeconds));
    }
    // Called only by the natural Returning -> car transition, before the
    // transformer restores its car state. Public Restore/Stop never arm this.
    public void CompleteReturn() {
        if(completing)return;
        if(!live||finished||tailLength<=0){Reset();return;}
        completing=firstTailFrame=true;tailAge=0;
    }
    public void Step(float dt,bool active,bool returning,float progress,bool blocked,float volume) {
        Playing=Paused=false;Gain=Finite(volume)?Math.Max(0,Math.Min(1,volume)):0;
        bool frozen=blocked||!Finite(dt)||dt<=0;
        if(completing) {
            // A direction change or renewed robot form cancels the closing
            // ring. Arbitrary inactive ticks cannot create this authorized tail.
            if(active){Reset();return;}
            if(firstTailFrame)firstTailFrame=false;
            else if(!frozen)tailAge+=dt;
            if(tailAge>=tailLength){Reset();return;}
            Position=TransformSeconds+tailAge;Playing=true;Paused=frozen;return;
        }
        if(!active||!returning||!Finite(progress)||progress<0||progress>1){Reset();return;}
        Position=(1-progress)*TransformSeconds;
        // Short optional WAVs finish once rather than replaying their final
        // sample; they cannot invent a ring at the five-second endpoint.
        if(Position>=clipLength||progress==0)finished=true;
        if(!finished) {
            live=Playing=true;
            Paused=frozen||(haveProgress&&progress==previousProgress);
        }
        haveProgress=true;previousProgress=progress;
    }
    public void Reset(){haveProgress=live=finished=completing=firstTailFrame=false;tailAge=previousProgress=Position=Gain=0;Playing=Paused=false;}
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
}
}
