using System;

namespace Rachel350Z {
public struct TransformerWaterPoint {
    public float X,Z;
    public TransformerWaterPoint(float x,float z){X=x;Z=z;}
}
public struct TransformerWaterPose {
    public float X,Z,Yaw;
    // Yaw is radians, with the same +Y convention as the vehicle root.
    public TransformerWaterPose(float x,float z,float yaw){X=x;Z=z;Yaw=yaw;}
}
public delegate bool TransformerWaterProbe(float x,float z,out float support,out float liquid);
// Pure shoreline policy. Native support/bridge and moving water are supplied
// by the adapter, rather than substituting sea level for the support surface.
public static class TransformerWaterRules {
    public const float Clearance=.03f,MaximumSweepStep=.12f;
    public const float DefaultMaximumDepth=2.75f;
    const float DepthEpsilon=.0001f;
    const int MaximumSamples=64;
    // Thorax is the lower torso pivot; thirty percent toward the shoulder
    // line places the waterline on the actual chest rather than on the head.
    public static float ChestDepth(float thoraxHeight,float shoulderHeight,float soleHeight) {
        if(!Finite(thoraxHeight)||!Finite(shoulderHeight)||!Finite(soleHeight)||shoulderHeight<=thoraxHeight||thoraxHeight<=soleHeight)return DefaultMaximumDepth;
        return thoraxHeight+(shoulderHeight-thoraxHeight)*.30f-soleHeight;
    }
    public static bool Dry(TransformerWaterPose pose,TransformerWaterPoint[] points,int count,TransformerWaterProbe probe) {
        if(!Valid(pose,points,count)||probe==null)return false;
        for(int i=0;i<count;i++) {
            World(pose,points[i],out float x,out float z);
            if(!Depth(probe,x,z,out float depth,Clearance)||depth>0)return false;
        }
        return true;
    }
    public static bool WithinDepth(TransformerWaterPose pose,TransformerWaterPoint[] points,int count,TransformerWaterProbe probe,float maximumDepth) {
        if(!Valid(pose,points,count)||probe==null||!Finite(maximumDepth)||maximumDepth<0)return false;
        for(int i=0;i<count;i++) {
            World(pose,points[i],out float x,out float z);
            if(!Depth(probe,x,z,out float depth)||depth>maximumDepth+DepthEpsilon)return false;
        }
        return true;
    }
    public static bool CanTraverse(TransformerWaterPose from,TransformerWaterPose to,TransformerWaterPoint[] points,int count,
        TransformerWaterProbe probe,float[] startingDepth,bool allowWetEscape=false,TransformerWaterPoint[] futurePoints=null,int futureCount=0,float maximumDepth=DefaultMaximumDepth) {
        if(!Valid(from,points,count)||!Valid(to,points,count)||probe==null||startingDepth==null||startingDepth.Length<count||!Finite(maximumDepth)||maximumDepth<0)return false;
        if(futureCount!=0&&!Valid(from,futurePoints,futureCount))return false;
        float radius=0;
        for(int i=0;i<count;i++) {
            World(from,points[i],out float x,out float z);
            if(!Depth(probe,x,z,out startingDepth[i]))return false;
            if(!allowWetEscape&&startingDepth[i]>maximumDepth+DepthEpsilon)return false;
            radius=Math.Max(radius,(float)Math.Sqrt(points[i].X*points[i].X+points[i].Z*points[i].Z));
        }
        for(int i=0;i<futureCount;i++)radius=Math.Max(radius,(float)Math.Sqrt(futurePoints[i].X*futurePoints[i].X+futurePoints[i].Z*futurePoints[i].Z));
        float yaw=to.Yaw-from.Yaw;
        yaw=(float)Math.Atan2(Math.Sin(yaw),Math.Cos(yaw));
        double dx=to.X-from.X,dz=to.Z-from.Z;
        double sweptDistance=Math.Sqrt(dx*dx+dz*dz)+Math.Abs(yaw)*radius;
        if(!Finite((float)sweptDistance)||sweptDistance>MaximumSamples*MaximumSweepStep)return false;
        int steps=Math.Max(1,(int)Math.Ceiling(sweptDistance/MaximumSweepStep));
        for(int step=1;step<=steps;step++) {
            float t=(float)step/steps;
            var pose=new TransformerWaterPose(from.X+(to.X-from.X)*t,from.Z+(to.Z-from.Z)*t,from.Yaw+yaw*t);
            for(int i=0;i<count;i++) {
                World(pose,points[i],out float x,out float z);
                float limit=allowWetEscape?Math.Max(maximumDepth,startingDepth[i]):maximumDepth;
                if(!Depth(probe,x,z,out float depth)||depth>limit+DepthEpsilon)return false;
            }
            // Predicted landings belong only to the requested travel direction.
            // They are never current wet baseline: a dry robot at the shore
            // must still be free to reverse away or turn toward shallow ground.
            if(!allowWetEscape&&futureCount>0&&!WithinDepth(pose,futurePoints,futureCount,probe,maximumDepth))return false;
        }
        // Too-deep saves may leave along equal or shallower support; support
        // already below the chest limit can use the normal wading allowance.
        return true;
    }
    static bool Depth(TransformerWaterProbe probe,float x,float z,out float depth,float clearance=0) {
        depth=0;
        if(!probe(x,z,out float support,out float liquid)||!Finite(support)||!Finite(liquid))return false;
        depth=Math.Max(0,liquid+clearance-support);return Finite(depth);
    }
    static void World(TransformerWaterPose pose,TransformerWaterPoint point,out float x,out float z) {
        float s=(float)Math.Sin(pose.Yaw),c=(float)Math.Cos(pose.Yaw);
        x=pose.X+c*point.X+s*point.Z;z=pose.Z-s*point.X+c*point.Z;
    }
    static bool Valid(TransformerWaterPose pose,TransformerWaterPoint[] points,int count) {
        if(!Finite(pose.X)||!Finite(pose.Z)||!Finite(pose.Yaw)||points==null||count<1||count>64||count>points.Length)return false;
        for(int i=0;i<count;i++)if(!Finite(points[i].X)||!Finite(points[i].Z))return false;
        return true;
    }
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
}
}
