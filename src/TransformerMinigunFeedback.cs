using System;
namespace Rachel350Z {
public struct TransformerMinigunHit {
    public string Name;
    public float Damage,Health,MaximumHealth;
}
// One bounded readout for actual health loss, independent of vanilla's short
// damage-text viewing distance. A dispatch/blocked hit alone cannot confirm it.
public sealed class TransformerMinigunFeedback {
    public const float Lifetime=2.5f;
    float remaining;
    public TransformerMinigunHit Hit {get;private set;}
    public float Distance {get;private set;}
    public bool Visible=>remaining>0;
    public bool Flash=>remaining>Lifetime-.18f;
    public void Step(float dt,bool enabled) {
        if(!enabled||!Finite(dt)||dt<0){Reset();return;}
        remaining=Math.Max(0,remaining-Math.Min(dt,.1f));
        if(remaining==0){Hit=default(TransformerMinigunHit);Distance=0;}
    }
    public void Observe(TransformerMinigunHit hit,float distance) {
        if(!Finite(hit.Damage)||!Finite(hit.Health)||!Finite(hit.MaximumHealth)||!Finite(distance)
            ||hit.Damage<=0||hit.Health<0||hit.MaximumHealth<=0||hit.Health>hit.MaximumHealth||distance<0)return;
        Hit=hit;Distance=distance;remaining=Lifetime;
    }
    public void Reset(){remaining=Distance=0;Hit=default(TransformerMinigunHit);}
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
}
}
