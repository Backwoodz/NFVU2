using System;
using System.Collections.Generic;

namespace Rachel350Z {
// One arrival belongs to a loaded world, not to a particular Player instance.
// Death, dismounting and vehicle removal must never re-trigger it.
internal sealed class DriftArrivalState {
    object world;
    public bool Pending {get;private set;}
    public bool Prepared {get;set;}
    public bool LoadRequested {get;set;}
    public void Observe(object next) {
        if(ReferenceEquals(world,next))return;
        world=next;Pending=false;Prepared=false;LoadRequested=false;
    }
    public void Begin(object next,bool initialSpawn,bool active) {
        Observe(next);
        if(active&&initialSpawn&&!Prepared)Pending=true;
    }
    public void Complete(){Pending=false;Prepared=true;}
}
internal readonly struct DriftArrivalCandidate {
    public readonly bool Hummer,Driven;
    public readonly float DistanceSquared;
    public DriftArrivalCandidate(bool hummer,bool driven,float distanceSquared) {
        Hummer=hummer;Driven=driven;DistanceSquared=distanceSquared;
    }
}
internal static class DriftParkingPlan {
    public const float X=148,Z=-8,Yaw=90;
    // A driven 350Z is preferred, then the closest saved car. Stable ties keep
    // the first record, so repeated loading never alternates between two cars.
    public static int Choose(IReadOnlyList<DriftArrivalCandidate> cars) {
        int best=-1;
        for(int i=0;i<cars.Count;i++) {
            var next=cars[i];
            if(next.Hummer||float.IsNaN(next.DistanceSquared)||float.IsInfinity(next.DistanceSquared)||next.DistanceSquared<0)continue;
            if(best<0||next.Driven&&!cars[best].Driven||next.Driven==cars[best].Driven&&next.DistanceSquared<cars[best].DistanceSquared)best=i;
        }
        return best;
    }
}
}
