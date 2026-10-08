using System;
using System.Collections.Generic;

namespace Rachel350Z {
// Game-independent rules: only these destructible vegetation prefabs can be hit.
public static class VehicleImpactRules {
    static readonly HashSet<string> vegetation=new HashSet<string>(StringComparer.Ordinal) {
        "Beech_small1","Beech_small2","FirTree_small","FirTree_small_dead",
        "Bush01","Bush01_heath","Bush01_deepnorth","Bush02_en"
    };
    public static bool SmallVegetation(string name) {
        if(name==null)return false;
        const string clone="(Clone)";
        if(name.EndsWith(clone,StringComparison.Ordinal))name=name.Substring(0,name.Length-clone.Length);
        return vegetation.Contains(name);
    }
    public static float Damage(float closingSpeed,bool vegetation) {
        if(float.IsNaN(closingSpeed)||float.IsInfinity(closingSpeed)||closingSpeed<2.5f)return 0;
        float speed=Math.Min(closingSpeed,20);
        return vegetation?Math.Min(500,80+speed*speed*4):Math.Min(300,speed*speed*2);
    }
}
public sealed class VehicleImpactCooldown {
    readonly Dictionary<int,float> until=new Dictionary<int,float>();
    readonly List<int> expired=new List<int>();
    public bool TryHit(int id,float time) {
        if(float.IsNaN(time)||float.IsInfinity(time))return false;
        if(until.TryGetValue(id,out float expiry)&&time<expiry)return false;
        expired.Clear();
        foreach(var entry in until)if(entry.Value<=time)expired.Add(entry.Key);
        foreach(int key in expired)until.Remove(key);
        until[id]=time+.8f;return true;
    }
}
}
