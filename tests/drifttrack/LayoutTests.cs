using System;

namespace Rachel350Z {
internal static class LayoutTests {
    static int count;
    static void Check(bool yes,string label) {
        count++;
        if(!yes)throw new Exception("Drift track layout: "+label);
    }
    static void Near(float actual,float expected,float tolerance,string label) =>
        Check(Math.Abs(actual-expected)<=tolerance,label+" ("+actual+" vs "+expected+")");
    public static int Run() {
        count=0;
        var samples=DriftTrackLayout.Samples;
        Check(samples.Count==337,"fixed sample count");
        var bounds=DriftTrackLayout.Bounds;
        Near(bounds.MinX,88,.001f,"west bounds");
        Near(bounds.MaxX,252,.001f,"east bounds");
        Near(bounds.MinZ,14,.001f,"south bounds");
        Near(bounds.MaxZ,110,.001f,"north bounds");
        Near(DriftTrackLayout.TotalLength,335.38052f,.001f,"total length");
        for(int i=0;i<samples.Count;i++) {
            var p=samples[i];
            Check(bounds.Contains(p.X,p.Z),"bounds contain every centreline sample");
            Near(p.TangentX*p.TangentX+p.TangentZ*p.TangentZ,1,.00001f,"unit tangent");
            Check(p.Width>=16&&p.Width<=20,"width limits");
            Near(DriftTrackLayout.Project(p.X,p.Z).DistanceAlong,p.DistanceAlong,.001f,"projection sample station");
            Check(DriftTrackLayout.Contains(p.X,p.Z),"sample inside road mask");
            if(i>0) {
                var previous=samples[i-1];
                Check(p.DistanceAlong>previous.DistanceAlong,"strict station ordering");
                Check(p.DistanceAlong-previous.DistanceAlong<=1.0001f,"maximum station spacing");
            }
        }
        for(int i=0;i<=1000;i++) {
            var p=DriftTrackLayout.AtDistance(DriftTrackLayout.TotalLength*i/1000);
            Check(DriftTrackLayout.DistanceToCenterline(p.X,p.Z)<.0035f,"analytic curve stays within chord error budget");
        }
        Check(!DriftTrackLayout.Contains(0,0),"world spawn stays clear");
        Check(DriftTrackLayout.Contains(88,24),"western rounded cap edge included");
        Check(!DriftTrackLayout.Contains(87.9f,24),"outside western rounded cap excluded");
        Check(DriftTrackLayout.Contains(96,33,1),"positive margin expands road mask");
        Check(!DriftTrackLayout.Contains(96,31.5f,-1),"negative margin insets road mask");
        Near(DriftTrackLayout.AtDistance(-100).X,96,.001f,"negative station clamps to start");
        Near(DriftTrackLayout.AtDistance(100000).X,96,.001f,"large station clamps to end");
        foreach(var invalid in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity}) {
            bool threw=false;
            try {DriftTrackLayout.Project(invalid,0);}
            catch(ArgumentOutOfRangeException) {threw=true;}
            Check(threw,"non-finite projection coordinate rejected");
        }
        return count;
    }
}
}
