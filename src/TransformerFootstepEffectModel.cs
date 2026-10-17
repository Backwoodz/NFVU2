using System;

namespace Rachel350Z {
// Visual contacts consume the same authoritative counters as audio, without
// depending on audio voices, volume, wall-clock timers or locomotion input.
public sealed class TransformerFootstepEffectModel {
    public const int Capacity=32;
    public const float HoldSeconds=18,FadeSeconds=6;
    readonly float[] ages=new float[Capacity];
    uint left,right;
    bool observed;
    int next;
    public TransformerFootstepEffectModel(){for(int i=0;i<Capacity;i++)ages[i]=-1;}
    public int Observe(bool walking,bool blocked,uint leftCounter,uint rightCounter) {
        uint dl=unchecked(leftCounter-left),dr=unchecked(rightCounter-right);
        int contacts=observed&&walking&&!blocked?((dl>0&&dl<=4?1:0)|(dr>0&&dr<=4?2:0)):0;
        left=leftCounter;right=rightCounter;observed=true;
        // At most one current contact per foot. Missing frames, streaming or a
        // reset cannot create a burst of historical footfalls at the new pose.
        return contacts;
    }
    public int Allocate(){int slot=next;next=(next+1)%Capacity;ages[slot]=0;return slot;}
    public void Tick(float dt,bool blocked) {
        if(blocked||!Finite(dt)||dt<=0)return;
        for(int i=0;i<Capacity;i++)if(ages[i]>=0) {
            ages[i]+=dt;
            if(ages[i]>=HoldSeconds+FadeSeconds)ages[i]=-1;
        }
    }
    public float Alpha(int slot)=>ages[slot]<0?0:Math.Max(0,Math.Min(1,(HoldSeconds+FadeSeconds-ages[slot])/FadeSeconds));
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
}
}
