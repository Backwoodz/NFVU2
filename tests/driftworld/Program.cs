using System;
using System.IO;
using System.Linq;
using HarmonyLib;
using Mono.Cecil;
using Rachel350Z;
using UnityEngine;

static class Program {
    static int checks;
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
    static WorldGenerator Generator(string seed,bool menu=false)=>new WorldGenerator{m_world=new World{m_seedName=seed,m_menu=menu}};
    static void NativeContracts(string gamePath,string pluginPath) {
        using(var assembly=AssemblyDefinition.ReadAssembly(Path.Combine(gamePath,"valheim_Data/Managed/assembly_valheim.dll"))) {
            var generator=assembly.MainModule.Types.Single(t=>t.Name=="WorldGenerator");
            var zone=assembly.MainModule.Types.Single(t=>t.Name=="ZoneSystem");
            Check(generator.Methods.Any(m=>m.Name=="GetBiome"&&string.Join(",",m.Parameters.Select(p=>p.ParameterType.FullName))=="System.Single,System.Single,System.Single,System.Boolean"),"installed game has the patched biome overload");
            var height=generator.Methods.Single(m=>m.Name=="GetBiomeHeight");
            Check(height.ReturnType.FullName=="System.Single"&&height.Parameters.Any(p=>p.Name=="wx"&&p.ParameterType.FullName=="System.Single")
                &&height.Parameters.Any(p=>p.Name=="wy"&&p.ParameterType.FullName=="System.Single")
                &&height.Parameters.Any(p=>p.Name=="mask"&&p.ParameterType.FullName=="UnityEngine.Color&"),"installed height API matches production prefix");
            Check(zone.Methods.Any(m=>m.Name=="GenerateLocationsTimeSliced"&&m.Parameters.Count==3
                &&m.ReturnType.FullName=="System.Collections.IEnumerator"&&m.Parameters[0].ParameterType.Name=="ZoneLocation"
                &&m.Parameters[1].ParameterType.FullName=="System.Diagnostics.Stopwatch"&&m.Parameters[2].Name=="iterationsPkg"
                &&m.Parameters[2].ParameterType.Name=="ZPackage"),"installed location worker supports empty-generation adapter");
            Check(zone.Methods.Count(m=>m.Name=="PlaceVegetation")==1&&zone.Methods.Count(m=>m.Name=="PlaceLocations")==1,"placement patch targets are unambiguous in installed game");
            var icon=zone.Methods.Single(m=>m.Name=="GetLocationIcon");
            Check(icon.ReturnType.FullName=="System.Boolean"&&icon.Parameters.Count==2
                &&icon.Parameters[0].Name=="name"&&icon.Parameters[1].Name=="pos"
                &&icon.Parameters[1].ParameterType.FullName=="UnityEngine.Vector3&","installed start-location lookup matches production prefix");
        }
        using(var assembly=AssemblyDefinition.ReadAssembly(pluginPath)) {
            Check(!assembly.MainModule.AssemblyReferences.Any(r=>r.Name.IndexOf("FlatGrass",StringComparison.OrdinalIgnoreCase)>=0||r.Name=="NFVU2.DriftWorld"),"NFVU2 references neither FlatGrass nor a separate drift plugin");
            var plugin=assembly.MainModule.Types.Single(t=>t.Name=="Plugin");
            Check(!plugin.CustomAttributes.Any(a=>a.AttributeType.Name=="BepInDependency"),"NFVU2 needs no FlatGrass dependency");
            Check(plugin.CustomAttributes.Any(a=>a.AttributeType.Name=="BepInPlugin"&&(string)a.ConstructorArguments[0].Value=="local.rachel350z.prototype"),"existing NFVU2 plugin identity is preserved");
            Check(assembly.MainModule.Types.Any(t=>t.FullName=="Rachel350Z.DriftWorldRules")&&assembly.MainModule.Types.Any(t=>t.FullName=="Rachel350Z.DriftHeight"),"world generator is compiled into the vehicle DLL");
            Check(plugin.Methods.Single(m=>m.Name=="InitializeControls").Body.Instructions.Any(i=>i.Operand is MethodReference called&&called.Name=="InitializeDriftWorld"),"NFVU2 initializes drift world patches at startup");
            Check(plugin.Methods.Single(m=>m.Name=="LateUpdate").Body.Instructions.Any(i=>i.Operand is MethodReference called&&called.Name=="UpdateDriftSeedFeedback"),"seed feedback runs after menu input each frame");
            Check(plugin.Methods.Single(m=>m.Name=="OnDestroy").Body.Instructions.Any(i=>i.Operand is MethodReference called&&called.DeclaringType.Name=="DriftSeedFeedback"&&called.Name=="Clear"),"plugin unload restores owned seed feedback");
        }
    }
    static void SeedFeedback() {
        var normal=new Color(.8f,.75f,.7f,1);
        var text=new TMPro.TMP_Text{color=normal};
        var input=new TMPro.TMP_InputField{ text="ordinary",textComponent=text };
        DriftSeedFeedback.Clear();DriftSeedFeedback.Tick(input);
        Check(text.color==normal,"ordinary seed keeps menu text color");
        foreach(var seed in new[]{"drift","DRIFT","DrIfT"}) {
            input.text=seed;DriftSeedFeedback.Tick(input);
            Check(text.color==DriftSeedFeedback.MatchColor,"exact drift seed is green: "+seed);
        }
        input.text="drift2";DriftSeedFeedback.Tick(input);
        Check(text.color==normal,"editing away from drift restores original color");
        input.text="drift";DriftSeedFeedback.Tick(input);input.isActiveAndEnabled=false;DriftSeedFeedback.Tick(input);
        Check(text.color==normal,"closing the seed field restores its original color");
        input.isActiveAndEnabled=true;DriftSeedFeedback.Tick(input);
        Check(text.color==DriftSeedFeedback.MatchColor,"reopened drift field becomes green again");
        var otherModGreen=new Color(.45f,1,.45f,1);
        input.text="flatgrass";text.color=otherModGreen;DriftSeedFeedback.Tick(input);
        Check(text.color==otherModGreen,"another mod's flatgrass feedback is preserved");
        text.color=normal;input.text="drift";DriftSeedFeedback.Tick(input);
        var replacement=new TMPro.TMP_Text{color=new Color(.6f,.6f,.6f)};
        input.textComponent=replacement;DriftSeedFeedback.Tick(input);
        Check(text.color==normal&&replacement.color==DriftSeedFeedback.MatchColor,"replaced menu text restores old label and highlights new label");
        DriftSeedFeedback.Clear();
        Check(replacement.color==new Color(.6f,.6f,.6f),"plugin cleanup restores the current label");
        input.textComponent=null;DriftSeedFeedback.Tick(input);DriftSeedFeedback.Tick(null);
        Check(true,"missing input or label is harmless");
        input.textComponent=text;input.text="drift";DriftSeedFeedback.Tick(input);
        text.color=otherModGreen;DriftSeedFeedback.Clear();
        Check(text.color==otherModGreen,"cleanup preserves a color another provider now owns");
    }
    static void Main(string[] args) {
        NativeContracts(args[0],args[1]);
        Check(typeof(ZPackage).BaseType.Assembly.GetName().Name=="assembly_valheim","generation result uses the installed game's real ZPackage stream");
        SeedFeedback();
        var harmony=new Harmony("nfvu2.tests.driftworld");
        harmony.PatchAll(typeof(DriftWorldRules).Assembly);
        try {
            foreach(var seed in new[]{"drift","DRIFT","DrIfT"}) {
                var generator=Generator(seed);WorldGenerator.instance=generator;
                Check(DriftWorldRules.Current,"exact seed recognized: "+seed);
                foreach(var point in new[]{new[]{0f,0f},new[]{130f,-200f},new[]{-450f,350f},new[]{10500f,0f},new[]{0f,-10500f}}) {
                    Check(generator.GetBiome(point[0],point[1])==Heightmap.Biome.Meadows,"drift has Meadows across interior");
                    Check(generator.GetBiomeHeight(Heightmap.Biome.Mountain,point[0],point[1],out var mask)==32&&mask.r==0&&mask.a==1,"flat 32m surface and initialized terrain mask");
                }
                Check(generator.HeightCalls==0&&generator.BiomeCalls==0,"drift bypasses original terrain/biome calculations");
                Check(generator.GetBiomeHeight(Heightmap.Biome.Ocean,0,0,out _,true,false)==32,"pregeneration uses same height as live chunks");
                Check(generator.GetBiome(10501,0)==Heightmap.Biome.Mountain&&generator.GetBiomeHeight(Heightmap.Biome.Ocean,10501,0,out _)==123,"native world edge remains outside interior override");
                var zone=new ZoneSystem();var definition=new ZoneSystem.ZoneLocation();
                zone.SavedPlayerBuildings.Add("player wall");
                Check(zone.GenerateOne(definition)==0&&zone.NativeAttempts==0&&zone.Completed==1,"empty locations still complete native outer generation contract");
                Check(definition.m_quantity==3&&definition.m_enable,"location definitions remain unchanged");
                zone.PlaceLocations();zone.PlaceVegetation();
                Check(zone.NativeLocations==0&&zone.NativeVegetation==0,"drift skips native structures trees rocks and ore placement");
                Check(zone.SavedPlayerBuildings.Single()=="player wall","generated-content suppression preserves saved player buildings");
                Game.instance=null;
                Check(zone.GetLocationIcon("StartTemple",out var start)&&start.x==0&&start.y==32&&start.z==0,"first spawn has a fixed ground-level centre without a temple");
                Check(!zone.GetLocationIcon("Eikthyrnir",out var other)&&other.x==1,"unrelated map icons retain native behavior");
                Game.instance=new Game{m_StartLocation="CustomStart"};
                Check(zone.GetLocationIcon("CustomStart",out start)&&start.y==32&&!zone.GetLocationIcon("StartTemple",out _),"start fallback respects native configured start location");
                Game.instance=null;
            }
            foreach(var seed in new[]{null,"","flatgrass","normal","adrift","drift2","my-drift"," drift","drift "}) {
                var generator=Generator(seed);WorldGenerator.instance=generator;
                Check(!DriftWorldRules.Current&&generator.GetBiome(0,0)==Heightmap.Biome.Mountain&&generator.GetBiomeHeight(Heightmap.Biome.Meadows,0,0,out _)==123,"other seed retains native generation: "+(seed??"null"));
                var zone=new ZoneSystem();
                Check(zone.GenerateOne(new ZoneSystem.ZoneLocation())==17&&zone.NativeAttempts==1,"normal-world location worker runs");
                zone.PlaceVegetation();zone.PlaceLocations();
                Check(zone.NativeLocations==1&&zone.NativeVegetation==1&&!zone.GetLocationIcon("StartTemple",out _),"normal-world content and start lookup run");
            }
            var menu=Generator("drift",true);WorldGenerator.instance=menu;
            Check(!DriftWorldRules.Current&&menu.GetBiome(0,0)==Heightmap.Biome.Mountain&&menu.GetBiomeHeight(Heightmap.Biome.Meadows,0,0,out _)==123,"menu world never flattened");
            WorldGenerator.instance=null;
            Check(!DriftWorldRules.Current&&!DriftWorldRules.Applies(null)&&!DriftWorldRules.Interior(null,0,0),"missing world/generator is a no-op");
            WorldGenerator.instance=Generator("normal");
            Check(Generator("drift").GetBiome(0,0)==Heightmap.Biome.Meadows,"terrain workers use their own world rather than global current world");
            WorldGenerator.instance=Generator("drift");
            Check(Generator("normal").GetBiome(0,0)==Heightmap.Biome.Mountain,"parallel normal generator does not inherit drift terrain");
            foreach(var seed in new[]{"drift","flatgrass","drift","normal"}) {
                WorldGenerator.instance=Generator(seed);var zone=new ZoneSystem();zone.PlaceVegetation();
                Check(zone.NativeVegetation==(seed=="drift"?0:1),"world switching cannot leak content suppression: "+seed);
            }
        } finally {harmony.UnpatchSelf();}
        WorldGenerator.instance=Generator("drift");
        Check(WorldGenerator.instance.GetBiome(0,0)==Heightmap.Biome.Mountain,"unload removes this mod's world patches");
        Console.WriteLine(checks+" integrated drift-world checks passed. Native API metadata + real Harmony against doubles; no Unity world loaded.");
    }
}
