using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Rachel350Z;

static class Program {
    static int checks;
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;}
    static void Native(string game,string plugin) {
        using(var assembly=AssemblyDefinition.ReadAssembly(Path.Combine(game,"valheim_Data/Managed/assembly_valheim.dll"))) {
            var terrain=assembly.MainModule.Types.Single(t=>t.Name=="TerrainComp");
            Check(terrain.Methods.Any(m=>m.Name=="DoOperation"&&string.Join(",",m.Parameters.Select(p=>p.ParameterType.FullName))=="UnityEngine.Vector3,UnityEngine.Vector3,TerrainOp/Settings"),"native terrain paint operation signature");
            Check(terrain.Fields.Any(f=>f.Name=="m_initialized"&&f.FieldType.FullName=="System.Boolean"),"native initialization guard exists");
            Check(terrain.Fields.Any(f=>f.Name=="m_lastDataRevision"&&f.FieldType.FullName=="System.UInt32"),"native marker revision can avoid redundant collider rebuilds");
            Check(terrain.Methods.Any(m=>m.Name=="IsOwner"&&m.ReturnType.FullName=="System.Boolean"),"terrain ownership check is available");
            var heightmap=assembly.MainModule.Types.Single(t=>t.Name=="Heightmap");
            Check(heightmap.Methods.Any(m=>m.Name=="GetAllHeightmaps"&&m.IsStatic)&&heightmap.Methods.Any(m=>m.Name=="GetAndCreateTerrainCompiler"),"native loaded heightmaps and compiler access exist");
            Check(assembly.MainModule.Types.Single(t=>t.Name=="ZoneSystem").Methods.Any(m=>m.Name=="IsZoneLoaded"&&m.Parameters.Count==1&&m.Parameters[0].ParameterType.FullName=="UnityEngine.Vector3"),"native loaded-zone gate exists");
            Check(assembly.MainModule.Types.Single(t=>t.Name=="Sign").Methods.Any(m=>m.Name=="SetText"&&m.IsPublic&&m.Parameters.Count==1&&m.Parameters[0].ParameterType.FullName=="System.String"),"native sign text path exists");
        }
        using(var assembly=AssemblyDefinition.ReadAssembly(plugin)) {
            var owner=assembly.MainModule.Types.Single(t=>t.FullName=="Rachel350Z.Plugin");
            Check(owner.Methods.Single(m=>m.Name=="Update").Body.Instructions.Any(i=>i.Operand is MethodReference method&&method.Name=="TickDriftTrack"),"NFVU2 ticks integrated track builder");
            Check(owner.Methods.Single(m=>m.Name=="OnDestroy").Body.Instructions.Any(i=>i.Operand is MethodReference method&&method.DeclaringType.Name=="DriftTrackBuilder"&&method.Name=="Clear"),"plugin unload releases temporary track pins/state");
            var builder=assembly.MainModule.Types.Single(t=>t.Name=="DriftTrackBuilder");
            Check(builder.Methods.Single(m=>m.Name=="SaveProgress").Body.Instructions.Any(i=>i.Operand is MethodReference method&&method.Name=="SetValue"),"marker update synchronizes native compiler revision");
        }
        // Exercise the compiled production paint settings with Valheim's real
        // Settings class; no mock defaults and no Unity world are involved.
        AppDomain.CurrentDomain.AssemblyResolve+=(sender,args)=> {
            var filename=new AssemblyName(args.Name).Name+".dll";
            foreach(var folder in new[]{Path.Combine(game,"valheim_Data/Managed"),Path.Combine(game,"BepInEx/core"),Path.GetDirectoryName(plugin)}) {
                var path=Path.Combine(folder,filename);if(File.Exists(path))return Assembly.LoadFrom(path);
            }
            return null;
        };
        var built=Assembly.LoadFrom(plugin);
        var type=built.GetType("Rachel350Z.DriftTrackBuilder",true);
        var settings=type.GetMethod("PaintSettings",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{8f,null});
        Func<string,object> read=name=>settings.GetType().GetField(name).GetValue(settings);
        Check(!(bool)read("m_level")&&!(bool)read("m_raise")&&!(bool)read("m_smooth"),"real settings never change terrain heights");
        Check((bool)read("m_paintCleared")&&read("m_paintType").ToString()=="Paved", "real native settings choose paved terrain");
        Check((float)read("m_paintRadius")==8f&&!(bool)read("m_halfOffset")&&!(bool)read("m_paintHeightCheck"),"real settings use world-centred full road stamp");
    }
    static void Pieces() {
        Check(DriftTrackPieces.Items.Count<100,"prototype has a bounded piece count");
        foreach(var piece in DriftTrackPieces.Items) {
            Check(!DriftTrackLayout.Contains(piece.X,piece.Z,1),"guide pieces remain off the driving surface");
            Check(piece.Prefab=="stone_wall_2x1"||piece.Prefab=="wood_pole2"||piece.Prefab=="piece_banner01"||piece.Prefab=="sign","only verified native prefabs are planned");
            Check(piece.HeightOffset>=.2f&&piece.HeightOffset<=3.6f,"pieces use measured grounded/support heights");
            Check(piece.Text==null||piece.Text.Length<=50,"native sign text limit is respected");
            Check(!float.IsNaN(piece.Yaw)&&!float.IsInfinity(piece.Yaw),"guide orientation is finite");
        }
        int walls=DriftTrackPieces.Items.Count(piece=>piece.Prefab=="stone_wall_2x1");
        Check(walls%2==0&&walls>=50,"sparse stones mark both sides of the complete U section");
        Check(DriftTrackPieces.Items.Count(piece=>piece.Text!=null)==3,"spawn approach start and end have native signs");
        Check(DriftTrackPieces.Items.Count(piece=>piece.Prefab=="piece_banner01")==2,"two supported start flags are planned");
        foreach(var flag in DriftTrackPieces.Items.Where(piece=>piece.Prefab=="piece_banner01")) {
            Check(flag.HeightOffset-2.92621f>.6f,"native banner mesh stays completely above terrain");
            Check(DriftTrackPieces.Items.Any(piece=>piece.Prefab=="wood_pole2"&&piece.X==flag.X&&piece.Z==flag.Z&&piece.HeightOffset==2.85f),"upper grounded-chain pole supports each high flag");
        }
    }
    static void Main(string[] args) {
        checks+=LayoutTests.Run();checks+=PlanTests.Run();
        Pieces();Native(args[0],args[1]);
        Console.WriteLine(checks+" drift track checks passed (geometry, tile progress, native API/settings). No Unity driving/render/save-load test performed.");
    }
}
