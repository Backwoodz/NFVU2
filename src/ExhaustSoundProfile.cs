namespace Rachel350Z {
// Alternative treatments of the installed original engine recordings.
// These are NFVU2 presets, not recovered NFS exhaust upgrade sound banks.
public static class ExhaustSoundProfile {
    public static int Clamp(int profile)=>profile<0||profile>2?0:profile;
    public static float Pitch(int profile)=>Clamp(profile)==1?.86f:Clamp(profile)==2?1.10f:1f;
    public static float PowerMix(int profile,float original)=>Clamp(profile)==1
        ?DrivingMath.Clamp(original*.85f,0,1):Clamp(profile)==2?DrivingMath.Clamp(original+.16f,0,1):original;
}
}
