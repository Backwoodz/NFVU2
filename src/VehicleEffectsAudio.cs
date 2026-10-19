using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace Rachel350Z {
// Optional local recordings driven by NFVU2 effect states.
public sealed class VehicleEffectsAudio : IDisposable {
    readonly List<AudioClip> clips=new List<AudioClip>();
    readonly List<AudioSource> sources=new List<AudioSource>();
    GameObject root;
    AudioSource nitro,purge,backfire,overrunBed;
    readonly AudioSource[] overrun=new AudioSource[2];
    readonly ExhaustOverrunModel overrunModel=new ExhaustOverrunModel();
    bool paused,disposed,previousBackfire;
    public bool BackfireAvailable => !disposed&&backfire!=null;
    public bool NitroAvailable => !disposed&&nitro!=null;
    public bool PurgeAvailable => !disposed&&purge!=null;
    public static VehicleEffectsAudio CreateThruster(GameObject car,Action<string> log,SpawnAudio prepared) {
        var result=new VehicleEffectsAudio();
        try {
            result.root=new GameObject("NFVU2_ThrusterAudio");result.root.transform.SetParent(car.transform,false);
            result.root.transform.localPosition=new Vector3(0,.2f,0);
            var mixer=Resources.FindObjectsOfTypeAll<AudioMixerGroup>().FirstOrDefault(g=>g.name.Equals("SFX",StringComparison.OrdinalIgnoreCase));
            var folder=Path.Combine(Path.GetDirectoryName(typeof(VehicleEffectsAudio).Assembly.Location),"audio","effects");
            result.nitro=result.Load(folder,"nitro.wav",true,mixer,log,prepared);
            if(result.nitro==null)result.nitro=result.Load(folder,"purge.wav",true,mixer,log,prepared);
            log("Flight sound: "+(result.nitro!=null?"local NFS gas loop, lowered pitch with thrust modulation":"optional local clips missing"));return result;
        }catch(Exception e){result.Dispose();log("Flight audio unavailable: "+e.Message);return null;}
    }
    public void TickFlight(bool active,float fold,float power,bool blocked,float volume) {
        if(nitro!=null)nitro.pitch=Mathf.Lerp(.65f,1.05f,power);
        Tick(active&&fold>.01f,false,false,blocked,volume*fold*Mathf.Lerp(.45f,.85f,power));
    }
    public static VehicleEffectsAudio Create(GameObject car,Action<string> log,SpawnAudio prepared) {
        var result=new VehicleEffectsAudio();
        try {
            result.root=new GameObject("350Z_EffectsAudio");result.root.transform.SetParent(car.transform,false);
            result.root.transform.localPosition=new Vector3(0,.55f,-1.4f);
            var mixer=Resources.FindObjectsOfTypeAll<AudioMixerGroup>().FirstOrDefault(g=>g.name.Equals("SFX",StringComparison.OrdinalIgnoreCase));
            var folder=Path.Combine(Path.GetDirectoryName(typeof(VehicleEffectsAudio).Assembly.Location),"audio","effects");
            result.nitro=result.Load(folder,"nitro.wav",true,mixer,log,prepared);
            result.purge=result.Load(folder,"purge.wav",true,mixer,log,prepared);
            var exhaustFolder=Path.Combine(Path.GetDirectoryName(typeof(VehicleEffectsAudio).Assembly.Location),"audio","exhaust");
            result.backfire=result.Load(exhaustFolder,"backfire.wav",false,mixer,log,prepared)
                ??result.Load(folder,"backfire.wav",false,mixer,log,prepared);
            result.overrun[0]=result.Load(exhaustFolder,"overrun_01.wav",false,mixer,log,prepared);
            result.overrun[1]=result.Load(exhaustFolder,"overrun_02.wav",false,mixer,log,prepared);
            result.overrunBed=result.Load(exhaustFolder,"overrun_loop.wav",true,mixer,log,prepared);
            if(result.overrunBed!=null)result.overrunBed.priority=100;
            log?.Invoke("350Z effect audio: "+result.sources.Count+" optional local clips loaded.");
            return result;
        }catch(Exception e){result.Dispose();log?.Invoke("350Z effect audio unavailable: "+e.Message);return null;}
    }
    AudioSource Load(string folder,string name,bool loop,AudioMixerGroup mixer,Action<string> log,SpawnAudio prepared) {
        var path=Path.Combine(folder,name);
        if(!File.Exists(path))return null;
        try {
            var data=prepared.FileData(path);
            var clip=AudioClip.Create("350Z_"+name,data.Samples.Length/data.Channels,data.Channels,data.SampleRate,false);clips.Add(clip);
            if(!clip.SetData(data.Samples,0))throw new InvalidDataException("Unable to upload "+name);
            var source=root.AddComponent<AudioSource>();sources.Add(source);
            source.playOnAwake=false;source.loop=loop;source.clip=clip;source.volume=0;
            source.spatialBlend=1;source.minDistance=6;source.maxDistance=65;
            source.rolloffMode=AudioRolloffMode.Linear;source.dopplerLevel=0;source.priority=90;
            source.outputAudioMixerGroup=mixer;
            return source;
        }catch(Exception e){log?.Invoke("350Z optional effect "+name+" rejected: "+e.Message);return null;}
    }
    public void Tick(bool boost,bool purge,bool backfire,bool blocked,float volume) {
        if(disposed)return;
        if(blocked) {
            if(!paused){foreach(var source in sources){source.Stop();source.volume=0;}paused=true;}
            // Do not replay stale one-shot requests after a pause.
            previousBackfire=backfire;
            return;
        }
        paused=false;
        volume=float.IsNaN(volume)||float.IsInfinity(volume)?0:Mathf.Clamp01(volume);
        float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
        Loop(nitro,boost,volume*.65f,dt);
        Loop(this.purge,purge&&!boost,volume*.6f,dt);
        if(this.backfire!=null) {
            this.backfire.volume=volume*.65f;
            if(backfire&&!previousBackfire&&volume>0){this.backfire.Stop();this.backfire.Play();}
        }
        previousBackfire=backfire;
    }
    public void TickExhaust(float dt,bool active,float rpm,float throttle,float speed,bool flame,bool blocked,float volume) {
        if(disposed)return;
        bool audible=active&&!blocked&&Finite(dt)&&dt>0&&Finite(rpm)&&Finite(throttle)&&Finite(speed)&&Finite(volume)&&volume>0;
        overrunModel.Step(dt,audible,rpm,throttle,speed,flame);
        if(overrunBed!=null) {
            if(overrunModel.Level>0)overrunBed.pitch=overrunModel.LoopPitch;
            if(!audible||speed<2) {overrunBed.Stop();overrunBed.volume=0;}
            else Loop(overrunBed,overrunModel.Level>.0001f,Mathf.Clamp01(volume)*overrunModel.Level,Mathf.Min(Mathf.Max(0,dt),.1f));
        }
        if(!audible||throttle>=.18f||rpm<2400||speed<2) {
            foreach(var source in overrun)if(source!=null&&source.isPlaying)source.Stop();
        }
        if(!overrunModel.Pop)return;
        var voice=overrun[overrunModel.Variant]??overrun[1-overrunModel.Variant];
        if(voice==null)return;
        voice.Stop();voice.volume=Mathf.Clamp01(volume)*overrunModel.Gain;voice.pitch=overrunModel.Pitch;voice.Play();
    }
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    static void Loop(AudioSource source,bool active,float volume,float dt) {
        if(source==null)return;
        if(active&&!source.isPlaying)source.Play();
        source.volume=Mathf.MoveTowards(source.volume,active?volume:0,dt*6);
        if(!active&&source.volume<=.0001f&&source.isPlaying)source.Stop();
    }
    public void Dispose() {
        if(disposed)return;disposed=true;
        foreach(var source in sources)if(source!=null){source.Stop();source.clip=null;}
        if(root!=null)UnityEngine.Object.Destroy(root);
        foreach(var clip in clips)if(clip!=null)UnityEngine.Object.Destroy(clip);
        sources.Clear();clips.Clear();
    }
}
}
