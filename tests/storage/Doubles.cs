using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
namespace HarmonyLib {
public sealed class HarmonyPatch:Attribute {public HarmonyPatch(){}public HarmonyPatch(Type t,string name){}}
public static class AccessTools {
    public static MethodInfo Method(Type t,string name)=>t.GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
    public static FieldInfo Field(Type t,string name)=>t.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
}}
namespace UnityEngine {
    public class Object {
        public bool destroyed;
        public static GameObject Instantiate(GameObject prefab,Vector3 position,Quaternion rotation)=>prefab.Clone(position,rotation);
        public static void Destroy(GameObject obj){obj.destroyed=true;foreach(var component in obj.components.Values)component.destroyed=true;}
        public static bool operator ==(Object a,Object b)=>ReferenceEquals(a,b)||(ReferenceEquals(a,null)?b.destroyed:ReferenceEquals(b,null)&&a.destroyed);
        public static bool operator !=(Object a,Object b)=>!(a==b);
        public override bool Equals(object obj)=>ReferenceEquals(this,obj);
        public override int GetHashCode()=>base.GetHashCode();
    }
    public struct Vector3 {
        public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
    }
    public struct Quaternion {public float y;public static Quaternion identity=>new Quaternion();public static Quaternion Euler(float x,float y,float z)=>new Quaternion{y=y};}
    public class Transform {
        public Vector3 position,eulerAngles;public Quaternion rotation;
        public Vector3 TransformPoint(Vector3 p)=>position+p;
        public void SetPositionAndRotation(Vector3 p,Quaternion q){position=p;rotation=q;}
    }
    public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>();}
    public class MonoBehaviour:Component {}
    public class Renderer:Component {public bool enabled=true;}
    public class Collider:Component {public bool enabled=true;}
    public class GameObject:Object {
        public readonly Transform transform=new Transform();public readonly Dictionary<Type,Component> components=new Dictionary<Type,Component>();
        public T Add<T>(T c) where T:Component {c.gameObject=this;components.Add(typeof(T),c);return c;}
        public T AddComponent<T>() where T:Component=>Add((T)Activator.CreateInstance(typeof(T),true));
        public T GetComponent<T>() where T:Component=>components.TryGetValue(typeof(T),out var c)?(T)c:null;
        public T[] GetComponentsInChildren<T>(bool inactive) where T:Component=>components.Values.OfType<T>().ToArray();
        public GameObject Clone(Vector3 position,Quaternion rotation) {
            var obj=new GameObject();obj.transform.SetPositionAndRotation(position,rotation);
            var data=new ZDO();data.SetPosition(position);data.SetRotation(rotation);
            obj.Add(new ZNetView{data=data});
            var source=GetComponent<Container>();obj.Add(new Container(source.m_width,source.m_height){m_name=source.m_name});
            obj.GetComponent<Container>().inventory.m_onChanged=()=>data.items=obj.GetComponent<Container>().inventory.items.ToList();
            obj.Add(new Renderer());obj.Add(new Collider());obj.Add(new WearNTear());
            ZNetScene.instance.objects[data.m_uid]=obj;ZNetScene.instance.created++;return obj;
        }
    }
}
public struct Vector2i {public int x,y;public Vector2i(int x,int y){this.x=x;this.y=y;}}
public class Item {public Vector2i m_gridPos;public int stack;public string custom;}
public class Inventory {
    string m_name="Native chest";readonly int width;int height;public List<Item> items=new List<Item>();public Action m_onChanged;
    public Inventory(int width,int height){this.width=width;this.height=height;}
    public int GetWidth()=>width;public int GetHeight()=>height;public void SetHeight(int value)=>height=value;
    public int NrOfItems()=>items.Count;public List<Item> GetAllItems()=>items;public string GetName()=>m_name;
}
public class Container:UnityEngine.Component {
    public int m_width,m_height;public string m_name="Native chest";public Inventory inventory;bool inUse;
    public Container(int width,int height){m_width=width;m_height=height;inventory=new Inventory(width,height);}
    public Inventory GetInventory()=>inventory;public bool IsInUse()=>inUse;public void SetInUse(bool value)=>inUse=value;
    void Load(){var data=gameObject.GetComponent<ZNetView>().data;if(data.items!=null)inventory.items=data.items.ToList();}
}
public struct ZDOID : IEquatable<ZDOID> {
    public int value;public bool Equals(ZDOID other)=>value==other.value;
    public override bool Equals(object other)=>other is ZDOID id&&Equals(id);
    public override int GetHashCode()=>value;
}
public static class HashExtensions {public static int GetStableHashCode(this string value)=>value.GetHashCode();}
public class ZDO {
    static int next;public readonly ZDOID m_uid=new ZDOID{value=++next};public bool Persistent;public bool deleted;public List<Item> items;
    public ZDO(){ZDOMan.instance.records[m_uid]=this;}
    UnityEngine.Vector3 pos;UnityEngine.Quaternion rot;
    readonly Dictionary<string,object> extra=new Dictionary<string,object>();
    public void Set(string key,int value)=>extra[key]=value;
    public void Set(string key,bool value)=>extra[key]=value;
    public void Set(string key,UnityEngine.Vector3 value)=>extra[key]=value;
    public void Set(string key,UnityEngine.Quaternion value)=>extra[key]=value;
    public int GetInt(string key)=>extra.TryGetValue(key,out var value)?(int)value:0;
    public bool HasInt(int hash)=>extra.Any(e=>e.Value is int&&e.Key.GetStableHashCode()==hash);
    public bool GetBool(string key)=>extra.TryGetValue(key,out var value)&&(bool)value;
    public UnityEngine.Vector3 GetVec3(string key,UnityEngine.Vector3 fallback)=>extra.TryGetValue(key,out var value)?(UnityEngine.Vector3)value:fallback;
    public UnityEngine.Quaternion GetQuaternion(string key,UnityEngine.Quaternion fallback)=>extra.TryGetValue(key,out var value)?(UnityEngine.Quaternion)value:fallback;
    public long owner=123;public void SetOwner(long value)=>owner=value;public bool IsOwner()=>owner==ZDOMan.GetSessionID();
    public void SetPosition(UnityEngine.Vector3 p)=>pos=p;public UnityEngine.Vector3 GetPosition()=>pos;
    public void SetRotation(UnityEngine.Quaternion r)=>rot=r;public UnityEngine.Quaternion GetRotation()=>rot;
}
public class ZNetView:UnityEngine.Component {
    public ZDO data;public bool owner=true;public ZDO GetZDO()=>data;public bool IsOwner()=>owner;
    public bool IsValid()=>!destroyed&&!data.deleted;
    public void Destroy(){data.deleted=true;UnityEngine.Object.Destroy(gameObject);ZNetScene.instance.objects.Remove(data.m_uid);}
}
public class ZDOMan {
    public static ZDOMan instance=new ZDOMan();public static long GetSessionID()=>123;
    public readonly Dictionary<ZDOID,ZDO> records=new Dictionary<ZDOID,ZDO>();
    public ZDO GetZDO(ZDOID id)=>records.TryGetValue(id,out var record)&&!record.deleted?record:null;
}
public static class ZDOExtraData {
    public enum Type {Int}
    public static List<ZDOID> GetAllZDOIDsWithHash(Type type,int hash)=>ZDOMan.instance.records.Values.Where(d=>!d.deleted&&d.HasInt(hash)).Select(d=>d.m_uid).ToList();
}
public class Player:UnityEngine.MonoBehaviour {
    public static Player m_localPlayer=new Player();public bool dead,teleport;
    public bool IsDead()=>dead;public bool IsTeleporting()=>teleport;
}
public class Game {}
public class ZNetScene {
    public static ZNetScene instance=new ZNetScene();public readonly Dictionary<ZDOID,UnityEngine.GameObject> objects=new Dictionary<ZDOID,UnityEngine.GameObject>();public int created;
    public UnityEngine.GameObject GetPrefab(string name){var obj=new UnityEngine.GameObject();obj.Add(new Container(name=="piece_chest"?6:8,4));return obj;}
    public UnityEngine.GameObject FindInstance(ZDOID id)=>objects.TryGetValue(id,out var root)?root:null;
}
public class WearNTear:UnityEngine.Component {public bool m_noRoofWear=true,m_noSupportWear=true;}
public class ZoneSystem {public static ZoneSystem instance=new ZoneSystem();public bool GetGroundHeight(UnityEngine.Vector3 pos,out float height){height=10;return true;}}
public class InventoryGui {
    public static InventoryGui instance=new InventoryGui();public Container current;
    public void Show(Container container)=>current=container;
    public void Hide(){current?.SetInUse(false);current=null;}
}
