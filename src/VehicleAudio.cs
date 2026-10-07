using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace Rachel350Z {
public sealed class VehicleAudio : IDisposable {
    readonly EngineSoundModel model=new EngineSoundModel();
    readonly List<AudioClip> clips=new List<AudioClip>();
    readonly List<AudioSource> sources=new List<AudioSource>();
    AudioSource[] power,coast;
    AudioSource road,skid,horn;
    GameObject root;
    Rigidbody body;
    WheelCollider[] wheels;
    AudioMixerGroup mixer;
    float roadLevel,skidLevel,hornLevel;
    bool paused,playing,disposed;
    public static VehicleAudio Create(GameObject car,Rigidbody body,WheelCollider[] wheels,Action<string> log) {
        var audio=new VehicleAudio();
        try {audio.Initialize(car,body,wheels,log);return audio;}
        catch(Exception e){audio.Dispose();log("350Z audio unavailable: "+e);return null;}
    }
    void Initialize(GameObject car,Rigidbody carBody,WheelCollider[] carWheels,Action<string> log) {
        body=carBody;wheels=carWheels;
        root=new GameObject("350Z_Sound");root.transform.SetParent(car.transform,false);root.transform.localPosition=new Vector3(0,.65f,0);
        mixer=Resources.FindObjectsOfTypeAll<AudioMixerGroup>().FirstOrDefault(g=>g.name.Equals("SFX",StringComparison.OrdinalIgnoreCase));
        power=new AudioSource[EngineSoundModel.Bands.Length];coast=new AudioSource[power.Length];
        for(int i=0;i<power.Length;i++) {
            power[i]=CreateSource("engine_on_"+EngineSoundModel.Bands[i]+".wav",80);
            coast[i]=CreateSource("engine_off_"+EngineSoundModel.Bands[i]+".wav",80);
        }
        road=CreateSource("road.wav",160);skid=CreateSource("skid.wav",120);horn=CreateSource("horn.wav",60);
        log("350Z audio ready: "+clips.Count+" clips; original NFSU2 350Z power/coast and tyre sounds; synthesized horn. Mixer: "+(mixer!=null?mixer.name:"AudioListener master"));
    }
    AudioSource CreateSource(string name,int priority) {
        var data=WavPcm16.Read(typeof(VehicleAudio).Assembly.GetManifestResourceStream("Rachel350Z.Audio."+name));
        var clip=AudioClip.Create("350Z_"+name,data.Samples.Length/data.Channels,data.Channels,data.SampleRate,false);
        clips.Add(clip);
        if(!clip.SetData(data.Samples,0))throw new InvalidOperationException("Could not load audio: "+name);
        var source=root.AddComponent<AudioSource>();sources.Add(source);
        source.playOnAwake=false;source.clip=clip;source.loop=true;source.volume=0;source.priority=priority;
        source.spatialBlend=1;source.minDistance=6;source.maxDistance=65;source.rolloffMode=AudioRolloffMode.Linear;
        source.dopplerLevel=.25f;source.spread=0;source.outputAudioMixerGroup=mixer;
        return source;
    }
    public void Tick(bool driving,float throttle,bool braking,bool honking,bool blocked,bool terrain,float volume) {
        if(disposed)return;
        if(blocked) {
            if(!paused){foreach(var source in sources)source.Pause();paused=true;}
            return;
        }
        if(paused){if(playing)foreach(var source in sources)source.UnPause();paused=false;}
        float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
        float forward=Vector3.Dot(body.linearVelocity,body.transform.forward),speed=body.linearVelocity.magnitude;
        float wheelRpm=(Mathf.Abs(wheels[2].rpm)+Mathf.Abs(wheels[3].rpm))*.5f;
        model.Step(dt,driving,forward,wheelRpm,throttle,braking);
        if(model.Level>.001f && !playing){foreach(var source in sources)source.Play();playing=true;}
        else if(model.Level<=.001f && playing){foreach(var source in sources)source.Stop();playing=false;}
        if(!playing)return;
        volume=Mathf.Clamp01(volume);
        float powerMix=Mathf.Lerp(.85f,.15f+.85f*model.Load,Mathf.Clamp01(speed/2));
        float engineGain=volume*model.Level*(.5f+.3f*model.Load)*model.ShiftGain;
        for(int i=0;i<power.Length;i++) {
            float gain=EngineSoundModel.BandGain(model.Rpm,i);
            float pitch=Mathf.Clamp(model.Rpm/EngineSoundModel.Bands[i],.5f,2);
            power[i].pitch=pitch;coast[i].pitch=pitch;
            power[i].volume=gain*engineGain*Mathf.Sqrt(powerMix);
            coast[i].volume=gain*engineGain*Mathf.Sqrt(1-powerMix);
        }
        int grounded=0;float slip=0;
        foreach(var wheel in wheels)if(wheel.GetGroundHit(out var hit)) {
            grounded++;
            float value=Mathf.Max(Mathf.Abs(hit.sidewaysSlip),Mathf.Abs(hit.forwardSlip));
            if(!float.IsNaN(value)&&!float.IsInfinity(value))slip=Mathf.Max(slip,value);
        }
        float contact=grounded/4f;
        roadLevel=Mathf.MoveTowards(roadLevel,contact*Mathf.Clamp01((speed-1)/16),dt*3);
        skidLevel=Mathf.MoveTowards(skidLevel,contact*Mathf.Clamp01((slip-.35f)/1.1f)*Mathf.Clamp01((speed-2)/5),dt*5);
        hornLevel=Mathf.MoveTowards(hornLevel,driving&&honking?1:0,dt*18);
        road.volume=volume*model.Level*roadLevel*.24f;road.pitch=Mathf.Lerp(.8f,1.4f,Mathf.Clamp01(speed/25));
        skid.volume=volume*model.Level*skidLevel*(terrain?.18f:.48f);skid.pitch=1;
        horn.volume=volume*model.Level*hornLevel*.8f;
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
