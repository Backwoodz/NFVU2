using System;
using System.Collections;
using System.Diagnostics;
using HarmonyLib;
using UnityEngine;

namespace Rachel350Z {
// Prefixes bypass native terrain calculation for the drift interior, including
// pregeneration. Ordinary worlds, the menu and the native world edge pass through.
[HarmonyPatch(typeof(WorldGenerator),"GetBiome",new[]{typeof(float),typeof(float),typeof(float),typeof(bool)})]
internal static class DriftBiome {
    static bool Prefix(WorldGenerator __instance,float wx,float wy,ref Heightmap.Biome __result) {
        if(!DriftWorldRules.Interior(__instance,wx,wy))return true;
        __result=Heightmap.Biome.Meadows;return false;
    }
}
[HarmonyPatch(typeof(WorldGenerator),"GetBiomeHeight")]
internal static class DriftHeight {
    static bool Prefix(WorldGenerator __instance,float wx,float wy,ref Color mask,ref float __result) {
        if(!DriftWorldRules.Interior(__instance,wx,wy))return true;
        mask=Color.black;__result=ParkadeLayout.TerrainHeightAt(wx,wy);return false;
    }
}
// Keep the native outer coroutine's loading/progress/completion/save lifecycle.
// Each location reports zero placement attempts without sampling absent biomes.
// No shared location/vegetation definitions are changed on world switches.
[HarmonyPatch(typeof(ZoneSystem),"GenerateLocationsTimeSliced",new[]{typeof(ZoneSystem.ZoneLocation),typeof(Stopwatch),typeof(ZPackage)})]
internal static class DriftLocationGeneration {
    static IEnumerator Empty(){yield break;}
    static bool Prefix(ZPackage iterationsPkg,ref IEnumerator __result) {
        if(!DriftWorldRules.Current)return true;
        iterationsPkg.Write(0);
        // Match the native worker contract: its caller reads the shared stream
        // immediately after this coroutine, so writing alone leaves it at EOF.
        iterationsPkg.SetPos(0);
        __result=Empty();return false;
    }
}
[HarmonyPatch(typeof(ZoneSystem),"PlaceVegetation")]
internal static class DriftVegetation {
    static bool Prefix()=>!DriftWorldRules.Current;
}
[HarmonyPatch(typeof(ZoneSystem),"PlaceLocations")]
internal static class DriftLocations {
    static bool Prefix()=>!DriftWorldRules.Current;
}
[HarmonyPatch(typeof(ZoneSystem),"GetLocationIcon")]
internal static class DriftStartPoint {
    static bool Prefix(string name,ref Vector3 pos,ref bool __result) {
        if(!DriftWorldRules.Current||!string.Equals(name,Game.instance?.m_StartLocation??"StartTemple",StringComparison.Ordinal))return true;
        // Game.FindSpawnPoint retains its own area-ready check, bed and logout
        // priorities. Only its default start-location lookup is supplied here.
        pos=new Vector3(0,DriftWorldRules.GroundHeight,0);__result=true;return false;
    }
}
}
