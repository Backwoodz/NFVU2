using System;

namespace Rachel350Z {
// NFVU2 listening perspective, not a recovered NFS mixer. The original engine
// recordings contain a combined sound; separate front/rear filters give
// those shared recordings different engine-bay and exhaust characters.
public sealed class VehicleSoundPerspective {
    float frontness;
    public float EngineGain => (float)Math.Sqrt(.5f+.3f*frontness);
    public float ExhaustGain => (float)Math.Sqrt(.5f-.3f*frontness);
    public float TurboGain => .75f+.25f*frontness;
    public void Step(float dt,float listenerDirection) {
        if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0)return;
        if(float.IsNaN(listenerDirection)||float.IsInfinity(listenerDirection))listenerDirection=0;
        listenerDirection=DrivingMath.Clamp(listenerDirection,-1,1);
        frontness+=(listenerDirection-frontness)*(1-(float)Math.Exp(-7*Math.Min(dt,.1f)));
    }
}
}
