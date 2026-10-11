using System;
namespace Rachel350Z {
public static class VehicleStorageRules {
    public static int Layout(int layout)=>Math.Max(0,Math.Min(3,layout));
    public static int Width(bool hummer)=>hummer?8:6;
    public static int Rows(bool hummer,int layout)=>hummer?4:4-Layout(layout);
    public static int Capacity(bool hummer,int layout)=>Width(hummer)*Rows(hummer,layout);
    public static bool CanResize(bool hummer,int layout,int stacks,bool inUse)=>!inUse&&stacks>=0&&stacks<=Capacity(hummer,layout);
}
}
