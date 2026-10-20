using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Rachel350Z;
using UnityEngine;
static class Program {
    static int checks;
    static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
    static void World(string seed,bool menu=false,bool single=true) {
        WorldGenerator.instance=new WorldGenerator{m_world=new World{m_seedName=seed,m_menu=menu}};ZNet.IsSinglePlayer=single;
    }
    static bool Prefix(Type patch,params object[] values)=>(bool)patch.GetMethod("Prefix",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,values);
    static Character Creature(bool player=false,bool tame=false,bool spectator=false,bool owner=true) {
        var root=new GameObject();var character=root.Add(new Character{Player=player,Tame=tame});
        var view=root.Add(new ZNetView{Owner=owner});view.Data.Set(DriftSessionRules.SpectatorTag,spectator);
        Character.All.Add(character);return character;
    }
    static ItemDrop Item(bool placed=false,bool owner=true,bool valid=true) {
        var root=new GameObject();var item=root.Add(new ItemDrop());
        var view=root.Add(new ZNetView{Owner=owner,Valid=valid});view.Data.Set(ZDOVars.s_piece,placed);
        ItemDrop.All.Add(item);return item;
    }
    static void RulesAndSpawns() {
        World(null);Check(!DriftSessionRules.Active,"null seed leaves normal world untouched");
        foreach(var seed in new[]{"drift","DRIFT","DrIfT"}) {
            World(seed);Check(DriftSessionRules.Active,"exact drift seed activates restrictions: "+seed);
            foreach(var type in new[]{typeof(DriftRandomSpawns),typeof(DriftCreatureSpawns),typeof(DriftAreaSpawns)})
                Check(!Prefix(type),"random spawning is blocked: "+type.Name);
            Check(!Prefix(typeof(DriftRandomRaids),new object[]{new RandomEvent()}),"new raid blocked");
            Check(Prefix(typeof(DriftRandomRaids),new object[]{null}),"native raid reset remains available");
            Check(!Prefix(typeof(DriftBuildSelection))&&!Prefix(typeof(DriftBuildOpen)),"build menu keyboard/controller and direct-open paths are blocked");
        }
        foreach(var seed in new[]{"ordinary","flatgrass","drift2"," drift","drift "}) {
            World(seed);Check(!DriftSessionRules.Active,"nonmatching seed remains native: "+seed);
            Check(Prefix(typeof(DriftRandomSpawns))&&Prefix(typeof(DriftCreatureSpawns))&&Prefix(typeof(DriftAreaSpawns)),"native spawning preserved: "+seed);
            Check(Prefix(typeof(DriftRandomRaids),new object[]{new RandomEvent()}),"native raids preserved: "+seed);
            Check(Prefix(typeof(DriftBuildSelection))&&Prefix(typeof(DriftBuildOpen)),"other worlds keep their native build menu");
        }
        World("drift",menu:true);Check(!DriftSessionRules.Active,"menu worlds excluded");
        World("drift",single:false);Check(!DriftSessionRules.Active,"network world excluded");
        WorldGenerator.instance=null;Check(!DriftSessionRules.Active,"absent world is harmless");
        for(int mode=-1;mode<6;mode++) {
            Check(DriftSessionRules.AllowsMode(true,mode)==(mode==2),"drift world accepts only drift mode: "+mode);
            Check(DriftSessionRules.AllowsMode(false,mode),"ordinary world keeps control policy: "+mode);
        }
        Check(DriftSessionRules.AllowsVehicle(true,false)&&!DriftSessionRules.AllowsVehicle(true,true),"drift world only permits compatible 350Z");
        Check(DriftSessionRules.AllowsVehicle(false,true),"H2 remains available in ordinary worlds");
        var local=Player.m_localPlayer=new Player();var table=new PieceTable();
        var placement=typeof(DriftBuildPlacement).GetMethod("Prefix",BindingFlags.Static|BindingFlags.NonPublic);
        World("drift");object[] data={local,table};placement.Invoke(null,data);Check(data[1]==null,"drift-world local hammer cannot enter placement mode");
        data=new object[]{new Player(),table};placement.Invoke(null,data);Check(data[1]==table,"placement policy only owns the local player");
        World("ordinary");data=new object[]{local,table};placement.Invoke(null,data);Check(data[1]==table,"normal-world placement is unchanged");
    }
    static void Environment() {
        var world=ZDOMan.instance=new ZDOMan();
        var first=EnvMan.instance=new EnvMan{m_debugTimeOfDay=false,m_debugTime=.64f,m_debugEnv="Mist"};
        var environment=new DriftSessionEnvironment(first,world);environment.Tick();
        Check(first.m_debugTimeOfDay&&first.m_debugTime==0&&first.m_debugEnv=="Clear","native local presentation is clear midnight");
        first.m_debugTime=.5f;first.m_debugEnv="ThunderStorm";environment.Tick();
        Check(first.m_debugTime==0&&first.m_debugEnv=="Clear","day/weather presentation remains pinned");
        var second=EnvMan.instance=new EnvMan{m_debugTime=.75f,m_debugEnv="Rain"};ZDOMan.instance=new ZDOMan();
        Check(!environment.IsCurrent,"world and EnvMan changes invalidate old override");environment.Tick();environment.Dispose();environment.Dispose();
        Check(!first.m_debugTimeOfDay&&first.m_debugTime==.64f&&first.m_debugEnv=="Mist","dispose restores captured environment exactly and is idempotent");
        Check(second.m_debugTime==.75f&&second.m_debugEnv=="Rain"&&!second.m_debugTimeOfDay,"cleanup never writes into new world's environment");
        var next=new DriftSessionEnvironment(second,ZDOMan.instance);next.Tick();next.Dispose();
        Check(second.m_debugTime==.75f&&second.m_debugEnv=="Rain","reentry round trip preserves settings");
        second.m_debugTimeOfDay=true;var debug=new DriftSessionEnvironment(second,ZDOMan.instance);debug.Tick();debug.Dispose();
        Check(second.m_debugTimeOfDay&&second.m_debugTime==.75f,"existing debug-time override restored");
    }
    static void Ravens() {
        var chat=Chat.instance=new Chat();World("ordinary");
        var raven=new GameObject().Add(new Raven());
        Check(Prefix(typeof(DriftTutorialRaven)),"ordinary worlds retain tutorial raven spawning");
        Check(Prefix(typeof(DriftRavenUpdate),raven)&&raven.gameObject.Active&&!raven.Cancelled&&!raven.gameObject.Destroyed,"ordinary raven update and visibility stay native");
        World("drift");Check(!Prefix(typeof(DriftTutorialRaven)),"drift tutorial requests cannot instantiate or queue a story raven");
        Check(!Prefix(typeof(DriftRavenUpdate),raven)&&raven.Cancelled&&!raven.gameObject.Active&&raven.gameObject.Destroyed,"already present story raven disappears and stops pending effects");
        Check(chat.Cleared==1,"existing raven dialogue is removed");
        var fresh=new GameObject().Add(new Raven());
        typeof(DriftRavenAwake).GetMethod("Postfix",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{fresh});
        Check(fresh.gameObject.Destroyed&&!fresh.gameObject.Active,"directly spawned raven is removed after native initialization");
        World("drift",single:false);Check(Prefix(typeof(DriftTutorialRaven)),"network games keep native tutorials");
        World("drift",menu:true);Check(Prefix(typeof(DriftTutorialRaven)),"menu scenes keep native tutorials");
    }
    static void Cleanup() {
        Character.All.Clear();ItemDrop.All.Clear();World("drift");ZNetScene.instance=new ZNetScene();
        var player=Creature(player:true);var pet=Creature(tame:true);var crowd=Creature(spectator:true);var remote=Creature(owner:false);
        var hostile=Creature();var hostileRoot=hostile.gameObject;
        var structure=Item(placed:true);var remoteItem=Item(owner:false);var invalidItem=Item(valid:false);var loose=Item();
        var cleanup=new DriftSessionCleanup();cleanup.Tick();
        Check(hostileRoot.Destroyed,"existing random character removed without death/drop callbacks");
        Check(!player.gameObject.Destroyed&&!pet.gameObject.Destroyed&&!crowd.gameObject.Destroyed,"players, deliberate tames and tagged spectators preserved");
        Check(!remote.gameObject.Destroyed,"unowned character is untouched");
        Check(loose.gameObject.Destroyed,"loose loaded ground item removed");
        Check(!structure.gameObject.Destroyed&&!remoteItem.gameObject.Destroyed&&!invalidItem.gameObject.Destroyed,"placed decorations, unowned and invalid drops preserved");
        Character.All.Clear();ItemDrop.All.Clear();ZNetScene.instance=new ZNetScene();
        for(int i=0;i<100;i++){Creature();Item();}cleanup=new DriftSessionCleanup();cleanup.Tick();
        Check(ZNetScene.instance.Destroyed==32,"cleanup bounded to sixteen characters plus sixteen items per pass");
        for(int i=0;i<8;i++)cleanup.Tick();
        Check(Character.All.Count==0&&ItemDrop.All.Count==0,"mutable native lists drain across bounded passes without omissions");
        var normal=Creature();var normalItem=Item();World("ordinary");cleanup.Tick();
        Check(!normal.gameObject.Destroyed&&!normalItem.gameObject.Destroyed,"ordinary world characters and ground loot preserved");
        World("drift",single:false);cleanup.Tick();Check(!normal.gameObject.Destroyed&&!normalItem.gameObject.Destroyed,"multiplayer loot and NPCs preserved");
    }
    static void NativeContracts(string game,string plugin) {
        using(var native=AssemblyDefinition.ReadAssembly(Path.Combine(game,"valheim_Data/Managed/assembly_valheim.dll"))) {
            foreach(var target in new[]{new[]{"Tutorial","SpawnRaven"},new[]{"Raven","Awake"},new[]{"Raven","Update"}})
                Check(native.MainModule.Types.Single(t=>t.Name==target[0]).Methods.Count(m=>m.Name==target[1])==1,"installed native story-raven binding: "+string.Join(".",target));
            foreach(var target in new[]{new[]{"SpawnSystem","UpdateSpawning"},new[]{"CreatureSpawner","UpdateSpawner"},new[]{"SpawnArea","UpdateSpawn"}}) {
                var type=native.MainModule.Types.Single(t=>t.Name==target[0]);
                Check(type.Methods.Count(m=>m.Name==target[1]&&m.Parameters.Count==0&&m.ReturnType.FullName=="System.Void")==1,"installed native spawn patch contract: "+string.Join(".",target));
            }
            var raid=native.MainModule.Types.Single(t=>t.Name=="RandEventSystem").Methods.Single(m=>m.Name=="SetRandomEvent");
            Check(raid.Parameters[0].Name=="ev"&&raid.Parameters[0].ParameterType.Name=="RandomEvent","native raid prefix parameter matches installed game");
            Check(native.MainModule.Types.Single(t=>t.Name=="ItemDrop").Fields.Any(f=>f.Name=="s_instances"&&f.IsStatic&&f.FieldType.FullName.Contains("ItemDrop")),"native loaded-item registry exists");
            var env=native.MainModule.Types.Single(t=>t.Name=="EnvMan");
            foreach(var name in new[]{"m_debugTimeOfDay","m_debugTime","m_debugEnv"})Check(env.Fields.Any(f=>f.Name==name&&f.IsPublic),"native environment field available: "+name);
            foreach(var target in new[]{new[]{"Hud","TogglePieceSelection"},new[]{"BuildUi","OpenBuildMenu"}})
                Check(native.MainModule.Types.Single(t=>t.Name==target[0]).Methods.Any(m=>m.Name==target[1]&&m.Parameters.Count==0),"native build-menu binding exists: "+string.Join(".",target));
            Check(native.MainModule.Types.Single(t=>t.Name=="Player").Methods.Any(m=>m.Name=="SetPlaceMode"&&m.Parameters.Single().Name=="buildPieces"&&m.Parameters.Single().ParameterType.Name=="PieceTable"),"native placement prefix binds by actual parameter name and type");
        }
        using(var built=AssemblyDefinition.ReadAssembly(plugin)) {
            var type=built.MainModule.Types.Single(t=>t.Name=="Plugin");
            foreach(var method in new[]{"SelectControlMode","SelectFlightMode","ToggleTransformer","StartHorde","DrawHordeEntryHint","Enter"}) {
                var instructions=type.Methods.Single(m=>m.Name==method).Body.Instructions;
                Check(instructions.Any(i=>i.Operand is MethodReference called&&called.DeclaringType.Name=="DriftSessionRules"&&called.Name=="get_Active"),"built control entry point checks drift-world restrictions: "+method);
            }
            foreach(var method in new[]{"get_EffectiveDrift","get_EffectiveTerrain","Spawn","BuildVehicle"}) {
                // BuildVehicle is an iterator: effective overrides live in its generated state machine.
                var target=type.Methods.Single(m=>m.Name==method);
                if(method=="BuildVehicle")continue;
                Check(target.Body.Instructions.Any(i=>i.Operand is MethodReference called&&called.DeclaringType.Name=="DriftSessionRules"),"built per-world mode policy: "+method);
            }
        }
    }
    static void Main(string[] args) {
        RulesAndSpawns();Environment();Cleanup();Ravens();
        if(args.Length>=2)NativeContracts(args[0],args[1]);
        Console.WriteLine(checks+" drift session checks passed.");
    }
}
