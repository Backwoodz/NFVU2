using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    ZDOMan vehicleWorld;
    readonly List<ZDOID> savedVehicleIds=new List<ZDOID>();
    readonly HashSet<ZDOID> deferredVehicles=new HashSet<ZDOID>();
    ZDO restoringVehicle;
    Game vehicleLoadGame;
    // Called only after native terrain/ZDO readiness, before FindSpawnPoint
    // consumes the logout position and creates the player. Keep its loading
    // screen up until nearby saved vehicles have completed their staged build.
    internal bool PrepareVehicleWorldEntry() {
        if(Player.m_localPlayer!=null||Game.instance==null||!Game.instance.WaitingForRespawn()
            ||Game.instance.IsShuttingDown()||Multiplayer())return true;
        vehicleLoadGame=Game.instance;
        TickVehicleRestore(vehicleLoadGame);
        return !SpawnBusy;
    }
    void AdoptVehicleWorldEntry(MonoBehaviour local) {
        if(vehicleLoadGame==null)return;
        if(fleetOwner==vehicleLoadGame)fleetOwner=local;
        vehicleLoadGame=null;
    }
    IEnumerable<ZDO> SavedVehicles=>vehicleWorld==ZDOMan.instance&&vehicleWorld!=null
        ?savedVehicleIds.Select(vehicleWorld.GetZDO).Where(VehiclePersistence.IsVehicle):Enumerable.Empty<ZDO>();
    int WorldVehicleCount=>vehicleWorld==ZDOMan.instance?SavedVehicles.Count():fleet.Count;
    void TickVehicleRestore(MonoBehaviour local) {
        if(Multiplayer()||ZDOMan.instance==null||ZNetScene.instance==null)return;
        if(vehicleWorld!=ZDOMan.instance) {
            vehicleWorld=ZDOMan.instance;savedVehicleIds.Clear();deferredVehicles.Clear();
            foreach(var id in ZDOExtraData.GetAllZDOIDsWithHash(ZDOExtraData.Type.Int,VehiclePersistence.Model.GetStableHashCode())) {
                var data=vehicleWorld.GetZDO(id);
                if(VehiclePersistence.IsVehicle(data))savedVehicleIds.Add(id);
            }
        }
        if(SpawnBusy||driving||local is Player live&&(live.IsDead()||live.IsTeleporting()))return;
        // Restore when Valheim loads the record's zone. Distant cars still count
        // toward the garage limit without loading distant terrain or assets.
        var saved=SavedVehicles.FirstOrDefault(d=>!deferredVehicles.Contains(d.m_uid)
            &&DriftSessionRules.AllowsVehicle(DriftSessionRules.Active,VehiclePersistence.Hummer(d))
            &&!fleet.Any(v=>v.storage?.Data==d)&&ZNetScene.instance.FindInstance(d.m_uid)!=null);
        if(saved!=null)StartVehicleLoad(local,VehiclePersistence.Hummer(saved),saved);
    }
    internal bool ProtectVehicleCargo(Component component)=>VehiclePersistence.IsVehicle(component.GetComponent<ZNetView>()?.GetZDO())&&!Multiplayer();
    internal void PrepareVehicleCargo(Container container) {
        if(ProtectVehicleCargo(container))VehicleStorage.Hide(container.gameObject);
    }
    internal void SaveVehiclesBeforeShutdown()=>CleanupAll();
    internal void CaptureVehicleSave() {
        foreach(var vehicle in fleet) {
            vehicle.storage?.Tick();
            vehicle.storage?.SaveVehicle(vehicle.flight?.Active==true,vehicle.hasBeenDriven,vehicle.transformer?.Active==true);
        }
    }
}
[HarmonyPatch(typeof(ZNetScene),"IsAreaReady")]
internal static class SavedVehicleWorldEntry {
    internal static void Postfix(ref bool __result) {
        if(__result&&Plugin.ControlsOwner!=null)__result=Plugin.ControlsOwner.PrepareVehicleWorldEntry();
    }
}
[HarmonyPatch(typeof(ZDOMan),"PrepareSave")]
internal static class SavedVehicleWorldSave {
    static void Prefix(){Plugin.ControlsOwner?.CaptureVehicleSave();}
}
[HarmonyPatch(typeof(Container),"Awake")]
internal static class SavedVehicleCargoLoad {
    static void Postfix(Container __instance){Plugin.ControlsOwner?.PrepareVehicleCargo(__instance);}
}
// Native wear/removal paths must never destroy the hidden world record.
[HarmonyPatch]
internal static class SavedVehicleCargoProtection {
    static IEnumerable<System.Reflection.MethodBase> TargetMethods() {
        foreach(var method in new[]{"UpdateWear","RPC_Damage","RPC_Remove"})yield return AccessTools.Method(typeof(WearNTear),method);
    }
    static bool Prefix(WearNTear __instance)=>Plugin.ControlsOwner?.ProtectVehicleCargo(__instance)!=true;
}
// Shutdown resets all native views before saving. Capture the vehicles while
// both the scene and driver still exist, including quitting during flight.
[HarmonyPatch(typeof(Game),"Shutdown")]
internal static class SavedVehicleShutdown {
    static void Prefix(){Plugin.ControlsOwner?.SaveVehiclesBeforeShutdown();}
}
}
