using System;
using HarmonyLib;
using Rachel350Z;
using UnityEngine;
static class Program {
    static int checks;
    static readonly object OwnerA=new object(),OwnerB=new object();
    static readonly Vector3 Center=new Vector3(0,1,0),Forward=new Vector3(0,0,1);
    static readonly Vector2 Size=new Vector2(1,2);
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;Console.WriteLine("PASS "+message);}
    static void Equal(Matrix4x4 a,Matrix4x4 b,string message)=>Check(a.Equals(b),message);
    static InstanceRenderer Setup(string entryName="grass_meadows",string prefabName="grass_meadows") {
        ClutterSystem.instance=new ClutterSystem();
        var mesh=new Mesh{name="grass blades"};var material=new Material{name="grass material"};
        var prototype=new InstanceRenderer{m_mesh=mesh,m_material=material};
        ClutterSystem.instance.m_clutter.Add(new ClutterSystem.Clutter{m_name=entryName,m_prefab=new GameObject{name=prefabName,Renderer=prototype}});
        var renderer=new InstanceRenderer{m_mesh=mesh,m_material=material};
        renderer.transform.parent=ClutterSystem.instance.transform;
        renderer.Configure(Matrix4x4.At(0,1,0));return renderer;
    }
    static void Mask(object owner= null,int slot=0,float strength=1)=>TransformerFootstepGrass.Set(owner??OwnerA,slot,Center,Forward,Size,strength);
    static void Reset(){TransformerFootstepGrass.RemoveOwner(OwnerA);TransformerFootstepGrass.RemoveOwner(OwnerB);}
    static void DrawRestoration() {
        var renderer=Setup();var original=renderer.Current;Mask();
        renderer.CustomUpdate(.02f,1);
        Check(renderer.SeenMasked&&renderer.Observed.m00==0&&renderer.Observed.m11==0,"actual production prefix masks grass only during the native draw callback");
        Check(renderer.Observed.m03==original.m03&&renderer.Observed.m13==original.m13&&renderer.Observed.m33==1,"world placement and homogeneous row survive zero scale");
        Equal(renderer.Current,original,"postfix restores every original matrix exactly");
        Check(renderer.BoundsBuilds==1&&renderer.BuiltScale==1&&!renderer.Dirty,"dirty bounds are built from original geometry before masking");
        renderer.CustomUpdate(.02f,2);
        Check(renderer.BoundsBuilds==1,"clean conservative bounds do not rebuild each masked frame");
        Equal(renderer.Current,original,"postfix and finalizer double restore is harmless");
        renderer.Throw=true;
        try{renderer.CustomUpdate(.02f,3);throw new Exception("expected native exception");}
        catch(InvalidOperationException error){Check(error.Message=="native draw failure","native rendering exception is propagated unchanged");}
        Equal(renderer.Current,original,"Harmony finalizer restores original matrices when native draw throws");Reset();
    }
    static void OverlapAndCleanup() {
        var renderer=Setup();Mask(OwnerA,0,.5f);Mask(OwnerB,0,.5f);renderer.CustomUpdate(.02f,0);
        Check(Math.Abs(renderer.Observed.m11-.5f)<.0001f,"overlapping owners take maximum suppression rather than double flattening");
        TransformerFootstepGrass.RemoveOwner(OwnerA);renderer.CustomUpdate(.02f,0);
        Check(Math.Abs(renderer.Observed.m11-.5f)<.0001f,"removing one owner preserves another overlapping footprint");
        TransformerFootstepGrass.Set(OwnerB,0,new Vector3(20,1,0),Forward,Size,1);renderer.CustomUpdate(.02f,0);
        Check(!renderer.SeenMasked,"slot reuse moves the footprint without stale suppression");
        Mask(OwnerB,0,1);TransformerFootstepGrass.Remove(OwnerB,0);renderer.CustomUpdate(.02f,0);
        Check(!renderer.SeenMasked,"expired footprint slot immediately stops masking");
        Mask(OwnerA,31,1);Mask(OwnerA,32,1);TransformerFootstepGrass.Remove(OwnerA,31);renderer.CustomUpdate(.02f,0);
        Check(!renderer.SeenMasked,"owner capacity rejects a thirty-third footprint slot");
        Mask();renderer.DuringDraw=()=>TransformerFootstepGrass.RemoveOwner(OwnerA);renderer.CustomUpdate(.02f,0);
        Equal(renderer.Current,Matrix4x4.At(0,1,0),"last-owner cleanup during a draw restores borrowed arrays immediately");renderer.DuringDraw=null;Reset();
    }
    static void FiltersAndFallback() {
        var renderer=Setup("grass_meadows","grass_fern");Mask();renderer.CustomUpdate(.02f,0);
        Check(!renderer.SeenMasked,"deny on prefab name cannot be bypassed by positive grass entry name");Reset();
        renderer=Setup("rocks","rocks");Mask();renderer.CustomUpdate(.02f,0);
        Check(!renderer.SeenMasked,"non-grass native clutter is untouched");Reset();
        renderer=Setup();renderer.transform.parent=null;Mask();renderer.CustomUpdate(.02f,0);
        Check(!renderer.SeenMasked,"matching materials outside native clutter hierarchy remain untouched");Reset();
        renderer=Setup();renderer.m_mesh=new Mesh();Mask();renderer.CustomUpdate(.02f,0);
        Check(!renderer.SeenMasked,"a shared grass material alone does not match a different mesh");Reset();
        renderer=Setup();renderer.transform.position=new Vector3(1000,0,1000);Mask();renderer.CustomUpdate(.02f,0);
        Check(!renderer.SeenMasked,"distant patch broadphase rejects masks before native instance inspection");Reset();
        renderer=Setup();TransformerFootstepGrass.Set(OwnerA,0,new Vector3(float.NaN,0,0),Forward,Size,1);renderer.CustomUpdate(.02f,0);
        Check(!renderer.SeenMasked,"nonfinite footprint input fails closed without touching native draw");Reset();
    }
    static void ReentrantAndTeardown(Harmony harmony) {
        var renderer=Setup();var original=renderer.Current;Mask();bool nested=false;
        renderer.DuringDraw=()=>{if(nested)return;nested=true;renderer.CustomUpdate(.02f,0);};
        renderer.CustomUpdate(.02f,0);
        Equal(renderer.Current,original,"reentrant native draw never captures already scaled matrices as originals");
        TransformerFootstepGrass.Shutdown();harmony.UnpatchSelf();
        renderer.DuringDraw=null;renderer.CustomUpdate(.02f,0);Check(!renderer.SeenMasked,"shutdown and unpatch leave original grass rendering");
        TransformerFootstepGrass.Initialize(harmony,message=>throw new Exception(message));Mask();renderer.CustomUpdate(.02f,0);
        Check(renderer.SeenMasked,"same-assembly teardown and reinitialization installs masking again");
        TransformerFootstepGrass.Shutdown();harmony.UnpatchSelf();
    }
    static void Main() {
        var harmony=new Harmony("NFVU2.tests.footstepgrass");
        TransformerFootstepGrass.Initialize(harmony,message=>throw new Exception(message));
        DrawRestoration();OverlapAndCleanup();FiltersAndFallback();ReentrantAndTeardown(harmony);
        Console.WriteLine(checks+" temporary grass callback checks passed; native Harmony detours and graphics still need in-game review.");
    }
}
