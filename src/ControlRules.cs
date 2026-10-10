using System;
namespace Rachel350Z {
// Input policy is separate from wheel/flight physics and can be tested without Unity.
public static class ControlRules {
    public static float Axis(float value,float deadzone) {
        if(float.IsNaN(value)||float.IsInfinity(value))return 0;
        deadzone=Math.Max(0,Math.Min(.5f,deadzone));
        return Math.Abs(value)<=deadzone?0:Math.Sign(value)*Math.Min(1,(Math.Abs(value)-deadzone)/(1-deadzone));
    }
    public static int CycleMode(int mode,int direction,bool hummer) {
        do {mode=(mode+direction+4)%4;}while(hummer&&mode==2);return mode;
    }
    public static bool AutomaticTurret(bool flying,bool driving,bool driven,bool parkedAuto)=>flying||!driving&&driven&&parkedAuto;
    public static int MenuMove(int focus,int direction,int count)=>count<=0?0:((focus+direction)%count+count)%count;
    public static void Pedals(float gas,float reverse,float speed,out float throttle,out float brake) {
        gas=Math.Max(0,Math.Min(1,gas));reverse=Math.Max(0,Math.Min(1,reverse));
        brake=reverse>0&&speed>.65f?reverse:gas>0&&speed<-.65f?gas:0;
        throttle=brake>0?0:gas-reverse;
    }
}
public sealed class ReleaseGate {
    bool waiting;
    public void Block(){waiting=true;}
    public bool Allow(bool held){if(waiting&&!held)waiting=false;return !waiting;}
}
}
