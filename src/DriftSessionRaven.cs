using HarmonyLib;
using UnityEngine;

namespace Rachel350Z {
// No profile/global tutorial switch: other worlds keep every unread tutorial.
[HarmonyPatch(typeof(Tutorial),"SpawnRaven")]
internal static class DriftTutorialRaven {
    static bool Prefix()=>!DriftSessionRules.Active;
}
internal static class DriftRavenRemoval {
    internal static bool Suppress(Raven raven) {
        if(!DriftSessionRules.Active)return true;
        if(raven!=null) {
            raven.CancelInvoke();
            Chat.instance?.ClearNpcText(raven.gameObject);
            raven.gameObject.SetActive(false);
            Object.Destroy(raven.gameObject);
        }
        return false;
    }
}
[HarmonyPatch(typeof(Raven),"Awake")]
internal static class DriftRavenAwake {
    static void Postfix(Raven __instance)=>DriftRavenRemoval.Suppress(__instance);
}
[HarmonyPatch(typeof(Raven),"Update")]
internal static class DriftRavenUpdate {
    static bool Prefix(Raven __instance)=>DriftRavenRemoval.Suppress(__instance);
}
}
