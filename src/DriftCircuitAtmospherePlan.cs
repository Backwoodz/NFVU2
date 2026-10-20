using System;
using System.Collections.Generic;

namespace Rachel350Z {
// Runtime-only decoration has its own layout. Saved v1/v2 piece order is immutable.
public readonly struct DriftSpectatorSpot {
    public readonly float X,Y,Z,Yaw,Phase;
    public readonly bool Grandstand;
    public DriftSpectatorSpot(float x,float y,float z,float yaw,float phase,bool grandstand) {
        X=x;Y=y;Z=z;Yaw=yaw;Phase=phase;Grandstand=grandstand;
    }
}
public readonly struct DriftLampSpot {
    public readonly float X,Z;
    public DriftLampSpot(float x,float z){X=x;Z=z;}
}
public static class DriftCircuitAtmospherePlan {
    public const int MaximumLights=6;
    public const float LightDistance=105,ActorDistance=100;
    public const float LineX=DriftTrackLayout.StartX,LineZ=DriftTrackLayout.StartZ;
    public const float LineHalfLength=.8f,LineHalfWidth=8,LineTile=1;
    public static readonly IReadOnlyList<DriftSpectatorSpot> Spectators=CreateSpectators();
    public static readonly IReadOnlyList<DriftLampSpot> Lamps=CreateLamps();
    static IReadOnlyList<DriftSpectatorSpot> CreateSpectators() {
        var result=new List<DriftSpectatorSpot>();
        // Stand on the clear terrace immediately in front of each bench. The
        // creature's native idle is used; a human chair pose does not fit it.
        for(int row=0;row<3;row++)for(int x=122;x<=142;x+=4)
            result.Add(new DriftSpectatorSpot(x,.25f+row,52.65f+row*4,180,(result.Count*.381966f)%1,true));
        // Four small groups behind the fence, looking toward the nearest lane.
        foreach(float station in new[]{170f,560f,915f,1240f})for(int i=0;i<4;i++) {
            var p=DriftCircuitLayout.AtDistance(station+(i-1.5f)*2.2f);
            float side=station==560f?-1:1,offset=side*(p.HalfWidth+9.5f);
            float x=p.X-p.TangentZ*offset,z=p.Z+p.TangentX*offset;
            if(DriftCircuitLayout.Contains(x,z,5)||DriftCircuitPlan.PitOpening(x,z))continue;
            float yaw=(float)(Math.Atan2(p.TangentZ*side,-p.TangentX*side)*180/Math.PI);
            result.Add(new DriftSpectatorSpot(x,0,z,yaw,(result.Count*.381966f)%1,false));
        }
        return result.AsReadOnly();
    }
    static IReadOnlyList<DriftLampSpot> CreateLamps() {
        var result=new List<DriftLampSpot>();
        int count=(int)Math.Ceiling(DriftCircuitLayout.TotalLength/34);
        for(int i=0;i<count;i++) {
            var p=DriftCircuitLayout.AtDistance(DriftCircuitLayout.TotalLength*(i+.3f)/count);
            float side=i%2==0?1:-1,offset=side*(p.HalfWidth+7.8f);
            float x=p.X-p.TangentZ*offset,z=p.Z+p.TangentX*offset;
            if(!DriftCircuitLayout.Contains(x,z,4)&&!DriftCircuitPlan.PitOpening(x,z))
                result.Add(new DriftLampSpot(x,z));
        }
        // Paddock and grandstand are illuminated without obstructing their stairs.
        foreach(var p in new[]{new DriftLampSpot(102,-24),new DriftLampSpot(194,-24),new DriftLampSpot(116,64),new DriftLampSpot(148,64)})
            result.Add(p);
        return result.AsReadOnly();
    }
    public static int[] NearestLights(float x,float z) {
        if(float.IsNaN(x)||float.IsNaN(z)||float.IsInfinity(x)||float.IsInfinity(z))return Array.Empty<int>();
        var result=new List<int>();
        var distance=new List<float>();
        for(int i=0;i<Lamps.Count;i++) {
            float dx=Lamps[i].X-x,dz=Lamps[i].Z-z,d=dx*dx+dz*dz;
            if(d>LightDistance*LightDistance)continue;
            int insert=0;while(insert<distance.Count&&distance[insert]<=d)insert++;
            if(insert>=MaximumLights)continue;
            result.Insert(insert,i);distance.Insert(insert,d);
            if(result.Count>MaximumLights){result.RemoveAt(MaximumLights);distance.RemoveAt(MaximumLights);}
        }
        return result.ToArray();
    }
}
}
