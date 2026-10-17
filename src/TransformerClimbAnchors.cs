using System;
using System.Collections.Generic;
using UnityEngine;
namespace Rachel350Z {
internal sealed partial class TransformerVisual {
    struct ClimbContact {public int Bone;public Vector3 Local,Normal;}
    const int climbLevels=9;
    ClimbContact[] climbFeet,climbHands;
    struct ClimbBodyTriangle {public Vector3 Min,Max;}
    const float climbBodyCell=.06f;
    float[] climbBodyRear,climbBodyFront;
    int climbBodyWidth,climbBodyHeight;
    Vector3 climbBodyMinimum;
    float climbBodyTop;
    public int MountClimbLevels=>climbLevels;
    static bool ClimbSurface(string n)=>n=="panel.hatch"||n=="panel.spoiler"||n=="panel.pelvis_rear"||
        n.StartsWith("panel.floor0_L_")||n.StartsWith("panel.floor1_L_")||n=="panel.sill1_L"||n=="panel.sill2_L"||
        n=="panel.rear_arch_L"||n=="panel.rear_corner_L"||n.StartsWith("panel.apron_back_")||n.StartsWith("panel.roof_");

    // Select contacts from the actual, currently skinned panel triangles once
    // the robot has parked its gait carriers. No synthetic floating ladder.
    public void CacheMountClimbRoute() {
        var bodyTriangles=new List<ClimbBodyTriangle>();climbBodyTop=float.NegativeInfinity;
        var feet=new ClimbContact[climbLevels*2];var hands=new ClimbContact[climbLevels*2];
        var targets=new Vector3[climbLevels*4];var scores=new float[targets.Length];var contacts=new ClimbContact[targets.Length];
        float[] heights={.40f,.90f,1.40f,1.80f,2.45f,2.95f,3.45f,3.82f,3.88f};
        float[] centers={.83f,.76f,.64f,.48f,.32f,.18f,.04f,0,0};
        float[] backs={-.48f,-.52f,-.78f,-1.35f,-1.08f,-1.02f,-.98f,-.60f,-.62f};
        for(int level=0;level<climbLevels;level++)for(int side=0;side<2;side++) {
            float spread=level==climbLevels-1?.24f:.13f;
            targets[level*2+side]=new Vector3(centers[level]+(side==0?spread:-spread),heights[level],backs[level]);
            int grip=Math.Min(climbLevels-1,level+2);
            targets[climbLevels*2+level*2+side]=new Vector3(centers[grip]+(side==0?.16f:-.16f),Mathf.Min(3.94f,heights[level]+1.02f),level>=5?-1.05f:backs[grip]);
        }
        for(int i=0;i<scores.Length;i++)scores[i]=float.PositiveInfinity;
        foreach(var batch in asset.Batches) {
            if(batch.Concealed)continue;
            for(int triangle=0;triangle<batch.Indices.Length;triangle+=3) {
                int ia=batch.Indices[triangle],ib=batch.Indices[triangle+1],ic=batch.Indices[triangle+2];
                int bone=batch.Bones[batch.Weights[ia]];
                if(batch.Bones[batch.Weights[ib]]!=bone||batch.Bones[batch.Weights[ic]]!=bone||!ClimbSurface(asset.Manifest.nodes[bone].name))continue;
                var transform=pose[bone]*neutral[bone].inverse;
                var a=ClimbVertex(batch,ia,transform);var b=ClimbVertex(batch,ib,transform);var c=ClimbVertex(batch,ic,transform);
                string panel=asset.Manifest.nodes[bone].name;
                if(panel=="panel.hatch"||panel=="panel.spoiler") {
                    var min=Vector3.Min(a,Vector3.Min(b,c));var max=Vector3.Max(a,Vector3.Max(b,c));
                    bodyTriangles.Add(new ClimbBodyTriangle{Min=min,Max=max});
                    climbBodyTop=Mathf.Max(climbBodyTop,max.y);
                }
                var normal=Vector3.Cross(b-a,c-a).normalized;
                if(normal.sqrMagnitude<.1f)continue;
                for(int i=0;i<targets.Length;i++) {
                    // Feet may press onto a rear face while the opposite hand
                    // carries weight; upward ledges win when equally near.
                    bool foot=i<climbLevels*2;
                    // The seated boots are forward of the helmet. Standing
                    // uses the upper rear roof edge, never the seat footrests.
                    if(foot&&i>=2*(climbLevels-1)&&!asset.Manifest.nodes[bone].name.StartsWith("panel.roof_"))continue;
                    if(foot?normal.y<.12f&&normal.z>-.2f:normal.z>-.15f&&normal.y<.2f)continue;
                    var target=targets[i];
                    if(target.y<Mathf.Min(a.y,b.y,c.y)-.4f||target.y>Mathf.Max(a.y,b.y,c.y)+.4f)continue;
                    var point=ClosestClimbTriangle(target,a,b,c);var distance=point-target;
                    float score=distance.x*distance.x+6*distance.y*distance.y+.6f*distance.z*distance.z;
                    if(foot)score+=.018f*(1-Mathf.Clamp01(normal.y));
                    if(score>=scores[i])continue;
                    scores[i]=score;contacts[i]=new ClimbContact{Bone=bone,Local=pose[bone].inverse.MultiplyPoint3x4(point),Normal=pose[bone].inverse.MultiplyVector(normal).normalized};
                }
            }
        }
        for(int i=0;i<feet.Length;i++) {
            if(float.IsInfinity(scores[i])||float.IsInfinity(scores[feet.Length+i]))throw new InvalidOperationException("Transformer climbing route has no real panel contact");
            feet[i]=contacts[i];hands[i]=contacts[feet.Length+i];
        }
        climbFeet=feet;climbHands=hands;CacheClimbBody(bodyTriangles);
    }
    void CacheClimbBody(List<ClimbBodyTriangle> triangles) {
        var min=new Vector3(float.PositiveInfinity,float.PositiveInfinity,0);
        var max=new Vector3(float.NegativeInfinity,float.NegativeInfinity,0);
        foreach(var triangle in triangles){min=Vector3.Min(min,triangle.Min);max=Vector3.Max(max,triangle.Max);}
        climbBodyMinimum=min;
        climbBodyWidth=Mathf.Max(1,Mathf.CeilToInt((max.x-min.x)/climbBodyCell));
        climbBodyHeight=Mathf.Max(1,Mathf.CeilToInt((max.y-min.y)/climbBodyCell));
        climbBodyRear=new float[climbBodyWidth*climbBodyHeight];climbBodyFront=new float[climbBodyRear.Length];
        for(int i=0;i<climbBodyRear.Length;i++){climbBodyRear[i]=float.PositiveInfinity;climbBodyFront[i]=float.NegativeInfinity;}
        foreach(var triangle in triangles) {
            int x0=Mathf.Clamp(Mathf.FloorToInt((triangle.Min.x-min.x)/climbBodyCell),0,climbBodyWidth-1);
            int x1=Mathf.Clamp(Mathf.FloorToInt((triangle.Max.x-min.x)/climbBodyCell),0,climbBodyWidth-1);
            int y0=Mathf.Clamp(Mathf.FloorToInt((triangle.Min.y-min.y)/climbBodyCell),0,climbBodyHeight-1);
            int y1=Mathf.Clamp(Mathf.FloorToInt((triangle.Max.y-min.y)/climbBodyCell),0,climbBodyHeight-1);
            for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++) {
                int i=y*climbBodyWidth+x;
                climbBodyRear[i]=Mathf.Min(climbBodyRear[i],triangle.Min.z);climbBodyFront[i]=Mathf.Max(climbBodyFront[i],triangle.Max.z);
            }
        }
    }
    static Vector3 ClimbVertex(TransformerBatch batch,int vertex,Matrix4x4 transform) {
        int k=vertex*8;return transform.MultiplyPoint3x4(new Vector3(batch.Vertices[k],batch.Vertices[k+1],batch.Vertices[k+2]));
    }
    Vector3 ClimbWorld(ClimbContact contact)=>Root.transform.TransformPoint(pose[contact.Bone].MultiplyPoint3x4(contact.Local));
    public Vector3 MountClimbFoot(int level,bool right) {
        if(climbFeet==null)CacheMountClimbRoute();
        return ClimbWorld(climbFeet[Mathf.Clamp(level,0,climbLevels-1)*2+(right?1:0)]);
    }
    // Public side follows the avatar: Right is +vehicle X, Left is -X.
    public Vector3 MountStandFoot(bool right)=>MountClimbFoot(climbLevels-1,!right);
    public Vector3 MountStandPlatform=>(MountStandFoot(false)+MountStandFoot(true))*.5f;
    public Vector3 MountClimbGrip(int level,bool right) {
        if(climbHands==null)CacheMountClimbRoute();
        return ClimbWorld(climbHands[Mathf.Clamp(level,0,climbLevels-1)*2+(right?1:0)]);
    }
    Vector3 ClimbNormal(ClimbContact contact)=>Root.transform.TransformDirection(pose[contact.Bone].MultiplyVector(contact.Normal)).normalized;
    public Vector3 MountClimbFootNormal(int level,bool right) {
        if(climbFeet==null)CacheMountClimbRoute();
        return ClimbNormal(climbFeet[Mathf.Clamp(level,0,climbLevels-1)*2+(right?1:0)]);
    }
    public Vector3 MountClimbGripNormal(int level,bool right) {
        if(climbHands==null)CacheMountClimbRoute();
        return ClimbNormal(climbHands[Mathf.Clamp(level,0,climbLevels-1)*2+(right?1:0)]);
    }
    public Vector3 MountClimbPelvis(int level) {
        level=Mathf.Clamp(level,0,climbLevels-1);
        var feet=(MountClimbFoot(level,false)+MountClimbFoot(level,true))*.5f;
        if(level==climbLevels-1)return feet+Vector3.up*.82f;
        var hands=(MountClimbGrip(level,false)+MountClimbGrip(level,true))*.5f;
        float handForward=Vector3.Dot(hands-feet,Root.transform.forward);
        // Follow the overhanging knee cassette without putting the torso too
        // far behind either planted feet or the next real handhold.
        float behind=Mathf.Clamp(.30f-handForward,.10f,.37f);
        float stance=level>=4&&level<=6?.68f:.82f;
        var pelvis=feet+Vector3.up*stance-Root.transform.forward*behind;
        // The hatch is a near-vertical backpack: feet can touch its face while
        // the hips and bent thighs must stay behind its actual outer surface.
        return MountClimbSafePelvis(pelvis);
    }
    public float MountClimbMantleHeight {
        get {
            if(climbBodyRear==null)CacheMountClimbRoute();
            return Root.transform.TransformPoint(new Vector3(0,climbBodyTop,0)).y+.24f;
        }
    }
    public Vector3 MountClimbSafePelvis(Vector3 world,float radius=.26f) {
        if(climbBodyRear==null)CacheMountClimbRoute();
        var local=Root.transform.InverseTransformPoint(world);
        float rear=float.PositiveInfinity,front=float.NegativeInfinity;
        // A cached cross-section grid keeps all mesh scanning at mount start.
        // Circular hip/chest/head sections taper at edges rather than switching
        // the rear limit abruptly at a single hatch height. Knees are posed
        // outside separately; a thigh-sized box would make grips unreachable.
        for(int section=0;section<3;section++) {
            float centerY=local.y+(section==0?0:section==1?.35f:.75f);
            float r=radius*(section==0?.77f:section==1?.92f:.62f);
            int x0=Mathf.Max(0,Mathf.FloorToInt((local.x-r-climbBodyMinimum.x)/climbBodyCell));
            int x1=Mathf.Min(climbBodyWidth-1,Mathf.FloorToInt((local.x+r-climbBodyMinimum.x)/climbBodyCell));
            int y0=Mathf.Max(0,Mathf.FloorToInt((centerY-r-climbBodyMinimum.y)/climbBodyCell));
            int y1=Mathf.Min(climbBodyHeight-1,Mathf.FloorToInt((centerY+r-climbBodyMinimum.y)/climbBodyCell));
            for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++) {
                int i=y*climbBodyWidth+x;if(float.IsInfinity(climbBodyRear[i]))continue;
                float left=climbBodyMinimum.x+x*climbBodyCell,bottom=climbBodyMinimum.y+y*climbBodyCell;
                float right=left+climbBodyCell,top=Mathf.Min(bottom+climbBodyCell,climbBodyTop);
                float dx=Mathf.Max(left-local.x,Mathf.Max(0,local.x-right));
                float dy=Mathf.Max(bottom-centerY,Mathf.Max(0,centerY-top));
                float remaining=r*r-dx*dx-dy*dy;
                if(remaining<=0)continue;
                float reach=Mathf.Sqrt(remaining);
                rear=Mathf.Min(rear,climbBodyRear[i]-reach);front=Mathf.Max(front,climbBodyFront[i]+reach);
            }
        }
        // Once the complete body has crossed above the lip it may descend on
        // its front side. Otherwise hold its centre behind the rear envelope.
        if(!float.IsInfinity(rear)&&local.z<front)local.z=Mathf.Min(local.z,rear);
        return Root.transform.TransformPoint(local);
    }
    static Vector3 ClosestClimbTriangle(Vector3 p,Vector3 a,Vector3 b,Vector3 c) {
        var ab=b-a;var ac=c-a;var ap=p-a;float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);
        if(d1<=0&&d2<=0)return a;
        var bp=p-b;float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);
        if(d3>=0&&d4<=d3)return b;
        float vc=d1*d4-d3*d2;if(vc<=0&&d1>=0&&d3<=0)return a+ab*(d1/(d1-d3));
        var cp=p-c;float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);
        if(d6>=0&&d5<=d6)return c;
        float vb=d5*d2-d1*d6;if(vb<=0&&d2>=0&&d6<=0)return a+ac*(d2/(d2-d6));
        float va=d3*d6-d5*d4;if(va<=0&&(d4-d3)>=0&&(d5-d6)>=0)return b+(c-b)*((d4-d3)/((d4-d3)+(d5-d6)));
        float inverse=1/(va+vb+vc);return a+ab*(vb*inverse)+ac*(vc*inverse);
    }
}
}
