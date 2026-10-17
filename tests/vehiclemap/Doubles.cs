using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine {
    public struct Vector3 {
        public float x,y,z;
        public Vector3(float a,float b,float c){x=a;y=b;z=c;}
        public static bool operator ==(Vector3 a,Vector3 b)=>a.x==b.x&&a.y==b.y&&a.z==b.z;
        public static bool operator !=(Vector3 a,Vector3 b)=>!(a==b);
        public override bool Equals(object obj)=>obj is Vector3 v&&this==v;
        public override int GetHashCode()=>HashCode.Combine(x,y,z);
    }
    public sealed class Sprite {}
    public sealed class Transform {public Vector3 position;}
    public sealed class GameObject {public readonly Transform transform=new Transform();}
}
public struct ZDOID {
    public int Value;
    public ZDOID(int value){Value=value;}
}
public sealed class ZDO {
    public ZDOID m_uid;
    public UnityEngine.Vector3 Position;
    public bool Hummer,Removed;
    public ZDO(int id,float x,bool hummer=false){m_uid=new ZDOID(id);Position=new UnityEngine.Vector3(x,0,0);Hummer=hummer;}
}
public sealed class ZDOMan {public static ZDOMan instance;}
public sealed class Player {public static Player m_localPlayer;}
public sealed class Minimap {
    public static Minimap instance;
    public enum PinType {Icon0,Icon1,Icon2,Icon3,Death,Bed,Icon4,Shout,None}
    public sealed class PinData {
        public UnityEngine.Vector3 m_pos;
        public PinType m_type;
        public string m_name;
        public bool m_save,m_checked;
        public UnityEngine.Sprite m_icon;
    }
    bool m_pinUpdateRequired;
    public bool Dirty=>m_pinUpdateRequired;
    public void Render(){m_pinUpdateRequired=false;}
    public readonly List<PinData> Pins=new List<PinData>();
    public readonly Dictionary<PinType,UnityEngine.Sprite> Icons=new Dictionary<PinType,UnityEngine.Sprite>();
    public PinData AddPin(UnityEngine.Vector3 p,PinType type,string name,bool save,bool isChecked){
        var pin=new PinData{m_pos=p,m_type=type,m_name=name,m_save=save,m_checked=isChecked};
        Pins.Add(pin);m_pinUpdateRequired=true;return pin;
    }
    public void RemovePin(PinData pin){Pins.Remove(pin);m_pinUpdateRequired=true;}
    public int SavedCount=>Pins.Count(p=>p.m_save);
}
namespace Rachel350Z {
    internal sealed class VehicleStorage {public ZDO Data;}
    internal sealed class VehicleRuntime {
        public VehicleStorage storage;
        public UnityEngine.GameObject car;
        public bool Hummer;
    }
    internal static class VehiclePersistence {
        public static bool IsVehicle(ZDO data)=>data!=null&&!data.Removed;
        public static UnityEngine.Vector3 Location(ZDO data)=>data.Position;
        public static bool Hummer(ZDO data)=>data.Hummer;
    }
    internal sealed class VehicleMapIcon : IDisposable {
        public static int Loads,Disposals;
        public UnityEngine.Sprite Sprite=new UnityEngine.Sprite();
        public static VehicleMapIcon Load(string directory){Loads++;return new VehicleMapIcon();}
        public void Dispose(){Disposals++;}
    }
    public partial class Plugin {
        internal sealed class PluginInfo {public string Location="ignored/Rachel350Z.dll";}
        readonly PluginInfo Info=new PluginInfo();
        readonly List<VehicleRuntime> fleet=new List<VehicleRuntime>();
        public readonly List<ZDO> Saved=new List<ZDO>();
        IEnumerable<ZDO> SavedVehicles=>Saved.Where(VehiclePersistence.IsVehicle);
        public bool MultiplayerMode;
        bool Multiplayer()=>MultiplayerMode;
        public void Tick()=>TickVehicleMap();
        public void Clear()=>CleanupVehicleMap();
        public void Dispose()=>DisposeVehicleMap();
        public void Live(ZDO data,float x){
            var vehicle=new VehicleRuntime{storage=new VehicleStorage{Data=data},car=new UnityEngine.GameObject(),Hummer=data.Hummer};
            vehicle.car.transform.position=new UnityEngine.Vector3(x,0,0);fleet.Add(vehicle);
        }
        public void MoveLive(float x)=>fleet[0].car.transform.position=new UnityEngine.Vector3(x,5,0);
    }
}
