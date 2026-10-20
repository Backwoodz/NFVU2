using System;
using System.Linq;
using Rachel350Z;

static class Program {
    static int checks;
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
    static void Main() {
        Check(DriftCircuitAtmospherePlan.Spectators.Count==34,"18 grandstand and 16 trackside spectators");
        Check(DriftCircuitAtmospherePlan.Spectators.Count(s=>s.Grandstand)==18,"grandstand bench count preserved");
        foreach(var p in DriftCircuitAtmospherePlan.Spectators) {
            Check(!DriftCircuitLayout.Contains(p.X,p.Z,5),"spectators outside driving surface and runoff");
            Check(!DriftCircuitPlan.PitOpening(p.X,p.Z),"spectators do not occupy paddock entrances");
            Check(p.Phase>=0&&p.Phase<1,"idle phases vary safely");
            Check(p.Y>=0&&p.Y<=2.25f,"spectators stay on terrace levels");
        }
        foreach(var p in DriftCircuitAtmospherePlan.Lamps) {
            Check(!DriftCircuitLayout.Contains(p.X,p.Z,4),"lantern post remains beyond driving surface");
            Check(!DriftCircuitPlan.PitOpening(p.X,p.Z),"lanterns leave paddock entry open");
        }
        Check(DriftCircuitAtmospherePlan.LineX==DriftTrackLayout.StartX&&DriftCircuitAtmospherePlan.LineZ==DriftTrackLayout.StartZ,"physical start and scoring origin agree");
        for(int i=0;i<1500;i++) {
            var sample=DriftCircuitLayout.AtDistance(DriftCircuitLayout.TotalLength*i/1500);
            var closest=DriftCircuitAtmospherePlan.NearestLights(sample.X,sample.Z);
            Check(closest.Length<=6&&closest.Length>0,"every road point has nearby lights within six-light budget");
            Check(closest.Distinct().Count()==closest.Length,"no light repeated in nearest set");
            float previous=-1;
            foreach(int index in closest) {
                var p=DriftCircuitAtmospherePlan.Lamps[index];
                float dx=p.X-sample.X,dz=p.Z-sample.Z,d=dx*dx+dz*dz;
                Check(d>=previous&&d<=105*105,"selected lights ordered by distance and within culling range");previous=d;
            }
            for(int index=0;index<DriftCircuitAtmospherePlan.Lamps.Count;index++) {
                var p=DriftCircuitAtmospherePlan.Lamps[index];float dx=p.X-sample.X,dz=p.Z-sample.Z;
                if(!closest.Contains(index)&&closest.Length==6)Check(dx*dx+dz*dz>=previous,"unselected light is no nearer than last selected");
            }
        }
        Check(DriftCircuitAtmospherePlan.NearestLights(10000,10000).Length==0,"distant player lights are off");
        Check(DriftCircuitAtmospherePlan.NearestLights(float.NaN,0).Length==0,"invalid viewer rejected");
        Check(DriftCircuitAtmospherePlan.NearestLights(0,float.PositiveInfinity).Length==0,"infinite viewer rejected");
        Console.WriteLine("Drift atmosphere plan: "+checks+" checks passed; "+DriftCircuitAtmospherePlan.Spectators.Count+" spectator spots, "+DriftCircuitAtmospherePlan.Lamps.Count+" lanterns, at most six active lights.");
    }
}
