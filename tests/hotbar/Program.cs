using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Rachel350Z;

// Unity-free stand-ins; the production Harmony callbacks and IL transformation
// are compiled unchanged. This does not execute Valheim or its HUD rendering.
public class Player { public static Player m_localPlayer; }
public class HotkeyBar { }
public static class ZInput {
    public static bool Pressed;
    public static bool GetButtonDown(string name)=>Pressed;
}
namespace Rachel350Z {
    public class Plugin {
        public static Plugin ControlsOwner;
        public bool Captured;
        public bool CaptureHotbar(Player player)=>Captured&&player!=null&&player==Player.m_localPlayer;
    }
}
static class Program {
    static int checks;
    static void Check(bool value,string name) { if(!value)throw new Exception(name);checks++;Console.WriteLine("PASS "+name); }
    static Func<string,bool> TransformedInput() {
        var original=new[]{new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Call,typeof(ZInput).GetMethod(nameof(ZInput.GetButtonDown))),
            new CodeInstruction(OpCodes.Ret)};
        var code=NativeHotbarNavigation.Transpiler(original).ToArray();
        Check(code.Length==4&&ReferenceEquals(code[0],original[0])&&ReferenceEquals(code[3],original[2]),"only input result is intercepted; original instruction objects are retained");
        var method=new DynamicMethod("HotbarInput",typeof(bool),new[]{typeof(string)},typeof(Program),true);
        var il=method.GetILGenerator();
        foreach(var instruction in code) {
            if(instruction.operand is MethodInfo called)il.Emit(instruction.opcode,called);
            else il.Emit(instruction.opcode);
        }
        return (Func<string,bool>)method.CreateDelegate(typeof(Func<string,bool>));
    }
    static void Main() {
        var read=TransformedInput();var local=new Player();Player.m_localPlayer=local;
        var plugin=new Plugin();Plugin.ControlsOwner=plugin;
        foreach(var button in new[]{"JoyHotbarLeft","JoyHotbarRight","JoyHotbarUse"}) {
            ZInput.Pressed=true;plugin.Captured=false;
            Check(read(button),button+" works on foot");
            plugin.Captured=true;
            Check(!read(button),button+" is blocked in vehicle/menu");
            Check(ZInput.GetButtonDown(button),"native input outside HotkeyBar remains intact");
            plugin.Captured=false;
            Check(read(button),button+" resumes after leaving vehicle/menu");
        }
        plugin.Captured=true;
        Check(!NativeHotbarUse.Prefix(local),"direct keyboard hotbar activation is blocked");
        Check(!NativeHotbarClick.Prefix(local),"click/touch cannot change selection while captured");
        Check(NativeHotbarUse.Prefix(new Player()),"other players are unaffected");
        plugin.Captured=false;
        Check(NativeHotbarUse.Prefix(local)&&NativeHotbarClick.Prefix(local),"keyboard and click resume on foot");
        ZInput.Pressed=false;Check(!read("JoyHotbarUse"),"release never synthesizes an activation");
        Plugin.ControlsOwner=null;ZInput.Pressed=true;
        Check(read("JoyHotbarLeft")&&NativeHotbarUse.Prefix(local),"unloaded mod leaves native controls available");
        Console.WriteLine(checks+" hotbar checks passed.");
    }
}
