using HarmonyLib;
namespace Rachel350Z {
[HarmonyPatch(typeof(SpawnSystem),"UpdateSpawning")]
internal static class DriftRandomSpawns {static bool Prefix()=>!DriftSessionRules.Active;}
[HarmonyPatch(typeof(CreatureSpawner),"UpdateSpawner")]
internal static class DriftCreatureSpawns {static bool Prefix()=>!DriftSessionRules.Active;}
[HarmonyPatch(typeof(SpawnArea),"UpdateSpawn")]
internal static class DriftAreaSpawns {static bool Prefix()=>!DriftSessionRules.Active;}
[HarmonyPatch(typeof(RandEventSystem),"SetRandomEvent")]
internal static class DriftRandomRaids {
    static bool Prefix(RandomEvent ev)=>ev==null||!DriftSessionRules.Active;
}
}
