using UnityEngine;
namespace Rachel350Z {
// Vehicle metadata and cargo share one vanilla world ZDO, so world backups,
// save rollback and deletion cannot split the car from its inventory.
internal static class VehiclePersistence {
    public const string Model="nfvu2_vehicle_v1";
    const string Position="nfvu2_position",Rotation="nfvu2_rotation",Layout="nfvu2_layout",Flight="nfvu2_flight",Driven="nfvu2_driven";
    public static bool IsVehicle(ZDO data)=>data!=null&&(data.GetInt(Model)==1||data.GetInt(Model)==2);
    public static bool Hummer(ZDO data)=>data.GetInt(Model)==2;
    public static int Trunk(ZDO data)=>VehicleStorageRules.Layout(data.GetInt(Layout));
    public static Vector3 Location(ZDO data)=>data.GetVec3(Position,data.GetPosition());
    public static Quaternion Orientation(ZDO data)=>data.GetQuaternion(Rotation,data.GetRotation());
    public static bool Flying(ZDO data)=>data.GetBool(Flight);
    public static bool WasDriven(ZDO data)=>data.GetBool(Driven);
    public static void Mark(ZDO data,bool hummer){data.Persistent=true;data.Set(Model,hummer?2:1);}
    public static void Save(ZDO data,Transform car,int layout,bool flying,bool driven) {
        data.Set(Position,car.position);data.Set(Rotation,car.rotation);
        data.Set(Layout,layout);data.Set(Flight,flying);data.Set(Driven,driven);
    }
    public static void Forget(ZDO data){data.Set(Model,0);}
}
}
