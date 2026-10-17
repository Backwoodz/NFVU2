// Controlled native damage API; not a Unity physics or resistance simulator.
namespace UnityEngine {
public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
public class Collider {
    public Character character;public Piece piece;public WearNTear wear;
    public T GetComponentInParent<T>() where T:class {
        if(typeof(T)==typeof(Character))return character as T;
        if(typeof(T)==typeof(Piece))return piece as T;
        if(typeof(T)==typeof(WearNTear))return wear as T;
        return null;
    }
}
public struct RaycastHit {public Collider collider;public Vector3 point;}
}
public class Piece {}
public class WearNTear {}
public class Character {
    public bool player,tamed,dead,hostile=true;
    public int hits;public float health=40;public HitData last;
    public bool IsPlayer()=>player;public bool IsTamed()=>tamed;public bool IsDead()=>dead;
    public void Damage(HitData hit){last=hit;hits++;health-=hit.m_damage.m_pierce;if(health<=0)dead=true;}
}
public static class BaseAI {public static bool IsEnemy(Character a,Character b)=>a.hostile;}
public class HitData {
    public struct DamageTypes {public float m_pierce;}
    public DamageTypes m_damage;
    public UnityEngine.Vector3 m_point,m_dir;public UnityEngine.Collider m_hitCollider;
    public float m_pushForce,m_backstabBonus,m_staggerMultiplier;
    public bool m_blockable,m_dodgeable;public Character attacker;
    public void SetAttacker(Character owner){attacker=owner;}
}
