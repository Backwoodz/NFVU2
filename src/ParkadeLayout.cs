using System;
using System.Collections.Generic;

namespace Rachel350Z {
// Measured event 4301 navigation line, translated to the separate Valheim venue.
// Original road triangles supply the relative grade. Native building materials
// and terrain replace the original meshes. Coordinates are metres at 1:1 scale;
// start is the projection of the original front grid onto the navigation line.
public static class ParkadeLayout {
    public const float StartX=1100,StartZ=80,RoadWidth=18,BaseHeight=40,WorldGround=32;
    public static readonly float TotalLength;
    public static readonly float StartTangentX,StartTangentZ;
    public const float StartHalfWidth=RoadWidth*.5f;
    static readonly Node[] nodes={
        new Node(0.000000f,0.000000f,0.000000f),
        new Node(21.331895f,0.511735f,-0.000000f),
        new Node(37.713185f,6.334025f,-0.000006f),
        new Node(53.127125f,11.719255f,-0.000006f),
        new Node(65.279585f,14.137925f,-0.000006f),
        new Node(77.722885f,12.087625f,-0.000006f),
        new Node(85.606795f,5.923235f,-0.054296f),
        new Node(89.087575f,-2.968405f,-0.485307f),
        new Node(89.267865f,-15.026625f,-1.412616f),
        new Node(89.280315f,-36.390975f,-3.302884f),
        new Node(89.074145f,-52.670735f,-4.801821f),
        new Node(89.436815f,-68.612625f,-6.134246f),
        new Node(89.338055f,-80.956495f,-6.896366f),
        new Node(86.590435f,-89.135975f,-7.087604f),
        new Node(80.913685f,-94.994215f,-7.100400f),
        new Node(72.924365f,-96.966085f,-7.058970f),
        new Node(64.764695f,-93.715075f,-6.961012f),
        new Node(59.542405f,-85.794875f,-6.503331f),
        new Node(55.984545f,-69.687635f,-5.711234f),
        new Node(50.508955f,-50.886005f,-4.651216f),
        new Node(47.623705f,-42.654105f,-4.329789f),
        new Node(43.716475f,-32.488265f,-4.252248f),
        new Node(34.973065f,-30.984425f,-4.243825f),
        new Node(28.745895f,-37.341445f,-4.240176f),
        new Node(25.590195f,-50.293015f,-3.981226f),
        new Node(24.861985f,-68.962965f,-2.884187f),
        new Node(16.262005f,-86.980855f,-1.485647f),
        new Node(3.975935f,-96.433645f,-0.242692f),
        new Node(-10.837785f,-97.779035f,-0.075274f),
        new Node(-20.928355f,-95.757345f,-0.000007f),
        new Node(-26.962115f,-88.520645f,-0.000008f),
        new Node(-25.075635f,-75.891525f,-0.000008f),
        new Node(-21.164385f,-62.603285f,-0.000006f),
        new Node(-19.741345f,-52.884695f,-0.000005f),
        new Node(-18.964855f,-38.895895f,-0.013960f),
        new Node(-20.745925f,-29.110215f,-0.013872f),
        new Node(-22.996655f,-16.920975f,-0.008427f),
        new Node(-22.924145f,-8.740435f,-0.006018f),
        new Node(-16.734515f,-2.086505f,0.000000f),
        new Node(-4.544755f,-0.109025f,0.000000f),
        new Node(0.000000f,0.000000f,0.000000f),
    };
    static readonly float[] along;
    public static readonly IReadOnlyList<DriftTrackSample> Samples;
    public static readonly IReadOnlyList<float> JunctionDistances;
    public static readonly DriftTrackBounds Bounds;
    struct Node {
        public readonly float X,Z,Height;
        public Node(float x,float z,float height){X=StartX+x;Z=StartZ+z;Height=height;}
    }
    static ParkadeLayout() {
        along=new float[nodes.Length];
        for(int i=1;i<nodes.Length;i++) {
            double dx=nodes[i].X-nodes[i-1].X,dz=nodes[i].Z-nodes[i-1].Z;
            along[i]=along[i-1]+(float)Math.Sqrt(dx*dx+dz*dz);
        }
        TotalLength=along[along.Length-1];
        JunctionDistances=Array.AsReadOnly(along);
        float firstLength=along[1];StartTangentX=(nodes[1].X-nodes[0].X)/firstLength;StartTangentZ=(nodes[1].Z-nodes[0].Z)/firstLength;
        var samples=new List<DriftTrackSample>{AtDistance(0)};
        for(int i=1;i<nodes.Length;i++) {
            int steps=(int)Math.Ceiling(along[i]-along[i-1]);
            for(int n=1;n<=steps;n++)samples.Add(AtDistance(along[i-1]+(along[i]-along[i-1])*n/steps));
        }
        Samples=samples.AsReadOnly();
        float x0=float.MaxValue,z0=x0,x1=float.MinValue,z1=x1;
        foreach(var p in nodes){x0=Math.Min(x0,p.X-StartHalfWidth);z0=Math.Min(z0,p.Z-StartHalfWidth);x1=Math.Max(x1,p.X+StartHalfWidth);z1=Math.Max(z1,p.Z+StartHalfWidth);}
        Bounds=new DriftTrackBounds(x0,z0,x1,z1);
    }
    static void Finite(float n){if(float.IsNaN(n)||float.IsInfinity(n))throw new ArgumentOutOfRangeException(nameof(n));}
    static int Segment(float distance) {
        int index=Array.BinarySearch(along,distance);
        if(index>=0)return Math.Min(index,nodes.Length-2);
        return Math.Max(0,Math.Min(nodes.Length-2,~index-1));
    }
    public static DriftTrackSample AtDistance(float distance) {
        Finite(distance);distance=Math.Max(0,Math.Min(TotalLength,distance));
        if(distance>=TotalLength)return new DriftTrackSample(StartX,StartZ,StartTangentX,StartTangentZ,TotalLength,StartHalfWidth);
        int i=Segment(distance);var a=nodes[i];var b=nodes[i+1];float length=along[i+1]-along[i],t=(distance-along[i])/length;
        return new DriftTrackSample(a.X+(b.X-a.X)*t,a.Z+(b.Z-a.Z)*t,(b.X-a.X)/length,(b.Z-a.Z)/length,distance,StartHalfWidth);
    }
    public static float HeightAtDistance(float distance) {
        Finite(distance);distance=Math.Max(0,Math.Min(TotalLength,distance));
        int i=Segment(distance);float t=(distance-along[i])/(along[i+1]-along[i]);
        return BaseHeight+nodes[i].Height+(nodes[i+1].Height-nodes[i].Height)*t;
    }
    public static DriftTrackProjection Project(float x,float z) {
        Finite(x);Finite(z);double best=double.PositiveInfinity,px=0,pz=0,progress=0;
        for(int i=0;i<nodes.Length-1;i++) {
            var a=nodes[i];var b=nodes[i+1];double dx=b.X-a.X,dz=b.Z-a.Z;
            double t=Math.Max(0,Math.Min(1,((x-a.X)*dx+(z-a.Z)*dz)/(dx*dx+dz*dz)));
            double qx=a.X+t*dx,qz=a.Z+t*dz,ds=(x-qx)*(x-qx)+(z-qz)*(z-qz);
            if(ds>=best)continue;best=ds;px=qx;pz=qz;progress=along[i]+t*(along[i+1]-along[i]);
        }
        var p=AtDistance((float)progress);
        return new DriftTrackProjection((float)px,(float)pz,p.TangentX,p.TangentZ,(float)progress,p.HalfWidth,(float)Math.Sqrt(best));
    }
    public static bool Contains(float x,float z,float margin=0){Finite(margin);return Project(x,z).SignedEdgeDistance<=margin;}
    // Pause belongs to this separate venue, including its narrow shoulders.
    // The original practice course and parking retain Valheim's native menu.
    public static bool InVenueArea(float x,float z)=>x>=Bounds.MinX-24&&x<=Bounds.MaxX+24&&z>=Bounds.MinZ-24&&z<=Bounds.MaxZ+24;
    // Generator-safe pure height field; the practice course and all surrounding
    // world terrain retain the previous 32 m base. No mutable Unity state used.
    public static float TerrainHeightAt(float x,float z) {
        if(x<Bounds.MinX-12||x>Bounds.MaxX+12||z<Bounds.MinZ-12||z>Bounds.MaxZ+12)return WorldGround;
        var p=Project(x,z);float blend=Math.Max(0,Math.Min(1,(p.SignedEdgeDistance-1)/10));
        blend=blend*blend*(3-2*blend);
        return WorldGround+(HeightAtDistance(p.DistanceAlong)-WorldGround)*(1-blend);
    }
    public static float GroundHeightAt(float x,float z)=>TerrainHeightAt(x,z);
}
}
