using System;
namespace Rachel350Z {
// A marked target rewards a single kill inside its deadline, never a later despawn.
public sealed class HordeBonus {
    public const float Duration=25;
    public float Left {get;private set;}
    public void Begin()=>Left=Duration;
    public void Clear()=>Left=0;
    public void Tick(float dt){if(dt>0&&!float.IsNaN(dt)&&!float.IsInfinity(dt))Left=Math.Max(0,Left-dt);}
    public bool Claim(){if(Left<=0)return false;Left=0;return true;}
}
}
