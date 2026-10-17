// The game's Mono Harmony/MonoMod cannot initialize AccessTools on the test
// runner's CoreCLR. Bind actual production callbacks directly instead. This
// tests callback/restore semantics, not the native Harmony detour engine.
using System;
using System.Reflection;
using System.Reflection.Emit;
using Rachel350Z;
namespace HarmonyLib {
[AttributeUsage(AttributeTargets.Class)]
public sealed class HarmonyPatch:Attribute {public HarmonyPatch(Type type,string method){}}
public sealed class Harmony {
    public static bool Installed;
    public Harmony(string id){}
    public void PatchAll(Type patch){NativeDrawHarness.Bind(patch);Installed=true;}
    public void UnpatchSelf(){Installed=false;}
}
public static class AccessTools {
    public delegate ref F FieldRef<T,F>(T instance);
    public static FieldRef<T,F> FieldRefAccess<T,F>(FieldInfo field) {
        var method=new DynamicMethod("TestFieldRef_"+field.Name,typeof(F).MakeByRefType(),
            new[]{typeof(T)},typeof(AccessTools).Module,true);
        var il=method.GetILGenerator();il.Emit(OpCodes.Ldarg_0);il.Emit(OpCodes.Ldflda,field);il.Emit(OpCodes.Ret);
        return (FieldRef<T,F>)method.CreateDelegate(typeof(FieldRef<T,F>));
    }
}
}
internal static class NativeDrawHarness {
    internal delegate void PrefixCallback(InstanceRenderer instance,out TransformerFootstepGrass.DrawState state);
    internal static PrefixCallback Prefix;
    internal static Action<TransformerFootstepGrass.DrawState> Postfix;
    internal static Func<Exception,TransformerFootstepGrass.DrawState,Exception> Finalizer;
    internal static void Bind(Type patch) {
        const BindingFlags flags=BindingFlags.Static|BindingFlags.NonPublic;
        Prefix=(PrefixCallback)patch.GetMethod("Prefix",flags).CreateDelegate(typeof(PrefixCallback));
        Postfix=(Action<TransformerFootstepGrass.DrawState>)patch.GetMethod("Postfix",flags).CreateDelegate(typeof(Action<TransformerFootstepGrass.DrawState>));
        Finalizer=(Func<Exception,TransformerFootstepGrass.DrawState,Exception>)patch.GetMethod("Finalizer",flags).CreateDelegate(typeof(Func<Exception,TransformerFootstepGrass.DrawState,Exception>));
    }
}
