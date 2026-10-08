using System;

namespace Rachel350Z {
public static class WheelFitMath {
    public static float Sanitize(float value,float min,float max,float fallback) =>
        float.IsNaN(value)||float.IsInfinity(value)?fallback:Math.Max(min,Math.Min(max,value));
    // Resize the rim and blend the tyre sidewall to the unchanged tread.
    public static void RadialShape(float radius,float bead,float outside,float scale,out float mapped,out float slope) {
        if(radius<=bead){mapped=radius*scale;slope=scale;return;}
        if(radius>=outside){mapped=radius;slope=1;return;}
        slope=(outside-bead*scale)/(outside-bead);
        mapped=bead*scale+(radius-bead)*slope;
    }
}
}
