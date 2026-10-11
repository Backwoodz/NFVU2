using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
namespace Rachel350Z {
// Only enemies spawned by this mode receive this marker. Ordinary world loot is untouched.
public sealed class HordeEnemyMarker : MonoBehaviour {
    public Action<Ragdoll> OnCorpse;
    public Action<Projectile> OnProjectile;
}
[HarmonyPatch(typeof(Ragdoll),nameof(Ragdoll.Setup))]
public static class HordeRagdollSetup {
    static void Postfix(Ragdoll __instance,CharacterDrop characterDrop) {
        if(characterDrop!=null)characterDrop.GetComponent<HordeEnemyMarker>()?.OnCorpse?.Invoke(__instance);
    }
}
public sealed class HordeCorpses {
    public const float Lifetime=3;
    public const int Limit=12;
    sealed class Entry {public ZDOID Id;public float Age;}
    readonly List<Entry> entries=new List<Entry>();
    readonly ZNetScene scene;
    readonly ZDOMan manager;
    bool closed;
    public HordeCorpses(ZNetScene scene,ZDOMan manager){this.scene=scene;this.manager=manager;}
    bool SameWorld=>scene!=null&&scene==ZNetScene.instance&&manager!=null&&manager==ZDOMan.instance;
    public void Add(Ragdoll corpse) {
        if(corpse==null||!SameWorld)return;
        var data=corpse.GetComponent<ZNetView>()?.GetZDO();
        if(data==null)return;
        // Setup has already copied the drop list to the corpse ZDO: clear that too.
        corpse.m_dropItems=false;data.Set(ZDOVars.s_drops,0);data.Persistent=false;
        if(closed){Destroy(data.m_uid);return;}
        corpse.CancelInvoke("DestroyNow");
        entries.Add(new Entry{Id=data.m_uid});
        while(entries.Count>Limit){Destroy(entries[0].Id);entries.RemoveAt(0);}
    }
    public void Tick(float dt) {
        if(closed||!SameWorld||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
        for(int i=entries.Count-1;i>=0;i--) {
            var entry=entries[i];entry.Age+=dt;
            if(entry.Age>=Lifetime||manager.GetZDO(entry.Id)==null){Destroy(entry.Id);entries.RemoveAt(i);}
        }
    }
    void Destroy(ZDOID id) {
        if(!SameWorld)return;
        var data=manager.GetZDO(id);if(data==null)return;
        data.SetOwner(ZDOMan.GetSessionID());
        var instance=scene.FindInstance(id);
        if(instance!=null)scene.Destroy(instance);else manager.DestroyZDO(data);
    }
    public void Clear(){closed=true;foreach(var entry in entries)Destroy(entry.Id);entries.Clear();}
}
}
