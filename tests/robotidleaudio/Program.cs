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
        Check(engine.Length==32&&sources.Length==39&&sources.All(a.Owns)&&!a.Owns(other),"adapter owns front/rear voices and all effects for transform capture exclusion");
        Check(engine.Select(s=>s.clip).Distinct().Count()==16,"front/rear voices share the original sixteen decoded engine clips");
        Check(GameObject.All.Count(o=>o.Components.OfType<AudioHighPassFilter>().Any())==16&&GameObject.All.Count(o=>o.Components.OfType<AudioLowPassFilter>().Any())==16,
            "each engine/exhaust voice has its own filter without filtering turbo or tyre effects");
        void Drive(float throttle=1,bool honk=true)=>a.Tick(true,throttle,false,honk,false,true,1);
        void Idle(bool running=true,bool blocked=false,float volume=1,float progress=1,bool occupied=true)=>a.TickRobotIdle(running,blocked,volume,progress,occupied);
        body.linearVelocity=new Vector3(0,0,12);foreach(var w in wheels){w.rpm=500;w.Hit=new WheelHit{sidewaysSlip=2,forwardSlip=1};}
        for(int i=0;i<150;i++)Drive();
        Check(engine.All(s=>Math.Abs(s.ScheduledAt-100.03)<1e-8),"front/rear engine voices start on the same DSP time to avoid a doubled playback offset");
        Check(a.Rpm>2000&&a.Load>.99f&&road.volume>0&&skid.volume>0&&horn.volume>0&&charge.volume>0,"actual driving path establishes revving, tyre, horn and turbo output");
        Check(!skid.isVirtual&&!charge.isVirtual&&!road.isVirtual,"32-voice budget preserves audible tyres, road and turbo beside all engine loops");
        Drive(0,false);
        Check(low.Playing||high.Playing,"normal throttle release establishes an active turbo tail before robot entry");
        Check((low.Playing&&!low.isVirtual)||(high.Playing&&!high.isVirtual),"release receives a real voice instead of being crowded out by silent engine bands");
        Idle();
        Check(road.volume==0&&skid.volume==0&&horn.volume==0&&reverse.volume==0&&charge.volume==0,"robot idle immediately removes tyre, horn, reverse and turbo charge gains");
        Check(!low.Playing&&!high.Playing&&low.volume==0&&high.volume==0&&a.TurboPressure==0,"robot idle stops turbo one-shot tails and clears charge state");
        for(int i=0;i<80;i++)Idle();
        Check(a.Rpm==950&&a.Load==0&&a.Gear==1&&!a.IsShifting,"revved driving model settles to steady 950 RPM without load or shifting");
        Check(engine.Any(s=>s.volume>0)&&sources.All(s=>s.minDistance==6&&s.maxDistance==65),"occupied robot retains the occupied car's spatial engine range without parked attenuation");
        float idle=engine.Sum(s=>s.volume);float[] pitches=engine.Select(s=>s.pitch).ToArray();
        body.linearVelocity=new Vector3(400,-300,-500);foreach(var w in wheels){w.rpm=100000;w.Hit=new WheelHit{sidewaysSlip=100,forwardSlip=100};}
        for(int i=0;i<30;i++)Idle();
        Check(a.Rpm==950&&a.Load==0&&Math.Abs(engine.Sum(s=>s.volume)-idle)<1e-6f&&engine.Select(s=>s.pitch).SequenceEqual(pitches),"robot body velocity, wheel RPM and slip cannot rev the reused engine recordings");
        Check(road.volume==0&&skid.volume==0&&horn.volume==0,"moving robot cannot reintroduce road or skid sound");
        int transformStarts=sources.Sum(s=>s.Starts);
        float previousGain=idle/.4f;
        foreach(float progress in new[]{0f,.2f,.4f,.6f,.8f,1f}) {
            Idle(progress:progress);
            float gain=engine.Sum(s=>s.volume);
            Check(engine.All(s=>s.Playing)&&gain<=previousGain+1e-6f&&gain>=idle-1e-6f&&
                engine.Select(s=>s.pitch).SequenceEqual(pitches)&&a.Rpm==950&&a.Load==0,
                "deployment lowers idle monotonically without changing pitch or stopping loops, progress "+progress);
            previousGain=gain;
        }
        Check(sources.Sum(s=>s.Starts)==transformStarts,"transforming in either direction never restarts the existing idle loops");
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
        // Compare with the actual car adapter at rest rather than a duplicated
        // expected gain formula. The same recordings make identical per-source
        // gains/pitches/range establish the baseline for the deployment fade.
        body.linearVelocity=new Vector3(0,0,0);foreach(var w in wheels){w.rpm=0;w.Hit=new WheelHit();}
        foreach(int profile in new[]{0,1,2})foreach(float master in new[]{1f,.37f,0f}) {
            a.SetExhaustProfile(profile);
            for(int i=0;i<80;i++)a.Tick(true,0,false,false,false,true,master);
            var carGains=engine.Select(s=>s.volume).ToArray();var carPitches=engine.Select(s=>s.pitch).ToArray();
            float carRms=(float)Math.Sqrt(engine.Sum(s=>s.volume*s.volume));
            int occupiedStarts=sources.Sum(s=>s.Starts);
            // Known envelope landmarks, traversed in both directions. The
            // adapter baseline above includes real band/exhaust/master mixing.
            foreach(var phase in new[]{new[]{0f,1f},new[]{.2f,1f},new[]{.2001f,1f},new[]{.6f,.7f},new[]{1f,.4f},new[]{.6f,.7f},new[]{.2f,1f},new[]{0f,1f}}) {
                Idle(volume:master,progress:phase[0]);
                Check(engine.Select((s,i)=>Math.Abs(s.volume-carGains[i]*phase[1])<1e-6f).All(v=>v)&&engine.Select(s=>s.pitch).SequenceEqual(carPitches)&&
                    Math.Abs(Math.Sqrt(engine.Sum(s=>s.volume*s.volume))-carRms*phase[1])<1e-6&&
                    sources.All(s=>s.minDistance==6&&s.maxDistance==65)&&a.Rpm==950&&a.Load==0,
                    "occupied transform follows car-based gain fade and unchanged pitch/range, including 20-percent continuity and reverse recovery; exhaust "+profile+", master "+master+", progress "+phase[0]);
            }
            Check(sources.Sum(s=>s.Starts)==occupiedStarts,"switching from seated car idle to transforming robot keeps the existing engine sources playing");
            for(int i=0;i<80;i++)a.Tick(false,0,false,false,false,true,master,false,true);
            var parkedGains=engine.Select(s=>s.volume).ToArray();var parkedPitches=engine.Select(s=>s.pitch).ToArray();
            foreach(var phase in new[]{new[]{0f,1f},new[]{.2f,1f},new[]{1f,.4f},new[]{.2f,1f},new[]{0f,1f}}) {
                Idle(volume:master,occupied:false,progress:phase[0]);
                Check(engine.Select((s,i)=>Math.Abs(s.volume-parkedGains[i]*phase[1])<1e-6f).All(v=>v)&&engine.Select(s=>s.pitch).SequenceEqual(parkedPitches)&&
                    sources.All(s=>s.minDistance==2&&s.maxDistance==30),
                    "parked transform applies the same deployment fade to ordinary parked gain with unchanged pitch/range; exhaust "+profile+", master "+master+", progress "+phase[0]);
            }
        }
        for(int i=0;i<30;i++)Idle(running:false,occupied:false);
        Check(sources.All(s=>!s.Playing),"unoccupied robot respects disabled keep-running and shuts down the same engine loops");
        foreach(var wheel in wheels)wheel.enabled=false;
        body.linearVelocity=new Vector3(0,0,12);
        var physical=new VehiclePhysicsTelemetry{EngineRpm=4200,Gear=2,RearWheelRpm=750,Contacts=4,Skid=.8f};
        for(int i=0;i<120;i++)a.Tick(true,.7f,false,false,false,false,1,physics:physical);
        Check(a.Rpm==4200&&a.Gear==2&&road.volume>0&&skid.volume>0,
            "native drivetrain and contact telemetry drive audio with WheelColliders disabled");
        var front=engine.Where(s=>s.transform.parent.localPosition.z>0).ToArray();
        var rear=engine.Where(s=>s.transform.parent.localPosition.z<0).ToArray();
        Camera.main=new Camera();Camera.main.transform.position=new Vector3(0,0,8);
        physical.EngineRpm=5900;
        for(int i=0;i<120;i++)a.Tick(true,1,false,false,false,false,1,physics:physical);
        float frontCharge=charge.volume;
        Check(front.Length==16&&rear.Length==16&&front.Sum(s=>s.volume)>rear.Sum(s=>s.volume)*1.9f,
            "actual adapter routes the front camera to engine-bay dominance");
        a.Tick(true,0,false,false,false,false,1,physics:physical);float frontRelease=Math.Max(low.volume,high.volume);
        for(int i=0;i<30;i++)a.Tick(true,0,false,false,false,false,1,physics:physical);
        Check(frontRelease>0&&!low.Playing&&!high.Playing&&low.volume==0&&high.volume==0,
            "adapter fades and stops long release samples within 600 milliseconds");
        Camera.main.transform.position=new Vector3(0,0,-8);
        for(int i=0;i<120;i++)a.Tick(true,1,false,false,false,false,1,physics:physical);
        Check(rear.Sum(s=>s.volume)>front.Sum(s=>s.volume)*1.9f&&charge.volume<frontCharge*.51f,
            "actual adapter emphasizes exhaust and softens charge from behind at the same RPM/load");
        a.Tick(true,0,false,false,false,false,1,physics:physical);
        Check(Math.Max(low.volume,high.volume)<frontRelease*.51f,"release tail follows the camera-dependent turbo gain");
        a.Tick(true,0,false,false,false,false,0,physics:physical);
        Check(sources.All(s=>s.volume==0),"master zero also silences front/rear voices and a running release tail");
        Camera.main=null;
        a.Dispose();a.Dispose();Idle();
        Check(sources.All(s=>!s.Playing&&s.clip==null)&&AudioClip.All.All(c=>c.Destroyed),"disposal is idempotent and releases owned engine and effect clips");
        Check(other.Stops==0&&!other.Destroyed,"robot idle and disposal leave unrelated sources untouched");
        int beforeHummer=AudioSource.All.Count;
        var h2=new VehicleAudio(true);foreach(var step in h2.LoadSteps(car,body,wheels,_=>{},new SpawnAudio())){}
        Check(AudioSource.All.Count-beforeHummer==EngineSoundModel.HummerBands.Length*2+4,"Hummer retains its existing voice count without front/rear duplication or turbo");h2.Dispose();
        Console.WriteLine(checks+" robot idle adapter checks passed; actual spatial mix requires game listening.");
    }
}
