using System;
using System.Linq;
using Rachel350Z;
static class Program {
    static int checks;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    static void Main() {
        foreach(bool parkade in new[]{false,true}) {
            var spots=DriftLandscapePlan.Spots.Where(p=>p.Parkade==parkade).ToArray();
            int trees=spots.Count(p=>p.Tree);
            Check(trees>=(parkade?900:2000)&&trees<=9000,"continuous dense forest with bounded size: "+trees);
            Check(spots.Count(p=>!p.Tree)>=12,"low foreground groups accompany trees");
            Check(spots.Any(p=>p.Prefab=="FirTree")&&spots.Any(p=>p.Prefab=="PineTree"),"mixed native conifers");
            var b=parkade?ParkadeLayout.Bounds:DriftCircuitLayout.Bounds;
            Check(spots.Any(p=>p.X<b.MinX)&&spots.Any(p=>p.X>b.MaxX)&&spots.Any(p=>p.Z<b.MinZ)&&spots.Any(p=>p.Z>b.MaxZ),"forest surrounds every side of venue");
            foreach(var p in spots) {
                Check(DriftLandscapePlan.Clear(p.X,p.Z,p.Radius,parkade,p.Tree?DriftLandscapePlan.TreeClearance:DriftLandscapePlan.DetailClearance),"entire crown clears structures, lanes, stages and paddock");
                Check(p.Height>0&&p.Radius>0&&p.Yaw>=0&&p.Yaw<=360,"finite positive asset dimensions");
                var road=parkade?ParkadeLayout.Project(p.X,p.Z):DriftCircuitLayout.Project(p.X,p.Z);
                Check(road.SignedEdgeDistance-p.Radius>=DriftLandscapePlan.DetailClearance,"all foliage clears road and runoff");
                if(p.Tree)Check(road.SignedEdgeDistance-p.Radius>=DriftLandscapePlan.TreeClearance,"tree crowns preserve viewing and runoff strip");
                foreach(int dx in new[]{-1,0,1})foreach(int dz in new[]{-1,0,1}) {
                    float x=p.X+dx*p.Radius,z=p.Z+dz*p.Radius;
                    Check(!(parkade?ParkadeLayout.Contains(x,z,8):DriftCircuitLayout.Contains(x,z,8)),"bounds samples cannot reach road or immediate roadside decor");
                }
            }
            var forest=spots.Where(p=>p.Tree).ToArray();
            int dense=forest.Count(p=>forest.Count(q=>q.X!=p.X||q.Z!=p.Z?(q.X-p.X)*(q.X-p.X)+(q.Z-p.Z)*(q.Z-p.Z)<15*15:false)>=4);
            Check(dense>forest.Length*.92f,"at least 92 percent of trees have four neighbours within fifteen metres: "+dense+"/"+forest.Length);
            Check(forest.Count(p=>(parkade?ParkadeLayout.Project(p.X,p.Z):DriftCircuitLayout.Project(p.X,p.Z)).SignedEdgeDistance>95)>forest.Length*.15f,"deep forest extends beyond first few rows");
            // Every route segment sees a nearby part of its own forest. This
            // also catches accidentally leaving an entire side empty.
            float length=parkade?ParkadeLayout.TotalLength:DriftCircuitLayout.TotalLength;
            for(int i=0;i<100;i++) {
                var p=parkade?ParkadeLayout.AtDistance(length*i/100):DriftCircuitLayout.AtDistance(length*i/100);
                float distance=(float)Math.Sqrt(spots.Where(s=>s.Tree).Min(s=>(s.X-p.X)*(s.X-p.X)+(s.Z-p.Z)*(s.Z-p.Z)));
                Check(distance<DriftLandscapePlan.ViewDistance,"forest backdrop stays within view around whole lap: "+parkade+" / "+i+" / "+distance);
            }
            Console.WriteLine((parkade?"Parkade":"Practice")+": "+trees+" trees, "+spots.Count(p=>!p.Tree)+" foreground groups");
        }
        Check(!DriftLandscapePlan.Clear(148,-8,6,false),"arrival and parking stay open");
        Check(!DriftLandscapePlan.Clear(0,0,6,false),"original player spawn stays open");
        Check(!DriftLandscapePlan.Clear(ParkadeLayout.StartX,ParkadeLayout.StartZ,6,true),"event start stays open");
        Check(ParkadeServiceYardPlan.Yards.Count==3,"three separate maintenance yards");
        foreach(var yard in ParkadeServiceYardPlan.Yards) {
            Check(ParkadePlan.ClearBox(yard.X,yard.Z,yard.Yaw,ParkadeServiceYardPlan.HalfX,ParkadeServiceYardPlan.HalfZ,8),"whole maintenance yard clears every lane");
            Check(!DriftLandscapePlan.Clear(yard.X,yard.Z,9,true),"forest leaves maintenance yards open");
        }
        Check(ParkadeServiceYardPlan.Pieces.Count<=150,"bounded native yard props");
        foreach(string prefab in new[]{"piece_chest_wood","barrell","wood_stack","wood_roof"})Check(ParkadeServiceYardPlan.Pieces.Any(p=>p.Prefab==prefab),"yard includes "+prefab);
        Check(DriftLandscapePlan.Spots.Select(p=>p.X+","+p.Z).Distinct().Count()==DriftLandscapePlan.Spots.Count,"no duplicate landscape roots");
        Console.WriteLine(checks+" landscape clearance, coverage and budget checks passed.");
    }
}
