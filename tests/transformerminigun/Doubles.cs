// Controlled native damage API; not a Unity physics or resistance simulator.
using System;
using System.Linq;
namespace Rachel350Z {public struct ModelVertex {public float x,y,z;}}
namespace UnityEngine {
public struct Vector3 {
    public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
    public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);
    public Vector3 normalized=>magnitude>0?new Vector3(x/magnitude,y/magnitude,z/magnitude):new Vector3();
    public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
}
public struct Vector2 {float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public float magnitude=>(float)Math.Sqrt(x*x+y*y);}
public class Transform {
    public Vector3 position;public Transform parent;
    public bool IsChildOf(Transform other)=>this==other||(parent!=null&&parent.IsChildOf(other));
    public Vector3 InverseTransformDirection(Vector3 value)=>value;
}
public class Collider {
    public Transform transform=new Transform();
    public Character character;public Piece piece;public WearNTear wear;
    public T GetComponentInParent<T>() where T:class {
        if(typeof(T)==typeof(Character))return character as T;
        if(typeof(T)==typeof(Piece))return piece as T;
        if(typeof(T)==typeof(WearNTear))return wear as T;
        return null;
    }
}
public struct RaycastHit {public Collider collider;public Vector3 point;public float distance;}
public enum QueryTriggerInteraction {Ignore}
public static class Physics {
    public const int DefaultRaycastLayers=-5;
    public static RaycastHit[] hits=Array.Empty<RaycastHit>();public static float LastRange;
    public static RaycastHit[] RaycastAll(Vector3 origin,Vector3 direction,float range,int mask,QueryTriggerInteraction triggers) {
        LastRange=range;return hits.Where(h=>h.distance>=0&&h.distance<=range).ToArray();
    }
}
public static class Mathf {public const float Rad2Deg=180f/(float)Math.PI;public static float Atan2(float y,float x)=>(float)Math.Atan2(y,x);public static float Max(float a,float b)=>Math.Max(a,b);}
}
public class Piece {}
public class WearNTear {}
public class Character {
    public UnityEngine.Transform transform=new UnityEngine.Transform();
    public bool player,tamed,dead,hostile=true;
    public int hits;public float health=40,maxHealth=40,resultScale=1;public bool ignoreDamage,healthFallbackAfterDeath;public string label="enemy";public HitData last;
    public bool IsPlayer()=>player;public bool IsTamed()=>tamed;public bool IsDead()=>dead;
    public float GetHealth()=>dead&&healthFallbackAfterDeath?maxHealth:health;public float GetMaxHealth()=>maxHealth;public string GetHoverName()=>label;
    public UnityEngine.Vector3 GetCenterPoint()=>transform.position;
    public void Damage(HitData hit){last=hit;hits++;if(ignoreDamage)return;health=Math.Max(0,health-hit.m_damage.m_pierce*resultScale);if(health<=0)dead=true;}
}
public static class BaseAI {public static bool IsEnemy(Character a,Character b)=>a.hostile;}
public class HitData {
    public struct DamageTypes {public float m_pierce;}
    public DamageTypes m_damage;
    public UnityEngine.Vector3 m_point,m_dir;public UnityEngine.Collider m_hitCollider;
    public float m_pushForce,m_backstabBonus,m_staggerMultiplier;
    public bool m_blockable,m_dodgeable,m_ranged;public Character attacker;
    public void SetAttacker(Character owner){attacker=owner;}
}
