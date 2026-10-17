using System;
namespace Rachel350Z {
// Robot and seated rider occupy more of the view than the car. Keep its HUD
// and camera ray on one elevated viewport point, with room above the rider.
public static class TransformerAimSettings {
    public const float ViewX=.5f,ViewY=.62f,CameraHeight=4.10f,CameraMinDistance=6;
    public static float PivotHeight(float pitch)=>CameraHeight+Math.Max(0,pitch-45)*.055f;
}
}
