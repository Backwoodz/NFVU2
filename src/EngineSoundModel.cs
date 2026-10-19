using System;

namespace Rachel350Z {
// Optional physical drivetrain/contact measurements. Ordinary WheelCollider
// vehicles continue using the existing acoustic gearbox when this is absent.
public struct VehiclePhysicsTelemetry {
    public float EngineRpm,RearWheelRpm,Skid;
    public int Gear,Contacts;
}
// A gearbox for sound only. It never writes to the vehicle's physics.
public sealed class EngineSoundModel {
    public static readonly int[] Bands={1400,2100,2800,3500,4300,5100,5900,6500};
    static readonly float[] Ratios={4.15f,2.32f,1.62f,1.27f,1f,.79f};
    public static readonly int[] HummerBands={1000,1500,2000,2500,3000,3600,4200,4782};
    readonly bool suv;
    readonly float[] ratios;
    public EngineSoundModel(bool hummer=false){suv=hummer;ratios=hummer?new[]{3.06f,1.63f,1f,.7f}:Ratios;}
    public float Rpm {get;private set;}=950;
    public float Load {get;private set;}
    public float Level {get;private set;}
    public float ShiftGain {get;private set;}=1;
    public int Gear {get;private set;}=1;
    public bool IsShifting => shiftRemaining>0;
    float shiftRemaining,cooldown;
    static float Move(float a,float b,float step)=>Math.Abs(b-a)<=step?b:a+Math.Sign(b-a)*step;
    static float Safe(float value)=>float.IsNaN(value)||float.IsInfinity(value)?0:value;
    public void Step(float dt,bool driving,float forwardSpeed,float wheelRpm,float throttle,bool brake,bool terrain=true,bool burnout=false) {
        dt=DrivingMath.Clamp(Safe(dt),0,.1f);forwardSpeed=Safe(forwardSpeed);wheelRpm=Safe(wheelRpm);throttle=DrivingMath.Clamp(Safe(throttle),-1,1);
        Level=Move(Level,driving?1:0,dt*3);
        cooldown=Math.Max(0,cooldown-dt);shiftRemaining=Math.Max(0,shiftRemaining-dt);
        float speed=Math.Abs(forwardSpeed),rollingRpm=speed*60/(2*(float)Math.PI*(suv?.4364f:.34f));
        float rpmAtWheel=Math.Max(rollingRpm,Math.Min(Math.Abs(wheelRpm),rollingRpm+180));
        bool reverse=forwardSpeed<-.5f || (speed<.5f && throttle<0);
        if(!driving){Gear=1;shiftRemaining=cooldown=0;}
        else if(reverse)Gear=-1;
        else if(Gear<1 || speed<.5f)Gear=1;
        // Compress the audible gears for short Valheim runs. This final drive is
        // acoustic only: neither wheel torque nor actual vehicle speed changes.
        float acousticDrive=3.54f*(terrain?2.4f:1.65f);
        float rawRpm=rpmAtWheel*(Gear<0?3.44f:ratios[Gear-1])*(Gear<0?3.54f:acousticDrive);
        if(driving && !burnout && Gear>0 && speed>2 && cooldown==0) {
            // Wheelspin can raise the note, but cannot race through the gears
            // while the car barely moves or a wheel is airborne.
            float shiftRpm=rollingRpm*ratios[Gear-1]*acousticDrive;
            if(shiftRpm>(suv?4100:5600) && Gear<ratios.Length){Gear++;shiftRemaining=.14f;cooldown=.45f;}
            else if(shiftRpm<(suv?1500:2000) && Gear>1){Gear--;shiftRemaining=.12f;cooldown=.45f;}
            rawRpm=rpmAtWheel*ratios[Gear-1]*acousticDrive;
        }
        float input=driving&&!DrivingMath.DirectionBrake(forwardSpeed,throttle)?Math.Abs(throttle):0;
        float target=driving?Math.Max(950+input*(brake?3300:1700),rawRpm):950;
        if(driving&&burnout){Gear=1;shiftRemaining=cooldown=0;input=1;target=Math.Max(5000,rawRpm);}
        target=DrivingMath.Clamp(target,suv?1000:950,suv?4782:6700);
        Rpm=Move(Rpm,target,dt*(target>Rpm?6000:9500));
        Load=Move(Load,input,dt*5);
        ShiftGain=Move(ShiftGain,shiftRemaining>0?.32f:1,dt*12);
    }
    public void StepPhysics(float dt,bool running,float throttle,VehiclePhysicsTelemetry physics) {
        dt=DrivingMath.Clamp(Safe(dt),0,.1f);
        float input=running?Math.Abs(DrivingMath.Clamp(Safe(throttle),-1,1)):0;
        bool changed=Gear!=physics.Gear;
        Gear=physics.Gear;
        cooldown=0;shiftRemaining=changed?.14f:Math.Max(0,shiftRemaining-dt);
        Level=Move(Level,running?1:0,dt*3);
        float target=DrivingMath.Clamp(Safe(physics.EngineRpm),0,20000);
        Rpm=Move(Rpm,target,dt*(target>Rpm?6000:9500));
        Load=Move(Load,input,dt*5);
        ShiftGain=Move(ShiftGain,shiftRemaining>0?.32f:1,dt*12);
    }
    // Equal-power blending: only the two surrounding recordings are audible.
    public static float BandGain(float rpm,int index)=>BandGain(rpm,index,Bands);
    public static float BandGain(float rpm,int index,int[] bands) {
        if(index<0 || index>=bands.Length)return 0;
        rpm=DrivingMath.Clamp(Safe(rpm),bands[0],bands[bands.Length-1]);
        float gain;
        if(rpm<=bands[index])gain=index==0?1:(rpm-bands[index-1])/(bands[index]-bands[index-1]);
        else gain=index==bands.Length-1?1:(bands[index+1]-rpm)/(bands[index+1]-bands[index]);
        return (float)Math.Sqrt(DrivingMath.Clamp(gain,0,1));
    }
}
}
