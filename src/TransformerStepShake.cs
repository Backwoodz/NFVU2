using System;
namespace Rachel350Z {
// Small angular landing impulse, sampled after the gait. Counters preserve
// contacts across its substeps without inventing a separate footstep clock.
public sealed class TransformerStepShake {
    const float Duration=.24f;
    uint left,right;
    float age=Duration,strength,side;
    bool sampled;
    public float Pitch {get;private set;}
    public float Roll {get;private set;}
    public void Step(float dt,bool enabled,uint leftFootfalls,uint rightFootfalls,float runBlend) {
        bool landing=sampled&&(leftFootfalls!=left||rightFootfalls!=right);
        bool reset=leftFootfalls<left||rightFootfalls<right;
        bool rightLanded=rightFootfalls!=right;
        left=leftFootfalls;right=rightFootfalls;sampled=true;
        if(!enabled||reset||float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0||float.IsNaN(runBlend)||float.IsInfinity(runBlend)) {
            ClearPulse();return;
        }
        if(landing) {
            age=0;strength=1+.8f*Math.Max(0,Math.Min(1,runBlend));
            side=rightLanded?-1:1;
        }
        age=Math.Min(Duration,age+Math.Min(dt,.1f));
        float t=age/Duration;
        float pulse=(float)Math.Sin(t*2*Math.PI)*(1-t)*(1-t)*strength;
        Pitch=.25f*pulse;Roll=.12f*side*pulse;
    }
    void ClearPulse(){age=Duration;Pitch=Roll=0;}
    public void Stop()=>ClearPulse();
    public void Reset(){left=right=0;sampled=false;ClearPulse();}
}
}
