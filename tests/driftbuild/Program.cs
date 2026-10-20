using System;
using System.Linq;
using System.Collections.Generic;
using Rachel350Z;
using UnityEngine;
static class Program {
    static int checks;static List<string> warnings=new List<string>();
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    static Heightmap Tile(float x,float z) {
        var root=new GameObject("terrain");root.transform.position=new Vector3(x,32,z);
        var view=root.Add<ZNetView>();view.Data=new ZDO("terrain",root.transform.position);
        var map=root.Add<Heightmap>();map.Compiler=root.Add<TerrainComp>();Heightmap.All.Add(map);return map;
    }
    static void Setup() {
        Heightmap.All.Clear();warnings.Clear();Time.unscaledTime=0;Time.timeScale=1;
        ZDOMan.instance=new ZDOMan();ZNetScene.instance=new ZNetScene();Game.instance=new Game();ZoneSystem.instance=new ZoneSystem();Minimap.instance=new Minimap();
        foreach(var name in DriftTrackPieces.Items.Concat(DriftCircuitPieces.Items).Select(p=>p.Prefab).Distinct())ZNetScene.instance.Prefabs[name]=new GameObject(name);
    }
    static int CountPaint(Heightmap m,IReadOnlyList<DriftTrackStamp> stamps)=>stamps.Count(s=>DriftTrackConstructionPlan.IntersectsTile(s,m.transform.position.x,m.transform.position.z,32));
    static int CountPieces(Heightmap m,IReadOnlyList<DriftTrackPiece> pieces)=>pieces.Count(p=>p.X>=m.transform.position.x-32&&p.X<m.transform.position.x+32&&p.Z>=m.transform.position.z-32&&p.Z<m.transform.position.z+32);
    static bool Complete(Heightmap m) {
        var z=m.GetComponent<ZNetView>().Data;
        return z.GetInt(DriftTrackBuilder.PaintProgress)==CountPaint(m,DriftTrackConstructionPlan.PaintStamps)
            &&z.GetInt(DriftTrackBuilder.PieceProgress)==CountPieces(m,DriftTrackPieces.Items)
            &&z.GetInt(DriftCircuitPlan.PaintProgress)==CountPaint(m,DriftCircuitPlan.PaintStamps)
            &&z.GetInt(DriftCircuitPlan.PieceProgress)==CountPieces(m,DriftCircuitPieces.Items);
    }
    static void Tick(DriftTrackBuilder b) {
        int before=Heightmap.All.Sum(m=>m.Compiler.Operations)+ZNetScene.instance.Created;Time.unscaledTime+=.02f;b.Tick();
        Check(Heightmap.All.Sum(m=>m.Compiler.Operations)+ZNetScene.instance.Created-before<=2,"main-thread construction budget exceeded");
        Check(warnings.Count==0,"builder failed: "+string.Join(";",warnings));
    }
    static void Finish(DriftTrackBuilder b) {
        for(int i=0;i<5000&&!Heightmap.All.All(Complete);i++)Tick(b);
        Check(Heightmap.All.All(Complete),"all loaded tiles complete independently");
    }
    static GameObject LegacySign(DriftTrackPiece piece,string text,float moved=0,bool owner=true) {
        var g=ZNetScene.instance.Spawn("sign",new Vector3(piece.X+moved,32+piece.HeightOffset,piece.Z),Quaternion.Euler(0,piece.Yaw,0));
        var view=g.GetComponent<ZNetView>();view.Owner=owner;view.Data.Persistent=true;
        view.Data.Set("nfvu2_drift_test_piece_v1",1);view.Data.Set(DriftTrackSignCorrection.VersionKey,1);g.GetComponent<Sign>().SetText(text);return g;
    }
    static void Main() {
        Setup();var one=Tile(128,0);var two=Tile(128,64);var far=Tile(320,-64);far.GetComponent<ZNetView>().Owner=false;
        var builder=new DriftTrackBuilder(warnings.Add);
        ZoneSystem.instance.Loaded=false;for(int i=0;i<35;i++)Tick(builder);
        Check(one.Compiler.Operations==0&&ZNetScene.instance.Created==0,"unloaded areas never get construction");
        ZoneSystem.instance.Loaded=true;one.Compiler.Initialized=false;two.Compiler.Initialized=false;
        for(int i=0;i<35;i++)Tick(builder);
        Check(one.Compiler.Operations==0&&two.Compiler.Operations==0&&far.Compiler.Operations==0,"uninitialised terrain and unowned tiles are untouched");
        one.Compiler.Initialized=two.Compiler.Initialized=true;
        for(int i=0;i<80;i++)Tick(builder);
        Check(far.Compiler.Operations==0,"native terrain ownership is honoured");
        far.GetComponent<ZNetView>().Owner=true;Finish(builder);
        Check(ZNetScene.instance.Instances.Values.All(g=>g.GetComponent<ZNetView>().Data.Persistent),"all spawned venue pieces use persistent native world data");
        Check(Heightmap.All.All(m=>m.Compiler.AppliedRevision==m.GetComponent<ZNetView>().Data.DataRevision),"both plan versions keep native terrain revisions synchronized");
        int count=ZNetScene.instance.Created,paint=Heightmap.All.Sum(m=>m.Compiler.Operations);
        var lamp=ZNetScene.instance.Instances.Values.First(g=>g.Name=="piece_groundtorch");lamp.GetComponent<ZNetView>().Data.Set(ZDOVars.s_fuel,1f);
        var removed=ZNetScene.instance.Instances.Values.First(g=>g.Name=="wood_roof");ZNetScene.instance.Destroy(removed);
        builder.Clear();for(int i=0;i<100;i++)Tick(builder);
        Check(ZNetScene.instance.Created==count&&Heightmap.All.Sum(m=>m.Compiler.Operations)==paint,"reload resumes completed v1/v2 markers without repainting or duplication");
        Check(!ZNetScene.instance.Instances.Values.Contains(removed),"dismantled venue pieces stay removed after reload");
        Check(lamp.GetComponent<ZNetView>().Data.GetFloat(ZDOVars.s_fuel)==1,"reloading does not refill an existing torch");
        Check(Minimap.instance.Pins.Count==1,"reload maintains one temporary map marker");
        builder.Clear();Check(Minimap.instance.Pins.Count==0,"world cleanup removes its temporary marker");
        // A pre-existing completed v1 tile must still receive the independent v2 plan.
        Setup();var saved=Tile(128,0);var data=saved.GetComponent<ZNetView>().Data;
        data.Set(DriftTrackBuilder.PaintProgress,CountPaint(saved,DriftTrackConstructionPlan.PaintStamps));
        data.Set(DriftTrackBuilder.PieceProgress,CountPieces(saved,DriftTrackPieces.Items));
        var start=DriftTrackPieces.Items.First(p=>p.Text=="DRIFT TEST | 335 m | 16-20 m");
        var end=DriftTrackPieces.Items.First(p=>p.Text=="ENDE / END");
        var upgrade=LegacySign(start,start.Text);var edited=LegacySign(end,"My own course");
        var moved=LegacySign(start,start.Text,2);var unowned=LegacySign(end,end.Text,0,false);int oldCount=ZNetScene.instance.Created;
        builder=new DriftTrackBuilder(warnings.Add);Finish(builder);
        Check(saved.Compiler.Operations==CountPaint(saved,DriftCircuitPlan.PaintStamps),"existing world receives only new-version paint");
        Check(ZNetScene.instance.Created-oldCount==CountPieces(saved,DriftCircuitPieces.Items),"existing world receives only new-version pieces");
        Check(upgrade.GetComponent<ZNetView>().Data.GetString(ZDOVars.s_text)==DriftCircuitLabels.Upgrade(start.Text),"untouched prototype sign receives the current lap label");
        Check(edited.GetComponent<ZNetView>().Data.GetString(ZDOVars.s_text)=="My own course","text edited by a player is preserved by the real builder");
        Check(moved.GetComponent<ZNetView>().Data.GetString(ZDOVars.s_text)==start.Text,"moved player sign is preserved by the real builder");
        Check(unowned.GetComponent<ZNetView>().Data.GetString(ZDOVars.s_text)==end.Text,"unowned sign is not modified");
        Check(ZNetScene.instance.Instances.Values.Where(g=>g.Name=="piece_groundtorch").All(g=>g.GetComponent<ZNetView>().Data.GetFloat(ZDOVars.s_fuel)==6),"new lamps start with native maximum fuel");
        builder.Clear();Console.WriteLine(checks+" production track-builder boundary checks passed; no Unity world loaded.");
    }
}
