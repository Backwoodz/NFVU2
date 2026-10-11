using System;
namespace Rachel350Z {
public sealed class HordeFrostTimer {
    public const float Duration=1.2f,Immunity=2.4f;
    public float Left {get;private set;}
    public float Cooldown {get;private set;}
    public bool TryFreeze(){if(Cooldown>0)return false;Left=Duration;Cooldown=Duration+Immunity;return true;}
    public void Tick(float dt){if(dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;Left=Math.Max(0,Left-dt);Cooldown=Math.Max(0,Cooldown-dt);}
}
public static class HordeLightningRules {
    public const int MaxJumps=3;
    public const float Range=8,Interval=.4f;
    public static float Damage(int jump)=>jump<0||jump>MaxJumps?0:18*(float)Math.Pow(.7,jump);
}
}
