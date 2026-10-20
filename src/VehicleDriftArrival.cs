using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Rachel350Z {
public partial class Plugin {
    readonly DriftArrivalState driftArrival=new DriftArrivalState();
    ZDO driftArrivalVehicle;
    static Vector3 DriftParkingPosition=>new Vector3(DriftParkingPlan.X,DriftWorldRules.GroundHeight+.35f,DriftParkingPlan.Z);
    static Quaternion DriftParkingRotation=>Quaternion.Euler(0,DriftParkingPlan.Yaw,0);
    void InitializeDriftArrival() {
        controlsHarmony.PatchAll(typeof(DriftParkingWorldEntry));
        controlsHarmony.PatchAll(typeof(DriftParkingIntro));
        controlsHarmony.PatchAll(typeof(DriftEventNativeMenu));
    }
    internal bool PrepareDriftParkingSpawn(bool initialSpawn,ref Vector3 point,ref bool usedLogoutPoint,ref bool result) {
        if(!initialSpawn||!DriftSessionRules.Active||Game.instance==null||Game.instance.IsShuttingDown()
            ||ZDOMan.instance==null||ZNetScene.instance==null||ZoneSystem.instance==null||ZNet.instance==null)return false;
        driftArrival.Begin(ZDOMan.instance,true,true);
        if(!driftArrival.Prepared) {
            driftArrival.Prepared=true;
            var records=ZDOExtraData.GetAllZDOIDsWithHash(ZDOExtraData.Type.Int,VehiclePersistence.Model.GetStableHashCode())
                .Select(ZDOMan.instance.GetZDO).Where(VehiclePersistence.IsVehicle).ToArray();
            var choice=DriftParkingPlan.Choose(records.Select(data=>new DriftArrivalCandidate(VehiclePersistence.Hummer(data),
                VehiclePersistence.WasDriven(data),(VehiclePersistence.Location(data)-DriftParkingPosition).sqrMagnitude)).ToArray());
            driftArrivalVehicle=choice<0?null:records[choice];
            if(driftArrivalVehicle!=null) {
                // Move the same native cargo record into the streamed parking
                // zone. Inventory/customization are retained; no cloned ZDO.
                driftArrivalVehicle.SetOwner(ZDOMan.GetSessionID());
                VehiclePersistence.Save(driftArrivalVehicle,DriftParkingPosition,DriftParkingRotation,
                    VehiclePersistence.Trunk(driftArrivalVehicle),false,VehiclePersistence.WasDriven(driftArrivalVehicle));
                driftArrivalVehicle.SetPosition(DriftParkingPosition);driftArrivalVehicle.SetRotation(DriftParkingRotation);
            }
        }
        point=DriftParkingPosition+new Vector3(0,0,3);usedLogoutPoint=true;result=false;
        ZNet.instance.SetReferencePosition(point);
        // The existing vehicle-entry hook on IsAreaReady keeps native loading
        // active while an already saved car is reconstructed in stages.
        if(!ZNetScene.instance.IsAreaReady(point)||!ZoneSystem.instance.GetGroundHeight(point,out var height))return true;
        point.y=height+.25f;result=true;
        // A first-ever drift-world visit also builds its initial 350Z behind
        // Valheim's loading screen. The existing staged worker accepts Game as
        // its temporary owner and AdoptVehicleWorldEntry hands it to Player.
        if(driftArrivalVehicle==null&&!driftArrival.LoadRequested&&!SpawnBusy
            &&!fleet.Any(v=>!v.Hummer&&v.car!=null)&&WorldVehicleCount<VehicleFleet<VehicleRuntime>.Capacity) {
            driftArrival.LoadRequested=true;vehicleLoadGame=Game.instance;
            StartVehicleLoad(vehicleLoadGame,false,null);result=false;
        }
        return true;
    }
    void TickDriftArrival(MonoBehaviour local) {
        var world=DriftSessionRules.Active&&!Multiplayer()&&Game.instance!=null&&!Game.instance.IsShuttingDown()?ZDOMan.instance:null;
        driftArrival.Observe(world);
        if(world==null){driftArrivalVehicle=null;return;}
        if(!driftArrival.Pending||local==null||SpawnBusy||Time.timeScale<=0)return;
        if(local is Player native&&(native.IsDead()||native.IsTeleporting()||native.IsAttached()))return;
        if(driving){driftArrival.Complete();return;}
        var selected=driftArrivalVehicle!=null?fleet.FirstOrDefault(v=>v.storage?.Data==driftArrivalVehicle):null;
        if(selected==null&&driftArrivalVehicle==null)selected=fleet.Where(v=>!v.Hummer&&v.car!=null)
            .OrderBy(v=>(v.car.transform.position-DriftParkingPosition).sqrMagnitude).FirstOrDefault();
        if(selected==null) {
            if(driftArrival.LoadRequested){driftArrival.Complete();return;} // Failure/cancellation stays cancelled.
            if(driftArrivalVehicle==null&&WorldVehicleCount>=VehicleFleet<VehicleRuntime>.Capacity) {
                status="Parkplatz: Garage voll. Bitte ein Fahrzeug entfernen, um einen 350Z bereitzustellen.";
                driftArrival.Complete();return;
            }
            driftArrival.LoadRequested=true;
            StartVehicleLoad(local,false,driftArrivalVehicle);return;
        }
        current=selected;
        if(!MoveDriftCarTo(DriftParkingPosition,DriftParkingRotation))return;
        Enter(local);
        if(!driving)return;
        // Uses the real cabin's supported seated phase; all player/camera/
        // collider ownership still goes through the normal Enter path.
        cabinDriver?.Motion.SitImmediately();
        if(cabinDriver!=null&&showroom!=null)cabinDriver.Tick(0,false,showroom.State);
        VehicleWorldStreaming.Follow(local as Player);
        driftArrival.Complete();
    }
    bool DriftArrivalSpawnPoint(out Vector3 point,out Quaternion rotation) {
        point=DriftParkingPosition;rotation=DriftParkingRotation;
        return driftArrival.Pending&&DriftSessionRules.Active&&!Multiplayer()&&!IsHummer&&restoringVehicle==null;
    }
    // Shared by arrival and race transfer. Readiness is checked before touching
    // the chassis; disposing the previous streaming hold prevents stale momentum.
    bool MoveDriftCarTo(Vector3 position,Quaternion rotation,bool requireGround=true) {
        if(car==null||body==null||IsHummer||!DriftSessionRules.Active||Multiplayer()||ZoneSystem.instance==null)return false;
        if(!requireGround&&!body.isKinematic)return false; // Race transfer must hold physics before moving into an unloaded zone.
        if(requireGround)for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2) {
            var map=Heightmap.FindHeightmap(ZoneSystem.GetZonePos(ZoneSystem.GetZone(position+new Vector3(x*4,0,z*4))));
            var collider=map!=null?map.GetComponent<MeshCollider>():null;
            if(collider==null||!collider.enabled||!collider.gameObject.activeInHierarchy||collider.sharedMesh==null)return false;
        }
        bool keepHeld=!requireGround;
        current.worldStreaming?.Dispose();current.worldStreaming=null;
        current.parkedStreaming?.Dispose();current.parkedStreaming=null;
        if(keepHeld)body.isKinematic=true;
        ReleaseDriftPhysics();
        if(!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
        body.position=position;body.rotation=rotation;car.transform.SetPositionAndRotation(position,rotation);
        Physics.SyncTransforms();
        BlockDrivingInput();nitroHeld=false;nitroInput.Reset();vehicleNitro?.State.Stop();
        if(driving) {
            current.worldStreaming=new VehicleWorldStreaming(body);
            if(cabinDriver!=null&&showroom!=null)cabinDriver.Tick(0,false,showroom.State);
            else if(player!=null)player.transform.position=car.transform.TransformPoint(new Vector3(0,1,0));
            SyncDriftDriverTransfer();
            VehicleWorldStreaming.Follow(player as Player);orbitCamera?.SnapDriftTransfer();
        } else current.parkedStreaming=new VehicleWorldStreaming(body);
        current.storage?.Tick();current.storage?.SaveVehicle(false,current.hasBeenDriven);
        if(driving&&ZNet.instance!=null)ZNet.instance.SetReferencePosition(position);
        return true;
    }
    void SyncDriftDriverTransfer() {
        if(player==null)return;
        // ZSyncTransform normally publishes on its next physics tick. A remote
        // transfer changes the streaming reference immediately: publish the
        // driver's sector first, or ZNetScene may unload the still-old ZDO.
        var pose=player.transform;
        if(playerBody!=null){playerBody.position=pose.position;playerBody.rotation=pose.rotation;}
        Physics.SyncTransforms();
        var view=player.GetComponent<ZNetView>();var data=view!=null?view.GetZDO():null;
        if(data!=null&&view.IsOwner()){data.SetPosition(pose.position);data.SetRotation(pose.rotation);}
    }
}
[HarmonyPatch(typeof(Game),"FindSpawnPoint")]
internal static class DriftParkingWorldEntry {
    static bool Prefix(bool ___m_firstSpawn,ref float ___m_respawnWait,float ___m_respawnLoadDuration,float dt,
        ref Vector3 point,ref bool usedLogoutPoint,ref bool __result) {
        if(Plugin.ControlsOwner?.PrepareDriftParkingSpawn(___m_firstSpawn,ref point,ref usedLogoutPoint,ref __result)!=true)return true;
        ___m_respawnWait+=dt;
        if(___m_respawnWait<=___m_respawnLoadDuration)__result=false;
        return false;
    }
}
[HarmonyPatch(typeof(Game),"SpawnPlayer")]
internal static class DriftParkingIntro {
    static void Prefix(bool ___m_firstSpawn,ref bool spawnValkyrie) {
        if(___m_firstSpawn&&DriftSessionRules.Active)spawnValkyrie=false;
    }
}
}
