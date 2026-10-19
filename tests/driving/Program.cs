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
        DriftView();
        Check(Near(HudMath.Speed(10,false),36)&&Near(HudMath.Speed(10,true),22.369363f),"HUD converts actual speed to KM/H or MPH");
        Check(HudMath.Speed(-10,false)==36&&HudMath.Speed(float.NaN,false)==0,"HUD reverse and invalid speed stay readable");
        Check(HudMath.NeedleAngle(0)==180&&HudMath.NeedleAngle(7500)==420&&HudMath.NeedleAngle(9000)==420,"needle matches original 7500 RPM sweep and clamps overrev");
        Check(HudMath.TurboNeedleAngle(-30)==-165&&HudMath.TurboNeedleAngle(0)==-75&&HudMath.TurboNeedleAngle(30)==45,"turbo needle matches the original vacuum, zero and boost ticks");
        Check(HudMath.TurboNeedleAngle(-15)==-120&&HudMath.TurboNeedleAngle(15)==-15,"vacuum and boost use their respective original sweep halves");
        Check(HudMath.TurboNeedleAngle(-100)==-165&&HudMath.TurboNeedleAngle(100)==45,"pressure needle clamps outside the signed dial range");
        Check(HudMath.TurboNeedleAngle(float.NaN)==-75&&HudMath.TurboNeedleAngle(float.PositiveInfinity)==-75,"invalid pressure returns needle to zero");
        Check(HudMath.Unit(-1)==0&&HudMath.Unit(2)==1&&HudMath.Unit(float.NaN)==0,"nitro fill clamps depleted full and invalid tank levels");
        Check(HudMath.NitroPixelProgress(14,117)<.05f&&HudMath.NitroPixelProgress(160,45)>.9f,"NOS arc fills from lower left toward upper right");
        var data=HudFile.Load(args[0]);Check(data.designs.Length==11&&data.nitro.rgba.Length==256*128*4,"all eleven original gauges and NOS atlas load");
        string temp=Path.Combine(Path.GetTempPath(),"nfvu2-hud-"+Guid.NewGuid().ToString("N")+".json");
        try {
            Check(data.designs[0].turboFill!=null&&data.designs[10].turboLines!=null,"original turbo artwork loads with all gauge styles");
            var turboFill=data.designs[0].turboFill;var turboLines=data.designs[0].turboLines;
            data.designs[0].turboFill=null;data.designs[0].turboLines=null;
            File.WriteAllText(temp,JsonConvert.SerializeObject(data));
            Check(HudFile.Load(temp).designs[0].turboFill==null,"legacy HUD catalog without turbo art supports the pressure fallback");
            data.designs[0].turboFill=turboFill;File.WriteAllText(temp,JsonConvert.SerializeObject(data));
            bool incomplete=false;try{HudFile.Load(temp);}catch(InvalidDataException){incomplete=true;}
            Check(incomplete,"incomplete turbo artwork pair is rejected");
            data.designs[0].turboLines=turboLines;
            var texture=data.designs[0].needle;texture.rgba=new byte[1];File.WriteAllText(temp,JsonConvert.SerializeObject(data));
            bool rejected=false;try{HudFile.Load(temp);}catch(InvalidDataException){rejected=true;}
            Check(rejected,"truncated HUD pixel data rejected before native upload");
        }finally{if(File.Exists(temp))File.Delete(temp);}
        Console.WriteLine(checks+" camera/HUD checks passed; Unity drawing and driving require a game test.");
    }
    static void DriftView() {
        foreach(float direction in new[]{-1f,1f}){
            var camera=new ChaseCameraState();camera.Reset(0);
            for(int i=0;i<300;i++)camera.Step(.02f,0,direction*45,20,14,2.5f,true);
            Check(direction*camera.Yaw>40&&direction*camera.Yaw<46,"sustained drift exposes the car's side while looking along travel, direction="+direction);
            float previous=camera.Yaw,maxJump=0;
            for(int i=0;i<300;i++){
                camera.Step(.02f,0,-direction*45,20,14,2.5f,true);
                maxJump=Math.Max(maxJump,Math.Abs(DrivingMath.WrapAngle(camera.Yaw-previous)));previous=camera.Yaw;
            }
            Check(-direction*camera.Yaw>40&&maxJump<3,"drift switch carries the camera smoothly onto the other flank, direction="+direction);
            for(int i=0;i<300;i++)camera.Step(.02f,0,0,20,20,2.5f,true);
            Check(Math.Abs(camera.Yaw)<.05f,"grip recovery smoothly returns the drift view behind straight travel");
        }
        var road=new ChaseCameraState();var drift=new ChaseCameraState();road.Reset(0);drift.Reset(0);
        road.Step(.1f,60,60,20,20,2.5f);drift.Step(.1f,60,60,20,20,2.5f,true);
        Check(drift.Yaw>0&&drift.Yaw<road.Yaw,"drift profile follows chassis rotation more softly than the normal dynamic view");
        drift.Reset(0);for(int i=0;i<300;i++)drift.Step(.02f,0,88,20,2,2.5f,true);
        Check(drift.Yaw>70&&drift.Yaw<=76.01f,"large slip remains bounded before a side-on camera flip");
        drift.Reset(0);for(int i=0;i<300;i++)drift.Step(.02f,0,180,12,-12,2.5f,true);
        Check(Near(drift.Yaw,0),"reverse never swings the drift camera to the front");
        drift.Reset(0);drift.Orbit(80);for(int i=0;i<50;i++)drift.Step(.02f,20,50,20,14,2.5f,true);
        Check(Near(drift.Yaw,80),"manual inspection holds its chosen world angle in drift mode");
        for(int i=0;i<400;i++)drift.Step(.02f,20,50,20,14,2.5f,true);
        Check(Math.Abs(DrivingMath.WrapAngle(drift.Yaw-48.5f))<.05f,"manual view returns to the drifting travel heading after its hold");
        drift.Reset(0);drift.Orbit(80);for(int i=0;i<300;i++)drift.Step(.02f,20,50,0,0,2.5f,true);
        Check(Near(drift.Yaw,80),"parked drift mode allows free inspection without automatic recentering");
        var slow=new ChaseCameraState();var fast=new ChaseCameraState();slow.Reset(179);fast.Reset(179);
        for(int i=0;i<60;i++)slow.Step(1f/30,-179,-145,20,15,2.5f,true);
        for(int i=0;i<240;i++)fast.Step(1f/120,-179,-145,20,15,2.5f,true);
        Check(Math.Abs(DrivingMath.WrapAngle(slow.Yaw-fast.Yaw))<.01f,"drift follow is stable across frame rates and the angle wrap");
    }
}
