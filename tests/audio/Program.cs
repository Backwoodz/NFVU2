using System;
using System.IO;
using System.Linq;
using System.Globalization;
using Rachel350Z;
class AudioTests {
    static int checks;
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
    static void Invalid(byte[] bytes,string name){
        try{WavPcm16.Read(new MemoryStream(bytes));}catch(InvalidDataException){Check(true,name);return;}
        throw new Exception("Accepted "+name);
    }
    static byte[] Resource(string name){using(var stream=typeof(WavPcm16).Assembly.GetManifestResourceStream(name))using(var m=new MemoryStream()){stream.CopyTo(m);return m.ToArray();}}
    static float WheelRpm(float speed)=>Math.Abs(speed)*60/(2*(float)Math.PI*.34f);
    static void Main(string[] args){
        var assembly=typeof(WavPcm16).Assembly;
        var names=assembly.GetManifestResourceNames().Where(n=>n.StartsWith("Rachel350Z.Audio.")).ToArray();
        Check(names.Length==19,"all 19 clips embedded in the actual plugin");
        foreach(var name in names){
            var data=WavPcm16.Read(assembly.GetManifestResourceStream(name));
            Check(data.Channels==1 && data.Samples.Length>2000 && data.Samples.All(s=>!float.IsNaN(s)&&Math.Abs(s)<=.83f) && data.Samples.Any(s=>Math.Abs(s)>.05f),"valid non-silent PCM: "+name);
        }
        var bytes=Resource(names[0]);Invalid(bytes.Take(30).ToArray(),"reject truncated WAV");
        var wrong=(byte[])bytes.Clone();wrong[0]=0;Invalid(wrong,"reject missing RIFF");
        wrong=(byte[])bytes.Clone();wrong[34]=24;Invalid(wrong,"reject unexpected sample depth");
        wrong=(byte[])bytes.Clone();wrong[22]=2;Invalid(wrong,"reject unexpected channels");
        wrong=(byte[])bytes.Clone();wrong[4]=255;wrong[5]=255;wrong[6]=255;wrong[7]=255;Invalid(wrong,"reject oversized RIFF");
        bool bandsOk=true;
        for(float rpm=950;rpm<=6700;rpm+=7){float sum=0;int active=0;for(int i=0;i<8;i++){float g=EngineSoundModel.BandGain(rpm,i);sum+=g*g;if(g>0)active++;}bandsOk&=Math.Abs(sum-1)<.0001f&&active<=2;}
        Check(bandsOk,"continuous equal-power blending over the full RPM range");
        var model=new EngineSoundModel();
        for(int i=0;i<60;i++)model.Step(1f/60,true,0,0,0,false);
        Check(model.Rpm==950&&model.Level==1,"stable idle after entry");
        for(int i=0;i<60;i++)model.Step(1f/60,true,0,0,1,true);
        Check(model.Rpm>4000&&model.Gear==1,"revving while holding the brake");
        bool shifted=false;float priorRpm=model.Rpm;
        for(int i=0;i<1000;i++){float speed=Math.Min(34,i*.04f);int gear=model.Gear;model.Step(1f/60,true,speed,WheelRpm(speed),1,false);if(model.Gear>gear)shifted=true;CheckFinite(model);}
        Check(shifted&&model.Gear>=3,"automatic upshifts during acceleration");
        for(int i=0;i<120;i++)model.Step(1f/60,true,0,0,0,false);
        Check(model.Gear==1&&model.Rpm==950,"return to first gear and idle after stopping");
        for(int i=0;i<120;i++)model.Step(1f/60,true,-4,WheelRpm(-4),-1,false);
        Check(model.Gear==-1&&model.Rpm>950,"reverse has engine sound without forward shifting");
        for(int i=0;i<60;i++)model.Step(1f/60,false,0,0,0,false);
        Check(model.Level==0&&model.Load==0,"sound fades out after exit");
        model.Step(.016f,true,float.NaN,float.PositiveInfinity,float.NaN,false);CheckFinite(model);Check(true,"invalid wheel samples cannot poison audio state");
        using(var writer=new StreamWriter(args[0])){
            writer.WriteLine("time,rpm,load,level,shift,speed,gear");model=new EngineSoundModel();
            for(int i=0;i<900;i++){
                float t=i/60f;float speed=t<2?0:t<10?(t-2)*3.8f:Math.Max(0,30.4f-(t-10)*8);
                float gas=t>=2&&t<10?1:0;
                model.Step(1f/60,t<14,speed,WheelRpm(speed),gas,false);
                writer.WriteLine(string.Join(",",new[]{t,model.Rpm,model.Load,model.Level,model.ShiftGain,speed,(float)model.Gear}.Select(v=>v.ToString("R",CultureInfo.InvariantCulture))));
            }
        }
        Console.WriteLine(checks+" audio checks passed against the compiled plugin. Unity playback still requires an in-game check.");
    }
    static void CheckFinite(EngineSoundModel m){if(float.IsNaN(m.Rpm)||float.IsInfinity(m.Rpm)||m.Rpm<950||m.Rpm>6700||m.Gear==0)throw new Exception("Invalid engine sound state");}
}
