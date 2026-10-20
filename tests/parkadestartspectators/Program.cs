using System;
using Rachel350Z;

static class Program {
    static int checks;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    static void Main() {
        var spots=ParkadeStartSpectatorPlan.Spots;
        Check(spots.Count==8,"eight bounded extra start-line spectators");
        int left=0,right=0,dancing=0;
        foreach(var p in spots) {
            if(p.Across<0)left++;else right++;
            if(p.Dancing)dancing++;
            Check(Math.Abs(p.Across)>=13,"crowd remains behind both barrier lines");
            Check(ParkadePlan.ClearBox(p.X,p.Z,p.Yaw,.9f,.9f,2.1f),"entire rotated viewing plinth stays away from road: "+p.Forward+", "+p.Across);
            Check(p.Y>=ParkadeLayout.HeightAtDistance(ParkadeLayout.Project(p.X,p.Z).DistanceAlong)+.14f,"viewers stand high enough to see over barriers");
            Check(p.Y-p.GroundY>1&&p.Y-p.GroundY<5.5f,"bounded viewing plinth extends to real sloped terrain");
            Check(p.Phase>=0&&p.Phase<1,"native idle phase valid");
            double yaw=p.Yaw*Math.PI/180,dx=ParkadeLayout.StartX-ParkadeLayout.StartTangentX*3-p.X,
                dz=ParkadeLayout.StartZ-ParkadeLayout.StartTangentZ*3-p.Z;
            Check((Math.Sin(yaw)*dx+Math.Cos(yaw)*dz)/Math.Sqrt(dx*dx+dz*dz)>.999,"spectators face waiting car");
            // The plinth uses native stone material without native colliders;
            // nevertheless avoid visually merging with existing stone piers.
            foreach(var piece in ParkadePlan.Pieces) {
                if(piece.HeightOffset>2)continue;
                double a=piece.Yaw*Math.PI/180,c=Math.Cos(a),s=Math.Sin(a);
                double ex=p.X-piece.X,ez=p.Z-piece.Z;
                double localX=c*ex-s*ez,localZ=s*ex+c*ez;
                double hx=piece.Prefab=="stone_floor"?2:piece.Prefab=="stone_wall_2x1"?1:.55;
                double hz=piece.Prefab=="stone_floor"?2:piece.Prefab=="stone_wall_2x1"?.5:.55;
                double gx=Math.Max(0,Math.Abs(localX)-hx),gz=Math.Max(0,Math.Abs(localZ)-hz);
                Check(Math.Sqrt(gx*gx+gz*gz)>1.28,"crowd plinth visually clears native building pieces: "+p.Forward+", "+p.Across+" vs "+piece.Prefab+" "+piece.X+", "+piece.Z);
            }
            foreach(var q in spots) {
                if(p.X==q.X&&p.Z==q.Z)continue;
                Check((p.X-q.X)*(p.X-q.X)+(p.Z-q.Z)*(p.Z-q.Z)>9,"crowd rigs and plinths do not overlap");
            }
        }
        Check(left==4&&right==4,"both sides of start are populated");
        Check(dancing>0&&dancing<8,"mixed native idle and dancing crowd");
        Console.WriteLine(checks+" Parkade start spectator layout checks passed.");
    }
}
