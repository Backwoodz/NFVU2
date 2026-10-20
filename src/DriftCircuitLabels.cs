using System;
namespace Rachel350Z {
// Upgrade only untouched, authored v1 labels. Text or pose edits belong to the player.
public static class DriftCircuitLabels {
    public static string Upgrade(string text) {
        if(text=="DRIFT TEST | 335 m | 16-20 m")return "NFVU2 DRIFT | RUNDE / LAP 1.49 km";
        if(text=="ENDE / END")return "RUNDKURS / CIRCUIT | WEITER";
        return null;
    }
    public static bool Matches(DriftTrackPiece piece,string text,float x,float y,float z,float yaw) {
        if(piece.Prefab!="sign"||piece.Text!=text||Upgrade(text)==null)return false;
        if(float.IsNaN(x)||float.IsNaN(y)||float.IsNaN(z)||float.IsNaN(yaw)
            ||float.IsInfinity(x)||float.IsInfinity(y)||float.IsInfinity(z)||float.IsInfinity(yaw))return false;
        double delta=(yaw-piece.Yaw)%360;if(delta>180)delta-=360;if(delta< -180)delta+=360;
        return Math.Abs(x-piece.X)<=.02f&&Math.Abs(y-piece.HeightOffset)<=.02f&&Math.Abs(z-piece.Z)<=.02f&&Math.Abs(delta)<=.5;
    }
}
}
