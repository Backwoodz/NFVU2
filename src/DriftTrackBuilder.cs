using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Rachel350Z {
// Build loaded tiles after native ZDO/terrain readiness. Each tile saves its own
// progress alongside its native terrain data, so unloading cannot skip neighbors.
internal sealed class DriftTrackBuilder {
    internal const string PaintProgress="nfvu2_drift_test_pave_v1";
    internal const string PieceProgress="nfvu2_drift_test_pieces_v1";
    const string PieceTag="nfvu2_drift_test_piece_v1";
    static readonly MethodInfo operation=typeof(TerrainComp).GetMethod("DoOperation",BindingFlags.Instance|BindingFlags.NonPublic,
        null,new[]{typeof(Vector3),typeof(Vector3),typeof(TerrainOp.Settings)},null);
    static readonly FieldInfo initialized=typeof(TerrainComp).GetField("m_initialized",BindingFlags.Instance|BindingFlags.NonPublic);
    static readonly FieldInfo lastRevision=typeof(TerrainComp).GetField("m_lastDataRevision",BindingFlags.Instance|BindingFlags.NonPublic);
    readonly List<Tile> tiles=new List<Tile>();
    readonly Action<string> log;
    readonly HashSet<string> missing=new HashSet<string>();
    readonly AnimationCurve paintCurve=new AnimationCurve(new Keyframe(0,1),new Keyframe(.99f,1),new Keyframe(1,0));
    ZDOMan world;
    Game game;
    float nextScan;
    int cursor;
    bool failed;
    Minimap map;
    Minimap.PinData pin;
    internal DriftTrackBuilder(Action<string> log){this.log=log;}
    sealed class Tile {
        public Heightmap Map;
        public TerrainComp Compiler;
        public ZDO Data;
        public readonly List<DriftTrackStamp> Paint=new List<DriftTrackStamp>();
        public readonly List<DriftTrackPiece> Pieces=new List<DriftTrackPiece>();
    }
    internal void Tick() {
        if(world!=ZDOMan.instance||game!=Game.instance){Clear();world=ZDOMan.instance;game=Game.instance;}
        if(failed||world==null||game==null||game.IsShuttingDown()||Time.timeScale<=0||ZNetScene.instance==null||ZoneSystem.instance==null)return;
        try {
            UpdatePin();
            if(Time.unscaledTime>=nextScan){Discover();nextScan=Time.unscaledTime+.5f;}
            // Bounded main-thread work; never ask the terrain builder to load
            // remote zones or hold up the game's world-loading coroutines.
            int work=0;
            for(int attempts=0;attempts<tiles.Count&&work<2;attempts++) {
                if(cursor>=tiles.Count)cursor=0;
                if(Step(tiles[cursor++]))work++;
            }
        } catch(Exception error) {
            failed=true;
            log?.Invoke("Drift test construction stopped: "+error.GetBaseException().Message);
        }
    }
    void Discover() {
        if(operation==null||initialized==null||lastRevision==null)throw new MissingMethodException("Native terrain paint API unavailable");
        tiles.RemoveAll(tile=>tile.Map==null||tile.Compiler==null);
        foreach(var heightmap in Heightmap.GetAllHeightmaps()) {
            if(heightmap==null||heightmap.IsDistantLod||tiles.Exists(tile=>tile.Map==heightmap))continue;
            var position=heightmap.transform.position;
            float half=heightmap.m_width*heightmap.m_scale*.5f;
            var tile=new Tile{Map=heightmap};
            foreach(var stamp in DriftTrackConstructionPlan.PaintStamps)
                if(DriftTrackConstructionPlan.IntersectsTile(stamp,position.x,position.z,half))tile.Paint.Add(stamp);
            foreach(var piece in DriftTrackPieces.Items)
                if(piece.X>=position.x-half&&piece.X<position.x+half&&piece.Z>=position.z-half&&piece.Z<position.z+half)tile.Pieces.Add(piece);
            if(tile.Paint.Count==0&&tile.Pieces.Count==0)continue;
            if(!Ready(position))continue;
            tile.Compiler=heightmap.GetAndCreateTerrainCompiler();
            if(tile.Compiler==null||!tile.Compiler.IsOwner()||!(bool)initialized.GetValue(tile.Compiler))continue;
            tile.Data=tile.Compiler.GetComponent<ZNetView>()?.GetZDO();
            if(tile.Data!=null)tiles.Add(tile);
        }
    }
    bool Ready(Vector3 position)=>ZoneSystem.instance.IsZoneLoaded(position)&&ZNetScene.instance.IsAreaReady(position);
    bool Step(Tile tile) {
        if(tile.Map==null||tile.Compiler==null||!tile.Compiler.IsOwner()||!Ready(tile.Map.transform.position))return false;
        int progress=DriftTrackConstructionPlan.ResumeIndex(tile.Data.GetInt(PaintProgress),tile.Paint.Count);
        if(progress<tile.Paint.Count) {
            var stamp=tile.Paint[progress];
            operation.Invoke(tile.Compiler,new object[]{new Vector3(stamp.X,DriftWorldRules.GroundHeight,stamp.Z),Vector3.zero,PaintSettings(stamp.Radius,paintCurve)});
            SaveProgress(tile,PaintProgress,progress+1);
            return true;
        }
        progress=DriftTrackConstructionPlan.ResumeIndex(tile.Data.GetInt(PieceProgress),tile.Pieces.Count);
        if(progress>=tile.Pieces.Count)return false;
        if(!Place(tile.Pieces[progress]))return false;
        // Placement progress lives even after a player deliberately dismantles
        // the piece. A missing piece is not permission to respawn it next login.
        SaveProgress(tile,PieceProgress,progress+1);
        return true;
    }
    static void SaveProgress(Tile tile,string key,int value) {
        tile.Data.Set(key,value);
        // The paint save has already updated the compiler's native data. Our
        // additional metadata is not a remote terrain edit: avoid CheckLoad
        // rebuilding the collider just because this marker advanced.
        lastRevision.SetValue(tile.Compiler,tile.Data.DataRevision);
    }
    internal static TerrainOp.Settings PaintSettings(float radius,AnimationCurve curve)=>new TerrainOp.Settings {
        m_level=false,m_raise=false,m_smooth=false,m_square=false,m_halfOffset=false,
        m_paintCleared=true,m_paintHeightCheck=false,m_paintRadius=radius,
        m_paintType=TerrainModifier.PaintType.Paved,m_paintStrength=1,m_paintExp=.1f,m_paintCurve=curve
    };
    bool Place(DriftTrackPiece piece) {
        var prefab=ZNetScene.instance.GetPrefab(piece.Prefab);
        if(prefab==null) {
            if(missing.Add(piece.Prefab))log?.Invoke("Drift test: native prefab not yet available: "+piece.Prefab);
            return false;
        }
        var position=new Vector3(piece.X,DriftWorldRules.GroundHeight+piece.HeightOffset,piece.Z);
        // Readiness has already loaded the owning tile. Preserve native
        // support, collisions, destruction and editing; never modify prefabs.
        var root=UnityEngine.Object.Instantiate(prefab,position,Quaternion.Euler(0,piece.Yaw,0));
        try {
            var view=root.GetComponent<ZNetView>();var data=view?.GetZDO();
            if(data==null)throw new InvalidOperationException("Track piece lacks native world data: "+piece.Prefab);
            data.Persistent=true;data.Set(PieceTag,1);
            if(piece.Text!=null) {
                var sign=root.GetComponent<Sign>();
                if(sign==null)throw new InvalidOperationException("Track sign prefab lacks Sign");
                sign.SetText(piece.Text);
            }
        } catch {
            if(root.GetComponent<ZNetView>()?.GetZDO()!=null)ZNetScene.instance.Destroy(root);
            else UnityEngine.Object.Destroy(root);
            throw;
        }
        return true;
    }
    void UpdatePin() {
        if(map==Minimap.instance)return;
        RemovePin();map=Minimap.instance;
        if(map!=null)pin=map.AddPin(new Vector3(DriftTrackLayout.StartX,DriftWorldRules.GroundHeight,DriftTrackLayout.StartZ),Minimap.PinType.Icon3,"NFVU2 Drift",false,false);
    }
    void RemovePin(){if(map!=null&&pin!=null)map.RemovePin(pin);map=null;pin=null;}
    internal void Clear(){RemovePin();tiles.Clear();missing.Clear();world=null;game=null;nextScan=0;cursor=0;failed=false;}
}
}
