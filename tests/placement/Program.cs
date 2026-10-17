using System;
using Rachel350Z;
static class Program {
    static int checks;
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
    static TransformerGroundSample[] Surface(float a=0,float b=0,float height=0) {
        var result=new TransformerGroundSample[9];int i=0;
        float ny=1/(float)Math.Sqrt(1+a*a+b*b);
        for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++) {
            float px=x*1.25f,pz=z*1.75f;result[i++]=new TransformerGroundSample(px,pz,height+a*px+b*pz,ny);
        }
        return result;
    }
    static void Main() {
        Check(TransformerPlacement.Fit(Surface(height:42),out var flat)&&Math.Abs(flat.Height-42)<1e-5&&flat.Residual==0,"flat supporting ground retains its actual world height");
        float mild=(float)Math.Tan(6*Math.PI/180);
        foreach(int sign in new[]{-1,1}) {
            Check(TransformerPlacement.Fit(Surface(a:sign*mild,height:10),out var side)&&Math.Abs(side.SlopeX-sign*mild)<1e-5,"ordinary cross-slope is accepted in direction "+sign);
            Check(TransformerPlacement.Fit(Surface(b:sign*mild,height:10),out var forward)&&Math.Abs(forward.SlopeZ-sign*mild)<1e-5,"ordinary forward slope is accepted in direction "+sign);
        }
        float boundary=(float)Math.Tan(8*Math.PI/180);
        Check(TransformerPlacement.Fit(Surface(a:boundary),out _),"reviewed eight-degree slope boundary is accepted");
        Check(!TransformerPlacement.Fit(Surface(a:(float)Math.Tan(9*Math.PI/180)),out _),"steeper unreviewed terrain is refused");
        Check(!TransformerPlacement.Fit(Surface(a:mild,b:mild),out _),"combined slope uses its true gradient rather than two independent axis limits");
        var rough=Surface(a:.1f,height:5);rough[4].Height+=.05f;
        Check(TransformerPlacement.Fit(rough,out var uneven)&&uneven.Residual>.03f,"minor unevenness retains a fitted support plane within the reviewed residual limit");
        var hole=Surface();hole[0].Height=-.5f;
        Check(!TransformerPlacement.Fit(hole,out _),"a missing-height corner cannot masquerade as a walkable slope");
        var step=Surface();for(int i=0;i<step.Length;i++)if(step[i].Z>0)step[i].Height=.7f;
        Check(!TransformerPlacement.Fit(step,out _),"a stair or ledge is rejected rather than stepping through it");
        var invalid=Surface();invalid[3].Height=float.NaN;
        Check(!TransformerPlacement.Fit(invalid,out _),"invalid terrain height does not enter the IK plane");
        invalid=Surface();invalid[3].NormalY=float.PositiveInfinity;
        Check(!TransformerPlacement.Fit(invalid,out _),"invalid surface normal is rejected");
        invalid=Surface();invalid[3].NormalY=.4f;
        Check(!TransformerPlacement.Fit(invalid,out _),"a ray hitting a steep obstacle does not count as a foothold");
        Check(!TransformerPlacement.Fit(null,out _)&&!TransformerPlacement.Fit(new TransformerGroundSample[4],out _),"missing support samples refuse placement");
        var line=Surface();for(int i=0;i<line.Length;i++)line[i].Z=0;
        Check(!TransformerPlacement.Fit(line,out _),"degenerate samples cannot prove support in both dimensions");
        Check(TransformerPlacement.StepAllowed(5,5.05f,.14f),"running along a mild incline advances continuously");
        Check(!TransformerPlacement.StepAllowed(5,5.5f,.14f)&&!TransformerPlacement.StepAllowed(5,4.5f,.14f),"sudden upward and downward ledges both stop movement");
        Check(!TransformerPlacement.StepAllowed(float.NaN,0,0)&&!TransformerPlacement.StepAllowed(0,0,-1),"invalid movement samples are refused");
        float walkingBoundary=(float)Math.Tan(20*Math.PI/180);
        Check(TransformerPlacement.FitWalking(Surface(a:walkingBoundary),out _)&&TransformerPlacement.FitWalking(Surface(b:-walkingBoundary),out _),"deployed walking accepts twenty-degree terrain in both boot directions");
        Check(!TransformerPlacement.FitWalking(Surface(a:(float)Math.Tan(21*Math.PI/180)),out _)&&!TransformerPlacement.Fit(Surface(a:walkingBoundary),out _),"walking slope remains bounded while entry and return keep their stricter support limit");
        var walkingBump=Surface();walkingBump[4].Height=.2f;
        Check(TransformerPlacement.FitWalking(walkingBump,out var walkingPlane)&&walkingPlane.Residual>.17f&&!TransformerPlacement.Fit(walkingBump,out _),"walking tolerates a small local bump without authorizing packed-car transformation on it");
        Check(!TransformerPlacement.FitWalking(hole,out _)&&!TransformerPlacement.FitWalking(step,out _),"walking tolerance does not turn a half-metre hole or large ledge into fitted support");
        Check(!TransformerPlacement.FitWalking(null,out _)&&!TransformerPlacement.FitWalking(invalid,out _),"relaxed walking still rejects absent ground and steep obstacle normals");
        Check(TransformerPlacement.StepAllowed(0,.30f,0)&&!TransformerPlacement.StepAllowed(0,.31f,0),"walking allows bounded thirty-centimetre height changes but retains a hard ledge limit");
        Check(TransformerPlacement.TerrainTolerance(true,true,.1f)==.22f&&TransformerPlacement.TerrainTolerance(true,true,1)==.045f,"only low boot and shin points receive the bounded walking terrain sink");
        Check(TransformerPlacement.TerrainTolerance(false,true,.1f)==.045f&&TransformerPlacement.TerrainTolerance(true,false,.1f)==.045f&&TransformerPlacement.TerrainTolerance(true,true,float.NaN)==.045f,"transformation and upper body terrain clearance stay strict without weakening solid obstacle checks");
        Check(TransformerPlacement.Align(0)==0&&TransformerPlacement.Align(.65f)==0&&Math.Abs(TransformerPlacement.Align(.825f)-.5f)<1e-5&&TransformerPlacement.Align(1)==1,"car tilt is preserved while packed and smoothly aligns during the final unfolding");
        float previous=0;bool continuous=true;
        for(int i=0;i<=1000;i++){float current=TransformerPlacement.Align(i/1000f);if(current<previous||current-previous>.005f)continuous=false;previous=current;}
        Check(continuous,"upright alignment progresses without an endpoint jump");
        Check(Math.Abs(TransformerPlacement.TriangleHeight(.25f,.25f,0,1,2,3)-.75f)<1e-6,"terrain collider height interpolates its first triangle");
        Check(Math.Abs(TransformerPlacement.TriangleHeight(.75f,.75f,0,1,2,3)-2.25f)<1e-6,"terrain collider height interpolates its second triangle");
        Check(TransformerPlacement.TriangleHeight(.25f,.25f,0,0,0,1)==0&&Math.Abs(TransformerPlacement.TriangleHeight(.75f,.75f,0,0,0,1)-.5f)<1e-6,"nonplanar height cells use actual collider triangles rather than bilinear or nearest-vertex samples");
        Check(TransformerPlacement.TriangleHeight(0,0,2,4,6,9)==2&&TransformerPlacement.TriangleHeight(1,1,2,4,6,9)==9,"height cell boundary vertices retain their exact values");
        Check(Math.Abs(TransformerPlacement.TriangleHeight(.4f,.6f,0,1,2,4)-1.6f)<1e-6,"both terrain triangles meet continuously at the diagonal");
        Console.WriteLine(checks+" placement checks passed; native Unity collision queries are NOT executed here.");
    }
}
