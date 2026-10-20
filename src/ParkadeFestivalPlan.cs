using System;
using System.Collections.Generic;

namespace Rachel350Z {
// Runtime scenery follows the existing saved roofs; the v1 construction order
// and terrain stamps are deliberately unchanged for existing worlds.
internal static class ParkadeFestivalPlan {
    internal const float ActorDistance=115,SceneryDistance=185,LightDistance=65;
    internal const int MaximumLights=4;
    internal static readonly IReadOnlyList<DriftTrackPiece> Terraces=FindTerraces();
    internal static readonly IReadOnlyList<ParkadeFestivalSpot> Trolls=FindTrolls();
    internal static readonly IReadOnlyList<ParkadeFestivalSpot> Lamps=FindLamps();
    internal static readonly IReadOnlyList<ParkadeFestivalSpot> Boards=FindBoards();
    static IReadOnlyList<DriftTrackPiece> FindTerraces() {
        var result=new List<DriftTrackPiece>();
        foreach(var piece in ParkadePlan.Pieces)if(piece.Prefab=="stone_floor")result.Add(piece);
        return result.AsReadOnly();
    }
    static IReadOnlyList<ParkadeFestivalSpot> FindTrolls() {
        var result=new List<ParkadeFestivalSpot>();
        foreach(float station in new[]{100f,425f}) {
            // Prefer the outside of the venue, checking the entire giant's
            // footprint against every leg of the course at tight corners.
            var p=ParkadeLayout.AtDistance(station);
            bool found=false;
            foreach(float offset in new[]{25f,31f,37f,43f,53f,65f,80f}) {
                foreach(int side in new[]{1,-1}) {
                    float x=p.X-p.TangentZ*side*offset,z=p.Z+p.TangentX*side*offset;
                    if(!ParkadePlan.ClearBox(x,z,0,4,4,7))continue;
                    float y=Math.Max(ParkadeLayout.TerrainHeightAt(x,z),ParkadeLayout.HeightAtDistance(p.DistanceAlong)-.8f);
                    result.Add(new ParkadeFestivalSpot(x,y,z,LookAt(x,z,p.X,p.Z)));found=true;break;
                }
                if(found)break;
            }
        }
        return result.AsReadOnly();
    }
    static IReadOnlyList<ParkadeFestivalSpot> FindLamps() {
        var result=new List<ParkadeFestivalSpot>();
        for(int i=0;i<24;i++) {
            var p=ParkadeLayout.AtDistance(ParkadeLayout.TotalLength*(i+.4f)/24);
            float side=i%2==0?1:-1,x=p.X-p.TangentZ*side*12.7f,z=p.Z+p.TangentX*side*12.7f;
            if(!ParkadePlan.ClearBox(x,z,0,.4f,.4f,1.3f))continue;
            result.Add(new ParkadeFestivalSpot(x,ParkadeLayout.HeightAtDistance(p.DistanceAlong)+3.5f,z,LookAt(x,z,p.X,p.Z)));
        }
        return result.AsReadOnly();
    }
    static IReadOnlyList<ParkadeFestivalSpot> FindBoards() {
        var result=new List<ParkadeFestivalSpot>();
        // Mounted above selected existing terrace roofs, facing the course.
        for(int i=0;i<Terraces.Count;i+=4) {
            var p=Terraces[i];var q=ParkadeLayout.Project(p.X,p.Z);
            result.Add(new ParkadeFestivalSpot(p.X,ParkadeLayout.TerrainHeightAt(p.X,p.Z)+p.HeightOffset+3.9f,p.Z,LookAt(p.X,p.Z,q.X,q.Z)));
        }
        return result.AsReadOnly();
    }
    internal static float LookAt(float x,float z,float toX,float toZ)=>(float)(Math.Atan2(toX-x,toZ-z)*180/Math.PI);
    internal static int[] NearestLights(float x,float z) {
        if(float.IsNaN(x)||float.IsNaN(z)||float.IsInfinity(x)||float.IsInfinity(z))return Array.Empty<int>();
        var result=new List<int>();var distances=new List<float>();
        for(int i=0;i<Lamps.Count;i++) {
            float dx=Lamps[i].X-x,dz=Lamps[i].Z-z,d=dx*dx+dz*dz;
            if(d>LightDistance*LightDistance)continue;
            int slot=0;while(slot<distances.Count&&distances[slot]<=d)slot++;
            if(slot>=MaximumLights)continue;
            result.Insert(slot,i);distances.Insert(slot,d);
            if(result.Count>MaximumLights){result.RemoveAt(MaximumLights);distances.RemoveAt(MaximumLights);}
        }
        return result.ToArray();
    }
}
internal readonly struct ParkadeFestivalSpot {
    internal readonly float X,Y,Z,Yaw;
    internal ParkadeFestivalSpot(float x,float y,float z,float yaw){X=x;Y=y;Z=z;Yaw=yaw;}
}
}
