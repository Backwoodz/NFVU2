using System;
using System.Reflection;
using UnityEngine;

namespace Rachel350Z {
// Disabling Player removes it from Character.Instances and stops UpdateBiome.
// Forward only native discovery after the cabin/Transformer has positioned the
// real player; its own timer, known locations, biome cache and banners stay native.
internal static class VehicleDiscovery {
    static readonly Action<Player,float> updateBiome=(Action<Player,float>)Delegate.CreateDelegate(
        typeof(Action<Player,float>),typeof(Player).GetMethod("UpdateBiome",BindingFlags.Instance|BindingFlags.NonPublic,
            null,new[]{typeof(float)},null));
    public static void Step(Player driver) {
        float dt=Time.deltaTime;
        if(driver==null||driver!=Player.m_localPlayer||driver.enabled||!driver.gameObject.activeInHierarchy||
            !driver.IsOwner()||driver.IsDead()||driver.IsTeleporting()||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt)||
            WorldGenerator.instance==null||Minimap.instance==null||MessageHud.instance==null)return;
        updateBiome(driver,Math.Min(dt,1f));
    }
}
}
