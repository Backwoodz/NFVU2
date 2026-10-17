using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace Rachel350Z {
internal struct VehicleMapEntry {
    public ZDOID Id;
    public Vector3 Position;
    public bool Hummer;
    public VehicleMapEntry(ZDOID id,Vector3 position,bool hummer){Id=id;Position=position;Hummer=hummer;}
}
// Use native small/large-map rendering without reserving an enum value or
// changing Valheim's icon/filter tables. Unsaved pins cannot be edited, removed
// or checked by the map's ordinary pin tools and never enter map/world saves.
internal sealed class VehicleMapPins {
    static readonly FieldInfo updateRequired=typeof(Minimap).GetField("m_pinUpdateRequired",BindingFlags.Instance|BindingFlags.NonPublic);
    readonly Dictionary<ZDOID,Minimap.PinData> pins=new Dictionary<ZDOID,Minimap.PinData>();
    readonly HashSet<ZDOID> seen=new HashSet<ZDOID>();
    readonly List<ZDOID> removed=new List<ZDOID>();
    Minimap map;
    object world;
    public void Tick(Minimap nextMap,object nextWorld,IEnumerable<VehicleMapEntry> vehicles,Sprite icon) {
        if(map!=nextMap||!ReferenceEquals(world,nextWorld)) {
            Clear();map=nextMap;world=nextWorld;
        }
        if(map==null||world==null||icon==null){Clear();return;}
        seen.Clear();
        bool moved=false;
        foreach(var vehicle in vehicles) {
            if(!Finite(vehicle.Position))continue;
            seen.Add(vehicle.Id);
            if(!pins.TryGetValue(vehicle.Id,out var pin)) {
                pin=map.AddPin(vehicle.Position,Minimap.PinType.None,
                    vehicle.Hummer?"Hummer H2":"Nissan 350Z",false,false);
                pin.m_icon=icon;
                pins.Add(vehicle.Id,pin);
            }
            if(pin.m_pos!=vehicle.Position){pin.m_pos=vehicle.Position;moved=true;}
        }
        removed.Clear();
        foreach(var pair in pins)if(!seen.Contains(pair.Key))removed.Add(pair.Key);
        foreach(var id in removed){map.RemovePin(pins[id]);pins.Remove(id);}
        if(moved)updateRequired?.SetValue(map,true);
    }
    static bool Finite(Vector3 position)=>!float.IsNaN(position.x)&&!float.IsInfinity(position.x)
        &&!float.IsNaN(position.y)&&!float.IsInfinity(position.y)
        &&!float.IsNaN(position.z)&&!float.IsInfinity(position.z);
    public void Clear() {
        if(map!=null)foreach(var pin in pins.Values)map.RemovePin(pin);
        pins.Clear();seen.Clear();removed.Clear();map=null;world=null;
    }
}
}
