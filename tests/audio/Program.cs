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
        if(args.Length>1 && Directory.Exists(args[1]))foreach(var path in Directory.GetFiles(args[1],"*.wav")) {
            var data=WavPcm16.Read(File.OpenRead(path));
            Check(data.Channels==1 && data.Samples.Length>1000 && data.Samples.All(s=>!float.IsNaN(s)&&Math.Abs(s)<=.83f) && data.Samples.Any(s=>Math.Abs(s)>.05f),"runtime reader accepts prepared effect: "+Path.GetFileName(path));
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
        var terrainModel=new EngineSoundModel();var streetModel=new EngineSoundModel();int terrainShifts=0,streetShifts=0;
        for(int i=0;i<720;i++) {
            float speed=Math.Min(16,i/60f*2);int terrainGear=terrainModel.Gear,streetGear=streetModel.Gear;
            terrainModel.Step(1f/60,true,speed,WheelRpm(speed),1,false,true);
            streetModel.Step(1f/60,true,speed,WheelRpm(speed),1,false,false);
            if(terrainModel.Gear>terrainGear)terrainShifts++;
            if(streetModel.Gear>streetGear)streetShifts++;
        }
        Check(terrainShifts>=3&&terrainModel.Gear>=4,"short terrain acceleration reaches four audible gears below 60 km/h");
        Check(streetShifts>=2&&streetModel.Gear<terrainModel.Gear,"street sound profile keeps longer gears than terrain");
        int steadyGear=terrainModel.Gear;
        for(int i=0;i<600;i++)terrainModel.Step(1f/60,true,16,WheelRpm(16),.5f,false,true);
        Check(terrainModel.Gear==steadyGear&&!terrainModel.IsShifting,"steady cruising does not hunt between gears");
        var spinningModel=new EngineSoundModel();
        for(int i=0;i<300;i++)spinningModel.Step(1f/60,true,3,2000,1,false,true);
        Check(spinningModel.Gear==1&&spinningModel.Rpm>4000,"wheelspin raises RPM without false upshifts at walking speed");
        var shiftModel=new EngineSoundModel();
        for(int i=0;i<60;i++)shiftModel.Step(.01f,true,0,0,0,false);
        shiftModel.Step(.01f,true,7,WheelRpm(7),1,false);
        Check(shiftModel.IsShifting&&shiftModel.Gear==2,"terrain upshift is audible on a short low-speed run");
        for(int i=0;i<16;i++)shiftModel.Step(.01f,true,7,WheelRpm(7),1,false);
        Check(!shiftModel.IsShifting,"upshift interruption ends within 160 milliseconds");
        for(int i=0;i<120;i++)model.Step(1f/60,true,0,0,0,false);
        Check(model.Gear==1&&model.Rpm==950,"return to first gear and idle after stopping");
        for(int i=0;i<120;i++)model.Step(1f/60,true,-4,WheelRpm(-4),-1,false);
        Check(model.Gear==-1&&model.Rpm>950,"reverse has engine sound without forward shifting");
        for(int i=0;i<60;i++)model.Step(1f/60,false,0,0,0,false);
        Check(model.Level==0&&model.Load==0,"sound fades out after exit");
        model.Step(.016f,true,float.NaN,float.PositiveInfinity,float.NaN,false);CheckFinite(model);Check(true,"invalid wheel samples cannot poison audio state");
        var turbo=new TurboSoundModel();
        for(int i=0;i<120;i++)turbo.Step(1f/60,true,950,1,false,false);
        turbo.Step(1f/60,true,950,0,false,false);
        Check(turbo.Charge==0&&turbo.BlowOff==0,"idle throttle release produces no turbo or blow-off");
        for(int i=0;i<60;i++)turbo.Step(1f/60,true,5900,1,false,false);
        Check(turbo.Charge>.9f,"turbo charge builds under sustained high engine load");
        turbo.Step(1f/60,true,5900,0,false,false);
        Check(turbo.BlowOff==2&&turbo.Charge<.2f,"high-boost throttle release emits one blow-off and consumes charge");
        bool repeated=false;
        for(int i=0;i<120;i++){turbo.Step(1f/60,true,5900,0,false,false);repeated|=turbo.BlowOff!=0;}
        Check(!repeated,"coasting does not repeat blow-off events");
        for(int i=0;i<60;i++)turbo.Step(1f/60,true,3200,1,false,false);
        turbo.Step(1f/60,true,3200,1,true,false);
        Check(turbo.BlowOff==1,"shift at modest boost emits the low-boost variant");
        turbo.Step(1f/60,true,3200,1,true,false);
        Check(turbo.BlowOff==0,"holding the shift window does not retrigger blow-off");
        turbo.Step(1f/60,true,6500,1,false,true);
        Check(turbo.Charge==0&&turbo.BlowOff==0,"reverse suppresses turbo charge and release");
        for(int i=0;i<60;i++)turbo.Step(1f/60,true,5900,1,false,false);
        turbo.Step(1f/60,false,5900,0,false,false);
        Check(turbo.Charge==0&&turbo.BlowOff==0,"exit resets turbo without a spurious release");
        turbo.Step(float.NaN,true,float.PositiveInfinity,float.NaN,false,false);
        Check(!float.IsNaN(turbo.Charge)&&turbo.BlowOff==0,"invalid samples cannot poison turbo state");
        using(var writer=new StreamWriter(args[0])){
            writer.WriteLine("time,rpm,load,level,shift,speed,gear,turbo,blowoff");model=new EngineSoundModel();turbo=new TurboSoundModel();
            for(int i=0;i<900;i++){
                float t=i/60f;float speed=t<2?0:t<10?(t-2)*2:Math.Max(0,16-(t-10)*5);
                float gas=t>=2&&t<10?1:0;
                model.Step(1f/60,t<14,speed,WheelRpm(speed),gas,false);
                turbo.Step(1f/60,t<14,model.Rpm,gas,model.IsShifting,false);
                writer.WriteLine(string.Join(",",new[]{t,model.Rpm,model.Load,model.Level,model.ShiftGain,speed,(float)model.Gear,turbo.Charge,(float)turbo.BlowOff}.Select(v=>v.ToString("R",CultureInfo.InvariantCulture))));
            }
        }
        Console.WriteLine(checks+" audio checks passed against the compiled plugin. Unity playback still requires an in-game check.");
    }
    static void CheckFinite(EngineSoundModel m){if(float.IsNaN(m.Rpm)||float.IsInfinity(m.Rpm)||m.Rpm<950||m.Rpm>6700||m.Gear==0)throw new Exception("Invalid engine sound state");}
}
