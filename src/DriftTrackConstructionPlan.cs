using System;
using System.Collections.Generic;

namespace Rachel350Z {
// Fixed ordering is part of the saved per-tile construction progress contract.
// Keep this first plan immutable when later course versions are introduced.
public static class DriftTrackConstructionPlan {
    public const float MaximumStampSpacing=4f;
    static readonly IReadOnlyList<DriftTrackStamp> paintStamps=CreatePaintStamps();
    public static IReadOnlyList<DriftTrackStamp> PaintStamps=>paintStamps;

    static IReadOnlyList<DriftTrackStamp> CreatePaintStamps() {
        int intervals=(int)Math.Ceiling(DriftTrackLayout.TotalLength/MaximumStampSpacing);
        var result=new List<DriftTrackStamp>(intervals+1);
        for(int i=0;i<=intervals;i++) {
            float along=i==intervals?DriftTrackLayout.TotalLength:DriftTrackLayout.TotalLength*i/intervals;
            var sample=DriftTrackLayout.AtDistance(along);
            result.Add(new DriftTrackStamp(sample.X,sample.Z,sample.HalfWidth,sample.DistanceAlong));
        }
        return result.AsReadOnly();
    }

    // Closed circle/rectangle intersection, including a tangent at a tile edge
    // or corner. Double intermediates avoid overflow for finite float inputs.
    public static bool IntersectsTile(DriftTrackStamp stamp,float centerX,float centerZ,float halfSize) {
        RequireFinite(stamp.X,nameof(stamp));RequireFinite(stamp.Z,nameof(stamp));
        RequireFinite(stamp.Radius,nameof(stamp));RequireFinite(centerX,nameof(centerX));
        RequireFinite(centerZ,nameof(centerZ));RequireFinite(halfSize,nameof(halfSize));
        if(stamp.Radius<0)throw new ArgumentOutOfRangeException(nameof(stamp));
        if(halfSize<0)throw new ArgumentOutOfRangeException(nameof(halfSize));
        double dx=Math.Max(0,Math.Abs((double)stamp.X-centerX)-halfSize);
        double dz=Math.Max(0,Math.Abs((double)stamp.Z-centerZ)-halfSize);
        return dx*dx+dz*dz<=(double)stamp.Radius*stamp.Radius;
    }

    // A newer saved marker must never cause an older build to repaint a tile.
    public static int ResumeIndex(int saved,int total) {
        if(total<0)throw new ArgumentOutOfRangeException(nameof(total));
        return Math.Max(0,Math.Min(total,saved));
    }
    static void RequireFinite(float value,string name) {
        if(float.IsNaN(value)||float.IsInfinity(value))throw new ArgumentOutOfRangeException(name);
    }
}
public struct DriftTrackStamp {
    public readonly float X,Z,Radius,DistanceAlong;
    public DriftTrackStamp(float x,float z,float radius,float distanceAlong) {
        X=x;Z=z;Radius=radius;DistanceAlong=distanceAlong;
    }
}
}
