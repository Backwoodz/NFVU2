using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
namespace Rachel350Z {
[HarmonyPatch(typeof(Projectile),nameof(Projectile.Setup))]
static class HordeProjectileSetup {
    static void Prefix(Projectile __instance,Character owner) {
        var marker=owner!=null?owner.GetComponent<HordeEnemyMarker>():null;
        if(marker==null)return;
        // Keep native projectile flight/impact FX, without persistent ice obstacles or item drops.
        __instance.m_spawnOnHit=null;__instance.m_randomSpawnOnHit=new List<GameObject>();__instance.m_respawnItemOnHit=false;
        marker.OnProjectile?.Invoke(__instance);
    }
}
public partial class Plugin {
    readonly List<ZDOID> hordeProjectiles=new List<ZDOID>();
    void RegisterHordeProjectile(Projectile projectile) {
        var data=projectile.GetComponent<ZNetView>()?.GetZDO();if(data==null)return;
        data.Persistent=false;hordeProjectiles.Add(data.m_uid);
        if(hordeProjectiles.Count>128){DestroyHordeProjectile(hordeProjectiles[0]);hordeProjectiles.RemoveAt(0);}
    }
    void DestroyHordeProjectile(ZDOID id) {
        if(hordeManager==null||hordeManager!=ZDOMan.instance||hordeScene==null||hordeScene!=ZNetScene.instance)return;
        var data=hordeManager.GetZDO(id);if(data==null)return;data.SetOwner(ZDOMan.GetSessionID());
        var obj=hordeScene.FindInstance(id);if(obj!=null)hordeScene.Destroy(obj);else hordeManager.DestroyZDO(data);
    }
    void ClearHordeProjectiles(){foreach(var id in hordeProjectiles)DestroyHordeProjectile(id);hordeProjectiles.Clear();}
    void TickHordeProjectiles(){for(int i=hordeProjectiles.Count-1;i>=0;i--)if(hordeManager?.GetZDO(hordeProjectiles[i])==null)hordeProjectiles.RemoveAt(i);}
}
}
