using System;

namespace Rachel350Z {
public struct TransformerTurnEvent {
    public bool Active;
    public int Clip;
    public float Pitch;
}
// The caller measures root rotation, so held steering without root rotation
// cannot sound like moving joints. Direction changes preserve the current tail.
public sealed class TransformerTurnSoundModel {
    readonly float[] durations;
    readonly int[] bag;
    readonly Random random;
    int cursor,last=-1;
    float remaining,envelope;
    public TransformerTurnEvent Event {get;private set;}
    public float Gain {get;private set;}
    public bool ImmediateSilence {get;private set;}
    public TransformerTurnSoundModel(float[] clipDurations,int seed=0) {
        if(clipDurations==null||clipDurations.Length>16)throw new ArgumentOutOfRangeException(nameof(clipDurations));
        durations=(float[])clipDurations.Clone();
        foreach(float duration in durations)if(!Finite(duration)||duration<=0||duration>3)throw new ArgumentOutOfRangeException(nameof(clipDurations));
        bag=new int[durations.Length];cursor=bag.Length;
        random=seed==0?new Random():new Random(seed);
    }
    public void Step(float dt,bool enabled,bool blocked,float actualTurnDegreesPerSecond,float masterVolume) {
        Event=default(TransformerTurnEvent);ImmediateSilence=false;
        if(!enabled||blocked||!Finite(dt)||dt<=0||dt>.25f||!Finite(actualTurnDegreesPerSecond)||!Finite(masterVolume)||masterVolume<=0||bag.Length==0) {
            Reset();ImmediateSilence=true;return;
        }
        float speed=Math.Abs(actualTurnDegreesPerSecond);
        float target=speed>2?.2f+.8f*Clamp((speed-2)/43):0;
        // Linear ramps make release finish within 150ms even after full speed.
        float step=dt/(target>envelope?.08f:.15f);
        envelope=target>envelope?Math.Min(target,envelope+step):Math.Max(target,envelope-step);
        Gain=.34f*envelope*Clamp(masterVolume);
        // Preserve scheduling across a brief zero crossing while reversing.
        // A fully settled stop forgets it, so the next real turn starts promptly.
        if(target<=0){remaining=envelope<=0?0:Math.Max(0,remaining-dt);return;}
        remaining-=dt;
        if(remaining>0)return;
        if(cursor>=bag.Length) {
            for(int i=0;i<bag.Length;i++)bag[i]=i;
            for(int i=bag.Length-1;i>0;i--){int j=random.Next(i+1),swap=bag[i];bag[i]=bag[j];bag[j]=swap;}
            if(bag.Length>1&&bag[0]==last){int j=random.Next(1,bag.Length),swap=bag[0];bag[0]=bag[j];bag[j]=swap;}
            cursor=0;
        }
        last=bag[cursor++];float pitch=.975f+.05f*(float)random.NextDouble();
        Event=new TransformerTurnEvent{Active=true,Clip=last,Pitch=pitch};
        // No backlog: one new texture at most per frame. The minimum spacing
        // keeps even a pathological tiny optional sample from machine-gunning.
        remaining=Math.Max(.18f,durations[last]*.72f/pitch);
    }
    public void Reset(){remaining=envelope=Gain=0;Event=default(TransformerTurnEvent);}
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    static float Clamp(float value)=>Math.Max(0,Math.Min(1,value));
}
}
