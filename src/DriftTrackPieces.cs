using System;
using System.Collections.Generic;

namespace Rachel350Z {
// Frozen ordering accompanies the v1 per-tile placement marker. These are sparse
// guide stones with runoff, not a continuous collision barrier or a full circuit.
public static class DriftTrackPieces {
    public const float Runoff=4f;
    public static readonly IReadOnlyList<DriftTrackPiece> Items=Create();
    static IReadOnlyList<DriftTrackPiece> Create() {
        var result=new List<DriftTrackPiece>();
        AddSign(result,8,3,"NFVU2 DRIFT | 100 m OST / EAST",90);
        AddSign(result,DriftTrackLayout.StartX-4,DriftTrackLayout.StartZ-12,"DRIFT TEST | 335 m | 16-20 m",90);
        AddSign(result,DriftTrackLayout.StartX-4,DriftTrackLayout.StartZ+2*DriftTrackLayout.CurveRadius+12,"ENDE / END",90);
        int intervals=(int)Math.Ceiling(DriftTrackLayout.TotalLength/12f);
        for(int i=0;i<=intervals;i++) {
            var sample=DriftTrackLayout.AtDistance(DriftTrackLayout.TotalLength*i/intervals);
            float yaw=(float)(Math.Atan2(-sample.TangentZ,sample.TangentX)*180/Math.PI);
            foreach(int side in new[]{-1,1}) {
                float offset=side*(sample.HalfWidth+Runoff);
                result.Add(new DriftTrackPiece("stone_wall_2x1",sample.X-sample.TangentZ*offset,.2f,
                    sample.Z+sample.TangentX*offset,yaw));
            }
        }
        foreach(int side in new[]{-1,1}) {
            float z=DriftTrackLayout.StartZ+side*(DriftTrackLayout.StraightWidth*.5f+Runoff);
            result.Add(new DriftTrackPiece("wood_pole2",DriftTrackLayout.StartX,.9f,z,0));
            result.Add(new DriftTrackPiece("wood_pole2",DriftTrackLayout.StartX,2.85f,z,0));
            // The real banner mesh hangs 2.926 m below its root. Two normally
            // supported poles keep the cloth above ground without wear hacks.
            result.Add(new DriftTrackPiece("piece_banner01",DriftTrackLayout.StartX,3.6f,z,0));
        }
        return result.AsReadOnly();
    }
    static void AddSign(List<DriftTrackPiece> pieces,float x,float z,string text,float yaw) {
        pieces.Add(new DriftTrackPiece("wood_pole2",x,.9f,z,yaw));
        double radians=yaw*Math.PI/180;
        pieces.Add(new DriftTrackPiece("sign",x-.2f*(float)Math.Sin(radians),1.4f,
            z-.2f*(float)Math.Cos(radians),(yaw+180)%360,text));
    }
}
public struct DriftTrackPiece {
    public readonly string Prefab,Text;
    public readonly float X,HeightOffset,Z,Yaw;
    public DriftTrackPiece(string prefab,float x,float heightOffset,float z,float yaw,string text=null) {
        Prefab=prefab;X=x;HeightOffset=heightOffset;Z=z;Yaw=yaw;Text=text;
    }
}
}
