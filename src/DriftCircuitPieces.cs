using System;
using System.Collections.Generic;

namespace Rachel350Z {
// A fixed, additive v2 venue. Native building pieces retain support, fuel,
// destruction and player editing. No objects or colliders are placed on the road.
public static class DriftCircuitPieces {
    public static readonly IReadOnlyList<DriftTrackPiece> Items=Create();
    static IReadOnlyList<DriftTrackPiece> Create() {
        var list=new List<DriftTrackPiece>();
        Garage(list);Grandstand(list);
        Sign(list,104,-12,"NFVU2 PADDOCK | BOXEN / PITS",0);
        Sign(list,96,48,"TRIBUENE / GRANDSTAND",90);
        int fences=(int)Math.Ceiling(DriftCircuitLayout.TotalLength/4.5f);
        for(int i=0;i<fences;i++) {
            var p=DriftCircuitLayout.AtDistance(DriftCircuitLayout.TotalLength*i/fences);
            foreach(int side in new[]{-1,1}) {
                float offset=side*(p.HalfWidth+DriftCircuitLayout.Runoff);
                float x=p.X-p.TangentZ*offset,z=p.Z+p.TangentX*offset;
                if(DriftCircuitPlan.PitOpening(x,z)||DriftCircuitLayout.Contains(x,z,3))continue;
                Add(list,"wood_fence",x,0,z,Yaw(p));
            }
        }
        // Repeated colour and height cues, without hundreds of particle lights.
        for(int i=0;i<12;i++) {
            var p=DriftCircuitLayout.AtDistance(DriftCircuitLayout.TotalLength*(i+.35f)/12);
            int side=i%2==0?1:-1;float offset=side*(p.HalfWidth+9);
            float x=p.X-p.TangentZ*offset,z=p.Z+p.TangentX*offset;
            if(DriftCircuitPlan.PitOpening(x,z)||DriftCircuitLayout.Contains(x,z,3))continue;
            Flag(list,x,z,Yaw(p),i%2==0?"piece_banner01":"piece_banner02");
        }
        // At most twelve native fuelled lamps; skip any course lamp at an opening.
        for(int i=0;i<8;i++) {
            var p=DriftCircuitLayout.AtDistance(DriftCircuitLayout.TotalLength*(i+.7f)/8);
            float offset=p.HalfWidth+8,x=p.X-p.TangentZ*offset,z=p.Z+p.TangentX*offset;
            if(!DriftCircuitPlan.PitOpening(x,z)&&!DriftCircuitLayout.Contains(x,z,3))Add(list,"piece_groundtorch",x,.6f,z,0);
        }
        foreach(float x in new[]{116f,148f}) {
            Add(list,"piece_groundtorch",x,.6f,-18,0);
            Add(list,"piece_groundtorch",x,.6f,62,0);
        }
        float[] stations={80,DriftTrackLayout.TotalLength-22,440,510,600,720,900,1130,1350};
        string[] labels={"1 | WEITE KEHRE / WIDE HAIRPIN","2 | RECHTS / RIGHT","3 | NORDKURVE / NORTH TURN",
            "4 | S-KOMBINATION / TRANSITIONS","5 | GEGENSCHWUNG / SWITCH","6 | AUSSENKURVE / OUTER TURN",
            "7 | LANGE LINIE / LONG LINE","8 | RUECKFUEHRUNG / RETURN","9 | ZIELKURVE / FINAL TURN"};
        for(int i=0;i<stations.Length;i++) {
            var p=DriftCircuitLayout.AtDistance(stations[i]);float offset=p.HalfWidth+6;
            float x=p.X-p.TangentZ*offset,z=p.Z+p.TangentX*offset;
            if(!DriftCircuitLayout.Contains(x,z,3))Sign(list,x,z,labels[i],(float)(Math.Atan2(p.TangentX,p.TangentZ)*180/Math.PI));
        }
        return list.AsReadOnly();
    }
    static void Garage(List<DriftTrackPiece> list) {
        // Four 6 m bays: enough for the 1.93 x 4.38 m 350Z and walking space.
        for(int x=120;x<=144;x+=6)foreach(int z in new[]{-17,-23}) {
            Add(list,"wood_pole2",x,.9f,z,0);Add(list,"wood_pole2",x,2.85f,z,0);
            if(z==-23){Add(list,"wood_pole2",x,4.8f,z,0);Add(list,"wood_pole",x,6.25f,z,0);}
        }
        // Build the back wall from the ground before the lean-to roof. Roof snap
        // points rise 1 m per 2 m toward -Z, measured from the installed prefab.
        for(int level=0;level<3;level++)for(int x=121;x<144;x+=2)
            Add(list,"woodwall",x,1+level*2,-23,0);
        for(int row=0;row<3;row++)for(int x=121;x<144;x+=2)
            Add(list,"wood_roof",x,3.6f+row,-18-row*2,0);
        for(int x=121;x<144;x+=2)Add(list,"wood_beam",x,3.6f,-17,0);
        Add(list,"piece_workbench",123,0,-21,180);Add(list,"piece_workbench",141,0,-21,180);
        Add(list,"piece_chest_wood",126,0,-21,0);Add(list,"piece_chest_wood",138,0,-21,0);
        Sign(list,120,-15,"NFVU2 GARAGE | 01 - 04",0);
        Flag(list,118,-24,0,"piece_banner02");Flag(list,146,-24,0,"piece_banner02");
    }
    static void Grandstand(List<DriftTrackPiece> list) {
        // Grounded stone terraces with timber seating; every elevated slab has
        // a complete column below it. No support checks are disabled.
        for(int row=0;row<3;row++)for(int level=0;level<=row;level++)for(int x=122;x<=142;x+=4)
            Add(list,"stone_floor",x,-.3f+level,54+row*4,0);
        for(int row=0;row<3;row++)for(int x=122;x<=142;x+=4)
            Add(list,"piece_bench01",x,.2f+row,54+row*4,0);
        for(int x=121;x<144;x+=2)Add(list,"wood_wall_half",x,2.7f,64,0);
        foreach(float x in new[]{119f,145f}) {
            // Native stair rises toward local -Z; yaw 180 makes north uphill.
            Add(list,"stone_floor_2x2",x,-.3f,53,0);
            Add(list,"stone_floor_2x2",x,-.3f,57,0);Add(list,"stone_floor_2x2",x,.7f,57,0);
            Add(list,"wood_stair",x,.2f,53,180);Add(list,"wood_stair",x,1.2f,57,180);
            for(int level=0;level<2;level++)Add(list,"stone_floor_2x2",x,-.3f+level,55,0);
            for(int level=0;level<3;level++)Add(list,"stone_floor_2x2",x,-.3f+level,59,0);
            Add(list,"wood_floor",x,1.2f,55,0);Add(list,"wood_floor",x,2.2f,59,0);
        }
        Flag(list,118,64,0,"piece_banner01");Flag(list,146,64,0,"piece_banner01");
    }
    static float Yaw(DriftTrackSample p)=>(float)(Math.Atan2(-p.TangentZ,p.TangentX)*180/Math.PI);
    static void Flag(List<DriftTrackPiece> list,float x,float z,float yaw,string prefab) {
        Add(list,"wood_pole2",x,.9f,z,yaw);Add(list,"wood_pole2",x,2.85f,z,yaw);Add(list,prefab,x,3.6f,z,yaw);
    }
    static void Sign(List<DriftTrackPiece> list,float x,float z,string text,float yaw) {
        Add(list,"wood_pole2",x,.9f,z,yaw);double angle=yaw*Math.PI/180;
        list.Add(new DriftTrackPiece("sign",x-.2f*(float)Math.Sin(angle),1.4f,z-.2f*(float)Math.Cos(angle),(yaw+180)%360,text));
    }
    static void Add(List<DriftTrackPiece> list,string prefab,float x,float y,float z,float yaw)=>list.Add(new DriftTrackPiece(prefab,x,y,z,yaw));
}
}
