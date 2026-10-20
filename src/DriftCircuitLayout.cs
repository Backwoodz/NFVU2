using System;
using System.Collections.Generic;

namespace Rachel350Z {
// Version 2 extends, rather than moves, the saved v1 U section. Metres, +X east,
// +Z north. Keep the route and ordering frozen once used by saved tile markers.
public static class DriftCircuitLayout {
    public const float RoadWidth=18,Runoff=7;
    static readonly IReadOnlyList<DriftCircuitSegment> segments=CreateSegments();
    public static IReadOnlyList<DriftCircuitSegment> Extension=>segments;
    public static readonly float TotalLength=EndDistance();
    public static readonly IReadOnlyList<DriftTrackSample> Samples=CreateSamples();
    public static readonly DriftTrackBounds Bounds=CreateBounds();
    static IReadOnlyList<DriftCircuitSegment> CreateSegments() {
        var route=new List<DriftCircuitSegment>();
        double x=96,z=100,heading=Math.PI,along=DriftTrackLayout.TotalLength;
        Action<double,double> add=(length,turn)=> {
            var part=new DriftCircuitSegment(x,z,heading,along,length,turn);route.Add(part);
            var end=part.At(length,9);x=end.X;z=end.Z;heading+=turn;along+=length;
        };
        Action<double,double> arc=(radius,degrees)=>add(radius*Math.Abs(degrees)*Math.PI/180,degrees*Math.PI/180);
        arc(32,-90);add(96,0);arc(32,-90);
        // Four alternating arcs form a broad double transition, without a crossing.
        arc(32,55);arc(32,-55);arc(32,-55);arc(32,55);
        add(312-x,0);arc(48,-90);add(228,0);arc(48,-90);
        add(208,0);arc(48,-90);arc(40,-90);
        return route.AsReadOnly();
    }
    static float EndDistance() {var last=segments[segments.Count-1];return (float)(last.StartDistance+last.Length);}
    public static DriftTrackSample AtDistance(float distance) {
        Finite(distance);distance=Math.Max(0,Math.Min(TotalLength,distance));
        if(distance<=DriftTrackLayout.TotalLength)return DriftTrackLayout.AtDistance(distance);
        if(distance>=TotalLength)return new DriftTrackSample(96,24,1,0,TotalLength,8);
        float blend=Math.Min(1,Math.Min((distance-DriftTrackLayout.TotalLength)/24,(TotalLength-distance)/24));
        float halfWidth=8+Math.Max(0,blend);
        foreach(var part in segments)if(distance<=part.StartDistance+part.Length)
            return part.At(distance-part.StartDistance,halfWidth);
        throw new InvalidOperationException("Missing circuit segment");
    }
    public static DriftTrackProjection Project(float x,float z) {
        Finite(x);Finite(z);double best=double.PositiveInfinity,px=0,pz=0,along=0;
        for(int i=1;i<Samples.Count;i++) {
            var a=Samples[i-1];var b=Samples[i];double dx=b.X-a.X,dz=b.Z-a.Z;
            double t=Math.Max(0,Math.Min(1,((x-a.X)*dx+(z-a.Z)*dz)/(dx*dx+dz*dz)));
            double qx=a.X+t*dx,qz=a.Z+t*dz,squared=(x-qx)*(x-qx)+(z-qz)*(z-qz);
            if(squared>=best)continue;
            best=squared;px=qx;pz=qz;along=a.DistanceAlong+t*(b.DistanceAlong-a.DistanceAlong);
        }
        var p=AtDistance((float)along);
        return new DriftTrackProjection((float)px,(float)pz,p.TangentX,p.TangentZ,(float)along,p.HalfWidth,(float)Math.Sqrt(best));
    }
    public static bool Contains(float x,float z,float margin=0) {Finite(margin);return Project(x,z).SignedEdgeDistance<=margin;}
    static IReadOnlyList<DriftTrackSample> CreateSamples() {
        var list=new List<DriftTrackSample>(DriftTrackLayout.Samples);
        foreach(var part in segments) {
            int steps=(int)Math.Ceiling(part.Length);
            for(int i=1;i<=steps;i++)list.Add(AtDistance(i==steps?(float)(part.StartDistance+part.Length):(float)(part.StartDistance+part.Length*i/steps)));
        }
        return list.AsReadOnly();
    }
    static DriftTrackBounds CreateBounds() {
        float x0=float.MaxValue,z0=x0,x1=float.MinValue,z1=x1;
        foreach(var p in Samples){x0=Math.Min(x0,p.X-p.HalfWidth);z0=Math.Min(z0,p.Z-p.HalfWidth);x1=Math.Max(x1,p.X+p.HalfWidth);z1=Math.Max(z1,p.Z+p.HalfWidth);}
        return new DriftTrackBounds(x0,z0,x1,z1);
    }
    static void Finite(float n){if(float.IsNaN(n)||float.IsInfinity(n))throw new ArgumentOutOfRangeException(nameof(n));}
}
public struct DriftCircuitSegment {
    public readonly double X,Z,Heading,StartDistance,Length,Turn;
    public DriftCircuitSegment(double x,double z,double heading,double along,double length,double turn) {
        X=x;Z=z;Heading=heading;StartDistance=along;Length=length;Turn=turn;
    }
    public DriftTrackSample At(double local,float halfWidth) {
        local=Math.Max(0,Math.Min(Length,local));double end=Heading+Turn*local/Length;
        double x,z;
        if(Math.Abs(Turn)<.00001){x=X+Math.Cos(Heading)*local;z=Z+Math.Sin(Heading)*local;}
        else {double radius=Length/Turn;x=X+radius*(Math.Sin(end)-Math.Sin(Heading));z=Z+radius*(Math.Cos(Heading)-Math.Cos(end));}
        return new DriftTrackSample((float)x,(float)z,(float)Math.Cos(end),(float)Math.Sin(end),(float)(StartDistance+local),halfWidth);
    }
}
}
