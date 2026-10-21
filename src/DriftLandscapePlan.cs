using System;
using System.Collections.Generic;

namespace Rachel350Z {
internal readonly struct DriftLandscapeSpot {
    internal readonly float X,Z,Yaw,Height,Radius;
    internal readonly string Prefab;
    internal readonly bool Parkade,Tree;
    internal DriftLandscapeSpot(float x,float z,float yaw,float height,float radius,string prefab,bool parkade,bool tree) {
        X=x;Z=z;Yaw=yaw;Height=height;Radius=radius;Prefab=prefab;Parkade=parkade;Tree=tree;
    }
}
// Deterministic, local scenery outside the circuits. No saved construction order,
// terrain stamps, random-world vegetation or vehicle settings are changed.
internal static class DriftLandscapePlan {
    internal const float ViewDistance=235,BuildDistance=260,DetailDistance=125;
    internal const float TreeSpacing=7.5f,ForestDepth=130,TreeClearance=14,DetailClearance=11;
    internal static readonly IReadOnlyList<DriftLandscapeSpot> Spots=Create();
    static IReadOnlyList<DriftLandscapeSpot> Create() {
        var result=new List<DriftLandscapeSpot>();
        AddVenue(result,false);AddVenue(result,true);return result.AsReadOnly();
    }
    static float Noise(int x,int z,int salt) {
        unchecked {uint n=(uint)(x*374761393+z*668265263+salt*1442695041);n=(n^(n>>13))*1274126177;return (n^(n>>16))/(float)uint.MaxValue;}
    }
    static void AddVenue(List<DriftLandscapeSpot> list,bool parkade) {
        var b=parkade?ParkadeLayout.Bounds:DriftCircuitLayout.Bounds;
        int salt=parkade?73:19;
        // A continuous deep belt with overlapping crowns, rather than sparse
        // decorative clusters. Large safe infield areas can also be wooded.
        int minX=(int)Math.Floor((b.MinX-ForestDepth)/TreeSpacing),maxX=(int)Math.Ceiling((b.MaxX+ForestDepth)/TreeSpacing);
        int minZ=(int)Math.Floor((b.MinZ-ForestDepth)/TreeSpacing),maxZ=(int)Math.Ceiling((b.MaxZ+ForestDepth)/TreeSpacing);
        for(int iz=minZ;iz<=maxZ;iz++)for(int ix=minX;ix<=maxX;ix++) {
            float x=ix*TreeSpacing+(Noise(ix,iz,salt)-.5f)*3.5f,z=iz*TreeSpacing+(Noise(ix,iz,salt+1)-.5f)*3.5f;
            var road=parkade?ParkadeLayout.Project(x,z):DriftCircuitLayout.Project(x,z);
            float radius=6.5f+Noise(ix,iz,salt+2)*2.5f;
            if(road.SignedEdgeDistance<TreeClearance+radius||road.SignedEdgeDistance>ForestDepth)continue;
            if(Noise(ix,iz,salt+4)<.035f||!Clear(x,z,radius,parkade))continue;
            list.Add(new DriftLandscapeSpot(x,z,Noise(ix,iz,salt+5)*360,19+Noise(ix,iz,salt+6)*13,radius,
                Noise(ix,iz,salt+7)<.72f?"FirTree":"PineTree",parkade,true));
            // Low, sparse foreground groups at the forest edge, also well
            // behind barriers. Reuse native art without destructible objects.
            if(road.SignedEdgeDistance>65||Noise(ix,iz,salt+8)<.72f)continue;
            float dx=road.X-x,dz=road.Z-z,len=(float)Math.Sqrt(dx*dx+dz*dz);
            float px=x+dx/len*8,pz=z+dz/len*8;
            if(Clear(px,pz,2.8f,parkade,DetailClearance))
                list.Add(new DriftLandscapeSpot(px,pz,Noise(ix,iz,salt+9)*360,.8f+Noise(ix,iz,salt+10)*1.1f,2.8f,
                    Noise(ix,iz,salt+11)<.55f?"Bush01":"rock4_coast",parkade,false));
        }
    }
    internal static bool Clear(float x,float z,float radius,bool parkade,float clearance=TreeClearance) {
        if(parkade) {
            if(ParkadeLayout.Project(x,z).SignedEdgeDistance<clearance+radius)return false;
            foreach(var p in ParkadePlan.Pieces)if(Near(x,z,p.X,p.Z,radius+6))return false;
            foreach(var p in ParkadeFestivalPlan.Trolls)if(Near(x,z,p.X,p.Z,radius+12))return false;
            foreach(var p in ParkadeServiceYardPlan.Yards)if(Near(x,z,p.X,p.Z,radius+9))return false;
        } else {
            if(DriftCircuitLayout.Project(x,z).SignedEdgeDistance<clearance+radius)return false;
            // Generous pedestrian/spawn/paddock reserve, including both ramps,
            // the original world start and all access to the pit apron.
            if(x+radius>=98&&x-radius<=200&&z+radius>=-42&&z-radius<=20)return false;
            if(x+radius>=28&&x-radius<=114&&z+radius>=-14&&z-radius<=10||Near(x,z,0,0,radius+18))return false;
            foreach(var p in DriftCircuitPieces.Items)if(Near(x,z,p.X,p.Z,radius+6))return false;
            foreach(var p in DriftTrackPieces.Items)if(Near(x,z,p.X,p.Z,radius+6))return false;
        }
        return true;
    }
    static bool Near(float x,float z,float px,float pz,float radius){float dx=x-px,dz=z-pz;return dx*dx+dz*dz<radius*radius;}
}
}
