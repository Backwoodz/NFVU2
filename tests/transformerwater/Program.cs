using System;
using Rachel350Z;
static class Program {
    static int checks;
    const float Limit=TransformerWaterRules.DefaultMaximumDepth;
    static readonly TransformerWaterPoint[] Center={new TransformerWaterPoint(0,0)};
    static readonly TransformerWaterPoint[] Feet={new TransformerWaterPoint(-1,-.6f),new TransformerWaterPoint(-1,.6f),new TransformerWaterPoint(1,-.6f),new TransformerWaterPoint(1,.6f),new TransformerWaterPoint(0,0)};
    static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
    static bool Walk(TransformerWaterPose from,TransformerWaterPose to,TransformerWaterProbe probe,TransformerWaterPoint[] points=null,bool escape=false,float maximumDepth=Limit) {
        var footprint=points??Center;return TransformerWaterRules.CanTraverse(from,to,footprint,footprint.Length,probe,new float[footprint.Length],escape,maximumDepth:maximumDepth);
    }
    static TransformerWaterPose At(float x=0,float z=0,float yaw=0)=>new TransformerWaterPose(x,z,yaw);
    static bool Slope(float x,float z,out float support,out float liquid){support=-x;liquid=0;return true;}
    static bool Dry(float x,float z,out float support,out float liquid){support=2;liquid=0;return true;}
    static bool Shallow(float x,float z,out float support,out float liquid){support=-.5f;liquid=0;return true;}
    static bool Deep(float x,float z,out float support,out float liquid){support=-3.25f;liquid=0;return true;}
    static bool DeepStrip(float x,float z,out float support,out float liquid){support=x>.9f&&x<1.15f?-3.25f:1;liquid=0;return true;}
    static bool DryBridge(float x,float z,out float support,out float liquid){support=Math.Abs(x)<1.2f?1:-3;liquid=.4f;return true;}
    static TransformerWaterProbe ConstantDepth(float depth)=>(float x,float z,out float support,out float liquid)=>{support=10;liquid=10+depth;return true;};
    static void Main() {
        float geometric=TransformerWaterRules.ChestDepth(2.43f,3.54f,.005f);
        Check(Math.Abs(geometric-2.758f)<.0001f,"primary thorax shoulders and real sole define a 2.758m standing chest depth");
        Check(TransformerWaterRules.ChestDepth(float.NaN,3.54f,.005f)==Limit&&TransformerWaterRules.ChestDepth(2.43f,2,.005f)==Limit,"invalid geometry safely uses reviewed standing chest fallback");
        Check(TransformerWaterRules.Dry(At(),Feet,Feet.Length,Dry),"dry supporting land allows a new car-to-robot transformation");
        Check(!TransformerWaterRules.Dry(At(),Feet,Feet.Length,Shallow),"new car transformation stays blocked underwater despite robot wading allowance");
        Check(TransformerWaterRules.Dry(At(),Feet,Feet.Length,DryBridge),"actual dry bridge deck allows transformation despite water beneath it");
        Check(Walk(At(-1),At(2.5f),Slope),"dry robot may enter shallows and wade up to chest depth");
        Check(Walk(At(),At(.7f),Shallow),"existing shallow water never requires a legacy recovery override");
        Check(Walk(At(),At(.1f),ConstantDepth(Limit-.001f)),"water just below the chest boundary remains walkable");
        Check(Walk(At(),At(.1f),ConstantDepth(Limit)),"water exactly at the chest boundary remains walkable");
        Check(!Walk(At(),At(.1f),ConstantDepth(Limit+.001f)),"water above the chest boundary is blocked");
        Check(Walk(At(),At(.1f),ConstantDepth(Limit+.00005f))&&TransformerWaterRules.WithinDepth(At(.1f),Center,1,ConstantDepth(Limit+.00005f),Limit),"shared sub-millimetre tolerance cannot admit a boundary pose then lock its next frame");
        Check(Walk(At(),At(.1f),ConstantDepth(geometric),maximumDepth:geometric),"adapter-provided real chest depth is the shared boundary");
        Check(!Walk(At(),At(.1f),ConstantDepth(geometric+.01f),maximumDepth:geometric),"custom geometry cannot silently use the larger fallback depth");
        Check(!Walk(At(),At(3),Slope),"forward entry beyond chest depth is blocked");
        Check(!Walk(At(),At(3,yaw:(float)Math.PI),Slope),"backwards or facing-away movement uses the same depth ceiling");
        Check(!Walk(At(),At(2.5f),DeepStrip),"sprint sweep catches too-deep water between two dry endpoints");
        Check(!Walk(At(2.5f),At(),DeepStrip),"reverse sweep catches an intermediate deep strip");
        Check(!Walk(At(1.7f),At(1.9f),Slope,Feet),"outer sole support stops at chest depth before the body center does");
        Check(Walk(At(),At(z:1),DryBridge,Feet)&&Walk(At(z:1),At(),DryBridge,Feet),"dry bridge remains traversable in both directions");
        Check(!Walk(At(),At(.3f),DryBridge,Feet),"stepping off bridge into beyond-chest water is refused");
        TransformerWaterProbe forwardDeep=(float x,float z,out float h,out float l)=>{h=z>.9f?-3.25f:1;l=0;return true;};
        var frontLanding=new[]{new TransformerWaterPoint(0,1.1f)};
        var rearLanding=new[]{new TransformerWaterPoint(0,-1.1f)};
        Check(!TransformerWaterRules.CanTraverse(At(),At(z:.1f),Center,1,forwardDeep,new float[1],false,frontLanding,1),"predicted forward landing beyond chest depth is blocked");
        Check(TransformerWaterRules.CanTraverse(At(),At(z:-.1f),Center,1,forwardDeep,new float[1],false,rearLanding,1),"robot can reverse away from deep shore even when a hypothetical forward landing is too deep");
        Check(TransformerWaterRules.CanTraverse(At(),At(yaw:.2f),Center,1,forwardDeep,new float[1]),"hypothetical forward landing never locks a safe in-place turn");
        Check(TransformerWaterRules.CanTraverse(At(),At(z:.1f),Center,1,Shallow,new float[1],false,frontLanding,1),"future sole checks allow normal shallow-water steps");
        TransformerWaterProbe futureStrip=(float x,float z,out float h,out float l)=>{h=Math.Abs(z-2)<.1f&&x>.1f&&x<.3f?-3.25f:1;l=0;return true;};
        var ahead=new[]{new TransformerWaterPoint(0,2)};
        Check(!TransformerWaterRules.CanTraverse(At(),At(.5f),Center,1,futureStrip,new float[1],false,ahead,1),"future landing sweep catches depth breach even with dry body and final landing");
        TransformerWaterProbe rotateDeep=(float x,float z,out float h,out float l)=>{h=x>1.08f?-3.25f:1;l=0;return true;};
        Check(!Walk(At(),At(yaw:(float)Math.PI/2),rotateDeep,Feet),"pure turning checks outer sole arc against deep water");
        Check(Walk(At(),At(yaw:(float)Math.PI/2),Shallow,Feet),"in-place turning remains possible in shallow water");
        Check(Walk(At(yaw:(float)(Math.PI*1.99)),At(yaw:.01f),Dry,Feet),"yaw wrap takes the short rotation instead of a full-circle sweep");
        Check(Walk(At(3.5f),At(3),Slope,escape:true),"too-deep loaded robot may retreat toward shallower support");
        Check(!Walk(At(3),At(3.5f),Slope,escape:true),"too-deep recovery cannot worsen existing excessive depth");
        Check(Walk(At(),At(.4f),Deep,escape:true),"flat deep legacy support permits progress toward shore instead of trapping a loaded robot");
        Check(!Walk(At(),At(.4f),Deep),"normal robot cannot silently activate deep recovery exception");
        Check(Walk(At(3.5f),At(2.5f),Slope,escape:true),"deep recovery transitions into permitted chest-deep wading");
        Check(TransformerWaterRules.WithinDepth(At(2.5f),Center,1,Slope,Limit),"recovery clears once all current support is within chest limit");
        Check(!Walk(At(2.5f),At(3),Slope),"robot cannot reenter excessive depth after normal wading resumes");
        Check(Walk(At(1),At(2.5f),Slope,escape:true),"support already within chest limit keeps ordinary wading freedom during recovery");
        TransformerWaterProbe waves=(float x,float z,out float h,out float l)=>{h=0;l=x>.5f?Limit+.05f:Limit-.05f;return true;};
        Check(!Walk(At(),At(1),waves),"native wave rise above chest blocks an otherwise level underwater path");
        TransformerWaterProbe localLiquid=(float x,float z,out float h,out float l)=>{h=40;l=x>.5f?43:42;return true;};
        Check(!Walk(At(),At(1),localLiquid),"local water or tar uses its actual elevated depth rather than mean sea level");
        TransformerWaterProbe noLiquid=(float x,float z,out float h,out float l)=>{h=-20;l=-10000;return true;};
        Check(Walk(At(),At(1),noLiquid),"native no-liquid sentinel leaves dry non-ocean support usable");
        TransformerWaterProbe boundary=(float x,float z,out float h,out float l)=>{h=TransformerWaterRules.Clearance;l=0;return true;};
        Check(TransformerWaterRules.Dry(At(),Center,1,boundary),"car transformation retains separate three-centimetre dry clearance");
        TransformerWaterProbe missing=(float x,float z,out float h,out float l)=>{h=0;l=0;return false;};
        Check(!Walk(At(),At(.1f),missing),"missing support refuses movement");
        TransformerWaterProbe invalid=(float x,float z,out float h,out float l)=>{h=float.NaN;l=0;return true;};
        Check(!Walk(At(),At(.1f),invalid),"invalid support height refuses movement safely");
        invalid=(float x,float z,out float h,out float l)=>{h=1;l=float.PositiveInfinity;return true;};
        Check(!Walk(At(),At(.1f),invalid),"invalid native liquid height cannot poison root pose");
        Check(!Walk(At(),At(float.NaN),Dry)&&!Walk(At(),At(20),Dry),"invalid coordinates and unbounded frame teleports remain rejected");
        Check(!Walk(At(),At(.1f),Shallow,maximumDepth:float.NaN)&&!Walk(At(),At(.1f),Shallow,maximumDepth:-1),"invalid depth allowances cannot disable water safety");
        Check(!TransformerWaterRules.CanTraverse(At(),At(1),Center,1,Dry,new float[0]),"missing reusable depth buffer cannot run unchecked");
        Check(!TransformerWaterRules.Dry(At(),null,0,Dry)&&!TransformerWaterRules.WithinDepth(At(),Center,1,null,Limit),"absent support footprint or native probe fails closed");
        Console.WriteLine(checks+" chest-depth water checks passed; native waves bridge physics and wading still need in-game verification.");
    }
}
