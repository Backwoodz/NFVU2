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
    public static bool MenuTabVisible(int tab,bool vehicleAvailable)=>vehicleAvailable||tab==10||tab==11;
    public static int MenuNextTab(int tab,int direction,int count,bool vehicleAvailable) {
        for(int i=0;i<count;i++){tab=MenuMove(tab,direction,count);if(MenuTabVisible(tab,vehicleAvailable))return tab;}
        return 10;
    }
    public static int MenuHorizontalNeighbor(int focus,int direction,MenuControlBounds[] items) {
        if(focus<0||focus>=items.Length)return -1;
        var current=items[focus];int best=-1;float distance=float.MaxValue;
        for(int i=0;i<items.Length;i++) {
            float dx=(items[i].X-current.X)*direction;
            if(i==focus||items[i].Region!=current.Region||dx<=0||Math.Abs(items[i].Y-current.Y)>Math.Min(items[i].Height,current.Height)*.5f)continue;
            if(dx<distance){best=i;distance=dx;}
        }
        return best;
    }
    public static void Pedals(float gas,float reverse,float speed,out float throttle,out float brake) {
        gas=Math.Max(0,Math.Min(1,gas));reverse=Math.Max(0,Math.Min(1,reverse));
        brake=reverse>0&&DrivingMath.DirectionBrake(speed,-1)?reverse:gas>0&&DrivingMath.DirectionBrake(speed,1)?gas:0;
        throttle=brake>0?0:gas-reverse;
    }
}
public struct MenuControlBounds {
    public float X,Y,Height;
    public int Region;
    public MenuControlBounds(float x,float y,float height,int region=0){X=x;Y=y;Height=height;Region=region;}
}
public sealed class MenuFocusState {
    public bool Sidebar {get;private set;}=true;
    public bool Editing {get;private set;}
    public void SelectPane(bool sidebar){Sidebar=sidebar;Editing=false;}
    public bool Confirm(bool adjustable) {
        if(Sidebar){SelectPane(false);return false;}
        if(adjustable){Editing=!Editing;return false;}
        return true;
    }
    // True asks the caller to close the menu; other back presses stay inside it.
    public bool Back() {
        if(Editing){Editing=false;return false;}
        if(!Sidebar){SelectPane(true);return false;}
        return true;
    }
}
public sealed class ReleaseGate {
    bool waiting;
    public void Block(){waiting=true;}
    public bool Allow(bool held){if(waiting&&!held)waiting=false;return !waiting;}
}
}
