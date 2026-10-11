using System;
using System.Collections.Generic;
using System.Reflection;
using Rachel350Z;
using UnityEngine;
namespace UnityEngine {
    public class MonoBehaviour {
        public readonly Dictionary<Type,object> Components=new Dictionary<Type,object>();
        public T GetComponent<T>() where T:class=>Components.TryGetValue(typeof(T),out var value)?value as T:null;
        public string Cancelled;public void CancelInvoke(string name){Cancelled=name;}
    }
    public class GameObject {public ZDO Data;}
}
public struct ZDOID {public int Value;}
public class Character:MonoBehaviour {}
public class Projectile:MonoBehaviour {public GameObject m_spawnOnHit=new GameObject();public List<GameObject> m_randomSpawnOnHit=new List<GameObject>{new GameObject()};public bool m_respawnItemOnHit=true;public void Setup(Character owner){}}
public static class ZDOVars {public const int s_drops=123;}
public class ZDO {
    public ZDOID m_uid;public bool Persistent=true;public int Drops=6;public long Owner;
    public void Set(int key,int value){if(key==ZDOVars.s_drops)Drops=value;}
    public void SetOwner(long owner){Owner=owner;}
}
public class ZNetView {public ZDO Data;public ZDO GetZDO()=>Data;}
public class ZDOMan {
    public static ZDOMan instance;public readonly Dictionary<ZDOID,ZDO> Data=new Dictionary<ZDOID,ZDO>();
    public ZDO GetZDO(ZDOID id)=>Data.TryGetValue(id,out var data)?data:null;
    public static long GetSessionID()=>42;
    public void DestroyZDO(ZDO data){Data.Remove(data.m_uid);}
}
public class ZNetScene {
    public static ZNetScene instance;public readonly Dictionary<ZDOID,GameObject> Loaded=new Dictionary<ZDOID,GameObject>();
    public GameObject FindInstance(ZDOID id)=>Loaded.TryGetValue(id,out var go)?go:null;
    public void Destroy(GameObject go){Loaded.Remove(go.Data.m_uid);ZDOMan.instance.DestroyZDO(go.Data);}
}
public class CharacterDrop:MonoBehaviour {}
public class Ragdoll:MonoBehaviour {public bool m_dropItems=true;public void Setup(CharacterDrop characterDrop){}}
namespace Rachel350Z {
 public partial class Plugin {
  ZNetScene hordeScene=ZNetScene.instance;ZDOMan hordeManager=ZDOMan.instance;
  public void Track(Projectile p)=>RegisterHordeProjectile(p);
  public void ClearShots()=>ClearHordeProjectiles();
  public int ShotCount=>hordeProjectiles.Count;
 }
}
static class Program {
    static int checks,serial;
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;}
    static Ragdoll Corpse(bool loaded=true) {
        var data=new ZDO{m_uid=new ZDOID{Value=++serial}};
        var corpse=new Ragdoll();corpse.Components[typeof(ZNetView)]=new ZNetView{Data=data};
        ZDOMan.instance.Data.Add(data.m_uid,data);
        if(loaded)ZNetScene.instance.Loaded.Add(data.m_uid,new GameObject{Data=data});
        return corpse;
    }
    static ZDO Data(Ragdoll corpse)=>corpse.GetComponent<ZNetView>().GetZDO();
    static bool Exists(Ragdoll corpse)=>ZDOMan.instance.GetZDO(Data(corpse).m_uid)!=null;
    static void Main() {
        ZNetScene.instance=new ZNetScene();ZDOMan.instance=new ZDOMan();
        var pool=new HordeCorpses(ZNetScene.instance,ZDOMan.instance);
        var patch=typeof(HordeRagdollSetup).GetMethod("Postfix",BindingFlags.Static|BindingFlags.NonPublic);
        void Setup(Ragdoll corpse,CharacterDrop drops)=>patch.Invoke(null,new object[]{corpse,drops});
        var ordinary=Corpse();Setup(ordinary,new CharacterDrop());Setup(ordinary,null);
        Check(ordinary.m_dropItems&&Data(ordinary).Drops==6&&Data(ordinary).Persistent&&ordinary.Cancelled==null,"ordinary creatures and missing drop components are untouched");
        var drops=new CharacterDrop();drops.Components[typeof(HordeEnemyMarker)]=new HordeEnemyMarker{OnCorpse=pool.Add};
        var first=Corpse();Setup(first,drops);
        Check(!first.m_dropItems&&Data(first).Drops==0&&!Data(first).Persistent,"horde corpse has no saved loot and is never persistent");
        Check(first.Cancelled=="DestroyNow","managed lifetime replaces native delayed loot callback");
        pool.Tick(0);pool.Tick(-1);pool.Tick(float.NaN);pool.Tick(float.PositiveInfinity);pool.Tick(2.9f);
        Check(Exists(first),"invalid delta ignored and corpse lives for the visible death reaction");
        pool.Tick(.11f);Check(!Exists(first)&&Data(first).Owner==42,"loaded corpse removed through network scene after three seconds");
        var unloaded=Corpse(false);Setup(unloaded,drops);pool.Tick(3);
        Check(!Exists(unloaded),"streamed-out corpse ZDO also removed after timeout");
        var oldest=Corpse();Setup(oldest,drops);
        for(int i=0;i<HordeCorpses.Limit;i++)Setup(Corpse(),drops);
        Check(!Exists(oldest)&&ZDOMan.instance.Data.Count==HordeCorpses.Limit+1,"burst kills retain at most twelve corpses plus unrelated world object");
        pool.Clear();Check(ZDOMan.instance.Data.Count==1&&Exists(ordinary),"abort clears all own corpses and preserves unrelated objects");
        var late=Corpse();Setup(late,drops);Check(!Exists(late),"late callback after abort is immediately removed");
        var oldScene=ZNetScene.instance;var oldManager=ZDOMan.instance;
        pool=new HordeCorpses(oldScene,oldManager);var oldCorpse=Corpse();pool.Add(oldCorpse);
        ZNetScene.instance=new ZNetScene();ZDOMan.instance=new ZDOMan();
        var sameId=new ZDO{m_uid=Data(oldCorpse).m_uid};ZDOMan.instance.Data.Add(sameId.m_uid,sameId);
        pool.Tick(30);pool.Clear();Check(ZDOMan.instance.GetZDO(sameId.m_uid)==sameId,"old session IDs cannot remove a new world's objects");
        Check(oldManager.GetZDO(Data(oldCorpse).m_uid)!=null,"cleanup does not call departed world managers");
        var plugin=new Plugin();var shooter=new Character();
        shooter.Components[typeof(HordeEnemyMarker)]=new HordeEnemyMarker{OnProjectile=plugin.Track};
        var setup=typeof(HordeProjectileSetup).GetMethod("Prefix",BindingFlags.Static|BindingFlags.NonPublic);
        Projectile Shot(Character owner,bool loaded=true) {
            var data=new ZDO{m_uid=new ZDOID{Value=++serial}};ZDOMan.instance.Data.Add(data.m_uid,data);
            if(loaded)ZNetScene.instance.Loaded.Add(data.m_uid,new GameObject{Data=data});
            var p=new Projectile();p.Components[typeof(ZNetView)]=new ZNetView{Data=data};setup.Invoke(null,new object[]{p,owner});return p;
        }
        var ordinaryShot=Shot(new Character());Check(ordinaryShot.m_spawnOnHit!=null&&ordinaryShot.m_respawnItemOnHit&&plugin.ShotCount==0,"normal world projectiles remain untouched");
        var owned=Shot(shooter);var streamed=Shot(shooter,false);
        Check(!owned.m_respawnItemOnHit&&owned.m_spawnOnHit==null&&owned.m_randomSpawnOnHit.Count==0&&!owned.GetComponent<ZNetView>().Data.Persistent,"horde projectiles cannot persist or leave ice obstacles and items");
        plugin.ClearShots();
        Check(ZDOMan.instance.GetZDO(owned.GetComponent<ZNetView>().Data.m_uid)==null&&ZDOMan.instance.GetZDO(streamed.GetComponent<ZNetView>().Data.m_uid)==null&&plugin.ShotCount==0,"wave end removes loaded and streamed-out horde projectiles");
        Check(ZDOMan.instance.GetZDO(ordinaryShot.GetComponent<ZNetView>().Data.m_uid)!=null,"projectile cleanup preserves ordinary world shots");
        var oldestShot=Shot(shooter);for(int i=0;i<128;i++)Shot(shooter);
        Check(plugin.ShotCount==128&&ZDOMan.instance.GetZDO(oldestShot.GetComponent<ZNetView>().Data.m_uid)==null,"projectile budget remains bounded during heavy boss fire");
        var lastShot=Shot(shooter);
        ZNetScene.instance=new ZNetScene();ZDOMan.instance=new ZDOMan();
        var replacement=new ZDO{m_uid=lastShot.GetComponent<ZNetView>().Data.m_uid};ZDOMan.instance.Data.Add(replacement.m_uid,replacement);
        plugin.ClearShots();Check(ZDOMan.instance.GetZDO(replacement.m_uid)==replacement,"projectile cleanup never crosses world ownership");
        Console.WriteLine(checks+" horde corpse/projectile adapter checks passed (network/Unity doubles).");
    }
}
