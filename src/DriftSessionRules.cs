namespace Rachel350Z {
// A session override, never a replacement for the player's saved settings.
internal static class DriftSessionRules {
    public const string SpectatorTag="nfvu2_drift_spectator_v1";
    public const float NightFraction=0f;
    public const string Weather="Clear";
    public static bool Active=>DriftWorldRules.Current&&ZNet.IsSinglePlayer;
    public static bool AllowsMode(bool driftWorld,int mode)=>!driftWorld||mode==2;
    public static bool AllowsVehicle(bool driftWorld,bool hummer)=>!driftWorld||!hummer;
    public static bool RemoveCharacter(bool player,bool tame,bool spectator)=>!player&&!tame&&!spectator;
    public static bool RemoveLooseItem(bool valid,bool owner,bool placed)=>valid&&owner&&!placed;
}
}
