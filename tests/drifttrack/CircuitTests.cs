using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Rachel350Z;
static class CircuitTests {
    static int checks;
    static void Check(bool ok,string text){checks++;if(!ok)throw new Exception("Circuit: "+text);}
    static float Distance(float x,float z,float xx,float zz)=>(float)Math.Sqrt((x-xx)*(x-xx)+(z-zz)*(z-zz));
    public static int Run() {
        var samples=DriftCircuitLayout.Samples;float total=DriftCircuitLayout.TotalLength;
        Check(total>1450&&total<1550,"a complete roughly 1.5 km lap");
        var start=samples[0];var end=samples[samples.Count-1];
        Check(Distance(start.X,start.Z,end.X,end.Z)<.001f&&start.TangentX==end.TangentX&&start.TangentZ==end.TangentZ,"closed seam has no position or heading jump");
        foreach(var p in DriftTrackLayout.Samples) {
            var v2=DriftCircuitLayout.AtDistance(p.DistanceAlong);
            Check(v2.X==p.X&&v2.Z==p.Z&&v2.HalfWidth==p.HalfWidth,"legacy road geometry remains exactly in place");
        }
        int left=0,right=0;
        foreach(var part in DriftCircuitLayout.Extension) {
            if(part.Turn>0)left++;if(part.Turn<0)right++;
            if(part.Turn!=0)Check(part.Length/Math.Abs(part.Turn)>=31.999,"new bend radius keeps the measured car/drift scale");
            var before=DriftCircuitLayout.AtDistance((float)part.StartDistance-.001f);
            var after=DriftCircuitLayout.AtDistance((float)part.StartDistance+.001f);
            Check(Distance(before.X,before.Z,after.X,after.Z)<.005f,"segment joins have no driving gap");
            Check(before.TangentX*after.TangentX+before.TangentZ*after.TangentZ>.9999f,"segment tangents agree");
        }
        Check(left>=2&&right>=6,"course includes opposite transitions and varied turns");
        for(int i=1;i<samples.Count;i++) {
            var p=samples[i];var a=samples[i-1];
            Check(p.DistanceAlong>a.DistanceAlong&&p.DistanceAlong-a.DistanceAlong<1.001f,"ordered metre sampling");
            Check(Distance(p.X,p.Z,a.X,a.Z)<1.001f,"no discontinuous centreline segment");
            Check(Math.Abs(p.TangentX*p.TangentX+p.TangentZ*p.TangentZ-1)<.0001f,"unit tangent");
            Check(p.Width>=16&&p.Width<=20,"wide drift corridor");
            Check(Distance(p.X,p.Z,0,0)>45,"racing line leaves world spawn clear");
        }
        // Distant sections may not overlap each other's driving corridors.
        float minimum=1000;
        for(int i=0;i<samples.Count;i+=4)for(int j=i+1;j<samples.Count;j+=4) {
            var a=samples[i];var b=samples[j];float along=b.DistanceAlong-a.DistanceAlong;
            if(Math.Min(along,total-along)<60)continue;
            float gap=Distance(a.X,a.Z,b.X,b.Z)-a.HalfWidth-b.HalfWidth;
            minimum=Math.Min(minimum,gap);
        }
        Check(minimum>12,"separate racing sections retain a generous clear gap: "+minimum);
        var allowed=new HashSet<string>{"wood_fence","wood_pole2","wood_pole","piece_banner01","piece_banner02","sign","piece_groundtorch",
            "stone_floor","stone_floor_2x2","piece_bench01","wood_wall_half","wood_stair","wood_floor","woodwall","wood_roof","wood_beam","piece_workbench","piece_chest_wood"};
        Check(DriftCircuitPieces.Items.Count>600&&DriftCircuitPieces.Items.Count<1200,"bounded, substantial native venue");
        foreach(var piece in DriftCircuitPieces.Items) {
            Check(allowed.Contains(piece.Prefab),"uses a locally verified native piece: "+piece.Prefab);
            Check(DriftCircuitLayout.Project(piece.X,piece.Z).SignedEdgeDistance>3,"decoration stays outside the full driving corridor: "+piece.Prefab+" "+piece.X+","+piece.Z);
            Check(piece.Text==null||piece.Text.Length<=50,"native sign length");
        }
        Check(DriftCircuitPieces.Items.Count(p=>p.Prefab=="piece_groundtorch")<=12,"limited number of native fire/light effects");
        Check(DriftCircuitPieces.Items.Count(p=>p.Prefab=="piece_bench01")>=18,"three spectator seating tiers");
        Check(DriftCircuitPieces.Items.Count(p=>p.Prefab=="wood_roof")>=30,"substantial covered workshop");
        Check(DriftCircuitPieces.Items.Any(p=>p.Prefab=="piece_workbench")&&DriftCircuitPieces.Items.Any(p=>p.Prefab=="piece_chest_wood"),"functional pit furnishings");
        foreach(var old in DriftTrackPieces.Items)
            Check(DriftCircuitLayout.Project(old.X,old.Z).SignedEdgeDistance>1,"new roadway does not run through old guide pieces");
        foreach(var s in DriftCircuitPlan.PaintStamps)
            Check(!DriftTrackConstructionPlan.IntersectsTile(s,0,0,32),"new paint leaves the spawn terrain tile untouched");
        for(float d=DriftTrackLayout.TotalLength;d<=total;d+=1) {
            var p=DriftCircuitLayout.AtDistance(d);
            foreach(float side in new[]{-.95f,.95f}) {
                float x=p.X-p.TangentZ*p.HalfWidth*side,z=p.Z+p.TangentX*p.HalfWidth*side;
                Check(DriftCircuitPlan.PaintStamps.Any(s=>Distance(x,z,s.X,s.Z)<=s.Radius),"full-width extension paving has no holes");
            }
        }
        foreach(var sign in DriftTrackPieces.Items.Where(p=>p.Prefab=="sign"&&DriftCircuitLabels.Upgrade(p.Text)!=null)) {
            Check(DriftCircuitLabels.Matches(sign,sign.Text,sign.X,sign.HeightOffset,sign.Z,sign.Yaw),"original sign can receive circuit label");
            Check(!DriftCircuitLabels.Matches(sign,sign.Text+"!",sign.X,sign.HeightOffset,sign.Z,sign.Yaw),"player-edited text preserved");
            Check(!DriftCircuitLabels.Matches(sign,sign.Text,sign.X+.1f,sign.HeightOffset,sign.Z,sign.Yaw),"player-moved sign preserved");
            Check(!DriftCircuitLabels.Matches(sign,sign.Text,sign.X,sign.HeightOffset,sign.Z,sign.Yaw+10),"player-rotated sign preserved");
        }
        Check(DriftCircuitLabels.Upgrade("NFVU2 DRIFT | RUNDE / LAP 1.49 km")==null,"upgraded label does not upgrade repeatedly");
        Export(samples);
        Console.WriteLine("Circuit: "+total.ToString("F2",CultureInfo.InvariantCulture)+" m, "+DriftCircuitPieces.Items.Count+" new pieces, minimum nonlocal edge gap "+minimum);
        return checks;
    }
    static void Export(IReadOnlyList<DriftTrackSample> samples) {
        using(var file=new StreamWriter(Path.Combine(AppContext.BaseDirectory,"circuit-plan.csv"))) {
            file.WriteLine("x,z,tx,tz,half_width,distance");
            foreach(var p in samples)file.WriteLine(string.Join(",",new[]{p.X,p.Z,p.TangentX,p.TangentZ,p.HalfWidth,p.DistanceAlong}.Select(v=>v.ToString("R",CultureInfo.InvariantCulture))));
        }
        using(var file=new StreamWriter(Path.Combine(AppContext.BaseDirectory,"circuit-pieces.csv"))) {
            file.WriteLine("prefab,x,y,z,yaw");
            foreach(var p in DriftCircuitPieces.Items)file.WriteLine(p.Prefab+","+string.Join(",",new[]{p.X,p.HeightOffset,p.Z,p.Yaw}.Select(v=>v.ToString("R",CultureInfo.InvariantCulture))));
        }
    }
}
