using System;
using System.IO;
using System.Linq;
using Rachel350Z;
class Program {
    static int checks;
    static void Check(bool valid,string text){if(!valid)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
    sealed class Car {public int ticks;public float distance;}
    static void Main(string[] args) {
        var empty=new Car();var fleet=new VehicleFleet<Car>(empty);var z=new Car{distance=4};var h=new Car{distance=1};
        fleet.Add(z);fleet.Current=z;fleet.Add(h);
        Check(fleet.Count==2&&fleet.Contains(z)&&fleet.Current==z,"adding H2 retains 350Z and selection");
        Check(fleet.Nearest(c=>c.distance)==h,"nearer H2 selected beside two cars");
        h.distance=9;Check(fleet.Nearest(c=>c.distance)==z,"walking to 350Z changes nearest vehicle");
        fleet.Visit(()=>fleet.Current.ticks++);
        Check(z.ticks==1&&h.ticks==1&&fleet.Current==z,"both cars tick independently and driver context restores");
        try{fleet.Visit(()=>{if(fleet.Current==h)throw new InvalidOperationException();});}catch(InvalidOperationException){}
        Check(fleet.Current==z,"tick exception restores selection");
        fleet.Remove(h);Check(fleet.Count==1&&fleet.Contains(z)&&fleet.Current==z,"removing H2 preserves 350Z");
        fleet.Add(h);fleet.Add(new Car());fleet.Add(new Car());
        bool full=false;try{fleet.Add(new Car());}catch(InvalidOperationException){full=true;}
        Check(full&&fleet.Count==4,"capacity refuses fifth without removing existing cars");
        bool duplicate=false;try{fleet.Add(z);}catch(ArgumentException){duplicate=true;}
        Check(duplicate&&fleet.Count==4,"duplicate instance cannot be owned twice");
        fleet.Visit(()=>fleet.Remove(fleet.Current));Check(fleet.Count==0,"world cleanup visits every car despite removals");
        Check(fleet.Nearest(c=>c.distance)==null,"empty garage has no nearest car");
        var spinner=new SpinnerMotion();spinner.Step(.1f,0);Check(spinner.Angle==0&&spinner.Rpm==0,"spinner rests before driving");
        for(int i=0;i<40;i++)spinner.Step(.05f,200);
        Check(spinner.Rpm>100&&spinner.Rpm<200,"spinner gains speed with bearing inertia");
        float initial=spinner.Rpm;for(int i=0;i<20;i++)spinner.Step(.05f,0);
        Check(spinner.Rpm>initial*.7f&&spinner.Rpm<initial,"spinner continues for seconds after tyre stops");
        float angle=spinner.Angle;spinner.Step(float.NaN,500);Check(spinner.Angle==angle,"invalid time cannot corrupt spinner");
        for(int i=0;i<600;i++)spinner.Step(.1f,0);Check(spinner.Rpm==0,"spinner eventually settles at rest");
        for(int i=0;i<60;i++)spinner.Step(.05f,-180);Check(spinner.Rpm<0&&Math.Abs(spinner.Angle)<360,"reverse drives spinner backwards with bounded phase");
        var sound=new EngineSoundModel(true);
        for(int i=0;i<50;i++)sound.Step(.02f,true,0,0,0,false);
        Check(sound.Rpm==1000&&sound.Gear==1,"H2 idles at 1000 RPM");
        int maxGear=1;float maxRpm=0;
        for(int i=0;i<1500;i++){float speed=i/45f;sound.Step(.02f,true,speed,speed*60/(2*(float)Math.PI*.4364f),1,false);maxGear=Math.Max(maxGear,sound.Gear);maxRpm=Math.Max(maxRpm,sound.Rpm);}
        Check(maxGear==4&&maxRpm<=4782,"SUV has four audible gears and its own RPM ceiling");
        for(int i=0;i<100;i++)sound.Step(.02f,true,-4,90,-1,false);
        Check(sound.Gear==-1&&sound.Rpm<=4782,"SUV reverse keeps its own sound range");
        bool mix=true;
        for(float rpm=1000;rpm<=4782;rpm+=7){double power=0;for(int i=0;i<8;i++){var gain=EngineSoundModel.BandGain(rpm,i,EngineSoundModel.HummerBands);power+=gain*gain;}mix&=Math.Abs(power-1)<.00001;}
        Check(mix,"H2 bands blend without volume holes");
        if(!File.Exists(args[0])) {Console.WriteLine(checks+" H2/garage state checks passed; optional local H2 assets not imported.");return;}
        var model=ModelFile.Load(args[0]);
        Check(model.preset=="NFVU2_HUMMER_YELLOW_SPINNER26","actual exported H2 validates in runtime loader");
        Check(model.parts.Any(p=>p.group=="spinner"&&p.name.StartsWith("SPINNER_STYLE01_26_34_SPIN")),"separate original spinner insert imports");
        Check(model.animations==null&&model.trunkLayouts==null&&model.parts.All(p=>!p.name.Contains("ENGINE")&&!p.name.Contains("TRUNK_AUDIO")),"H2 omits engine and trunk upgrade models");
        Check(model.materials.Any(m=>m.color[0]==1&&m.color[1]>.6f&&m.color[1]<.8f&&m.color[2]<.02f),"H2 default body paint is yellow");
        var tyre=model.parts.Single(p=>p.group=="wheel"&&model.materials[p.material].name.StartsWith("78743ca1_"));
        var points=tyre.indices.Select(i=>tyre.vertices[i]).ToArray();
        Check(Math.Abs(points.Max(v=>Math.Sqrt(v.y*v.y+v.z*v.z))-.4364)<.00001,"H2 tyre diameter is 87.28 cm");
        Check(Math.Abs(points.Max(v=>v.x)-points.Min(v=>v.x)-.34)<.00001,"H2 tyres have 34 cm tread width");
        float bead=(float)points.Min(v=>Math.Sqrt(v.y*v.y+v.z*v.z));bool fit=true;
        foreach(float size in new[]{22f,26f,28f}) {float previous=-1;for(int i=0;i<=1000;i++){WheelFitMath.RadialShape(.4364f*i/1000,bead,.4364f,size/26,out var radius,out var slope);fit&=radius>previous&&radius<=.43641f&&slope>0;previous=radius;}}
        Check(fit,"22-28 inch H2 rim adjustment preserves tread and cannot invert sidewall");
        Check(model.parts.Count(p=>p.lightRole=="taillight")==2&&model.parts.Count(p=>p.lightRole=="reverse")==2,"H2 red and white rear lamp surfaces are separate");
        bool audio=true;
        foreach(var rpm in EngineSoundModel.HummerBands){var data=WavPcm16.Read(File.OpenRead(Path.Combine(args[1],"engine_"+rpm+".wav")));audio&=data.Channels==1&&data.Samples.Length>2000&&data.Samples.Any(v=>Math.Abs(v)>.1f);}
        Check(audio,"all eight original H2 engine loops load as non-silent PCM");
        Console.WriteLine(checks+" H2/garage checks passed; Unity rendering, physics and selection still require an in-game test.");
    }
}
