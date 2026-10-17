using System;
using Rachel350Z;
using UnityEngine;
static class Program {
    static int checks;
    static readonly Vector3 Point=new Vector3(0,51,0);
    static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
    static bool Near(float a,float b)=>Math.Abs(a-b)<.0001f;
    static Collider Surface(string name,float height,int layer,float distance,bool trigger=false) {
        var collider=new Collider(name,layer){isTrigger=trigger};Physics.Hits.Add(new RaycastHit{collider=collider,distance=distance,point=new Vector3(0,height,0),normal=Vector3.up});return collider;
    }
    static void Reset(float hint=48){Physics.Hits.Clear();Heightmap.Current=new Heightmap{Height=hint};}
    static void Main() {
        var probe=new TransformerGroundProbe(null);
        Reset();var trigger=Surface("WaterSurface trigger",50,4,2.8f,true);var water=Surface("WaterSurface solid",50,4,2.8f);var bed=Surface("terrain",48,11,4.8f);
        Check(probe.Sample(Point,out float floor,out _,out var support)&&Near(floor,48)&&ReferenceEquals(support,bed),"native non-trigger water-surface BoxCollider cannot replace actual lake bed");
        Check((Physics.LastMask&LayerMask.GetMask("Water","WaterVolume","Liquid"))==0,"support mask excludes water and volume layers while liquid sampling remains separate");
        Check(TransformerGroundProbe.IsFluid(water)&&TransformerGroundProbe.IsFluid(trigger),"fluid classifier recognizes both native water surface colliders");
        TransformerWaterProbe actualFloor=(float x,float z,out float h,out float liquid)=>{
            liquid=50;return probe.Sample(new Vector3(x,51,z),out h,out _,out _);
        };
        var feet=new[]{new TransformerWaterPoint(0,0)};
        var pose=new TransformerWaterPose(0,0,0);
        Check(TransformerWaterRules.WithinDepth(pose,feet,1,actualFloor,2.758f),"real two-metre bed below non-trigger water remains permitted chest-depth wading");
        Reset(45);Surface("WaterSurface solid",50,4,2.8f);Surface("deep terrain",45,11,7.8f);
        Check(!TransformerWaterRules.WithinDepth(pose,feet,1,actualFloor,2.758f),"same native surface over a five-metre bed cannot report zero water depth");
        Check(!TransformerWaterRules.CanTraverse(pose,new TransformerWaterPose(1,0,0),feet,1,actualFloor,new float[1]),"ground adapter and depth policy together reject walking across deep water");
        Reset();Surface("WaterSurface solid",50,4,2.8f);var bridge=Surface("dry bridge",50.5f,8,2.3f);Surface("terrain",48,11,4.8f);
        Check(probe.Sample(Point,out floor,out _,out support)&&Near(floor,50.5f)&&ReferenceEquals(support,bridge),"dry piece bridge above lake stays a legitimate higher support");
        Reset();var defaultWater=Surface("custom surface",50,0,2.8f);defaultWater.gameObject.parent=new GameObject("water volume");defaultWater.gameObject.parent.Add(new WaterVolume());bed=Surface("terrain",48,11,4.8f);
        Check(probe.Sample(Point,out floor,out _,out support)&&ReferenceEquals(support,bed)&&TransformerGroundProbe.IsFluid(defaultWater),"water-volume child collider is excluded even if incorrectly placed on Default layer");
        Reset();var tar=Surface("tar mesh",50,0,2.8f);tar.gameObject.Add(new LiquidVolume());bed=Surface("terrain",48,11,4.8f);
        Check(probe.Sample(Point,out floor,out _,out support)&&Near(floor,48)&&TransformerGroundProbe.IsFluid(tar),"native LiquidVolume MeshCollider cannot make robot walk on tar surface");
        Reset();var local=Surface("liquid surface child",50,0,2.8f);local.gameObject.parent=new GameObject("surface");local.gameObject.parent.Add(new LiquidSurface());Surface("terrain",48,11,4.8f);
        Check(probe.Sample(Point,out floor,out _,out _)&&Near(floor,48)&&TransformerGroundProbe.IsFluid(local),"separate LiquidSurface hierarchy is rejected on otherwise solid layer");
        Reset();var own=Surface("own chassis",51,0,1.8f);bed=Surface("terrain",48,11,4.8f);
        var owned=new TransformerGroundProbe(c=>ReferenceEquals(c,own));
        Check(owned.Sample(Point,out floor,out _,out support)&&ReferenceEquals(support,bed),"own car robot or rider support is excluded by owner callback");
        Reset();var actor=Surface("actor",50,0,2.8f);actor.gameObject.Add(new Character());bed=Surface("terrain",48,11,4.8f);
        Check(probe.Sample(Point,out floor,out _,out support)&&ReferenceEquals(support,bed),"an actor cannot become a raised floor support");
        Check((TransformerGroundProbe.MovementMask&LayerMask.GetMask("character","character_net","ghost"))==LayerMask.GetMask("character","character_net","ghost"),"movement collision mask preserves existing actor and ghost obstacles");
        Check((TransformerGroundProbe.MovementMask&LayerMask.GetMask("Water","WaterVolume","Liquid","effect"))==0,"core collision and exit mask exclude fluids without weakening previous effect exclusion");
        Check(TransformerGroundProbe.IsFluid(tar)&&TransformerGroundProbe.IsFluid(defaultWater),"core overlap parent-fluid filter also rejects Default-layer liquid meshes when descending");
        Reset();Surface("nearest rounded grid",47.1f,11,5.7f);
        Check(probe.Sample(Point,out floor,out _,out _)&&Near(floor,47.1f),"actual collider triangle always wins over a higher rounded height-grid hint");
        Reset(100);
        Check(!probe.Sample(Point,out _,out _,out _),"height-grid hint alone never becomes standable support when collider is missing");
        Heightmap.Current=null;Surface("terrain",48,11,4.8f);
        Check(probe.Sample(Point,out floor,out _,out _)&&Near(floor,48),"a loaded bridge or ground collider remains usable without a heightmap hint");
        Reset();for(int i=0;i<48;i++)Surface("crowded terrain",48,11,4.8f);
        Check(!probe.Sample(Point,out _,out _,out _),"filled query buffer cannot silently choose an arbitrary support");
        Reset();var debris=Surface("vegetation or debris",50,0,2.8f);debris.IgnoreForMovement=true;Surface("terrain",48,11,4.8f);
        Check(probe.Sample(Point,out floor,out _,out _)&&Near(floor,48),"existing vegetation and debris pass-through policy stays separate from solid ground");
        Reset();Surface("terrain",48,11,4.8f);
        Check(!probe.Sample(new Vector3(float.NaN,0,0),out _,out _,out _),"invalid query coordinate cannot reach native physics");
        float y=50;
        for(int i=0;i<10;i++)y=TransformerGroundProbe.FollowHeight(y,48,.02f);
        Check(Near(y,48),"blocked horizontal path still converges a hovering kinematic root onto the real lake bed");
        Check(Near(TransformerGroundProbe.FollowHeight(48,50.5f,.02f),48.36f),"same support following climbs actual dry bridge instead of using a sea-level clamp");
        Check(TransformerGroundProbe.FollowHeight(50,48,0)==50&&TransformerGroundProbe.FollowHeight(50,48,float.NaN)==50,"pause or invalid frame time cannot advance vertical settling");
        Console.WriteLine(checks+" actual ground adapter checks passed using physics doubles; native wet support is logged for in-game confirmation.");
    }
}
