using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine {
    public class Object {
        public static GameObject Instantiate(GameObject prefab,Vector3 position,Quaternion rotation)=>ZNetScene.instance.Spawn(prefab.Name,position,rotation);
        public static void Destroy(GameObject root)=>ZNetScene.instance.Destroy(root);
    }
    public class Component:Object {
        public GameObject gameObject;
        public Transform transform=>gameObject.transform;
        public T GetComponent<T>() where T:class=>gameObject.GetComponent<T>();
    }
    public class MonoBehaviour:Component {}
    public class GameObject:Object {
        public string Name;public readonly Transform transform=new Transform();
        readonly Dictionary<System.Type,object> parts=new Dictionary<System.Type,object>();
        public GameObject(string name){Name=name;}
        public T Add<T>() where T:Component,new(){var part=new T{gameObject=this};parts[typeof(T)]=part;return part;}
        public T GetComponent<T>() where T:class=>parts.TryGetValue(typeof(T),out var value)?value as T:null;
    }
    public class Transform {public Vector3 position;public Quaternion rotation;}
    public struct Vector3 {
        public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 zero=>new Vector3();
    }
    public struct Quaternion {
        public float Yaw;public Vector3 eulerAngles=>new Vector3(0,Yaw,0);
        public static Quaternion Euler(float x,float y,float z)=>new Quaternion{Yaw=y};
        public static float Angle(Quaternion a,Quaternion b){float d=(a.Yaw-b.Yaw)%360;return Math.Abs(d>180?d-360:d< -180?d+360:d);}
    }
    public struct Keyframe {public Keyframe(float x,float y){}}
    public class AnimationCurve {public AnimationCurve(params Keyframe[] keys){}}
    public static class Time {public static float timeScale=1,unscaledTime;}
}
public static class TextHash {
    public static int GetStableHashCode(this string text){unchecked{int value=17;foreach(char c in text)value=value*31+c;return value;}}
}
public struct ZDOID:IEquatable<ZDOID> {
    public int Id;public bool Equals(ZDOID other)=>Id==other.Id;
    public override bool Equals(object value)=>value is ZDOID other&&Equals(other);
    public override int GetHashCode()=>Id;
}
public class ZDO {
    static int next;
    public readonly ZDOID m_uid=new ZDOID{Id=++next};public bool Persistent;public uint DataRevision;
    public string Prefab;public UnityEngine.Vector3 Position;public UnityEngine.Quaternion Rotation;
    public readonly Dictionary<int,int> Ints=new Dictionary<int,int>();
    readonly Dictionary<int,float> floats=new Dictionary<int,float>();readonly Dictionary<int,string> strings=new Dictionary<int,string>();
    public ZDO(string prefab,UnityEngine.Vector3 pos){Prefab=prefab;Position=pos;ZDOMan.instance.Data[m_uid]=this;}
    public void Set(string key,int value){Ints[key.GetStableHashCode()]=value;DataRevision++;}
    public void Set(int key,float value){floats[key]=value;DataRevision++;}
    public void Set(int key,string value){strings[key]=value;DataRevision++;}
    public int GetInt(string key)=>Ints.TryGetValue(key.GetStableHashCode(),out int n)?n:0;
    public float GetFloat(int key)=>floats.TryGetValue(key,out float n)?n:0;
    public string GetString(int key)=>strings.TryGetValue(key,out string s)?s:"";
    public int GetPrefab()=>Prefab.GetStableHashCode();
    public UnityEngine.Vector3 GetPosition()=>Position;
    public UnityEngine.Quaternion GetRotation()=>Rotation;
    public void SetRotation(UnityEngine.Quaternion q){Rotation=q;DataRevision++;}
}
public class ZDOMan {
    public static ZDOMan instance;public readonly Dictionary<ZDOID,ZDO> Data=new Dictionary<ZDOID,ZDO>();
    public ZDO GetZDO(ZDOID id)=>Data.TryGetValue(id,out var z)?z:null;
}
public static class ZDOExtraData {
    public enum Type{Int}
    public static List<ZDOID> GetAllZDOIDsWithHash(Type type,int key)=>ZDOMan.instance.Data.Values.Where(z=>z.Ints.ContainsKey(key)).Select(z=>z.m_uid).ToList();
}
public static class ZDOVars {public static readonly int s_text="text".GetStableHashCode(),s_fuel="fuel".GetStableHashCode();}
public class ZNetView:UnityEngine.Component {public ZDO Data;public bool Owner=true;public ZDO GetZDO()=>Data;public bool IsOwner()=>Owner;}
public class Sign:UnityEngine.Component {public void SetText(string s)=>GetComponent<ZNetView>().Data.Set(ZDOVars.s_text,s);}
public class Fireplace:UnityEngine.Component {public float m_maxFuel=6;}
public class ZNetScene {
    public static ZNetScene instance;public bool Ready=true;public int Created;
    public readonly Dictionary<string,UnityEngine.GameObject> Prefabs=new Dictionary<string,UnityEngine.GameObject>();
    public readonly Dictionary<ZDOID,UnityEngine.GameObject> Instances=new Dictionary<ZDOID,UnityEngine.GameObject>();
    public UnityEngine.GameObject GetPrefab(string name)=>Prefabs.TryGetValue(name,out var p)?p:null;
    public bool IsAreaReady(UnityEngine.Vector3 p)=>Ready;
    public UnityEngine.GameObject FindInstance(ZDOID id)=>Instances.TryGetValue(id,out var g)?g:null;
    public UnityEngine.GameObject Spawn(string name,UnityEngine.Vector3 p,UnityEngine.Quaternion q) {
        var g=new UnityEngine.GameObject(name);g.transform.position=p;g.transform.rotation=q;
        var view=g.Add<ZNetView>();view.Data=new ZDO(name,p){Rotation=q};Instances[view.Data.m_uid]=g;Created++;
        if(name=="sign")g.Add<Sign>();if(name=="piece_groundtorch")g.Add<Fireplace>();return g;
    }
    public void Destroy(UnityEngine.GameObject g){var z=g.GetComponent<ZNetView>().Data;Instances.Remove(z.m_uid);ZDOMan.instance.Data.Remove(z.m_uid);}
}
public class Game {public static Game instance;public bool Shutdown;public bool IsShuttingDown()=>Shutdown;}
public class ZoneSystem {public static ZoneSystem instance;public bool Loaded=true;public bool IsZoneLoaded(UnityEngine.Vector3 p)=>Loaded;}
public class Minimap {
    public static Minimap instance;public enum PinType{Icon3}public class PinData{}
    public readonly List<PinData> Pins=new List<PinData>();
    public PinData AddPin(UnityEngine.Vector3 p,PinType t,string text,bool save,bool check){var pin=new PinData();Pins.Add(pin);return pin;}
    public void RemovePin(PinData pin)=>Pins.Remove(pin);
}
public class Heightmap:UnityEngine.Component {
    public static readonly List<Heightmap> All=new List<Heightmap>();public bool IsDistantLod;public int m_width=64;public float m_scale=1;
    public TerrainComp Compiler;public static IEnumerable<Heightmap> GetAllHeightmaps()=>All;
    public TerrainComp GetAndCreateTerrainCompiler()=>Compiler;
}
public class TerrainModifier {public enum PaintType{Paved}}
public class TerrainOp {
    public class Settings {
        public bool m_level,m_raise,m_smooth,m_square,m_halfOffset,m_paintCleared,m_paintHeightCheck;
        public float m_paintRadius,m_paintStrength,m_paintExp;public TerrainModifier.PaintType m_paintType;public UnityEngine.AnimationCurve m_paintCurve;
    }
}
public class TerrainComp:UnityEngine.Component {
    bool m_initialized=true;uint m_lastDataRevision;public int Operations;
    public bool Initialized{get=>m_initialized;set=>m_initialized=value;}public uint AppliedRevision=>m_lastDataRevision;
    public bool IsOwner()=>GetComponent<ZNetView>().Owner;
    void DoOperation(UnityEngine.Vector3 p,UnityEngine.Vector3 delta,TerrainOp.Settings settings) {
        if(settings.m_level||settings.m_raise||settings.m_smooth||!settings.m_paintCleared)throw new Exception("Painting changed terrain height");
        Operations++;GetComponent<ZNetView>().Data.DataRevision++;
    }
}
namespace Rachel350Z {public static class DriftWorldRules {public const float GroundHeight=32;}}
