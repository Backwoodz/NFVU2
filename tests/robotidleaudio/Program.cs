using System;
using System.IO;
using System.Linq;
using Rachel350Z;
using UnityEngine;
static class Program {
    static int checks;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);checks++;Console.WriteLine("PASS "+message);}
    static void Main() {
        // The fixture is under the ignored test output. Existing file checks in
        // LoadTurbo stay real; only worker-decoded samples and Unity are doubles.
        string folder=Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location),"audio","turbo");Directory.CreateDirectory(folder);
        foreach(var name in new[]{"charge.wav","blowoff_low.wav","blowoff_high.wav"})File.WriteAllBytes(Path.Combine(folder,name),new byte[0]);
        var car=new GameObject("car");var body=new Rigidbody();var wheels=Enumerable.Range(0,4).Select(_=>new WheelCollider()).ToArray();
        var a=new VehicleAudio();foreach(var step in a.LoadSteps(car,body,wheels,_=>{},new SpawnAudio())){}
        var sources=AudioSource.All.ToArray();var engine=sources.Where(s=>s.clip.name.Contains("engine_")).ToArray();
        AudioSource Find(string name)=>sources.Single(s=>s.clip.name=="350Z_"+name);
        var road=Find("road.wav");var skid=Find("skid.wav");var horn=Find("horn.wav");var reverse=Find("reverse_whine_synth");
        var charge=Find("charge.wav");var low=Find("blowoff_low.wav");var high=Find("blowoff_high.wav");
        var other=new AudioSource();
        Check(engine.Length==16&&sources.Length==23&&sources.All(a.Owns)&&!a.Owns(other),"adapter owns exactly its engine/effect/turbo sources for transform capture exclusion");
        void Drive(float throttle=1,bool honk=true)=>a.Tick(true,throttle,false,honk,false,true,1);
        void Idle(bool running=true,bool blocked=false,float volume=1,float progress=1)=>a.TickRobotIdle(running,blocked,volume,progress);
        body.linearVelocity=new Vector3(0,0,12);foreach(var w in wheels){w.rpm=500;w.Hit=new WheelHit{sidewaysSlip=2,forwardSlip=1};}
        for(int i=0;i<150;i++)Drive();
        Check(a.Rpm>2000&&a.Load>.99f&&road.volume>0&&skid.volume>0&&horn.volume>0&&charge.volume>0,"actual driving path establishes revving, tyre, horn and turbo output");
        Drive(0,false);
        Check(low.Playing||high.Playing,"normal throttle release establishes an active turbo tail before robot entry");
        Idle();
        Check(road.volume==0&&skid.volume==0&&horn.volume==0&&reverse.volume==0&&charge.volume==0,"robot idle immediately removes tyre, horn, reverse and turbo charge gains");
        Check(!low.Playing&&!high.Playing&&low.volume==0&&high.volume==0&&a.TurboPressure==0,"robot idle stops turbo one-shot tails and clears charge state");
        for(int i=0;i<80;i++)Idle();
        Check(a.Rpm==950&&a.Load==0&&a.Gear==1&&!a.IsShifting,"revved driving model settles to steady 950 RPM without load or shifting");
        Check(engine.Any(s=>s.volume>0)&&engine.All(s=>s.volume<.18f)&&sources.All(s=>s.minDistance==2&&s.maxDistance==30),"robot retains quiet spatial engine idle at parked attenuation");
        float idle=engine.Sum(s=>s.volume);float[] pitches=engine.Select(s=>s.pitch).ToArray();
        body.linearVelocity=new Vector3(400,-300,-500);foreach(var w in wheels){w.rpm=100000;w.Hit=new WheelHit{sidewaysSlip=100,forwardSlip=100};}
        for(int i=0;i<30;i++)Idle();
        Check(a.Rpm==950&&a.Load==0&&Math.Abs(engine.Sum(s=>s.volume)-idle)<1e-6f&&engine.Select(s=>s.pitch).SequenceEqual(pitches),"robot body velocity, wheel RPM and slip cannot rev the reused engine recordings");
        Check(road.volume==0&&skid.volume==0&&horn.volume==0,"moving robot cannot reintroduce road or skid sound");
        Idle(progress:0);Check(engine.All(s=>s.volume==0),"transform start keeps idle completely inaudible");
        Idle(progress:.72f);Check(engine.All(s=>s.volume==0),"idle remains silent until transformation progress exceeds 72 percent");
        Idle(progress:.86f);Check(Math.Abs(engine.Sum(s=>s.volume)/idle-.5f)<1e-5f,"transformation midpoint of final fade supplies half idle gain");
        Idle(progress:1);Check(Math.Abs(engine.Sum(s=>s.volume)-idle)<1e-6f,"completed transformation reaches full quiet idle gain");
        Idle(volume:.25f);Check(Math.Abs(engine.Sum(s=>s.volume)/idle-.25f)<1e-5f,"master sound volume scales robot idle");
        Idle(volume:0);Check(sources.All(s=>s.volume==0),"zero master volume silences every reused audio output");Idle();
        int starts=sources.Sum(s=>s.Starts);Idle(blocked:true);
        Check(sources.All(s=>!s.Playing)&&engine.All(s=>s.Paused),"pause and focus gating pause engine loops immediately");
        int pauses=sources.Sum(s=>s.Pauses);Idle(blocked:true);
        Check(sources.Sum(s=>s.Pauses)==pauses,"continued blocking does not pause the same sources repeatedly");
        Idle();Check(engine.All(s=>s.Playing&&!s.Paused)&&sources.Sum(s=>s.Starts)==starts&&!low.Playing&&!high.Playing,"resume unpauses the engine without restarting loops or stopped turbo tails");
        for(int i=0;i<30;i++)Idle(running:false);
        Check(sources.All(s=>!s.Playing),"disabling keep-running fades the model to silence and stops every source");
        Idle(running:false,blocked:true);Idle(running:false);
        Check(sources.All(s=>!s.Playing),"pausing an already stopped robot does not start sounds on resume");
        for(int i=0;i<30;i++)Idle();
        Check(engine.All(s=>s.Playing)&&Math.Abs(engine.Sum(s=>s.volume)-idle)<1e-6f,"restoring keep-running restarts the same idle sources at the same gain");
        body.linearVelocity=new Vector3(0,0,12);foreach(var w in wheels)w.rpm=500;
        for(int i=0;i<100;i++)Drive();
        Check(a.Rpm>2000&&a.Load>.99f&&road.volume>0&&skid.volume>0&&horn.volume>0&&charge.volume>0,"returning to normal driving restores RPM response and existing effects");
        body.linearVelocity=new Vector3(0,0,-4);for(int i=0;i<20;i++)Drive(-1,false);
        Check(reverse.volume>0,"normal reverse restores its existing drivetrain whine");Idle();
        Check(reverse.volume==0,"entering robot idle also clears an existing reverse whine");
        for(int i=0;i<80;i++)Idle();
        float robotGain=engine.Sum(s=>s.volume);
        for(int i=0;i<80;i++)a.Tick(false,0,false,false,false,true,1,false,true);
        Check(Math.Abs(robotGain/engine.Sum(s=>s.volume)-.65f)<1e-5f,"robot idle uses 65 percent of the ordinary parked engine volume");
        a.Dispose();a.Dispose();Idle();
        Check(sources.All(s=>!s.Playing&&s.clip==null)&&AudioClip.All.All(c=>c.Destroyed),"disposal is idempotent and releases owned engine and effect clips");
        Check(other.Stops==0&&!other.Destroyed,"robot idle and disposal leave unrelated sources untouched");
        Console.WriteLine(checks+" robot idle adapter checks passed; actual spatial mix requires game listening.");
    }
}
