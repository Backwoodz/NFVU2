using System;
using System.Collections.Generic;
namespace UnityEngine {
public class Object {
    static int next; readonly int id=++next;
    public string name;
    public int GetInstanceID()=>id;
}
public class Component:Object {
    public Transform transform=new Transform();
    public readonly Dictionary<Type,Component> parents=new Dictionary<Type,Component>();
    public T GetComponentInParent<T>() where T:Component => parents.TryGetValue(typeof(T),out var value)?(T)value:null;
}
public class MonoBehaviour:Component {}
public class Transform {
    public Quaternion rotation;
    public Vector3 TransformPoint(Vector3 point)=>point;
    public bool IsChildOf(Transform other)=>this==other;
}
public struct Quaternion {}
public struct Vector3 {
    public float x,y,z;
    public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
    public float sqrMagnitude=>x*x+y*y+z*z;
    public float magnitude=>(float)Math.Sqrt(sqrMagnitude);
    public Vector3 normalized=>magnitude>0?this*(1/magnitude):new Vector3();
    public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
    public static Vector3 operator *(Vector3 v,float f)=>new Vector3(v.x*f,v.y*f,v.z*f);
}
public class Rigidbody:Component {public Vector3 linearVelocity;}
public class Collider:Component {}
public class BoxCollider:Collider {public Vector3 center,size;}
public struct RaycastHit {public Collider collider;public Vector3 point,normal;public float distance;}
public struct ContactPoint {public Vector3 point,normal;}
public class Collision {
    public Collider collider;public ContactPoint[] contacts;
    public int contactCount=>contacts.Length;
    public ContactPoint GetContact(int i)=>contacts[i];
}
public enum QueryTriggerInteraction {Ignore}
public static class Physics {
    public static RaycastHit? nearest;
    public static RaycastHit[] hits;
    public static float castDistance;
    public static int BoxCastNonAlloc(Vector3 center,Vector3 half,Vector3 direction,RaycastHit[] results,Quaternion rotation,float distance,int mask,QueryTriggerInteraction triggers) {
        castDistance=distance;
        if(hits!=null){int count=Math.Min(hits.Length,results.Length);Array.Copy(hits,results,count);return count;}
        if(!nearest.HasValue)return 0;
        results[0]=nearest.Value;results[0].distance=.05f;return 1;
    }
}
public static class Time {public static float time;public static float fixedDeltaTime=.02f;}
public static class Mathf {
    public static float Max(float a,float b)=>Math.Max(a,b);
    public static float Min(float a,float b)=>Math.Min(a,b);
}
}
public class Character:UnityEngine.MonoBehaviour {
    public bool player,tamed,dead,enemy=true;
    public HitData lastHit;public int hits;
    public bool IsPlayer()=>player;
    public bool IsTamed()=>tamed;
    public bool IsDead()=>dead;
    public void Damage(HitData hit){lastHit=hit;hits++;}
}
public class Destructible:UnityEngine.Component {
    public HitData lastHit;public int hits;
    public void Damage(HitData hit){lastHit=hit;hits++;}
}
public class Piece:UnityEngine.Component {}
public static class BaseAI {public static bool IsEnemy(Character a,Character b)=>a.enemy;}
public class HitData {
    public struct DamageTypes {public float m_chop,m_blunt;}
    public enum HitType {Impact}
    public DamageTypes m_damage;
    public UnityEngine.Vector3 m_point,m_dir;
    public UnityEngine.Collider m_hitCollider;
    public float m_pushForce;public short m_toolTier;
    public bool m_dodgeable,m_blockable;public HitType m_hitType;
    public Character attacker;
    public void SetAttacker(Character value){attacker=value;}
}
