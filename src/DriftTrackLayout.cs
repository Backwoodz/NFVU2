using System;
using System.Collections.Generic;

namespace Rachel350Z {
// Metres in Valheim's horizontal world plane: +X east, +Z north, Y supplied
// by DriftWorldRules. The course runs east first, then turns left to the west.
// This is an open driving prototype, not yet the complete reference circuit.
public static class DriftTrackLayout {
    public const float StartX=96f, StartZ=24f, StraightLength=108f, CurveRadius=38f;
    public const float StraightWidth=16f, CurveWidth=20f, WidthTransitionLength=28f;
    public const float MaximumSampleSpacing=1f;
    public static readonly float CurveLength=(float)(Math.PI*CurveRadius);
    public static readonly float TotalLength=2*StraightLength+CurveLength;
    static readonly IReadOnlyList<DriftTrackSample> samples=CreateSamples();
    public static IReadOnlyList<DriftTrackSample> Samples=>samples;
    public static readonly DriftTrackBounds Bounds=CreateBounds();

    public static float WidthAt(float distance) {
        RequireFinite(distance,nameof(distance));
        float endCurve=StraightLength+CurveLength;
        float blend=distance<StraightLength
            ?Smooth((distance-(StraightLength-WidthTransitionLength))/WidthTransitionLength)
            :distance>endCurve?1-Smooth((distance-endCurve)/WidthTransitionLength):1;
        return StraightWidth+(CurveWidth-StraightWidth)*blend;
    }

    // Exact centreline and tangent at a clamped distance. The joins are tangent
    // continuous; curvature changes from zero to 1/38 at the semicircle ends.
    public static DriftTrackSample AtDistance(float distance) {
        RequireFinite(distance,nameof(distance));
        distance=Math.Max(0,Math.Min(TotalLength,distance));
        float x,z,tx,tz;
        if(distance<=StraightLength) {x=StartX+distance;z=StartZ;tx=1;tz=0;}
        else if(distance<StraightLength+CurveLength) {
            double angle=(distance-StraightLength)/CurveRadius;
            x=StartX+StraightLength+CurveRadius*(float)Math.Sin(angle);
            z=StartZ+CurveRadius*(1-(float)Math.Cos(angle));
            tx=(float)Math.Cos(angle);tz=(float)Math.Sin(angle);
        } else {
            x=StartX+StraightLength-(distance-StraightLength-CurveLength);
            z=StartZ+2*CurveRadius;tx=-1;tz=0;
        }
        return new DriftTrackSample(x,z,tx,tz,distance,WidthAt(distance)*.5f);
    }

    // Projection onto the fixed <=1 m polyline, including its two endpoints.
    // The semicircle chord error is below 3.3 mm. SignedEdgeDistance is the
    // centre distance minus local half-width, with round end caps. It is a
    // sampling mask, not a Unity collider or exact variable-width edge metric.
    public static DriftTrackProjection Project(float x,float z) {
        RequireFinite(x,nameof(x));RequireFinite(z,nameof(z));
        double best=double.PositiveInfinity,bestX=0,bestZ=0,bestAlong=0;
        for(int i=1;i<samples.Count;i++) {
            var a=samples[i-1];var b=samples[i];
            double dx=(double)b.X-a.X,dz=(double)b.Z-a.Z;
            double t=(((double)x-a.X)*dx+((double)z-a.Z)*dz)/(dx*dx+dz*dz);
            t=Math.Max(0,Math.Min(1,t));
            double px=a.X+t*dx,pz=a.Z+t*dz;
            double ex=x-px,ez=z-pz,squared=ex*ex+ez*ez;
            if(squared>=best)continue;
            best=squared;bestX=px;bestZ=pz;
            bestAlong=a.DistanceAlong+t*(b.DistanceAlong-a.DistanceAlong);
        }
        var exact=AtDistance((float)bestAlong);
        return new DriftTrackProjection((float)bestX,(float)bestZ,exact.TangentX,exact.TangentZ,
            exact.DistanceAlong,exact.HalfWidth,(float)Math.Sqrt(best));
    }
    public static float DistanceToCenterline(float x,float z)=>Project(x,z).DistanceToCenter;
    // Positive margin expands the road; a negative margin asks for an inset.
    public static bool Contains(float x,float z,float margin=0) {
        RequireFinite(margin,nameof(margin));return Project(x,z).SignedEdgeDistance<=margin;
    }
    static IReadOnlyList<DriftTrackSample> CreateSamples() {
        var result=new List<DriftTrackSample>{AtDistance(0)};
        AppendSamples(result,0,StraightLength);
        AppendSamples(result,StraightLength,CurveLength);
        AppendSamples(result,StraightLength+CurveLength,StraightLength);
        return result.AsReadOnly();
    }
    static void AppendSamples(List<DriftTrackSample> result,float start,float length) {
        int count=(int)Math.Ceiling(length/MaximumSampleSpacing);
        for(int i=1;i<=count;i++)result.Add(AtDistance(start+length*i/count));
    }
    static DriftTrackBounds CreateBounds() {
        float minX=float.PositiveInfinity,minZ=minX,maxX=float.NegativeInfinity,maxZ=maxX;
        foreach(var p in samples) {
            minX=Math.Min(minX,p.X-p.HalfWidth);maxX=Math.Max(maxX,p.X+p.HalfWidth);
            minZ=Math.Min(minZ,p.Z-p.HalfWidth);maxZ=Math.Max(maxZ,p.Z+p.HalfWidth);
        }
        return new DriftTrackBounds(minX,minZ,maxX,maxZ);
    }
    static float Smooth(float t) {t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
    static void RequireFinite(float value,string name) {
        if(float.IsNaN(value)||float.IsInfinity(value))throw new ArgumentOutOfRangeException(name,"Expected a finite world coordinate or distance.");
    }
}
public struct DriftTrackSample {
    public readonly float X,Z,TangentX,TangentZ,DistanceAlong,HalfWidth;
    public float Width=>HalfWidth*2;
    public DriftTrackSample(float x,float z,float tx,float tz,float along,float halfWidth) {
        X=x;Z=z;TangentX=tx;TangentZ=tz;DistanceAlong=along;HalfWidth=halfWidth;
    }
}
public struct DriftTrackProjection {
    public readonly float X,Z,TangentX,TangentZ,DistanceAlong,HalfWidth,DistanceToCenter;
    public float Width=>HalfWidth*2;
    public float SignedEdgeDistance=>DistanceToCenter-HalfWidth;
    public DriftTrackProjection(float x,float z,float tx,float tz,float along,float halfWidth,float distance) {
        X=x;Z=z;TangentX=tx;TangentZ=tz;DistanceAlong=along;HalfWidth=halfWidth;DistanceToCenter=distance;
    }
}
public struct DriftTrackBounds {
    public readonly float MinX,MinZ,MaxX,MaxZ;
    public DriftTrackBounds(float minX,float minZ,float maxX,float maxZ) {MinX=minX;MinZ=minZ;MaxX=maxX;MaxZ=maxZ;}
    public bool Contains(float x,float z)=>x>=MinX&&x<=MaxX&&z>=MinZ&&z<=MaxZ;
}
}
