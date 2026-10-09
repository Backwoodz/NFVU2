using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    Func<int,bool> nativeMouseHeld;
    void InitializeBoostInput() {
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("ZInput")).FirstOrDefault(t=>t!=null);
        var method=type?.GetMethod("GetMouseButton",BindingFlags.Public|BindingFlags.Static,null,new[]{typeof(int)},null);
        if(method!=null&&method.ReturnType==typeof(bool))nativeMouseHeld=(Func<int,bool>)Delegate.CreateDelegate(typeof(Func<int,bool>),method);
        Logger.LogInfo("Nitro input: "+(nativeMouseHeld!=null?"Valheim ZInput + legacy mouse fallback":"legacy mouse")+"; Left Ctrl alternative in flight.");
    }
    bool ReadBoostInput() {
        current.nativeBoostMouse=nativeMouseHeld?.Invoke(0)??false;
        current.legacyBoostMouse=Input.GetMouseButton(0);
        current.keyboardBoost=Flying&&Input.GetKey(KeyCode.LeftControl);
        return nitroInput.ReadSources(current.nativeBoostMouse,current.legacyBoostMouse,current.keyboardBoost,driving&&nitroEnabled.Value);
    }
    void TraceFlightBoost() {
        if(!Flying||!driving||current.boostLogCount>=12)return;
        if(!current.nativeBoostMouse&&!current.legacyBoostMouse&&!current.keyboardBoost){current.boostLogTimer=0;return;}
        current.boostLogTimer-=Time.fixedDeltaTime;if(current.boostLogTimer>0)return;
        current.boostLogTimer=1;current.boostLogCount++;
        Logger.LogInfo(string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "Flight nitro: nativeMouse={0}, legacyMouse={1}, ctrl={2}, gated={3}, enabled={4}, phase={5}, input={6:F1}, fuel={7:F2}, boost={8}, speed={9:F1}m/s",
            current.nativeBoostMouse,current.legacyBoostMouse,current.keyboardBoost,nitroHeld,nitroEnabled.Value,current.flight.State.Phase,throttle,ActiveNitro.Charge,ActiveNitro.Boosting,body.linearVelocity.magnitude));
    }
    string FlightBoostLabel=>!nitroEnabled.Value?"NOS AUS":ActiveNitro==null?"NOS nicht verfügbar":
        ActiveNitro.Boosting?"NITRO AKTIV":ActiveNitro.Charge<=0?"NOS leer – loslassen und weiterfliegen":
        nitroInput.NeedsRelease?"Nitro: Taste kurz loslassen":"Nitro: W + LMB / linke Strg";
}
}
