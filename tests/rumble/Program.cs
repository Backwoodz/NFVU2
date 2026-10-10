using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Rachel350Z;

// Exercise the production prefix against a native-camera test double.
// Runtime Harmony detouring is only supported in the game's Mono runtime.
public class GameCamera {
    float m_shakeIntensity;
    public bool enabled=true;
    public void AddShake(float value){m_shakeIntensity=value;}
    [MethodImpl(MethodImplOptions.NoInlining)]
    public float GetAttenuatedShakeIntensity(){NativeVehicleCameraShake.Prefix(this,ref m_shakeIntensity);return m_shakeIntensity*m_shakeIntensity*m_shakeIntensity;}
}
namespace Rachel350Z {
public class Plugin {
    public static Plugin ControlsOwner;
    public GameCamera owned;
    public bool CaptureCameraShake(GameCamera camera)=>camera==owned&&!camera.enabled;
}
}
class RumbleTests {
    static int checks;
    static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
    static void Main() {
        try {
            var camera=new GameCamera();camera.AddShake(2);
            Check(camera.GetAttenuatedShakeIntensity()==8,"ordinary game rumble remains intact");
            Plugin.ControlsOwner=new Plugin{owned=camera};
            Check(camera.GetAttenuatedShakeIntensity()==8,"enabled native camera retains normal shake");
            camera.enabled=false;
            Check(camera.GetAttenuatedShakeIntensity()==0,"suspended vehicle camera stops frozen rumble");
            camera.AddShake(3);
            Check(camera.GetAttenuatedShakeIntensity()==0,"new shakes during flight/menu are discarded");
            camera.enabled=true;
            Check(camera.GetAttenuatedShakeIntensity()==0,"exit cannot revive a stored shake");
            camera.AddShake(1);
            Check(camera.GetAttenuatedShakeIntensity()==1,"fresh native rumble works after exit");
            var other=new GameCamera{enabled=false};other.AddShake(2);
            Check(other.GetAttenuatedShakeIntensity()==8,"unrelated disabled cameras remain untouched");
            Plugin.ControlsOwner=null;camera.enabled=false;camera.AddShake(2);
            Check(camera.GetAttenuatedShakeIntensity()==8,"no interception after plugin ownership ends");
        }finally{Plugin.ControlsOwner=null;}
        Console.WriteLine(checks+" rumble checks passed.");
    }
}
