using System;
using System.Reflection;
using Rachel350Z;
using UnityEngine;
// Native callback adapters compiled unchanged against controlled engine doubles.
namespace UnityEngine {
    public class MonoBehaviour {}
    public struct Vector3 {
        public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
    }
    public static class Mathf {public static float Abs(float x)=>Math.Abs(x);public static float Max(float x,float y)=>Math.Max(x,y);}
    public static class Time {public static float time;}
    public class Collider {
        public object Parent;
        public T GetComponentInParent<T>() where T:class=>Parent as T;
    }
    public struct ContactPoint {public Vector3 normal;}
    public class Collision {
        public Collider collider=new Collider();public Vector3 relativeVelocity;
        public ContactPoint[] contacts=Array.Empty<ContactPoint>();public int contactCount=>contacts.Length;
        public ContactPoint GetContact(int i)=>contacts[i];
    }
}
public enum DestructibleType {Default}
public interface IDestructible {void Damage(HitData hit);DestructibleType GetDestructibleType();}
public class HitData {}
public class Character {public void Damage(HitData hit){}}
public class Destructible {public string name;}
namespace Rachel350Z {
    public class Plugin {
        public static Plugin ControlsOwner;public Character Driver;public bool Active;
        public bool RouteHordeDriverDamage(Character target,HitData hit)=>Active&&target==Driver;
    }
}
static class Program {
    static int checks;
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;}
    static void Main() {
        var hull=new VehicleHordeHull();int hits=0,crashes=0;float speed=0;
        var hit=new HitData();hull.OnHit=h=>{Check(ReferenceEquals(h,hit),"native hit data is forwarded intact");hits++;};
        ((IDestructible)hull).Damage(hit);Check(hits==1,"native IDestructible attack reaches hull receiver");
        hull.OnHit=null;hull.Damage(hit);Check(hits==1,"detached hull cannot route further damage");
        hull.OnCrash=s=>{crashes++;speed=s;};
        var contact=typeof(VehicleHordeHull).GetMethod("OnCollisionEnter",BindingFlags.Instance|BindingFlags.NonPublic);
        var collision=new Collision{relativeVelocity=new Vector3(18,0,0),contacts=new[]{new ContactPoint{normal=new Vector3(1,0,0)}}};
        Time.time=1;collision.collider.Parent=new Character();contact.Invoke(hull,new object[]{collision});
        Check(crashes==0,"running over a creature is not obstacle damage");
        collision.collider.Parent=new Destructible{name="Beech_small1(Clone)"};contact.Invoke(hull,new object[]{collision});
        Check(crashes==0,"small vegetation does not penalize driving through it");
        collision.collider.Parent=null;collision.contacts[0]=new ContactPoint{normal=new Vector3(0,1,0)};
        contact.Invoke(hull,new object[]{collision});Check(crashes==0,"ground contact does not damage car");
        collision.contacts[0]=new ContactPoint{normal=new Vector3(1,0,0)};
        contact.Invoke(hull,new object[]{collision});Check(crashes==1&&speed==18,"hard wall impact reports closing speed");
        contact.Invoke(hull,new object[]{collision});Check(crashes==1,"collision chatter does not stack damage");
        Time.time=2;collision.relativeVelocity=new Vector3(0,0,18);contact.Invoke(hull,new object[]{collision});
        Check(crashes==1,"glancing movement parallel to wall is not a hard impact");
        hull.OnCrash=null;collision.relativeVelocity=new Vector3(18,0,0);contact.Invoke(hull,new object[]{collision});
        Check(crashes==1,"detached component cannot report crashes");
        var prefix=typeof(HordeDriverDamage).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static);
        var driver=new Character();var other=new Character();
        bool Pass(Character who)=>(bool)prefix.Invoke(null,new object[]{who,hit});
        Check(Pass(driver),"without plugin native damage passes through");
        Plugin.ControlsOwner=new Plugin{Driver=driver,Active=true};
        Check(!Pass(driver)&&Pass(other),"only the horde driver is redirected");
        Plugin.ControlsOwner.Active=false;Check(Pass(driver),"ending mode restores native player damage");
        Console.WriteLine(checks+" horde hull adapter checks passed (controlled callbacks, not Unity physics).");
    }
}
