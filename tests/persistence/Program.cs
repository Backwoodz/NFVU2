using System;
using System.Collections.Generic;
using Rachel350Z;
using UnityEngine;
// Exercise the actual world/restore coordinator with the native ZDO/scene doubles.
// Asset construction is replaced with a completed load of the same storage record.
namespace Rachel350Z {
internal static class DriftSessionRules {
    public static bool Active;
    public static bool AllowsVehicle(bool drift,bool hummer)=>!drift||!hummer;
}
internal sealed class VehicleRuntime {
    public VehicleStorage storage;public TestFlight flight=new TestFlight();public TestTransformer transformer=new TestTransformer();public bool hasBeenDriven;
}
internal sealed class TestFlight {public bool Active;}
internal sealed class TestTransformer {public bool Active;}
public partial class Plugin {
    internal static Plugin ControlsOwner=null;
    readonly List<VehicleRuntime> fleet=new List<VehicleRuntime>();
    public bool MultiplayerMode,SpawnBusy,driving;public int starts;public ZDO requested;
    MonoBehaviour fleetOwner,loadOwner;
    public MonoBehaviour Owner=>fleetOwner;
    bool Multiplayer()=>MultiplayerMode;
    void StartVehicleLoad(MonoBehaviour local,bool hummer,ZDO saved){starts++;requested=saved;restoringVehicle=saved;SpawnBusy=true;loadOwner=local;}
    void CleanupAll(){CaptureVehicleSave();foreach(var v in fleet)v.storage.Release();fleet.Clear();SpawnBusy=false;}
    public void Tick()=>TickVehicleRestore(Player.m_localPlayer);
    public int Count=>WorldVehicleCount;
    public bool Entry(bool nativeReady=true){ControlsOwner=this;SavedVehicleWorldEntry.Postfix(ref nativeReady);return nativeReady;}
    public void Adopt()=>AdoptVehicleWorldEntry(Player.m_localPlayer);
    public void Complete() {
        var car=new GameObject();car.transform.SetPositionAndRotation(VehiclePersistence.Location(requested),VehiclePersistence.Orientation(requested));
        fleet.Add(new VehicleRuntime{storage=new VehicleStorage(car,requested),hasBeenDriven=VehiclePersistence.WasDriven(requested),flight=new TestFlight{Active=VehiclePersistence.Flying(requested)},transformer=new TestTransformer{Active=VehiclePersistence.Transformed(requested)}});fleetOwner=loadOwner;SpawnBusy=false;restoringVehicle=null;
    }
    public void Cancel(){deferredVehicles.Add(requested.m_uid);SpawnBusy=false;restoringVehicle=null;}
}
}
class Program {
    static int checks;
    static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;}
    static void World(){ZDOMan.instance=new ZDOMan();ZNetScene.instance=new ZNetScene();Player.m_localPlayer=new Player();}
    static ZDO Car(bool hummer=false,bool loaded=true) {
        var storage=new VehicleStorage(new GameObject(),hummer,0);VehiclePersistence.Mark(storage.Data,hummer);storage.SaveVehicle(false,false);
        storage.Release();if(!loaded)ZNetScene.instance.objects.Remove(storage.Data.m_uid);return storage.Data;
    }
    static void Main() {
        World();var a=Car();var b=Car(true,false);Car(false,false);Car(true,false);
        new ZDO();var unsupported=new ZDO();unsupported.Set(VehiclePersistence.Model,99);
        var plugin=new Plugin();plugin.Tick();
        Check(plugin.Count==4,"all four world records count, including distant cars; ordinary/unknown records excluded");
        Check(plugin.starts==1&&plugin.requested==a,"loaded car is automatically restored");
        plugin.Tick();Check(plugin.starts==1,"restore cannot race an active load");
        plugin.Complete();plugin.Tick();Check(plugin.starts==1,"restored car is never duplicated");
        var bRoot=ZNetScene.instance.GetPrefab("piece_chest_blackmetal").Clone(b.GetPosition(),b.GetRotation());
        bRoot.GetComponent<ZNetView>().data=b;ZNetScene.instance.objects[b.m_uid]=bRoot;
        plugin.PrepareVehicleCargo(bRoot.GetComponent<Container>());
        Check(!bRoot.GetComponent<Renderer>().enabled&&!bRoot.GetComponent<Collider>().enabled,"saved cargo is hidden before staged restoration");
        Check(plugin.ProtectVehicleCargo(bRoot.GetComponent<WearNTear>()),"saved marker blocks native destruction");
        plugin.driving=true;plugin.Tick();Check(plugin.starts==1,"restoration waits while driving");
        plugin.driving=false;Player.m_localPlayer.teleport=true;plugin.Tick();Check(plugin.starts==1,"restoration waits during teleport");
        Player.m_localPlayer.teleport=false;Player.m_localPlayer.dead=true;plugin.Tick();Check(plugin.starts==1,"restoration waits for living player");
        Player.m_localPlayer.dead=false;plugin.Tick();Check(plugin.starts==2&&plugin.requested==b,"distant car restores when its native record loads");
        plugin.Cancel();plugin.Tick();Check(plugin.starts==2&&plugin.Count==4,"cancelled or failed restoration preserves record without retry loop");
        plugin.MultiplayerMode=true;Check(!plugin.ProtectVehicleCargo(bRoot.GetComponent<WearNTear>()),"single-player marker hooks do not interfere in multiplayer");
        plugin.MultiplayerMode=false;VehiclePersistence.Forget(b);plugin.Tick();Check(plugin.Count==3,"explicit deletion frees world capacity");
        Check(!plugin.ProtectVehicleCargo(bRoot.GetComponent<WearNTear>()),"released cargo regains native damage/removal paths");
        plugin.SaveVehiclesBeforeShutdown();Check(VehiclePersistence.IsVehicle(a),"shutdown resource cleanup preserves car metadata");
        World();var other=Car(true);plugin.Tick();
        Check(plugin.Count==1&&plugin.requested==other&&plugin.starts==3,"world switch resets registry and cancellation state");
        plugin.Complete();plugin.SaveVehiclesBeforeShutdown();
        World();ZDOMan.instance.records[a.m_uid]=a;var root=ZNetScene.instance.GetPrefab("piece_chest").Clone(a.GetPosition(),a.GetRotation());
        root.GetComponent<ZNetView>().data=a;ZNetScene.instance.objects[a.m_uid]=root;plugin.Tick();
        Check(plugin.Count==1&&plugin.requested==a&&plugin.starts==4,"returning to first world restores its original car");
        World();Game.instance=new Game();Player.m_localPlayer=null;
        var first=Car();var second=Car(true);Car(false,false);plugin=new Plugin();
        Check(!plugin.Entry(false)&&plugin.starts==0,"native terrain readiness is never bypassed or used to scan an unfinished world");
        plugin.MultiplayerMode=true;
        Check(plugin.Entry()&&plugin.starts==0,"multiplayer world entry is unchanged");plugin.MultiplayerMode=false;
        Check(!plugin.Entry()&&plugin.requested==first&&plugin.starts==1,"nearby vehicle starts before the player exists and keeps native world entry waiting");
        Check(!plugin.Entry()&&plugin.starts==1,"repeated readiness checks cannot duplicate a staged load");
        plugin.Complete();
        Check(!plugin.Entry()&&plugin.requested==second&&plugin.starts==2,"world entry waits for every nearby saved car");
        plugin.Cancel();
        Check(plugin.Entry()&&plugin.starts==2&&plugin.Count==3,"failed or cancelled cars release world entry; distant cars do not block it");
        Player.m_localPlayer=new Player();plugin.Adopt();
        Check(plugin.Owner==Player.m_localPlayer,"completed preload transfers fleet ownership to the newly spawned player");
        Check(plugin.Entry()&&plugin.starts==2,"live-player area and teleport checks never extend world entry");
        World();Player.m_localPlayer=null;plugin=new Plugin();
        Check(plugin.Entry()&&plugin.starts==0,"a world without saved cars enters immediately");
        Car();plugin=new Plugin();Game.instance.shuttingDown=true;
        Check(plugin.Entry()&&plugin.starts==0,"shutdown cannot start a world-entry vehicle load");
        Game.instance.shuttingDown=false;Game.instance.waiting=false;
        Check(plugin.Entry()&&plugin.starts==0,"unrelated area-readiness calls outside respawn cannot trigger loading");
        World();var robot=Car();VehiclePersistence.Save(robot,new Vector3(5,.35f,8),Quaternion.identity,0,false,true,true);
        plugin=new Plugin();plugin.Tick();plugin.Complete();plugin.CaptureVehicleSave();
        Check(VehiclePersistence.Transformed(robot)&&!VehiclePersistence.Flying(robot),"ordinary world save captures active parked transformer form");
        plugin.SaveVehiclesBeforeShutdown();
        Check(VehiclePersistence.Transformed(robot)&&VehiclePersistence.IsVehicle(robot),"shutdown preserves transformer form and original world record before disposal");
        plugin=new Plugin();plugin.Tick();
        Check(plugin.requested==robot&&VehiclePersistence.Transformed(plugin.requested),"world re-entry reuses the saved transformer record");
        World();DriftSessionRules.Active=true;var storedHummer=Car(true);var storedZ=Car();
        plugin=new Plugin();plugin.Tick();
        Check(plugin.Count==2&&plugin.requested==storedZ,"drift world restores 350Z while saved H2 still counts toward capacity");
        Check(VehiclePersistence.IsVehicle(storedHummer),"restricted H2 record remains intact");
        plugin.Complete();plugin.Tick();Check(plugin.starts==1,"drift restriction does not schedule H2 load");
        DriftSessionRules.Active=false;plugin.Tick();Check(plugin.requested==storedHummer,"other worlds allow H2 restoration normally");
        Console.WriteLine(checks+" persistence coordination checks passed; native save/load and Harmony runtime require a game test.");
    }
}
