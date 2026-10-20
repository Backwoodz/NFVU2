using System;
using System.Linq;
using Rachel350Z;
static class Program {
    static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static void Main() {
        Check(ParkadeFestivalPlan.Terraces.Count>=20&&ParkadeFestivalPlan.Terraces.Count<=40,"bounded crowd uses existing roofs");
        Check(ParkadeFestivalPlan.Terraces.All(p=>p.Prefab=="stone_floor"&&ParkadePlan.Pieces.Contains(p)),"all terraces already exist in the saved plan");
        Check(ParkadeFestivalPlan.Trolls.Count==2,"exactly two giant fans find safe roadside sites (found "+ParkadeFestivalPlan.Trolls.Count+")");
        foreach(var troll in ParkadeFestivalPlan.Trolls) {
            Check(ParkadePlan.ClearBox(troll.X,troll.Z,0,4,4,7),"entire giant footprint clears every driving lane by seven metres");
            Check(troll.Y>=ParkadeLayout.TerrainHeightAt(troll.X,troll.Z),"supported fan stage never buries giants below terrain");
            var road=ParkadeLayout.Project(troll.X,troll.Z);
            Check(troll.Y>=ParkadeLayout.HeightAtDistance(road.DistanceAlong)-1.5f,"giants remain visible above raised road and walls");
        }
        Check(ParkadeFestivalPlan.Boards.Count>=6&&ParkadeFestivalPlan.Boards.Count<=10,"all six marks have bounded board locations");
        foreach(var board in ParkadeFestivalPlan.Boards)
            Check(ParkadePlan.ClearBox(board.X,board.Z,board.Yaw,3.1f,.3f,1),"sponsor boards stay off the road");
        Check(ParkadeFestivalPlan.Lamps.Count>=18&&ParkadeFestivalPlan.Lamps.Count<=24,"continuous but bounded illumination");
        foreach(var lamp in ParkadeFestivalPlan.Lamps)
            Check(ParkadePlan.ClearBox(lamp.X,lamp.Z,0,.4f,.4f,1.3f),"lamp mast clears all lanes at corners");
        for(int i=0;i<600;i++) {
            var p=ParkadeLayout.AtDistance(ParkadeLayout.TotalLength*i/600);
            var nearest=ParkadeFestivalPlan.NearestLights(p.X,p.Z);
            Check(nearest.Length<=4&&nearest.Distinct().Count()==nearest.Length,"light cap and distinct choices");
            float previous=0;
            foreach(int index in nearest) {
                var lamp=ParkadeFestivalPlan.Lamps[index];float dx=p.X-lamp.X,dz=p.Z-lamp.Z,d=dx*dx+dz*dz;
                Check(d>=previous&&d<=65*65,"nearest lamps are sorted and within range");previous=d;
            }
        }
        Check(ParkadeFestivalPlan.NearestLights(0,0).Length==0,"practice area receives no remote festival lights");
        Check(ParkadeFestivalPlan.NearestLights(float.NaN,0).Length==0,"invalid camera position is harmless");
        Console.WriteLine(checks+" festival geometry and light-budget checks passed.");
    }
}
