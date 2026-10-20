using System;
using System.Collections.Generic;

namespace Rachel350Z {
// New save keys isolate this fixed plan from the original test section's indices.
public static class DriftCircuitPlan {
    public const string PaintProgress="nfvu2_drift_circuit_pave_v2";
    public const string PieceProgress="nfvu2_drift_circuit_pieces_v2";
    public const string PieceTag="nfvu2_drift_circuit_piece_v2";
    public static readonly IReadOnlyList<DriftTrackStamp> PaintStamps=CreatePaint();
    static IReadOnlyList<DriftTrackStamp> CreatePaint() {
        var list=new List<DriftTrackStamp>();
        float start=DriftTrackLayout.TotalLength,length=DriftCircuitLayout.TotalLength-start;
        int count=(int)Math.Ceiling(length/4);
        for(int i=0;i<=count;i++) {
            var p=DriftCircuitLayout.AtDistance(i==count?DriftCircuitLayout.TotalLength:start+length*i/count);
            list.Add(new DriftTrackStamp(p.X,p.Z,p.HalfWidth+.25f,p.DistanceAlong));
        }
        // Pit apron: broad paved ground, no slab joints beneath the car.
        for(int z=-28;z<=8;z+=6)for(int x=112;x<=184;x+=6)list.Add(new DriftTrackStamp(x,z,4.5f,-1));
        list.Add(new DriftTrackStamp(114,14,6,-1));list.Add(new DriftTrackStamp(178,14,6,-1));
        // A dirt-free pedestrian approach from the world start. Stop short of
        // the spawn itself and join the apron, not the racing line.
        for(int x=40;x<=106;x+=6)list.Add(new DriftTrackStamp(x,-2,3.5f,-1));
        return list.AsReadOnly();
    }
    public static bool PitOpening(float x,float z)=>(x>=104&&x<=192&&z>=-36&&z<=15)
        ||(x>=34&&x<=110&&z>=-8&&z<=4);
}
}
