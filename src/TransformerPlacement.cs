using System;
namespace Rachel350Z {
// Terrain policy is independent of Unity, so slopes, holes and ledges can be
// checked without pretending that the managed tests execute native physics.
public struct TransformerGroundSample {
    public float X,Z,Height,NormalY;
    public TransformerGroundSample(float x,float z,float height,float normalY=1){X=x;Z=z;Height=height;NormalY=normalY;}
}
public struct TransformerGroundPlane {
    public float Height,SlopeX,SlopeZ,NormalY,Residual;
    public float At(float x,float z)=>Height+SlopeX*x+SlopeZ*z;
}
public static class TransformerPlacement {
    public const float MaximumSlope=8,MaximumResidual=.06f,MaximumEntryTilt=20;
    // Opening/closing the packed car keeps its reviewed strict support plane.
    // The deployed machine may step over ordinary uneven walking terrain.
    public const float WalkingMaximumSlope=20,WalkingMaximumResidual=.25f,WalkingMaximumStep=.30f,WalkingTerrainSink=.22f;
    public static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    public static float Align(float progress) {
        float t=Math.Max(0,Math.Min(1,(progress-.65f)/.35f));return t*t*(3-2*t);
    }
    public static bool FitWalking(TransformerGroundSample[] samples,out TransformerGroundPlane plane) {
        if(!Fit(samples,out plane,WalkingMaximumResidual,WalkingMaximumSlope))return false;
        float low=0,high=0;
        foreach(var p in samples){float error=p.Height-plane.At(p.X,p.Z);low=Math.Min(low,error);high=Math.Max(high,error);}
        // A fitted incline may be steep, but a discontinuous ledge must not
        // become acceptable merely because its positive/negative errors cancel.
        return high-low<=WalkingMaximumStep+1e-6f;
    }
    public static float TerrainTolerance(bool walking,bool lowerPart,float heightAboveRoot)=>
        walking&&lowerPart&&Finite(heightAboveRoot)&&heightAboveRoot<=.9f?WalkingTerrainSink:.045f;
    public static bool Fit(TransformerGroundSample[] samples,out TransformerGroundPlane plane,float residualLimit=MaximumResidual,float slopeLimit=MaximumSlope) {
        plane=new TransformerGroundPlane();
        if(samples==null||samples.Length<5||!Finite(residualLimit)||residualLimit<0||!Finite(slopeLimit)||slopeLimit<0||slopeLimit>=90)return false;
        double x=0,z=0,h=0;
        foreach(var p in samples) {
            if(!Finite(p.X)||!Finite(p.Z)||!Finite(p.Height)||!Finite(p.NormalY)||p.NormalY<.8f||p.NormalY>1.001f)return false;
            x+=p.X;z+=p.Z;h+=p.Height;
        }
        x/=samples.Length;z/=samples.Length;h/=samples.Length;
        double xx=0,zz=0,xz=0,xh=0,zh=0;
        foreach(var p in samples) {
            double dx=p.X-x,dz=p.Z-z,dh=p.Height-h;
            xx+=dx*dx;zz+=dz*dz;xz+=dx*dz;xh+=dx*dh;zh+=dz*dh;
        }
        double determinant=xx*zz-xz*xz;
        if(determinant<1e-8)return false;
        double a=(xh*zz-zh*xz)/determinant,b=(zh*xx-xh*xz)/determinant;
        double slope=Math.Sqrt(a*a+b*b),normalY=1/Math.Sqrt(1+slope*slope);
        if(slope>Math.Tan(slopeLimit*Math.PI/180)+1e-6)return false;
        plane.Height=(float)(h-a*x-b*z);plane.SlopeX=(float)a;plane.SlopeZ=(float)b;plane.NormalY=(float)normalY;
        foreach(var p in samples)plane.Residual=Math.Max(plane.Residual,Math.Abs(p.Height-plane.At(p.X,p.Z)));
        return plane.Residual<=residualLimit;
    }
    public static bool StepAllowed(float previous,float next,float distance) {
        return Finite(previous)&&Finite(next)&&Finite(distance)&&distance>=0&&
            Math.Abs(next-previous)<=WalkingMaximumStep+(float)Math.Tan(WalkingMaximumSlope*Math.PI/180)*distance;
    }
    // Valheim's collider splits each height cell across (1,0)-(0,1).
    // Nearest-grid GetWorldHeight is not the surface under a walking vertex.
    public static float TriangleHeight(float x,float z,float h00,float h10,float h01,float h11)=>
        x+z<=1?h00+(h10-h00)*x+(h01-h00)*z:h11+(h01-h11)*(1-x)+(h10-h11)*(1-z);
}
}
