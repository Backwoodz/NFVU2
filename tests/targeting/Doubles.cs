// Controlled API responses, not a Unity visibility/physics simulation.
using System;
namespace Rachel350Z { public struct ModelVertex { public float x,y,z; } }
namespace UnityEngine {
public struct Vector3 {
    public float x,y,z;
    public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
    public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);
    public Vector3 normalized=>magnitude>0?new Vector3(x/magnitude,y/magnitude,z/magnitude):new Vector3();
    public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
}
public struct Vector2 {float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public float magnitude=>(float)Math.Sqrt(x*x+y*y);}
public class Transform {
    public Vector3 position;
    public Transform parent;
    public bool IsChildOf(Transform other)=>this==other||(parent!=null&&parent.IsChildOf(other));
    public Vector3 InverseTransformDirection(Vector3 v)=>v;
}
public class Collider {
    public Transform transform=new Transform();
    public Character character;
    public T GetComponentInParent<T>() where T:class=>character as T;
}
public struct RaycastHit {public Collider collider;public float distance;}
public enum QueryTriggerInteraction {Ignore}
public static class Physics {
    public const int DefaultRaycastLayers=-5;
    public static RaycastHit[] hits=Array.Empty<RaycastHit>();
    public static RaycastHit[] RaycastAll(Vector3 origin,Vector3 direction,float range,int mask,QueryTriggerInteraction triggers)=>hits;
}
public static class Mathf {public const float Rad2Deg=180f/(float)Math.PI;public static float Atan2(float y,float x)=>(float)Math.Atan2(y,x);}
}
public class Character {
    public UnityEngine.Transform transform=new UnityEngine.Transform();
    public bool player,tamed,dead,hostile=true;
    public bool IsPlayer()=>player;public bool IsTamed()=>tamed;public bool IsDead()=>dead;
    public UnityEngine.Vector3 GetCenterPoint()=>transform.position;
}
public static class BaseAI { public static bool IsEnemy(Character a,Character b)=>a.hostile; }
