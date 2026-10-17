using System;
namespace Rachel350Z {
// Cosmetic idle vapour only; no coupling to handling or engine/audio state.
public static class ExhaustSmokeRules {
    public static float Rate(bool running,float speed,float throttle,bool blocked) {
        if(!running||blocked||float.IsNaN(speed)||float.IsInfinity(speed)||float.IsNaN(throttle)||float.IsInfinity(throttle))return 0;
        return 6*Math.Max(0,1-Math.Abs(speed)/3)*Math.Max(0,1-Math.Abs(throttle)/.2f);
    }
}
}
