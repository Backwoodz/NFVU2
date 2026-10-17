using System;
using System.Collections.Generic;
namespace UnityEngine {
    public class GameObject {
        readonly Dictionary<Type,object> components=new Dictionary<Type,object>();
        public int layer;public string name;public GameObject parent;
        public GameObject(string name,int layer=0){this.name=name;this.layer=layer;}
        public void Add<T>(T value){components[typeof(T)]=value;}
        public T ParentComponent<T>()where T:class {for(var node=this;node!=null;node=node.parent)if(node.components.TryGetValue(typeof(T),out object value))return value as T;return null;}
    }
    public class Collider {
        public readonly GameObject gameObject;public bool isTrigger,IgnoreForMovement;
        public Collider(string name,int layer=0){gameObject=new GameObject(name,layer);}
        public T GetComponentInParent<T>()where T:class=>gameObject.ParentComponent<T>();
    }
    public struct Vector3 {
        public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 up=>new Vector3(0,1,0);public static Vector3 down=>new Vector3(0,-1,0);
    }
    public struct RaycastHit {public Collider collider;public float distance;public Vector3 point,normal;}
    public enum QueryTriggerInteraction {Ignore}
    public static class Mathf {
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static float MoveTowards(float current,float target,float delta)=>Math.Abs(target-current)<=delta?target:current+Math.Sign(target-current)*delta;
    }
    public static class LayerMask {
        public static readonly Dictionary<string,int> Named=new Dictionary<string,int>{{"Default",0},{"Water",4},{"static_solid",6},{"Default_small",7},{"piece",8},{"blocker",10},{"terrain",11},{"vehicle",12},{"WaterVolume",21},{"Liquid",22},{"character",23},{"character_net",24},{"ghost",25},{"effect",26}};
        public static int GetMask(params string[] names){int mask=0;foreach(var name in names)if(Named.TryGetValue(name,out int layer))mask|=1<<layer;return mask;}
    }
    public static class Physics {
        public const int DefaultRaycastLayers=~(1<<2);
        public static readonly List<RaycastHit> Hits=new List<RaycastHit>();
        public static int LastMask;public static float LastDistance;public static Vector3 LastOrigin;
        public static int RaycastNonAlloc(Vector3 origin,Vector3 direction,RaycastHit[] output,float distance,int mask,QueryTriggerInteraction triggers) {
            LastMask=mask;LastOrigin=origin;LastDistance=distance;int count=0;
            foreach(var hit in Hits) {
                if(hit.collider.isTrigger||(mask&(1<<hit.collider.gameObject.layer))==0||hit.distance>distance)continue;
                if(count==output.Length)return count;
                output[count++]=hit;
            }
            return count;
        }
    }
}
public class Heightmap {
    public static Heightmap Current;public float Height;public bool Loaded=true;
    public static Heightmap FindHeightmap(UnityEngine.Vector3 point)=>Current;
    public bool GetWorldHeight(UnityEngine.Vector3 point,out float height){height=Height;return Loaded;}
}
public class WaterVolume {}
public class LiquidVolume {}
public class LiquidSurface {}
public class Character {}
namespace Rachel350Z {
    public static class TransformerImpact {
        public static readonly int CollisionMask=UnityEngine.Physics.DefaultRaycastLayers&~UnityEngine.LayerMask.GetMask("effect");
        public static bool IgnoreMovementCollider(UnityEngine.Collider collider)=>collider.IgnoreForMovement;
    }
}
