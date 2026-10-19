using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;

namespace Rachel350Z {
public sealed class VehicleAudio : IDisposable {
    // Unity ranks priority before audibility. Equal loop priority lets silent
    // RPM bands yield real voices to audible tyres/charge/road recordings.
    const int DrivingLoopPriority=128;
    readonly EngineSoundModel model;
    readonly bool suv;
    readonly int[] bands;
    public VehicleAudio(bool hummer=false){suv=hummer;model=new EngineSoundModel(hummer);bands=hummer?EngineSoundModel.HummerBands:EngineSoundModel.Bands;}
    readonly TurboSoundModel turboModel=new TurboSoundModel();
    readonly VehicleSoundPerspective perspective=new VehicleSoundPerspective();
    public float Rpm => model.Rpm;
    public float Load => model.Load;
    public bool IsShifting => model.IsShifting;
    public int Gear => model.Gear;
    public float TurboPressure => suv?0:turboModel.Pressure;
    readonly List<AudioClip> clips=new List<AudioClip>();
    readonly List<AudioSource> sources=new List<AudioSource>();
    AudioSource[] power,coast;
    AudioSource[] exhaustPower,exhaustCoast;
    GameObject engineEmitter,exhaustEmitter;
    AudioSource road,skid,horn,reverseWhine;
    AudioSource turboCharge,turboLow,turboHigh;
    GameObject root;
    Rigidbody body;
    WheelCollider[] wheels;
    AudioMixerGroup mixer;
    SpawnAudio prepared;
    float roadLevel,skidLevel,hornLevel,turboReleaseGain,turboReleaseAge;
    bool paused,playing,disposed;
    int exhaustProfile;
    float exhaustPitch=1f;
    public bool Owns(AudioSource source)=>sources.Contains(source);
    public void TickRobotIdle(bool running,bool blocked,float volume,float progress,bool occupied=true) {
        // Keep the car mix through the first fifth, then gently lower its idle
        // beneath the reactor. The same phase restores it while folding back.
        // Gain changes never restart the existing engine loops.
        float phase=Mathf.Clamp01((progress-.2f)/.8f);
        float gain=1-.6f*phase*phase*(3-2*phase);
        Tick(false,0,false,false,blocked,true,Mathf.Clamp01(volume)*gain,false,running,false,false,true,occupied);
    }
    public void SetExhaustProfile(int profile){exhaustProfile=ExhaustSoundProfile.Clamp(profile);}
    public IEnumerable<int> LoadSteps(GameObject car,Rigidbody carBody,WheelCollider[] carWheels,Action<string> log,SpawnAudio audio) {
        prepared=audio;
        body=carBody;wheels=carWheels;
        root=new GameObject("350Z_Sound");root.transform.SetParent(car.transform,false);root.transform.localPosition=new Vector3(0,.65f,0);
        mixer=Resources.FindObjectsOfTypeAll<AudioMixerGroup>().FirstOrDefault(g=>g.name.Equals("SFX",StringComparison.OrdinalIgnoreCase));
        if(!suv) {
            engineEmitter=new GameObject("350Z_EngineBaySound");engineEmitter.transform.SetParent(root.transform,false);
            engineEmitter.transform.localPosition=new Vector3(0,.15f,1.1f);
            exhaustEmitter=new GameObject("350Z_ExhaustSound");exhaustEmitter.transform.SetParent(root.transform,false);
            exhaustEmitter.transform.localPosition=new Vector3(0,-.2f,-2f);
            exhaustPower=new AudioSource[bands.Length];exhaustCoast=new AudioSource[bands.Length];
        }
        power=new AudioSource[bands.Length];coast=new AudioSource[power.Length];
        for(int i=0;i<power.Length;i++) {
            if(suv) {
                var path=Path.Combine(Path.GetDirectoryName(typeof(VehicleAudio).Assembly.Location),"audio","hummer","engine_"+bands[i]+".wav");
                var data=prepared.FileData(path);
                power[i]=CreateSource("h2_power_"+bands[i],80,data);coast[i]=CreateSource("h2_coast_"+bands[i],80,data);
            } else {
                power[i]=CreateSource("engine_on_"+bands[i]+".wav",DrivingLoopPriority,emitter:engineEmitter);
                yield return 0;
                coast[i]=CreateSource("engine_off_"+bands[i]+".wav",DrivingLoopPriority,emitter:engineEmitter);
                // Share decoded PCM rather than loading another set of clips.
                exhaustPower[i]=CreateVoice(power[i].clip,DrivingLoopPriority,exhaustEmitter);
                exhaustCoast[i]=CreateVoice(coast[i].clip,DrivingLoopPriority,exhaustEmitter);
            }
            yield return 0;
        }
        road=CreateSource("road.wav",suv?160:DrivingLoopPriority);yield return 0;skid=CreateSource("skid.wav",120);yield return 0;horn=CreateSource("horn.wav",60);yield return 0;
        reverseWhine=CreateSource("reverse_whine_synth",110,prepared.Reverse);reverseWhine.dopplerLevel=0;
        if(!suv)LoadTurbo(log);
        prepared=null;
        log((suv?"H2 original GIN_Hummer engine; ":"350Z original engine; ")+"audio ready: "+clips.Count+" clips; original engine and tyre recordings; synthesized horn and reverse-gear whine. Mixer: "+(mixer!=null?mixer.name:"AudioListener master"));
    }
    void LoadTurbo(Action<string> log) {
        var folder=Path.Combine(Path.GetDirectoryName(typeof(VehicleAudio).Assembly.Location),"audio","turbo");
        var names=new[]{"charge.wav","blowoff_low.wav","blowoff_high.wav"};
        if(!names.All(name=>File.Exists(Path.Combine(folder,name)))) {
            log("350Z turbo unavailable: optional audio/turbo clips missing. Engine audio remains available.");return;
        }
        // Validate the complete optional set before creating any sources.
        int firstSource=sources.Count,firstClip=clips.Count;
        try {
            var data=names.Select(name=>prepared.FileData(Path.Combine(folder,name))).ToArray();
            // Turbo remains full-band: the engine-bay high-pass must not thin
            // the release transient. Its own emitter follows the same position.
            var turboEmitter=new GameObject("350Z_TurboSound");turboEmitter.transform.SetParent(root.transform,false);
            turboEmitter.transform.localPosition=engineEmitter.transform.localPosition;
            turboCharge=CreateSource(names[0],95,data[0],turboEmitter);
            turboLow=CreateSource(names[1],90,data[1],turboEmitter);turboLow.loop=false;
            turboHigh=CreateSource(names[2],90,data[2],turboEmitter);turboHigh.loop=false;
            log("350Z turbo: local samples loaded; subdued lift/shift releases and front/rear listening mix enabled (NFVU2 adaptation).");
        }catch(Exception e){
            for(int i=sources.Count-1;i>=firstSource;i--){UnityEngine.Object.Destroy(sources[i]);sources.RemoveAt(i);}
            for(int i=clips.Count-1;i>=firstClip;i--){UnityEngine.Object.Destroy(clips[i]);clips.RemoveAt(i);}
            turboCharge=turboLow=turboHigh=null;
            log("350Z optional turbo clips rejected: "+e.Message);
        }
    }
    AudioSource CreateSource(string name,int priority,WavData data=null,GameObject emitter=null) {
        if(data==null)data=prepared.Embedded(name);
        var clip=AudioClip.Create("350Z_"+name,data.Samples.Length/data.Channels,data.Channels,data.SampleRate,false);
        clips.Add(clip);
        if(!clip.SetData(data.Samples,0))throw new InvalidOperationException("Could not load audio: "+name);
        return CreateVoice(clip,priority,emitter??root);
    }
    AudioSource CreateVoice(AudioClip clip,int priority,GameObject emitter) {
        bool front=emitter==engineEmitter,rear=emitter==exhaustEmitter;
        if(front||rear) {
            // One source per filtered object: the filter is unambiguously bound
            // to that voice, while all of the PCM remains shared.
            var voice=new GameObject(clip.name+"_Voice");voice.transform.SetParent(emitter.transform,false);
            emitter=voice;
        }
        var source=emitter.AddComponent<AudioSource>();sources.Add(source);
        source.playOnAwake=false;source.clip=clip;source.loop=true;source.volume=0;source.priority=priority;
        source.spatialBlend=1;source.minDistance=6;source.maxDistance=65;source.rolloffMode=AudioRolloffMode.Linear;
        source.dopplerLevel=.25f;source.spread=0;source.outputAudioMixerGroup=mixer;
        // Native audio filters require an AudioSource/AudioListener on their
        // own object. Adding the filter first returns null and aborts loading.
        if(front) {
            var filter=emitter.AddComponent<AudioHighPassFilter>();filter.cutoffFrequency=280;filter.highpassResonanceQ=1;
        } else if(rear) {
            var filter=emitter.AddComponent<AudioLowPassFilter>();filter.cutoffFrequency=1500;filter.lowpassResonanceQ=1;
        }
        return source;
    }
    public void Tick(bool driving,float throttle,bool braking,bool honking,bool blocked,bool terrain,float volume,bool burnout=false,bool parkedRunning=false,bool flying=false,bool turboInputAllowed=true,bool robotIdle=false,bool robotOccupied=true,VehiclePhysicsTelemetry? physics=null) {
        if(disposed)return;
        bool audioOccupied=robotIdle?robotOccupied:driving;
        if(robotIdle) {
            driving=false;throttle=0;braking=honking=burnout=flying=turboInputAllowed=false;
            roadLevel=skidLevel=hornLevel=0;
        }
        if(blocked) {
            if(!paused){foreach(var source in sources)source.Pause();paused=true;}
            turboModel.Reset();turboReleaseGain=0;
            if(turboLow!=null)turboLow.Stop();if(turboHigh!=null)turboHigh.Stop();
            return;
        }
        if(paused){if(playing)foreach(var source in sources)source.UnPause();paused=false;}
        float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
        float forward=Vector3.Dot(body.linearVelocity,body.transform.forward),speed=body.linearVelocity.magnitude;
        float wheelRpm=flying?0:(Mathf.Abs(wheels[2].rpm)+Mathf.Abs(wheels[3].rpm))*.5f;
        bool native=physics.HasValue&&driving&&!flying&&!robotIdle;
        if(native)wheelRpm=Mathf.Abs(physics.Value.RearWheelRpm);
        if(flying){forward=speed=throttle=0;braking=burnout=false;}
        if(!driving){forward=speed=wheelRpm=throttle=0;braking=honking=burnout=false;}
        if(native)model.StepPhysics(dt,true,burnout?1:throttle,physics.Value);
        else model.Step(dt,driving||parkedRunning,forward,wheelRpm,burnout?1:throttle,braking,terrain,burnout);
        turboModel.Step(dt,driving&&!flying&&!suv&&turboInputAllowed,model.Rpm,braking?0:Mathf.Max(0,throttle),model.IsShifting,model.Gear<0,model.Load);
        if(model.Level>.001f && !playing) {
            // Start the shared front/rear recordings on the same DSP boundary
            // so their overlap cannot develop an arbitrary playback offset.
            double start=AudioSettings.dspTime+.03;
            foreach(var source in sources)if(source.loop){if(suv)source.Play();else source.PlayScheduled(start);}
            playing=true;
        }
        else if(model.Level<=.001f && playing){foreach(var source in sources)source.Stop();playing=false;}
        if(!playing)return;
        volume=Mathf.Clamp01(volume);
        if(!audioOccupied)volume*=.55f;
        foreach(var source in sources){source.minDistance=audioOccupied?6:2;source.maxDistance=audioOccupied?65:30;}
        bool directional=!suv;
        var camera=directional?Camera.main:null;
        float listenerDirection=0;
        if(camera!=null) {
            var offset=camera.transform.position-body.transform.position;
            if(offset.magnitude>.01f)listenerDirection=Vector3.Dot(offset,body.transform.forward)/offset.magnitude;
        }
        perspective.Step(dt,listenerDirection);
        float enginePerspective=directional?perspective.EngineGain:1;
        float exhaustPerspective=directional?perspective.ExhaustGain:0;
        float powerMix=Mathf.Lerp(.85f,.15f+.85f*model.Load,Mathf.Clamp01(speed/2));
        powerMix=ExhaustSoundProfile.PowerMix(exhaustProfile,powerMix);
        exhaustPitch=Mathf.MoveTowards(exhaustPitch,ExhaustSoundProfile.Pitch(exhaustProfile),dt*.6f);
        float engineGain=volume*model.Level*(.5f+.3f*model.Load)*model.ShiftGain;
        for(int i=0;i<power.Length;i++) {
            float gain=EngineSoundModel.BandGain(model.Rpm,i,bands);
            float pitch=Mathf.Clamp(model.Rpm/bands[i]*exhaustPitch,.4f,2.2f);
            power[i].pitch=pitch;coast[i].pitch=pitch;
            float on=gain*engineGain*Mathf.Sqrt(powerMix),off=gain*engineGain*Mathf.Sqrt(1-powerMix)*(suv?.65f:1);
            power[i].volume=on*enginePerspective;
            coast[i].volume=off*enginePerspective;
            if(exhaustPower!=null) {
                exhaustPower[i].pitch=coast[i].pitch;exhaustCoast[i].pitch=coast[i].pitch;
                exhaustPower[i].volume=on*exhaustPerspective;exhaustCoast[i].volume=off*exhaustPerspective;
            }
        }
        int grounded=0;float slip=0;
        foreach(var wheel in wheels)if(wheel.enabled&&wheel.GetGroundHit(out var hit)) {
            grounded++;
            float value=Mathf.Max(Mathf.Abs(hit.sidewaysSlip),Mathf.Abs(hit.forwardSlip));
            if(!float.IsNaN(value)&&!float.IsInfinity(value))slip=Mathf.Max(slip,value);
        }
        if(native)grounded=Math.Max(0,Math.Min(4,physics.Value.Contacts));
        float contact=grounded/4f;
        roadLevel=Mathf.MoveTowards(roadLevel,contact*Mathf.Clamp01((speed-1)/16),dt*3);
        float skidTarget=contact*Mathf.Clamp01((slip-.35f)/1.1f)*Mathf.Clamp01((speed-2)/5);
        if(native)skidTarget=contact*Mathf.Clamp01(physics.Value.Skid);
        if(burnout&&grounded>=3)skidTarget=Mathf.Max(skidTarget,Mathf.Clamp01((wheelRpm-30)/180));
        skidLevel=Mathf.MoveTowards(skidLevel,skidTarget,dt*5);
        hornLevel=Mathf.MoveTowards(hornLevel,driving&&honking?1:0,dt*18);
        road.volume=volume*model.Level*roadLevel*.24f;road.pitch=Mathf.Lerp(.8f,1.4f,Mathf.Clamp01(speed/25));
        skid.volume=volume*model.Level*skidLevel*(burnout?.5f:terrain?.18f:.48f);skid.pitch=1;
        reverseWhine.volume=volume*model.Level*ReverseGearSound.Level(driving,model.Gear<0&&!burnout,forward)*.22f;
        reverseWhine.pitch=ReverseGearSound.Pitch(forward);
        horn.volume=volume*model.Level*hornLevel*.8f;
        if(turboCharge!=null) {
            if(!driving||flying||model.Gear<0||!turboInputAllowed) {
                turboReleaseGain=0;turboLow?.Stop();turboHigh?.Stop();
            }
            float turboPerspective=directional?perspective.TurboGain:1;
            turboCharge.volume=volume*model.Level*turboModel.ChargeGain*turboPerspective;
            turboCharge.pitch=turboModel.ChargePitch;
            if(turboModel.BlowOff!=0) {
                turboLow?.Stop();turboHigh?.Stop();
                turboReleaseGain=turboModel.BlowOffGain;
                turboReleaseAge=0;
                var source=turboModel.BlowOff==2?turboHigh:turboLow;
                if(source!=null)source.Play();
            }else turboReleaseAge+=dt;
            float release=volume*model.Level*turboReleaseGain*TurboSoundModel.ReleaseEnvelope(turboReleaseAge)*turboPerspective;
            if(turboLow!=null)turboLow.volume=release;
            if(turboHigh!=null)turboHigh.volume=release;
            if(turboReleaseGain>0&&turboReleaseAge>=TurboSoundModel.ReleaseDuration){turboLow?.Stop();turboHigh?.Stop();turboReleaseGain=0;}
        }
    }
    public void Dispose() {
        if(disposed)return;disposed=true;prepared=null;
        foreach(var source in sources)if(source!=null){source.Stop();source.clip=null;}
        if(root!=null)UnityEngine.Object.Destroy(root);
        foreach(var clip in clips)if(clip!=null)UnityEngine.Object.Destroy(clip);
        sources.Clear();clips.Clear();
    }
}
}
