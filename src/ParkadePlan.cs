using System;
using System.Collections.Generic;

namespace Rachel350Z {
// Native-material interpretation of the measured route and genuine road grade;
// original concrete meshes, textures and original game scripts are unused.
// Ordering is part of the independent v1 saved construction contract.
public static class ParkadePlan {
    public static readonly IReadOnlyList<DriftTrackStamp> PaintStamps=Paint();
    public static readonly IReadOnlyList<DriftTrackPiece> Pieces=Build();
    static IReadOnlyList<DriftTrackStamp> Paint() {
        var result=new List<DriftTrackStamp>();int count=(int)Math.Ceiling(ParkadeLayout.TotalLength/3);
        for(int i=0;i<count;i++) {
            var p=ParkadeLayout.AtDistance(ParkadeLayout.TotalLength*i/count);
            result.Add(new DriftTrackStamp(p.X,p.Z,p.HalfWidth+.15f,p.DistanceAlong));
        }
        // The source is a navigation polyline, so explicitly stamp its vertices
        // as well. Otherwise a sharp corner can leave an unpainted road edge.
        foreach(float distance in ParkadeLayout.JunctionDistances) {
            var p=ParkadeLayout.AtDistance(distance);
            result.Add(new DriftTrackStamp(p.X,p.Z,p.HalfWidth+.15f,p.DistanceAlong));
        }
        return result.AsReadOnly();
    }
    static IReadOnlyList<DriftTrackPiece> Build() {
        var result=new List<DriftTrackPiece>();
        int walls=(int)Math.Ceiling(ParkadeLayout.TotalLength/1.9f);
        for(int i=0;i<walls;i++) {
            var p=ParkadeLayout.AtDistance(ParkadeLayout.TotalLength*i/walls);
            foreach(int side in new[]{-1,1}) {
                float o=side*(p.HalfWidth+2.2f),x=p.X-p.TangentZ*o,z=p.Z+p.TangentX*o;
                float yaw=Yaw(p);
                if(!ClearBox(x,z,yaw,1,.5f,.6f))continue;
                result.Add(new DriftTrackPiece("stone_wall_2x1",x,.25f,z,yaw));
            }
        }
        // Stone piers and small supported covered bays give the parking venue a
        // built structure without hundreds of colliders over the driving lane.
        // Every upper piece has a native support chain to the ground.
        for(int i=0;i<20;i++) {
            var p=ParkadeLayout.AtDistance(ParkadeLayout.TotalLength*(i+.5f)/20);
            foreach(int side in new[]{-1,1}) {
                float o=side*(p.HalfWidth+5.5f),x=p.X-p.TangentZ*o,z=p.Z+p.TangentX*o,yaw=Yaw(p);
                if(!ClearBox(x,z,yaw,2,2,1.4f))continue;
                for(int level=0;level<5;level++)result.Add(new DriftTrackPiece("stone_wall_2x1",x,.25f+level,z,yaw));
                result.Add(new DriftTrackPiece("stone_floor",x,4.75f,z,yaw));
                if(i%3==0)result.Add(new DriftTrackPiece("piece_banner02",x,4.4f,z,yaw));
            }
        }
        // Small number of real, normally fuelled Valheim lamps. Never copy
        // thousands of particle lights or disable native destruction/support.
        for(int i=0;i<12;i++) {
            var p=ParkadeLayout.AtDistance(ParkadeLayout.TotalLength*(i+.2f)/12);
            float o=p.HalfWidth+3.6f,x=p.X-p.TangentZ*o,z=p.Z+p.TangentX*o;
            if(ClearBox(x,z,0,.13f,.13f,1))result.Add(new DriftTrackPiece("piece_groundtorch",x,.65f,z,0));
        }
        var start=ParkadeLayout.AtDistance(0);float off=start.HalfWidth+4.2f;
        float sx=start.X-start.TangentZ*off,sz=start.Z+start.TangentX*off;
        if(ClearBox(sx,sz,0,.55f,.55f,1)) {
            float yaw=(float)(Math.Atan2(start.TangentX,start.TangentZ)*180/Math.PI);
            result.Add(new DriftTrackPiece("wood_pole2",sx,.9f,sz,yaw));
            double a=yaw*Math.PI/180;
            result.Add(new DriftTrackPiece("sign",sx-.2f*(float)Math.Sin(a),1.4f,sz-.2f*(float)Math.Cos(a),(yaw+180)%360,"PARKADE 1 | START / ZIEL | 3 RUNDEN"));
        }
        return result.AsReadOnly();
    }
    static float Yaw(DriftTrackSample p)=>(float)(Math.Atan2(-p.TangentZ,p.TangentX)*180/Math.PI);
    // Exact native collider extents for the pieces above, transformed into the
    // road plane. Checking corners plus mid-edge catches tight inner hairpins.
    public static bool ClearBox(float x,float z,float yaw,float halfX,float halfZ,float clearance) {
        double a=yaw*Math.PI/180,c=Math.Cos(a),s=Math.Sin(a);
        foreach(int ix in new[]{-1,0,1})foreach(int iz in new[]{-1,0,1}) {
            float wx=x+(float)(c*halfX*ix+s*halfZ*iz),wz=z+(float)(-s*halfX*ix+c*halfZ*iz);
            if(ParkadeLayout.Project(wx,wz).SignedEdgeDistance<clearance)return false;
        }
        return true;
    }
}
}
