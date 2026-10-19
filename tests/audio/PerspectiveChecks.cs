using System;
using System.IO;
using Rachel350Z;

static class PerspectiveChecks {
    public static void Run(Action<bool,string> check,string output) {
        var p=new VehicleSoundPerspective();
        for(int i=0;i<120;i++)p.Step(1f/60,1);
        check(p.EngineGain>p.ExhaustGain*1.9f&&p.TurboGain>.99f,"front listener emphasizes the engine bay and turbo");
        float frontTurbo=p.TurboGain,previous=p.ExhaustGain;
        p.Step(1f/60,-1);
        check(p.EngineGain>p.ExhaustGain&&p.ExhaustGain>previous,"orbiting the camera starts a smooth mix rather than switching instantly");
        for(int i=0;i<120;i++)p.Step(1f/60,-1);
        check(p.ExhaustGain>p.EngineGain*1.9f&&p.TurboGain<frontTurbo*.51f,"rear listener emphasizes exhaust and softens turbo release");
        foreach(float direction in new[]{-1f,0,1,float.NaN,float.PositiveInfinity}) {
            p.Step(.1f,direction);
            check(Math.Abs(p.EngineGain*p.EngineGain+p.ExhaustGain*p.ExhaustGain-1)<1e-5f&&p.TurboGain>=.5f&&p.TurboGain<=1,"perspective stays bounded with constant branch power for direction "+direction);
        }
        var slow=new VehicleSoundPerspective();var fast=new VehicleSoundPerspective();
        for(int i=0;i<30;i++)slow.Step(1f/30,1);
        for(int i=0;i<144;i++)fast.Step(1f/144,1);
        check(Math.Abs(slow.EngineGain-fast.EngineGain)<1e-5f,"listening mix responds equally over time at 30 and 144 FPS");
        using(var writer=new StreamWriter(Path.Combine(Path.GetDirectoryName(output),"perspective-state.csv"))) {
            writer.WriteLine("time,direction,engine_gain,exhaust_gain,turbo_gain,rpm,load,level,shift,charge_gain,charge_pitch,blowoff,blowoff_gain,release_envelope");p=new VehicleSoundPerspective();
            var engine=new EngineSoundModel();var turbo=new TurboSoundModel();float age=1;
            var telemetry=new VehiclePhysicsTelemetry{EngineRpm=2400,Gear=2};
            for(int i=0;i<120;i++){engine.StepPhysics(1f/60,true,0,telemetry);p.Step(1f/60,-1);}
            for(int i=0;i<720;i++) {
                float t=i/60f,direction=t<4?-1:t<8?1:-1;p.Step(1f/60,direction);
                float local=t%4,gas=local<3?1:0;
                telemetry.EngineRpm=local<3?2400+3200*Math.Min(1,local/1.5f):5600-1600*(local-3);
                engine.StepPhysics(1f/60,true,gas,telemetry);
                turbo.Step(1f/60,true,engine.Rpm,gas,engine.IsShifting,false,engine.Load);
                age=turbo.BlowOff!=0?0:age+1f/60;
                writer.WriteLine(FormattableString.Invariant($"{t:R},{direction:R},{p.EngineGain:R},{p.ExhaustGain:R},{p.TurboGain:R},{engine.Rpm:R},{engine.Load:R},{engine.Level:R},{engine.ShiftGain:R},{turbo.ChargeGain:R},{turbo.ChargePitch:R},{turbo.BlowOff},{turbo.BlowOffGain:R},{TurboSoundModel.ReleaseEnvelope(age):R}"));
            }
        }
    }
}
