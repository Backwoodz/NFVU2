using System;
using System.Collections.Generic;
using System.Linq;

namespace Rachel350Z {
internal readonly struct ParkadeYardPiece {
    internal readonly string Prefab;
    internal readonly float X,Y,Z,Yaw;
    internal readonly bool Grounded;
    internal ParkadeYardPiece(string prefab,float x,float y,float z,float yaw,bool grounded){Prefab=prefab;X=x;Y=y;Z=z;Yaw=yaw;Grounded=grounded;}
}
internal static class ParkadeServiceYardPlan {
    internal const float HalfX=4.5f,HalfZ=4.5f,ViewDistance=155;
    internal static readonly IReadOnlyList<ParkadeFestivalSpot> Yards=Find();
    internal static readonly IReadOnlyList<ParkadeYardPiece> Pieces=Build();
    static IReadOnlyList<ParkadeFestivalSpot> Find() {
        var result=new List<ParkadeFestivalSpot>();var b=ParkadeLayout.Bounds;
        foreach(float station in new[]{60f,230f,405f}) {
            foreach(var terrace in ParkadeFestivalPlan.Terraces.OrderBy(p=>Separation(ParkadeLayout.Project(p.X,p.Z).DistanceAlong,station))) {
                var road=ParkadeLayout.Project(terrace.X,terrace.Z);
                float dx=terrace.X-road.X,dz=terrace.Z-road.Z,d=(float)Math.Sqrt(dx*dx+dz*dz);
                if(d<1)continue;
                float x=terrace.X+dx/d*15,z=terrace.Z+dz/d*15;
                // Behind an outer building, rather than in the tight infield.
                if(x>b.MinX&&x<b.MaxX&&z>b.MinZ&&z<b.MaxZ)continue;
                float yaw=ParkadeFestivalPlan.LookAt(x,z,road.X,road.Z);
                if(!ParkadePlan.ClearBox(x,z,yaw,HalfX,HalfZ,8))continue;
                if(ParkadePlan.Pieces.Any(p=>Near(x,z,p.X,p.Z,9)))continue;
                if(ParkadeFestivalPlan.Trolls.Any(p=>Near(x,z,p.X,p.Z,17)))continue;
                if(result.Any(p=>Near(x,z,p.X,p.Z,38)))continue;
                result.Add(new ParkadeFestivalSpot(x,ParkadeLayout.TerrainHeightAt(x,z),z,yaw));break;
            }
        }
        return result.AsReadOnly();
    }
    static float Separation(float a,float b){float d=Math.Abs(a-b);return Math.Min(d,ParkadeLayout.TotalLength-d);}
    static bool Near(float x,float z,float px,float pz,float radius){float dx=x-px,dz=z-pz;return dx*dx+dz*dz<radius*radius;}
    static IReadOnlyList<ParkadeYardPiece> Build() {
        var pieces=new List<ParkadeYardPiece>();
        foreach(var p in Yards) {
            Action<string,float,float,float,float,bool> add=(prefab,x,y,z,yaw,grounded)=> {
                double a=p.Yaw*Math.PI/180;float c=(float)Math.Cos(a),s=(float)Math.Sin(a);
                pieces.Add(new ParkadeYardPiece(prefab,p.X+x*c+z*s,y,p.Z-x*s+z*c,p.Yaw+yaw,grounded));
            };
            // Open front faces the venue; shallow covered storage at the back.
            for(int x=-2;x<=2;x+=2)for(int z=-2;z<=2;z+=2)add("wood_floor",x,.1f,z,0,false);
            for(int x=-2;x<=2;x+=2)add("wood_fence",x,.2f,-3.5f,0,true);
            foreach(float x in new[]{-3.5f,3.5f}) {
                add("wood_fence",x,.2f,-1.5f,90,true);
            }
            foreach(float x in new[]{-2.8f,2.8f})foreach(float z in new[]{-4f,0f}) {
                add("wood_pole2",x,.9f,z,0,false);add("wood_pole2",x,2.85f,z,0,false);
                if(z<0)add("wood_pole2",x,4.8f,z,0,false);
            }
            for(int x=-2;x<=2;x+=2) {
                add("wood_roof",x,3.6f,-1,0,false);add("wood_roof",x,4.6f,-3,0,false);
                add("wood_beam",x,3.6f,0,0,false);add("wood_beam",x,5.6f,-4,0,false);
            }
            add("piece_chest_wood",-2,.3f,-1.5f,0,true);add("piece_chest_wood",-2,.3f,.5f,90,true);
            add("barrell",2,.3f,-1.6f,0,true);add("barrell",2,.3f,.1f,20,true);
            add("wood_stack",0,.3f,-2.3f,90,true);
        }
        return pieces.AsReadOnly();
    }
}
}
