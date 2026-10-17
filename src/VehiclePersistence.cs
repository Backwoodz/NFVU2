using UnityEngine;
namespace Rachel350Z {
// Vehicle metadata and cargo share one vanilla world ZDO, so world backups,
// save rollback and deletion cannot split the car from its inventory.
internal static class VehiclePersistence {
    public const string Model="nfvu2_vehicle_v1";
    const string Position="nfvu2_position",Rotation="nfvu2_rotation",Layout="nfvu2_layout",Flight="nfvu2_flight",Driven="nfvu2_driven",Robot="nfvu2_transformer";
    public static bool IsVehicle(ZDO data)=>data!=null&&(data.GetInt(Model)==1||data.GetInt(Model)==2);
    public static bool Hummer(ZDO data)=>data.GetInt(Model)==2;
    public static int Trunk(ZDO data)=>VehicleStorageRules.Layout(data.GetInt(Layout));
    public static Vector3 Location(ZDO data)=>data.GetVec3(Position,data.GetPosition());
    public static Quaternion Orientation(ZDO data)=>data.GetQuaternion(Rotation,data.GetRotation());
    public static bool Flying(ZDO data)=>data.GetBool(Flight);
    public static bool WasDriven(ZDO data)=>data.GetBool(Driven);
    public static bool Transformed(ZDO data)=>!Hummer(data)&&data.GetBool(Robot);
    public static void Mark(ZDO data,bool hummer){data.Persistent=true;data.Set(Model,hummer?2:1);}
    public static void Save(ZDO data,Transform car,int layout,bool flying,bool driven,bool transformed=false) {
        Save(data,car.position,car.rotation,layout,flying,driven,transformed);
    }
    public static void Save(ZDO data,Vector3 position,Quaternion rotation,int layout,bool flying,bool driven,bool transformed=false) {
        transformed=transformed&&!Hummer(data);
        data.Set(Position,position);data.Set(Rotation,rotation);
        data.Set(Layout,layout);data.Set(Flight,flying&&!transformed);data.Set(Driven,driven);data.Set(Robot,transformed);
    }
    public static void Forget(ZDO data){data.Set(Model,0);}
}
}
