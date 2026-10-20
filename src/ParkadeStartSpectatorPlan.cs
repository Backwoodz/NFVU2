using System;
using System.Collections.Generic;

namespace Rachel350Z {
// Scenic crowd stays behind the stone barriers. Offsets are relative to the
// start tangent; neither the starter's centre corridor nor the sign is occupied.
public readonly struct ParkadeSpectatorSpot {
    public readonly float X,Y,Z,GroundY,Yaw,Phase,Forward,Across;
    public readonly bool Dancing;
    public ParkadeSpectatorSpot(float forward,float across,int index) {
        Forward=forward;Across=across;
        float fx=ParkadeLayout.StartTangentX,fz=ParkadeLayout.StartTangentZ;
        X=ParkadeLayout.StartX+fx*forward-fz*across;
        Z=ParkadeLayout.StartZ+fz*forward+fx*across;
        // The surrounding generated terrain slopes down quickly. Small scenic
        // stone viewing plinths keep faces visible above the course barriers.
        Y=ParkadeLayout.HeightAtDistance(ParkadeLayout.Project(X,Z).DistanceAlong)+.15f;
        GroundY=float.PositiveInfinity;
        // Enclose the rotated 1.8 m square, including its furthest corners.
        foreach(float dx in new[]{-1.28f,0,1.28f})foreach(float dz in new[]{-1.28f,0,1.28f})
            GroundY=Math.Min(GroundY,ParkadeLayout.TerrainHeightAt(X+dx,Z+dz));
        GroundY-=.06f;
        // Watch the car waiting just before the line, rather than facing in
        // two identical ranks directly across the road.
        float lookX=ParkadeLayout.StartX-fx*3-X,lookZ=ParkadeLayout.StartZ-fz*3-Z;
        Yaw=(float)(Math.Atan2(lookX,lookZ)*180/Math.PI);
        Phase=(index*.381966f)%1;Dancing=index%3==1;
    }
}
public static class ParkadeStartSpectatorPlan {
    public const float ActorDistance=150;
    public static readonly IReadOnlyList<ParkadeSpectatorSpot> Spots=Build();
    static IReadOnlyList<ParkadeSpectatorSpot> Build() {
        var result=new List<ParkadeSpectatorSpot>();
        // Stagger the two ranks to clear the incoming bend's stone pier and
        // the covered bay just beyond the line, as well as the start sign.
        float[] forwards={-7,-3,3,7,-9,-5,3,7};
        float[] across={-14.2f,-13.3f,-13.3f,-13.3f,13.3f,13.3f,13.3f,14.2f};
        for(int i=0;i<forwards.Length;i++)result.Add(new ParkadeSpectatorSpot(forwards[i],across[i],i));
        return result.AsReadOnly();
    }
}
}
