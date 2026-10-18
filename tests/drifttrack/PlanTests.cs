using System;

namespace Rachel350Z {
public static class PlanTests {
    static int checks;
    static void Check(bool condition,string name) {
        if(!condition)throw new Exception("Drift track plan: "+name);
        checks++;
    }
    static bool Covered(float x,float z) {
        foreach(var stamp in DriftTrackConstructionPlan.PaintStamps) {
            double dx=(double)x-stamp.X,dz=(double)z-stamp.Z;
            if(dx*dx+dz*dz<=(double)stamp.Radius*stamp.Radius)return true;
        }
        return false;
    }
    static void Reject(Action action,string name) {
        bool rejected=false;
        try {action();}catch(ArgumentOutOfRangeException){rejected=true;}
        Check(rejected,name);
    }
    public static int Run() {
        checks=0;
        var stamps=DriftTrackConstructionPlan.PaintStamps;
        int intervals=(int)Math.Ceiling(DriftTrackLayout.TotalLength/4f);
        Check(stamps.Count==intervals+1,"bounded deterministic count");
        Check(stamps[0].DistanceAlong==0,"start included");
        Check(stamps[stamps.Count-1].DistanceAlong==DriftTrackLayout.TotalLength,"exact end included");
        bool straight=false,curve=false,returnStraight=false;
        for(int i=0;i<stamps.Count;i++) {
            var s=stamps[i];
            float expectedAlong=i==intervals?DriftTrackLayout.TotalLength:DriftTrackLayout.TotalLength*i/intervals;
            var p=DriftTrackLayout.AtDistance(expectedAlong);
            Check(s.DistanceAlong==expectedAlong&&s.X==p.X&&s.Z==p.Z&&s.Radius==p.HalfWidth,
                "stamp follows immutable exact layout "+i);
            if(i>0)Check(s.DistanceAlong>stamps[i-1].DistanceAlong
                &&s.DistanceAlong-stamps[i-1].DistanceAlong<=4.0001f,"ordered <=4 m spacing "+i);
            Check(!DriftTrackConstructionPlan.IntersectsTile(s,0,0,32),"spawn tile excluded "+i);
            straight|=s.DistanceAlong<DriftTrackLayout.StraightLength;
            curve|=s.DistanceAlong>DriftTrackLayout.StraightLength
                &&s.DistanceAlong<DriftTrackLayout.StraightLength+DriftTrackLayout.CurveLength;
            returnStraight|=s.DistanceAlong>DriftTrackLayout.StraightLength+DriftTrackLayout.CurveLength;
        }
        Check(straight&&curve&&returnStraight,"all three course sections sampled");
        for(int i=0;i<=2000;i++) {
            var p=DriftTrackLayout.AtDistance(DriftTrackLayout.TotalLength*i/2000);
            Check(Covered(p.X,p.Z),"continuous straight and curve centre coverage "+i);
        }
        Check(Covered(DriftTrackLayout.StartX,DriftTrackLayout.StartZ),"start round cap");
        Check(Covered(DriftTrackLayout.StartX,DriftTrackLayout.StartZ+2*DriftTrackLayout.CurveRadius),"return round cap");
        var tangent=new DriftTrackStamp(2,1,5,0);
        Check(DriftTrackConstructionPlan.IntersectsTile(tangent,10,10,5),"3-4-5 corner tangent included");
        Check(!DriftTrackConstructionPlan.IntersectsTile(new DriftTrackStamp(1.99f,1,5,0),10,10,5),"outside corner rejected");
        Check(DriftTrackConstructionPlan.IntersectsTile(new DriftTrackStamp(-2,-1,5,0),-10,-10,5),"negative corner tangent");
        Check(DriftTrackConstructionPlan.IntersectsTile(new DriftTrackStamp(0,0,2,0),4,0,2),"edge tangent included");
        Check(!DriftTrackConstructionPlan.IntersectsTile(new DriftTrackStamp(0,0,2,0),4.01f,0,2),"outside edge rejected");
        Check(DriftTrackConstructionPlan.IntersectsTile(new DriftTrackStamp(-64,-64,0,0),-64,-64,0),"zero-radius coincident tile");
        Check(!DriftTrackConstructionPlan.IntersectsTile(new DriftTrackStamp(-64,-64,0,0),-63,-64,0),"zero-radius disjoint tile");
        Check(DriftTrackConstructionPlan.IntersectsTile(new DriftTrackStamp(32,32,1,0),64,64,32),"positive zone corner");
        Check(DriftTrackConstructionPlan.IntersectsTile(new DriftTrackStamp(-32,-32,1,0),-64,-64,32),"negative zone corner");
        Check(DriftTrackConstructionPlan.IntersectsTile(new DriftTrackStamp(float.MaxValue,0,float.MaxValue,0),0,0,0),"large finite tangent avoids overflow");
        Check(!DriftTrackConstructionPlan.IntersectsTile(new DriftTrackStamp(float.MaxValue,float.MaxValue,float.MaxValue,0),0,0,0),"large finite corner avoids overflow");
        Check(DriftTrackConstructionPlan.ResumeIndex(int.MinValue,10)==0,"negative progress clamps to start");
        Check(DriftTrackConstructionPlan.ResumeIndex(0,10)==0,"fresh tile");
        Check(DriftTrackConstructionPlan.ResumeIndex(4,10)==4,"partial tile resumes");
        Check(DriftTrackConstructionPlan.ResumeIndex(10,10)==10,"finished tile stays finished");
        Check(DriftTrackConstructionPlan.ResumeIndex(11,10)==10,"future marker does not restart");
        Check(DriftTrackConstructionPlan.ResumeIndex(int.MaxValue,10)==10,"maximum marker does not restart");
        Check(DriftTrackConstructionPlan.ResumeIndex(12,0)==0,"empty tile stays finished");
        Reject(()=>DriftTrackConstructionPlan.ResumeIndex(0,-1),"negative total rejected");
        Reject(()=>DriftTrackConstructionPlan.IntersectsTile(tangent,0,0,-1),"negative tile size rejected");
        Reject(()=>DriftTrackConstructionPlan.IntersectsTile(new DriftTrackStamp(0,0,-1,0),0,0,1),"negative radius rejected");
        Reject(()=>DriftTrackConstructionPlan.IntersectsTile(tangent,float.NaN,0,1),"NaN tile rejected");
        Reject(()=>DriftTrackConstructionPlan.IntersectsTile(new DriftTrackStamp(float.PositiveInfinity,0,1,0),0,0,1),"infinite stamp rejected");
        return checks;
    }
}
}
