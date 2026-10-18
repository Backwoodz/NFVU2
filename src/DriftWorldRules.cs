using System;

namespace Rachel350Z {
// Fixed generation constants keep a saved world's terrain stable across loads.
// Future track content is separate from this base height field.
internal static class DriftWorldRules {
    public const float GroundHeight=32f;
    const double WorldRadiusSquared=10500d*10500d;
    public static bool MatchesSeed(string seed)=>string.Equals(seed,"drift",StringComparison.OrdinalIgnoreCase);
    public static bool Applies(World world)=>world!=null&&!world.m_menu&&MatchesSeed(world.m_seedName);
    public static bool Current=>Applies(WorldGenerator.instance?.m_world);
    public static bool Interior(WorldGenerator generator,float x,float z)=>Applies(generator?.m_world)
        &&(double)x*x+(double)z*z<=WorldRadiusSquared;
}
}
