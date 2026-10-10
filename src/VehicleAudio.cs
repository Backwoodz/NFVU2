using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;

namespace Rachel350Z {
public sealed class VehicleAudio : IDisposable {
    readonly EngineSoundModel model;
    readonly bool suv;
    readonly int[] bands;
    public VehicleAudio(bool hummer=false){suv=hummer;model=new EngineSoundModel(hummer);bands=hummer?EngineSoundModel.HummerBands:EngineSoundModel.Bands;}
    readonly TurboSoundModel turboModel=new TurboSoundModel();
    public float Rpm => model.Rpm;
    public float Load => model.Load;
    public bool IsShifting => model.IsShifting;
    public int Gear => model.Gear;
    public float TurboPressure => suv?0:turboModel.Pressure;
    readonly List<AudioClip> clips=new List<AudioClip>();
    readonly List<AudioSource> sources=new List<AudioSource>();
    AudioSource[] power,coast;
    AudioSource road,skid,horn,reverseWhine;
    AudioSource turboCharge,turboLow,turboHigh;
    GameObject root;
    Rigidbody body;
    WheelCollider[] wheels;
    AudioMixerGroup mixer;
    SpawnAudio prepared;
    float roadLevel,skidLevel,hornLevel,turboReleaseGain;
    bool paused,playing,disposed;
    int exhaustProfile;
    float exhaustPitch=1f;
    public void SetExhaustProfile(int profile){exhaustProfile=ExhaustSoundProfile.Clamp(profile);}
    public IEnumerable<int> LoadSteps(GameObject car,Rigidbody carBody,WheelCollider[] carWheels,Action<string> log,SpawnAudio audio) {
        prepared=audio;
        body=carBody;wheels=carWheels;
        root=new GameObject("350Z_Sound");root.transform.SetParent(car.transform,false);root.transform.localPosition=new Vector3(0,.65f,0);
        mixer=Resources.FindObjectsOfTypeAll<AudioMixerGroup>().FirstOrDefault(g=>g.name.Equals("SFX",StringComparison.OrdinalIgnoreCase));
        power=new AudioSource[bands.Length];coast=new AudioSource[power.Length];
        for(int i=0;i<power.Length;i++) {
            if(suv) {
                var path=Path.Combine(Path.GetDirectoryName(typeof(VehicleAudio).Assembly.Location),"audio","hummer","engine_"+bands[i]+".wav");
                var data=prepared.FileData(path);
                power[i]=CreateSource("h2_power_"+bands[i],80,data);coast[i]=CreateSource("h2_coast_"+bands[i],80,data);
            } else {
                power[i]=CreateSource("engine_on_"+bands[i]+".wav",80);
                yield return 0;
                coast[i]=CreateSource("engine_off_"+bands[i]+".wav",80);
            }
            yield return 0;
        }
        road=CreateSource("road.wav",160);yield return 0;skid=CreateSource("skid.wav",120);yield return 0;horn=CreateSource("horn.wav",60);yield return 0;
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
            turboCharge=CreateSource(names[0],95,data[0]);
            turboLow=CreateSource(names[1],90,data[1]);turboLow.loop=false;
            turboHigh=CreateSource(names[2],90,data[2]);turboHigh.loop=false;
            log("350Z turbo: local samples loaded; acoustic boost/event approximation enabled.");
        }catch(Exception e){
            for(int i=sources.Count-1;i>=firstSource;i--){UnityEngine.Object.Destroy(sources[i]);sources.RemoveAt(i);}
            for(int i=clips.Count-1;i>=firstClip;i--){UnityEngine.Object.Destroy(clips[i]);clips.RemoveAt(i);}
            turboCharge=turboLow=turboHigh=null;
            log("350Z optional turbo clips rejected: "+e.Message);
        }
    }
    AudioSource CreateSource(string name,int priority,WavData data=null) {
        if(data==null)data=prepared.Embedded(name);
        var clip=AudioClip.Create("350Z_"+name,data.Samples.Length/data.Channels,data.Channels,data.SampleRate,false);
        clips.Add(clip);
        if(!clip.SetData(data.Samples,0))throw new InvalidOperationException("Could not load audio: "+name);
        var source=root.AddComponent<AudioSource>();sources.Add(source);
        source.playOnAwake=false;source.clip=clip;source.loop=true;source.volume=0;source.priority=priority;
        source.spatialBlend=1;source.minDistance=6;source.maxDistance=65;source.rolloffMode=AudioRolloffMode.Linear;
        source.dopplerLevel=.25f;source.spread=0;source.outputAudioMixerGroup=mixer;
        return source;
    }
    public void Tick(bool driving,float throttle,bool braking,bool honking,bool blocked,bool terrain,float volume,bool burnout=false,bool parkedRunning=false,bool flying=false,bool turboInputAllowed=true) {
        if(disposed)return;
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
        if(flying){forward=speed=throttle=0;braking=burnout=false;}
        if(!driving){forward=speed=wheelRpm=throttle=0;braking=honking=burnout=false;}
        model.Step(dt,driving||parkedRunning,forward,wheelRpm,burnout?1:throttle,braking,terrain,burnout);
        turboModel.Step(dt,driving&&!flying&&!suv&&turboInputAllowed,model.Rpm,braking?0:Mathf.Max(0,throttle),model.IsShifting,model.Gear<0,model.Load);
        if(model.Level>.001f && !playing){foreach(var source in sources)if(source.loop)source.Play();playing=true;}
        else if(model.Level<=.001f && playing){foreach(var source in sources)source.Stop();playing=false;}
        if(!playing)return;
        volume=Mathf.Clamp01(volume);
        if(!driving)volume*=.55f;
        foreach(var source in sources){source.minDistance=driving?6:2;source.maxDistance=driving?65:30;}
        float powerMix=Mathf.Lerp(.85f,.15f+.85f*model.Load,Mathf.Clamp01(speed/2));
        powerMix=ExhaustSoundProfile.PowerMix(exhaustProfile,powerMix);
        exhaustPitch=Mathf.MoveTowards(exhaustPitch,ExhaustSoundProfile.Pitch(exhaustProfile),dt*.6f);
        float engineGain=volume*model.Level*(.5f+.3f*model.Load)*model.ShiftGain;
        for(int i=0;i<power.Length;i++) {
            float gain=EngineSoundModel.BandGain(model.Rpm,i,bands);
            float pitch=Mathf.Clamp(model.Rpm/bands[i]*exhaustPitch,.4f,2.2f);
            power[i].pitch=pitch;coast[i].pitch=pitch;
            power[i].volume=gain*engineGain*Mathf.Sqrt(powerMix);
            coast[i].volume=gain*engineGain*Mathf.Sqrt(1-powerMix)*(suv?.65f:1);
        }
        int grounded=0;float slip=0;
        foreach(var wheel in wheels)if(wheel.enabled&&wheel.GetGroundHit(out var hit)) {
            grounded++;
            float value=Mathf.Max(Mathf.Abs(hit.sidewaysSlip),Mathf.Abs(hit.forwardSlip));
            if(!float.IsNaN(value)&&!float.IsInfinity(value))slip=Mathf.Max(slip,value);
        }
        float contact=grounded/4f;
        roadLevel=Mathf.MoveTowards(roadLevel,contact*Mathf.Clamp01((speed-1)/16),dt*3);
        float skidTarget=contact*Mathf.Clamp01((slip-.35f)/1.1f)*Mathf.Clamp01((speed-2)/5);
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
            turboCharge.volume=volume*model.Level*turboModel.ChargeGain;
            turboCharge.pitch=turboModel.ChargePitch;
            if(turboModel.BlowOff!=0) {
                turboLow?.Stop();turboHigh?.Stop();
                turboReleaseGain=turboModel.BlowOffGain;
                var source=turboModel.BlowOff==2?turboHigh:turboLow;
                if(source!=null){source.volume=volume*model.Level*turboReleaseGain;source.Play();}
            }
            if(turboLow!=null)turboLow.volume=volume*model.Level*turboReleaseGain;
            if(turboHigh!=null)turboHigh.volume=volume*model.Level*turboReleaseGain;
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
