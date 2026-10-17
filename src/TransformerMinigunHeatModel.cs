using System;
namespace Rachel350Z {
// Cosmetic thermal history only: no fire/ammunition/input authority.
public sealed class TransformerMinigunHeatModel {
    public const float VisibleAfterSeconds=2,FullAfterSeconds=6,HoldSeconds=2,CoolSeconds=5;
    float load,idle;
    public float Heat {
        get {
            float t=Math.Max(0,Math.Min(1,(load-VisibleAfterSeconds)/(FullAfterSeconds-VisibleAfterSeconds)));
            return t*t*(3-2*t);
        }
    }
    public void Step(float dt,int shots) {
        if(float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0)return;
        dt=Math.Min(dt,.1f);
        if(shots>0) {
            // One native 600-rpm round contributes 0.1 seconds of firing.
            // Match the weapon's bounded two-round per-frame dispatcher.
            load=Math.Min(FullAfterSeconds,load+Math.Min(shots,2)*.1f);idle=0;
        } else {
            float previous=Math.Max(0,idle-HoldSeconds);
            idle=Math.Min(20,idle+dt);
            float cooling=Math.Max(0,idle-HoldSeconds)-previous;
            load=Math.Max(0,load-cooling*(FullAfterSeconds-VisibleAfterSeconds)/CoolSeconds);
        }
    }
    public void Reset(){load=idle=0;}
    // Source-space Z stays valid when the actual barrels rotate about +Z.
    public static float FrontMask(float sourceZ,float maximumZ) {
        if(float.IsNaN(sourceZ)||float.IsInfinity(sourceZ)||float.IsNaN(maximumZ)||float.IsInfinity(maximumZ)||maximumZ<=.30f)return 0;
        float t=Math.Max(0,Math.Min(1,(sourceZ-.30f)/Math.Max(.01f,maximumZ-.34f)));
        return t*t*(3-2*t);
    }
}
}
