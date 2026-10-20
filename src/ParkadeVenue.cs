using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Rachel350Z {
// Independent saved construction contract. Only already loaded native terrain
// is touched; the event controller owns loading and staging the driver.
internal sealed class ParkadeVenue {
    const string PaintKey="nfvu2_parkade_one_pave_v1",PieceKey="nfvu2_parkade_one_pieces_v1";
    const string PieceTag="nfvu2_parkade_one_piece_v1";
    static readonly MethodInfo operation=typeof(TerrainComp).GetMethod("DoOperation",BindingFlags.Instance|BindingFlags.NonPublic,
        null,new[]{typeof(Vector3),typeof(Vector3),typeof(TerrainOp.Settings)},null);
    static readonly FieldInfo initialized=typeof(TerrainComp).GetField("m_initialized",BindingFlags.Instance|BindingFlags.NonPublic);
    static readonly FieldInfo lastRevision=typeof(TerrainComp).GetField("m_lastDataRevision",BindingFlags.Instance|BindingFlags.NonPublic);
    readonly List<Tile> tiles=new List<Tile>();
    readonly Action<string> log;
    readonly HashSet<string> missing=new HashSet<string>();
    readonly AnimationCurve curve=new AnimationCurve(new Keyframe(0,1),new Keyframe(.99f,1),new Keyframe(1,0));
    ZDOMan world;Game game;float nextScan;int cursor;bool failed;
    sealed class Tile {
        public Vector3 Position;
        public Heightmap Map;public TerrainComp Compiler;public ZDO Data;
        public readonly List<DriftTrackStamp> Paint=new List<DriftTrackStamp>();
        public readonly List<DriftTrackPiece> Pieces=new List<DriftTrackPiece>();
    }
    internal ParkadeVenue(Action<string> log){this.log=log;}
    internal int CompletedOperations {get {int count=0;foreach(var t in tiles)if(t.Data!=null)count+=Math.Min(t.Paint.Count,t.Data.GetInt(PaintKey))+Math.Min(t.Pieces.Count,t.Data.GetInt(PieceKey));return count;}}
    internal int PlannedOperations {get {int count=0;foreach(var t in tiles)count+=t.Paint.Count+t.Pieces.Count;return count;}}
    // Saved work survives streaming out. The complete plan, including tiles not
    // discovered yet, is the denominator; 100% never means only nearby work.
    internal bool ConstructionComplete {
        get {if(!ContextValid||tiles.Count==0)return false;foreach(var t in tiles)if(!Complete(t))return false;return true;}
    }
    bool ContextValid=>!failed&&world!=null&&world==ZDOMan.instance&&game!=null&&game==Game.instance&&!game.IsShuttingDown();
    internal bool RaceReady {
        get {
            return ConstructionComplete&&StartReady;
        }
    }
    // Only the start's live tile must be owned now. Distant completed tiles may
    // be unloaded normally, even at the game's smallest simulation distance.
    internal bool StartReady {
        get {
            if(!ContextValid)return false;
            var tile=At(ParkadeLayout.StartX,ParkadeLayout.StartZ);
            return tile!=null&&Usable(tile)&&Complete(tile);
        }
    }
    Tile At(float x,float z) {
        foreach(var tile in tiles) {
            var p=tile.Position;const float half=32;
            if(x>=p.x-half&&x<p.x+half&&z>=p.z-half&&z<p.z+half)return tile;
        }
        return null;
    }
    static bool Complete(Tile tile)=>tile.Data!=null&&tile.Data.GetInt(PaintKey)>=tile.Paint.Count&&tile.Data.GetInt(PieceKey)>=tile.Pieces.Count;
    static bool Ready(Vector3 p)=>ZoneSystem.instance!=null&&ZNetScene.instance!=null&&ZoneSystem.instance.IsZoneLoaded(p);
    static bool Usable(Tile tile)=>tile.Data!=null&&tile.Map!=null&&tile.Compiler!=null&&tile.Compiler.IsOwner()
        &&(bool)initialized.GetValue(tile.Compiler)&&Ready(tile.Position);
    // Finish work in the current active area first, then move the held car's
    // native streaming reference to the nearest remaining tile. No global
    // graphics settings, native streaming distances or saved keys are changed.
    internal bool TryPreparationPoint(Vector3 current,out Vector3 point) {
        point=current;if(!ContextValid||ConstructionComplete)return false;
        Tile nearest=null;float distance=float.MaxValue;
        foreach(var tile in tiles) {
            if(Complete(tile))continue;
            if(Usable(tile))return false;
            float dx=tile.Position.x-current.x,dz=tile.Position.z-current.z,ds=dx*dx+dz*dz;
            if(ds<distance){distance=ds;nearest=tile;}
        }
        if(nearest==null||distance<1)return false;
        point=new Vector3(nearest.Position.x,ParkadeLayout.TerrainHeightAt(nearest.Position.x,nearest.Position.z)+.35f,nearest.Position.z);
        return true;
    }
    internal string PendingStatus() {
        foreach(var t in tiles)if(!Complete(t))return "tile "+t.Position.x+","+t.Position.z
            +"; terrain="+(t.Map!=null)+"; loaded="+Ready(t.Position)+"; owned="+(t.Compiler!=null&&t.Compiler.IsOwner())
            +"; initialized="+(t.Compiler!=null&&(bool)initialized.GetValue(t.Compiler));
        return ConstructionComplete?"construction complete; waiting for start terrain":"construction unavailable";
    }
    internal void Tick() {
        if(world!=ZDOMan.instance||game!=Game.instance){Clear();world=ZDOMan.instance;game=Game.instance;if(world!=null&&game!=null)PlanTiles();}
        if(failed||world==null||game==null||game.IsShuttingDown()||Time.timeScale<=0||ZNetScene.instance==null||ZoneSystem.instance==null)return;
        try {
            if(Time.unscaledTime>=nextScan){Discover();nextScan=Time.unscaledTime+.5f;}
            int work=0;
            for(int attempts=0;attempts<tiles.Count&&work<2;attempts++) {
                if(cursor>=tiles.Count)cursor=0;
                if(Step(tiles[cursor++]))work++;
            }
        } catch(Exception error) {failed=true;log?.Invoke("Parkade construction stopped: "+error.GetBaseException().Message);}
    }
    void Discover() {
        if(operation==null||initialized==null||lastRevision==null)throw new MissingMethodException("Native terrain paint API unavailable");
        foreach(var map in Heightmap.GetAllHeightmaps()) {
            if(map==null||map.IsDistantLod)continue;
            var p=map.transform.position;var tile=At(p.x,p.z);
            if(tile==null||!Ready(p))continue;
            if(tile.Map==map&&tile.Compiler!=null&&tile.Data!=null)continue;
            // Before creating a compiler, allow native saved objects to arrive
            // first. Otherwise GetAndCreate could duplicate an existing ZDO.
            // A blocked edge is handled by preparation moving its reference;
            // already attached owned compilers need no neighbouring-area gate.
            if(!ZNetScene.instance.IsAreaReady(p))continue;
            tile.Map=map;
            tile.Compiler=map.GetAndCreateTerrainCompiler();
            if(tile.Compiler!=null)tile.Data=tile.Compiler.GetComponent<ZNetView>()?.GetZDO()??tile.Data;
        }
    }
    void PlanTiles() {
        // Native GetZone uses 64 m squares centred on multiples of 64. Keep
        // exactly the original per-tile stamp/piece order for existing saves.
        var b=ParkadeLayout.Bounds;
        for(int x=(int)Math.Floor((b.MinX-12)/64);x<=(int)Math.Ceiling((b.MaxX+12)/64);x++)
        for(int z=(int)Math.Floor((b.MinZ-12)/64);z<=(int)Math.Ceiling((b.MaxZ+12)/64);z++) {
            var tile=new Tile{Position=new Vector3(x*64,0,z*64)};var p=tile.Position;
            foreach(var stamp in ParkadePlan.PaintStamps)
                if(DriftTrackConstructionPlan.IntersectsTile(stamp,p.x,p.z,32))tile.Paint.Add(stamp);
            foreach(var piece in ParkadePlan.Pieces)
                if(piece.X>=p.x-32&&piece.X<p.x+32&&piece.Z>=p.z-32&&piece.Z<p.z+32)tile.Pieces.Add(piece);
            if(tile.Paint.Count>0||tile.Pieces.Count>0)tiles.Add(tile);
        }
    }
    bool Step(Tile tile) {
        if(!Usable(tile))return false;
        int progress=DriftTrackConstructionPlan.ResumeIndex(tile.Data.GetInt(PaintKey),tile.Paint.Count);
        if(progress<tile.Paint.Count) {
            var stamp=tile.Paint[progress];
            operation.Invoke(tile.Compiler,new object[]{new Vector3(stamp.X,ParkadeLayout.TerrainHeightAt(stamp.X,stamp.Z),stamp.Z),Vector3.zero,new TerrainOp.Settings {
                m_level=false,m_raise=false,m_smooth=false,m_square=false,m_halfOffset=false,
                m_paintCleared=true,m_paintHeightCheck=false,m_paintRadius=stamp.Radius,
                m_paintType=TerrainModifier.PaintType.Paved,m_paintStrength=1,m_paintExp=.1f,m_paintCurve=curve}});
            Save(tile,PaintKey,progress+1);return true;
        }
        progress=DriftTrackConstructionPlan.ResumeIndex(tile.Data.GetInt(PieceKey),tile.Pieces.Count);
        if(progress>=tile.Pieces.Count||!Place(tile.Pieces[progress]))return false;
        Save(tile,PieceKey,progress+1);return true;
    }
    static void Save(Tile tile,string key,int value){tile.Data.Set(key,value);lastRevision.SetValue(tile.Compiler,tile.Data.DataRevision);}
    bool Place(DriftTrackPiece piece) {
        var prefab=ZNetScene.instance.GetPrefab(piece.Prefab);
        if(prefab==null){if(missing.Add(piece.Prefab))log?.Invoke("Parkade: waiting for native prefab "+piece.Prefab);return false;}
        var root=UnityEngine.Object.Instantiate(prefab,new Vector3(piece.X,ParkadeLayout.TerrainHeightAt(piece.X,piece.Z)+piece.HeightOffset,piece.Z),Quaternion.Euler(0,piece.Yaw,0));
        try {
            var data=root.GetComponent<ZNetView>()?.GetZDO();
            if(data==null)throw new InvalidOperationException("Parkade piece lacks native world data: "+piece.Prefab);
            data.Persistent=true;data.Set(PieceTag,1);
            if(root.GetComponent<Fireplace>() is Fireplace fire)data.Set(ZDOVars.s_fuel,fire.m_maxFuel);
            if(piece.Text!=null) {
                var sign=root.GetComponent<Sign>();if(sign==null)throw new InvalidOperationException("Parkade sign lacks Sign");sign.SetText(piece.Text);
            }
        } catch {
            if(root.GetComponent<ZNetView>()?.GetZDO()!=null)ZNetScene.instance.Destroy(root);else UnityEngine.Object.Destroy(root);throw;
        }
        return true;
    }
    internal void Clear(){tiles.Clear();missing.Clear();world=null;game=null;cursor=0;nextScan=0;failed=false;}
}
}
