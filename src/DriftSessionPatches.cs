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
[HarmonyPatch(typeof(Hud),"TogglePieceSelection")]
internal static class DriftBuildSelection {static bool Prefix()=>!DriftSessionRules.Active;}
[HarmonyPatch(typeof(BuildUi),"OpenBuildMenu")]
internal static class DriftBuildOpen {static bool Prefix()=>!DriftSessionRules.Active;}
[HarmonyPatch(typeof(Player),"SetPlaceMode")]
internal static class DriftBuildPlacement {
    static void Prefix(Player __instance,ref PieceTable buildPieces) {
        if(DriftSessionRules.Active&&__instance==Player.m_localPlayer)buildPieces=null;
    }
}
}
