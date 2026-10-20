using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Rachel350Z;
using UnityEngine;
static class Program {
    static int checks;static readonly List<string> warnings=new List<string>();
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static Heightmap Tile(float x,float z) {
        var root=new GameObject("terrain");root.transform.position=new Vector3(x,32,z);
        var view=root.Add<ZNetView>();view.Data=new ZDO("terrain",root.transform.position);
        var map=root.Add<Heightmap>();map.Compiler=root.Add<TerrainComp>();Heightmap.All.Add(map);return map;
    }
    static void Setup() {
        Heightmap.All.Clear();warnings.Clear();Time.unscaledTime=0;Time.timeScale=1;
        ZDOMan.instance=new ZDOMan();ZNetScene.instance=new ZNetScene();Game.instance=new Game();ZoneSystem.instance=new ZoneSystem();
        foreach(var name in ParkadePlan.Pieces.Select(p=>p.Prefab).Distinct())ZNetScene.instance.Prefabs[name]=new GameObject(name);
    }
    static void Tick(ParkadeVenue venue) {
        int before=Heightmap.All.Sum(m=>m.Compiler.Operations)+ZNetScene.instance.Created;
        Time.unscaledTime+=.02f;venue.Tick();
        Check(Heightmap.All.Sum(m=>m.Compiler.Operations)+ZNetScene.instance.Created-before<=2,"construction stays within two operations per frame");
        Check(warnings.Count==0,"builder failure: "+string.Join(";",warnings));
    }
    static bool Complete(Heightmap map) {
        var p=map.transform.position;var data=map.GetComponent<ZNetView>().Data;
        int paint=ParkadePlan.PaintStamps.Count(s=>DriftTrackConstructionPlan.IntersectsTile(s,p.x,p.z,32));
        int pieces=ParkadePlan.Pieces.Count(s=>s.X>=p.x-32&&s.X<p.x+32&&s.Z>=p.z-32&&s.Z<p.z+32);
        return data.GetInt("nfvu2_parkade_one_pave_v1")==paint&&data.GetInt("nfvu2_parkade_one_pieces_v1")==pieces;
    }
    static void Main() {
        Check(ParkadeLayout.TotalLength>495&&ParkadeLayout.TotalLength<510,"measured original half-kilometre lap retained");
        Check(ParkadeLayout.Bounds.MinX>800,"race venue is physically separate from the unchanged practice course");
        Check(ParkadeLayout.Samples.Count>500,"centreline densely sampled for distance and scoring");
        var first=ParkadeLayout.AtDistance(0);var last=ParkadeLayout.AtDistance(ParkadeLayout.TotalLength);
        Check(Math.Abs(first.X-last.X)<.001f&&Math.Abs(first.Z-last.Z)<.001f,"closed route has a coincident seam");
        Check(Math.Abs(first.X-ParkadeLayout.StartX)<.001f&&Math.Abs(first.Z-ParkadeLayout.StartZ)<.001f,"event start matches route start");
        Check(Math.Abs(ParkadeLayout.HeightAtDistance(0)-40)<.001f,"start is raised clear of the native water level");
        Check(ParkadeLayout.TerrainHeightAt(128,0)==32&&ParkadeLayout.TerrainHeightAt(-600,120)==32,"practice and unrelated flat world heights are preserved");
        float minHeight=float.MaxValue,maxHeight=float.MinValue;
        for(int i=0;i<ParkadeLayout.Samples.Count;i++) {
            var p=ParkadeLayout.Samples[i];
            Check(Math.Abs(p.TangentX*p.TangentX+p.TangentZ*p.TangentZ-1)<.001,"route tangent normalized");
            Check(ParkadeLayout.Project(p.X,p.Z).DistanceToCenter<.002,"projection agrees with authored centreline");
            Check(ParkadeLayout.Bounds.Contains(p.X,p.Z),"all course samples fit declared bounds");
            Check(ParkadePlan.PaintStamps.Any(s=>(s.X-p.X)*(s.X-p.X)+(s.Z-p.Z)*(s.Z-p.Z)<s.Radius*s.Radius),"native paint covers every road centre sample");
            float height=ParkadeLayout.TerrainHeightAt(p.X,p.Z);
            minHeight=Math.Min(minHeight,height);maxHeight=Math.Max(maxHeight,height);
            Check(height>32.8f&&height<=40.001f,"all graded road stays above Valheim water");
            Check(Math.Abs(height-ParkadeLayout.HeightAtDistance(p.DistanceAlong))<.005f,"generator and event height agree on actual centreline");
            foreach(int side in new[]{-1,1}) {
                float x=p.X-p.TangentZ*p.HalfWidth*side,z=p.Z+p.TangentX*p.HalfWidth*side;
                Check(ParkadePlan.PaintStamps.Any(s=>(s.X-x)*(s.X-x)+(s.Z-z)*(s.Z-z)<s.Radius*s.Radius),"native paved stamps cover both driving edges");
            }
        }
        Check(maxHeight-minHeight>7&&maxHeight-minHeight<7.2f,"genuine original approximately seven-metre grade retained");
        Check(ParkadePlan.Pieces.Count<1000&&ParkadePlan.Pieces.Count>400,"bounded substantial garage venue");
        Check(ParkadePlan.Pieces.Count(p=>p.Prefab=="piece_groundtorch")<=12,"particle lights remain bounded");
        foreach(var piece in ParkadePlan.Pieces) {
            float hx=piece.Prefab=="stone_floor"?2:piece.Prefab=="stone_wall_2x1"?1:piece.Prefab=="sign"?.5f:.2f;
            float hz=piece.Prefab=="stone_floor"?2:piece.Prefab=="stone_wall_2x1"?.5f:piece.Prefab=="sign"?.05f:.2f;
            Check(ParkadePlan.ClearBox(piece.X,piece.Z,piece.Yaw,hx,hz,.35f),"native obstacle footprint stays off the full driving width: "+piece.Prefab);
            Check(piece.Text==null||piece.Text.Length<=50,"native sign string fits");
        }
        Setup();var venue=new ParkadeVenue(warnings.Add);var b=ParkadeLayout.Bounds;
        for(int x=(int)Math.Floor((b.MinX-12)/64);x<=(int)Math.Ceiling((b.MaxX+12)/64);x++)
            for(int z=(int)Math.Floor((b.MinZ-12)/64);z<=(int)Math.Ceiling((b.MaxZ+12)/64);z++)Tile(x*64,z*64);
        var ordinary=Tile(128,0);ordinary.GetComponent<ZNetView>().Data.Set("nfvu2_drift_circuit_pave_v2",123);
        ZoneSystem.instance.Loaded=false;for(int i=0;i<30;i++)Tick(venue);
        Check(ZNetScene.instance.Created==0&&Heightmap.All.Sum(t=>t.Compiler.Operations)==0&&!venue.StartReady,"unloaded terrain is neither built nor race-ready");
        ZoneSystem.instance.Loaded=true;foreach(var tile in Heightmap.All)tile.Compiler.Initialized=false;
        for(int i=0;i<30;i++)Tick(venue);
        Check(ZNetScene.instance.Created==0,"uninitialized compilers are never touched");
        foreach(var tile in Heightmap.All){tile.Compiler.Initialized=true;tile.GetComponent<ZNetView>().Owner=false;}
        for(int i=0;i<30;i++)Tick(venue);Check(ZNetScene.instance.Created==0,"unowned terrain is untouched");
        foreach(var tile in Heightmap.All)tile.GetComponent<ZNetView>().Owner=true;
        // Ordinary practice tile has no Parkade plan; exclude its intentional
        // unrelated saved marker when testing completion.
        for(int i=0;i<10000&&!Heightmap.All.Where(t=>t!=ordinary).All(Complete);i++)Tick(venue);
        Check(Heightmap.All.Where(t=>t!=ordinary).All(Complete),"all loaded venue tiles complete");
        Check(venue.StartReady,"finished native construction opens race readiness");
        Check(venue.RaceReady,"full completed native venue permits countdown");
        var far=Heightmap.All.Where(t=>t!=ordinary&&t.GetComponent<ZNetView>().Data.GetInt("nfvu2_parkade_one_pave_v1")>0)
            .OrderByDescending(t=>(t.transform.position.x-first.X)*(t.transform.position.x-first.X)+(t.transform.position.z-first.Z)*(t.transform.position.z-first.Z)).First();
        var farPosition=far.transform.position;
        ZoneSystem.instance.LoadedAt=p=>p.x!=farPosition.x||p.z!=farPosition.z;
        Check(venue.ConstructionComplete&&venue.RaceReady,"completed distant work survives unloading without blocking the ready start");
        ZoneSystem.instance.LoadedAt=null;far.Compiler.Initialized=false;
        Check(venue.RaceReady,"completed distant work needs no live compiler during countdown");
        far.Compiler.Initialized=true;
        var startTile=Heightmap.All.Single(t=>Math.Abs(t.transform.position.x-first.X)<32&&Math.Abs(t.transform.position.z-first.Z)<32);
        startTile.Compiler.Initialized=false;Check(!venue.RaceReady,"start still requires its own initialized native compiler");startTile.Compiler.Initialized=true;
        startTile.GetComponent<ZNetView>().Owner=false;Check(!venue.RaceReady,"start still requires local native ownership");startTile.GetComponent<ZNetView>().Owner=true;
        Check(ordinary.Compiler.Operations==0&&ordinary.GetComponent<ZNetView>().Data.GetInt("nfvu2_drift_circuit_pave_v2")==123,"practice and its old v2 marker remain untouched");
        Check(ZNetScene.instance.Instances.Values.All(o=>o.GetComponent<ZNetView>().Data.Persistent),"all native authored pieces are persistent");
        Check(Heightmap.All.Where(t=>t!=ordinary).All(t=>t.Compiler.AppliedRevision==t.GetComponent<ZNetView>().Data.DataRevision),"metadata advances do not make native compiler revision stale");
        int count=ZNetScene.instance.Created,painted=Heightmap.All.Sum(t=>t.Compiler.Operations);
        var removed=ZNetScene.instance.Instances.Values.First(o=>o.Name=="stone_wall_2x1");ZNetScene.instance.Destroy(removed);
        var lamp=ZNetScene.instance.Instances.Values.First(o=>o.Name=="piece_groundtorch");lamp.GetComponent<ZNetView>().Data.Set(ZDOVars.s_fuel,1f);
        venue.Clear();for(int i=0;i<100;i++)Tick(venue);
        Check(count==ZNetScene.instance.Created&&painted==Heightmap.All.Sum(t=>t.Compiler.Operations),"reload never duplicates completed terrain or pieces");
        Check(!ZNetScene.instance.Instances.Values.Contains(removed),"deliberately removed walls stay removed");
        Check(lamp.GetComponent<ZNetView>().Data.GetFloat(ZDOVars.s_fuel)==1,"existing fuel remains unchanged");
        ZoneSystem.instance.Loaded=false;Check(!venue.StartReady&&!venue.RaceReady,"race readiness revokes on unloaded native areas");
        venue.Clear();Check(!venue.StartReady,"world cleanup clears readiness");
        StreamingPreparation();
        Directory.CreateDirectory("artifacts/tests/parkade");
        using(var writer=new StreamWriter("artifacts/tests/parkade/route.csv")) {
            writer.WriteLine("x,z,tx,tz,half_width,distance,height");
            foreach(var p in ParkadeLayout.Samples)writer.WriteLine(FormattableString.Invariant($"{p.X},{p.Z},{p.TangentX},{p.TangentZ},{p.HalfWidth},{p.DistanceAlong},{ParkadeLayout.HeightAtDistance(p.DistanceAlong)}"));
        }
        using(var writer=new StreamWriter("artifacts/tests/parkade/pieces.csv")) {
            writer.WriteLine("prefab,x,y,z,yaw");
            foreach(var p in ParkadePlan.Pieces)writer.WriteLine(FormattableString.Invariant($"{p.Prefab},{p.X},{p.HeightOffset+ParkadeLayout.TerrainHeightAt(p.X,p.Z)},{p.Z},{p.Yaw}"));
        }
        Console.WriteLine(checks+" Parkade route, native-plan and saved-builder checks passed; no Unity world loaded.");
    }
    static void StreamingPreparation() {
        Setup();var venue=new ParkadeVenue(warnings.Add);var b=ParkadeLayout.Bounds;
        for(int x=(int)Math.Floor((b.MinX-12)/64);x<=(int)Math.Ceiling((b.MaxX+12)/64);x++)
            for(int z=(int)Math.Floor((b.MinZ-12)/64);z<=(int)Math.Ceiling((b.MaxZ+12)/64);z++)Tile(x*64,z*64);
        var reference=new Vector3(1088,32,64);
        Action apply=()=> {
            ZoneSystem.instance.LoadedAt=p=>Math.Abs(p.x-reference.x)<=64&&Math.Abs(p.z-reference.z)<=64;
            foreach(var tile in Heightmap.All)tile.GetComponent<ZNetView>().Owner=ZoneSystem.instance.LoadedAt(tile.transform.position);
        };
        // Mimic the minimum native loaded/owned neighbourhood. Adjacent-area
        // IsAreaReady can stay false at its edge; local loaded/owned terrain is
        // nevertheless valid for authoring this tile's own saved work.
        ZNetScene.instance.Ready=false;apply();Tick(venue);
        Check(Heightmap.All.Sum(t=>t.CompilerRequests)==0,"initial native saved-object loading cannot create duplicate terrain compilers");
        ZNetScene.instance.Ready=true;
        ZNetScene.instance.AreaReadyAt=p=>p.x==reference.x&&p.z==reference.z;
        int planned=venue.PlannedOperations;Check(planned>724&&venue.CompletedOperations<planned,"progress includes the full plan before distant tiles are discoverable");
        for(int i=0;i<1500;i++){apply();Tick(venue);}
        int stalled=venue.CompletedOperations;
        Check(stalled>0&&stalled<planned&&!venue.ConstructionComplete,"fixed start reference reproduces incomplete distant construction");
        for(int i=0;i<100;i++){apply();Tick(venue);}
        Check(venue.CompletedOperations==stalled,"waiting longer cannot finish work outside native local ownership");
        Console.WriteLine("Reproduced fixed-reference stall at "+stalled+"/"+planned+" operations.");
        int hops=0;
        for(int i=0;i<6000&&!venue.ConstructionComplete;i++) {
            apply();Tick(venue);
            Check(venue.PlannedOperations==planned,"streaming never changes the full-plan denominator");
            if(venue.TryPreparationPoint(reference,out var next)){reference=new Vector3(next.x,32,next.z);hops++;}
        }
        Check(hops>0&&hops<30&&venue.ConstructionComplete,"bounded preparation moves finish every tile with minimum native streaming");
        int complete=venue.CompletedOperations;Check(complete==planned,"100 percent means all authored paint and native pieces completed");
        reference=new Vector3(1088,32,64);apply();Tick(venue);
        Check(venue.RaceReady,"returning to ready start permits countdown without owning the whole course at once");
        Check(venue.CompletedOperations==complete&&!venue.TryPreparationPoint(reference,out _),"completed saved work never restarts preparation hops");
        // Native terrain GameObjects can be destroyed while their persistent
        // ZDO stays loaded on the local server. Keep the completed work cached.
        foreach(var tile in Heightmap.All.Where(t=>!ZoneSystem.instance.LoadedAt(t.transform.position)).ToArray())Heightmap.All.Remove(tile);
        for(int i=0;i<30;i++)Tick(venue);
        Check(venue.ConstructionComplete&&venue.PlannedOperations==planned&&venue.CompletedOperations==complete,"native unload does not discard completed tile records or progress");
        venue.Clear();Check(!venue.ConstructionComplete,"world exit clears cached construction context");
    }
}
