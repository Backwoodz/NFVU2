using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    readonly VehicleMapPins vehicleMapPins=new VehicleMapPins();
    readonly Dictionary<ZDOID,VehicleMapEntry> vehicleMapEntries=new Dictionary<ZDOID,VehicleMapEntry>();
    VehicleMapIcon vehicleMapIcon;
    void TickVehicleMap() {
        if(Player.m_localPlayer==null||Minimap.instance==null||ZDOMan.instance==null||Multiplayer()) {
            CleanupVehicleMap();return;
        }
        vehicleMapEntries.Clear();
        foreach(var data in SavedVehicles)
            vehicleMapEntries[data.m_uid]=new VehicleMapEntry(data.m_uid,VehiclePersistence.Location(data),VehiclePersistence.Hummer(data));
        // Live roots override their stored location, including driving, flying
        // and the robot form. Distant/unloaded cars still use their world ZDO.
        foreach(var vehicle in fleet) {
            var data=vehicle.storage?.Data;
            if(vehicle.car==null||!VehiclePersistence.IsVehicle(data))continue;
            vehicleMapEntries[data.m_uid]=new VehicleMapEntry(data.m_uid,vehicle.car.transform.position,vehicle.Hummer);
        }
        if(vehicleMapEntries.Count>0&&vehicleMapIcon==null)
            vehicleMapIcon=VehicleMapIcon.Load(Path.GetDirectoryName(Info.Location));
        vehicleMapPins.Tick(Minimap.instance,ZDOMan.instance,vehicleMapEntries.Values,vehicleMapIcon?.Sprite);
    }
    void CleanupVehicleMap(){vehicleMapPins.Clear();vehicleMapEntries.Clear();}
    void DisposeVehicleMap(){CleanupVehicleMap();vehicleMapIcon?.Dispose();vehicleMapIcon=null;}
}
}
