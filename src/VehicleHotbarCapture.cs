using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Rachel350Z {
// HotkeyBar reads input independently of PlayerController. Filter only its
// button results so icon refresh, durability and stack counts keep updating.
[HarmonyPatch(typeof(HotkeyBar), "Update")]
static class NativeHotbarNavigation {
    internal static bool Blocked(Player player)=>Plugin.ControlsOwner!=null&&Plugin.ControlsOwner.CaptureHotbar(player);
    internal static bool FilterInput(bool pressed)=>pressed&&!Blocked(Player.m_localPlayer);
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
        var filter=typeof(NativeHotbarNavigation).GetMethod(nameof(FilterInput),BindingFlags.Static|BindingFlags.NonPublic);
        foreach(var instruction in instructions) {
            yield return instruction;
            if(instruction.opcode==OpCodes.Call&&instruction.operand is MethodInfo method&&
                method.DeclaringType.Name=="ZInput"&&method.Name=="GetButtonDown"&&method.ReturnType==typeof(bool))
                yield return new CodeInstruction(OpCodes.Call,filter);
        }
    }
}

[HarmonyPatch(typeof(HotkeyBar), "ElementClicked")]
static class NativeHotbarClick {
    internal static bool Prefix(Player player)=>!NativeHotbarNavigation.Blocked(player);
}

// Covers keyboard bindings and any direct hotbar activation as well.
[HarmonyPatch(typeof(Player), "UseHotbarItem")]
static class NativeHotbarUse {
    internal static bool Prefix(Player __instance)=>!NativeHotbarNavigation.Blocked(__instance);
}
}
