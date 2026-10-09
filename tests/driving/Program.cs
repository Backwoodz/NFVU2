using System;
using System.IO;
using Newtonsoft.Json;
using Rachel350Z;
class DrivingTests {
    static int checks;
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
    static bool Near(float a,float b,float tolerance=.001f)=>Math.Abs(a-b)<tolerance;
    static void Main(string[] args) {
        var camera=new ChaseCameraState();camera.Reset(0);camera.Step(.02f,90,90,10,10,2.5f);
        Check(camera.Yaw>0&&camera.Yaw<10,"camera lags a chassis turn instead of snapping with it");
        for(int i=0;i<200;i++)camera.Step(.02f,90,90,10,10,2.5f);
        Check(Near(camera.Yaw,90,.02f),"camera settles behind steady travel");
        camera.Reset(179);camera.Step(.02f,-179,-179,10,10,2.5f);
        Check(Math.Abs(DrivingMath.WrapAngle(camera.Yaw-179))<1,"camera crosses angle wrap by the short route");
        camera.Reset(0);for(int i=0;i<200;i++)camera.Step(.02f,0,45,10,7,2.5f);
        Check(camera.Yaw>20&&camera.Yaw<30,"drift camera partly follows velocity while retaining chassis view");
        camera.Reset(0);for(int i=0;i<200;i++)camera.Step(.02f,0,180,8,-8,2.5f);
        Check(Near(camera.Yaw,0),"reverse does not flip the chase camera");
        camera.Reset(0);camera.Orbit(80);for(int i=0;i<50;i++)camera.Step(.02f,20,20,8,8,2.5f);
        Check(Near(camera.Yaw,80),"manual mouse view remains world anchored during a turn");
        for(int i=0;i<250;i++)camera.Step(.02f,20,20,8,8,2.5f);
        Check(Near(camera.Yaw,20,.02f),"camera gently recentres after manual hold expires");
        camera.Reset(0);camera.Orbit(80);for(int i=0;i<250;i++)camera.Step(.02f,20,0,0,0,2.5f);
        Check(Near(camera.Yaw,80),"stationary inspection does not auto recenter");
        camera.Reset(32);camera.Step(float.NaN,90,90,10,10,2.5f);
        Check(Near(camera.Yaw,32),"invalid frame delta cannot poison camera heading");
        var slow=new ChaseCameraState();var fast=new ChaseCameraState();slow.Reset(0);fast.Reset(0);
        for(int i=0;i<30;i++)slow.Step(1f/30,80,80,10,10,2.5f);
        for(int i=0;i<120;i++)fast.Step(1f/120,80,80,10,10,2.5f);
        Check(Near(slow.Yaw,fast.Yaw,.01f),"camera follow response is stable across frame rates");
        Check(Near(HudMath.Speed(10,false),36)&&Near(HudMath.Speed(10,true),22.369363f),"HUD converts actual speed to KM/H or MPH");
        Check(HudMath.Speed(-10,false)==36&&HudMath.Speed(float.NaN,false)==0,"HUD reverse and invalid speed stay readable");
        Check(HudMath.NeedleAngle(0)==180&&HudMath.NeedleAngle(7500)==420&&HudMath.NeedleAngle(9000)==420,"needle matches original 7500 RPM sweep and clamps overrev");
        Check(HudMath.SpeedNeedleAngle(0,false)==-130&&Near(HudMath.SpeedNeedleAngle(120/3.6f,false),0)&&HudMath.SpeedNeedleAngle(100,false)==130,"speed needle follows actual speed and clamps at dial maximum");
        Check(Near(HudMath.SpeedNeedleAngle(80/2.2369363f,true),0)&&HudMath.SpeedNeedleAngle(float.NaN,true)==-130,"MPH dial has matching labels and invalid speed falls to zero");
        Check(HudMath.Unit(-1)==0&&HudMath.Unit(2)==1&&HudMath.Unit(float.NaN)==0,"nitro fill clamps depleted full and invalid tank levels");
        Check(HudMath.NitroPixelProgress(14,117)<.05f&&HudMath.NitroPixelProgress(160,45)>.9f,"NOS arc fills from lower left toward upper right");
        var data=HudFile.Load(args[0]);Check(data.designs.Length==11&&data.nitro.rgba.Length==256*128*4,"all eleven original gauges and NOS atlas load");
        string temp=Path.Combine(Path.GetTempPath(),"nfvu2-hud-"+Guid.NewGuid().ToString("N")+".json");
        try {
            var texture=data.designs[0].needle;texture.rgba=new byte[1];File.WriteAllText(temp,JsonConvert.SerializeObject(data));
            bool rejected=false;try{HudFile.Load(temp);}catch(InvalidDataException){rejected=true;}
            Check(rejected,"truncated HUD pixel data rejected before native upload");
        }finally{if(File.Exists(temp))File.Delete(temp);}
        Console.WriteLine(checks+" camera/HUD checks passed; Unity drawing and driving require a game test.");
    }
}
