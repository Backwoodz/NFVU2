// Controlled Valheim/Unity API responses; no simulation of physics or native RPCs.
using System;
using System.Collections.Generic;
namespace Rachel350Z { public struct ModelVertex {public float x,y,z;} }
namespace UnityEngine {
public class Component { }
public class Collider:Component {
    public readonly List<Component> parents=new List<Component>();
    public Vector3 closest;
    public T GetComponentInParent<T>() where T:class {
        foreach(var parent in parents)if(parent is T match)return match;return null;
    }
    public Vector3 ClosestPoint(Vector3 origin)=>closest;
}
public struct Vector3 {
    public float x,y,z;
    public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
    public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);
    public Vector3 normalized=>magnitude>0?this*(1/magnitude):new Vector3();
    public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
    public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
    public static Vector3 operator *(Vector3 a,float f)=>new Vector3(a.x*f,a.y*f,a.z*f);
    public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;
}
public struct RaycastHit {public Collider collider;public Vector3 point,normal;}
public enum QueryTriggerInteraction {Ignore}
public static class Physics {
    public const int DefaultRaycastLayers=-5;
    public static Collider[] overlaps=Array.Empty<Collider>();
    public static Collider cover;
    public static Collider[] OverlapSphere(Vector3 point,float radius,int mask,QueryTriggerInteraction query)=>overlaps;
    public static bool Linecast(Vector3 start,Vector3 end,out RaycastHit hit,int mask,QueryTriggerInteraction query){hit=new RaycastHit{collider=cover};return cover!=null;}
}
}
public interface IDestructible {void Damage(HitData hit);}
public class Damageable:UnityEngine.Component,IDestructible {
    public int hits;public HitData last;
    public void Damage(HitData hit){hits++;last=hit;}
}
public class Character:Damageable {
    public bool player,tamed,dead,enemy=true;
    public bool IsPlayer()=>player;public bool IsTamed()=>tamed;public bool IsDead()=>dead;
}
public static class BaseAI {public static bool IsEnemy(Character a,Character b)=>a.enemy;}
public class TreeBase:Damageable { }
public class TreeLog:Damageable { }
public class MineRock:Damageable { }
public class MineRock5:Damageable { }
public class Piece:UnityEngine.Component { }
public class WearNTear:UnityEngine.Component { }
public enum DestructibleType {Default,Tree}
public class Destructible:Damageable {public DestructibleType kind;public DestructibleType GetDestructibleType()=>kind;}
public class HitData {
    public struct DamageTypes {public float m_pierce,m_blunt,m_fire,m_chop,m_pickaxe;}
    public DamageTypes m_damage;
    public UnityEngine.Vector3 m_point,m_dir;
    public UnityEngine.Collider m_hitCollider;
    public float m_pushForce;public short m_toolTier;
    public bool m_blockable,m_dodgeable;
    public Character attacker;public void SetAttacker(Character character){attacker=character;}
}
