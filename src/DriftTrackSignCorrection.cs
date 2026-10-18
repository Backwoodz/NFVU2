using System;

namespace Rachel350Z {
// Only match the authored v1 pose/text. Existing player edits, moved objects
// and dismantled signs are not a request to reconstruct or reposition them.
internal static class DriftTrackSignCorrection {
    internal const string VersionKey="nfvu2_drift_sign_orientation_v1";
    internal static bool LegacyPose(DriftTrackPiece planned,float x,float heightOffset,float z,float yaw,string text) {
        if(planned.Prefab!="sign"||!string.Equals(planned.Text,text,StringComparison.Ordinal))return false;
        if(!Finite(x)||!Finite(heightOffset)||!Finite(z)||!Finite(yaw))return false;
        return Math.Abs(x-planned.X)<=.02f&&Math.Abs(z-planned.Z)<=.02f&&Math.Abs(heightOffset-planned.HeightOffset)<=.02f
            &&Angle(yaw,planned.Yaw-180)<=.5f;
    }
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    static float Angle(float a,float b) {
        double difference=((double)a-b)%360;
        return (float)Math.Abs(difference>180?difference-360:difference< -180?difference+360:difference);
    }
}
}
