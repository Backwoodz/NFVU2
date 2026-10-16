// Controlled API responses; these do not simulate Unity physics or Valheim RPCs.
using System;
using System.Collections.Generic;
namespace UnityEngine {
public class Component {
    static int next;readonly int id=++next;
    public Transform transform=new Transform();
    public int GetInstanceID()=>id;
}
public class Transform {
    public Vector3 position,lossyScale=new Vector3(1,1,1);
    public bool IsChildOf(Transform other)=>this==other;
}
public class Collider:Component {
    public readonly GameObject gameObject=new GameObject();
    public readonly List<Component> parents=new List<Component>();public Vector3 closest;
    public T GetComponentInParent<T>() where T:class {foreach(var p in parents)if(p is T t)return t;return null;}
    public Vector3 ClosestPoint(Vector3 origin)=>closest;
}
public class GameObject {public int layer;}
public static class LayerMask {public static int NameToLayer(string name)=>name=="effect"?12:-1;}
public struct Vector3 {
    public float x,y,z;
    public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
    public static Vector3 up=>new Vector3(0,1,0);
    public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);
    public void Normalize(){float n=magnitude;if(n>0){x/=n;y/=n;z/=n;}}
    public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
    public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
    public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
    public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
    public static Vector3 operator /(Vector3 a,float b)=>a*(1/b);
}
public static class Mathf {
    public static float Abs(float v)=>Math.Abs(v);
    public static float Clamp(float v,float a,float b)=>Math.Max(a,Math.Min(b,v));
}
public static class Time {public static float time;}
public struct RaycastHit {public Collider collider;}
public enum QueryTriggerInteraction {Ignore}
public static class Physics {
    public const int DefaultRaycastLayers=-5;
    public static Collider[] overlaps=Array.Empty<Collider>(),cover=Array.Empty<Collider>();
    public static int OverlapCapsuleNonAlloc(Vector3 a,Vector3 b,float r,Collider[] result,int mask,QueryTriggerInteraction q) {
        int n=Math.Min(overlaps.Length,result.Length);Array.Copy(overlaps,result,n);return n;
    }
    public static int RaycastNonAlloc(Vector3 a,Vector3 b,RaycastHit[] result,float d,int mask,QueryTriggerInteraction q) {
        int n=Math.Min(cover.Length,result.Length);for(int i=0;i<n;i++)result[i].collider=cover[i];return n;
    }
}
}
public interface IDestructible {void Damage(HitData hit);}
public class Damageable:UnityEngine.Component,IDestructible {
    public int hits;public HitData last;
    public void Damage(HitData hit){hits++;last=hit;}
}
public class Character:Damageable {
    public bool player,tamed,dead,boss,enemy=true;public float height=2,radius=.5f;
    public bool IsPlayer()=>player;public bool IsTamed()=>tamed;public bool IsDead()=>dead;public bool IsBoss()=>boss;
    public float GetHeight()=>height;public float GetRadius()=>radius;
}
public static class BaseAI {public static bool IsEnemy(Character a,Character b)=>a.enemy;}
public class TreeBase:Damageable { }
public class TreeLog:Damageable { }
public class ItemDrop:UnityEngine.Component { }
public class Gibber:UnityEngine.Component { }
public class Piece:UnityEngine.Component { }
public class WearNTear:UnityEngine.Component { }
public enum DestructibleType {Default,Tree}
public class Destructible:Damageable {public DestructibleType kind;public DestructibleType GetDestructibleType()=>kind;}
public class HitData {
    public struct DamageTypes {public float m_blunt,m_chop;}
    public enum HitType {Impact}
    public DamageTypes m_damage;public UnityEngine.Vector3 m_point,m_dir;public UnityEngine.Collider m_hitCollider;
    public int m_toolTier;public float m_pushForce;public bool m_dodgeable,m_blockable;public HitType m_hitType;
    public Character attacker;public void SetAttacker(Character c){attacker=c;}
}
