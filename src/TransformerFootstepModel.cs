using System;

namespace Rachel350Z {
public struct TransformerFootstepEvent {
    public bool Active;
    public int Clip;
    public float Gain,Pitch;
}
// Landings come exclusively from the gait. Sampling while muted/blocked keeps
// old contacts from becoming a burst when the listener returns.
public sealed class TransformerFootstepModel {
    readonly Random random;
    readonly int[] bag;
    int cursor,last=-1;
    uint left,right;
    bool sampled;
    public TransformerFootstepEvent Left {get;private set;}
    public TransformerFootstepEvent Right {get;private set;}
    public TransformerFootstepModel(int clipCount,int seed=0) {
        if(clipCount<0||clipCount>16)throw new ArgumentOutOfRangeException(nameof(clipCount));
        bag=new int[clipCount];cursor=bag.Length;
        random=seed==0?new Random():new Random(seed);
    }
    public void Step(float dt,bool enabled,bool blocked,uint leftFootfalls,uint rightFootfalls,float runBlend,float volume) {
        Left=Right=default(TransformerFootstepEvent);
        bool baseline=!sampled||leftFootfalls<left||rightFootfalls<right;
        bool leftLanded=leftFootfalls!=left,rightLanded=rightFootfalls!=right;
        left=leftFootfalls;right=rightFootfalls;sampled=true;
        if(baseline||!enabled||blocked||bag.Length==0||!Finite(dt)||dt<=0||!Finite(runBlend)||!Finite(volume)||volume<=0)return;
        float run=Clamp(runBlend),master=Clamp(volume);
        // A bounded pair per frame retains both simultaneous contacts without
        // replaying an arbitrary counter jump as a backlog of stacked sounds.
        if(leftLanded)Left=Next(run,master);
        if(rightLanded)Right=Next(run,master);
    }
    TransformerFootstepEvent Next(float run,float master) {
        if(cursor>=bag.Length) {
            for(int i=0;i<bag.Length;i++)bag[i]=i;
            for(int i=bag.Length-1;i>0;i--){int j=random.Next(i+1),swap=bag[i];bag[i]=bag[j];bag[j]=swap;}
            if(bag.Length>1&&bag[0]==last){int j=random.Next(1,bag.Length),swap=bag[0];bag[0]=bag[j];bag[j]=swap;}
            cursor=0;
        }
        last=bag[cursor++];
        return new TransformerFootstepEvent{Active=true,Clip=last,
            Gain=1.05f*master*(.70f+.20f*run)*(.94f+.06f*(float)random.NextDouble()),
            Pitch=1f-.025f*run+((float)random.NextDouble()-.5f)*.04f};
    }
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    static float Clamp(float value)=>Math.Max(0,Math.Min(1,value));
    public void Reset(){sampled=false;left=right=0;Left=Right=default(TransformerFootstepEvent);}
}
}
