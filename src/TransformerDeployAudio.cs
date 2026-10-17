using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace Rachel350Z {
// One pre-mixed local PCM texture, synchronized to authoritative deployment
// progress. Unity objects are created and used only on the main thread.
public sealed class TransformerDeployAudio : IDisposable {
    readonly Transform car;
    readonly Action<string> log;
    TransformerDeploySoundModel model;
    GameObject root;
    AudioClip clip;
    AudioSource source;
    int sampleRate,sampleCount;
    bool started,paused,disposed;
    public TransformerDeployAudio(GameObject vehicle,WavData prepared,Action<string> log) {
        car=vehicle.transform;this.log=log;
        if(prepared==null){log?.Invoke("Transformer deployment unavailable: optional audio/transformer/deploy.wav missing.");return;}
        try {
            if(prepared.Channels!=1||prepared.SampleRate<8000||prepared.SampleRate>96000||prepared.Samples==null||prepared.Samples.Length==0||prepared.Samples.Length>prepared.SampleRate*6)
                throw new InvalidOperationException("Expected bounded mono deployment audio");
            foreach(float sample in prepared.Samples)if(!Finite(sample)||Math.Abs(sample)>1)throw new InvalidOperationException("Invalid deployment sample");
            sampleRate=prepared.SampleRate;sampleCount=prepared.Samples.Length;
            model=new TransformerDeploySoundModel((float)sampleCount/sampleRate);
            clip=AudioClip.Create("Transformer_Deploy",sampleCount,1,sampleRate,false);
            if(!clip.SetData(prepared.Samples,0))throw new InvalidOperationException("Could not upload deployment samples");
            root=new GameObject("Transformer_Deployment");root.transform.SetParent(car,false);
            source=root.AddComponent<AudioSource>();
            source.playOnAwake=false;source.loop=false;source.clip=clip;source.pitch=1;source.volume=0;source.priority=60;
            source.spatialBlend=1;source.minDistance=6;source.maxDistance=70;
            source.rolloffMode=AudioRolloffMode.Linear;source.dopplerLevel=0;source.spread=0;
            source.outputAudioMixerGroup=Resources.FindObjectsOfTypeAll<AudioMixerGroup>().FirstOrDefault(g=>g.name.Equals("SFX",StringComparison.OrdinalIgnoreCase));
            log?.Invoke("Transformer deployment: local layered texture follows the five-second forward transformation.");
        } catch(Exception e){Release();log?.Invoke("Transformer deployment unavailable: "+e.Message);}
    }
    public bool Owns(AudioSource candidate)=>source!=null&&ReferenceEquals(source,candidate);
    public void Tick(float dt,bool active,bool returning,float progress,bool blocked,float volume,Vector3 emitterPosition) {
        if(disposed||source==null||model==null)return;
        try {
            model.Step(dt,active,returning,progress,blocked,volume);
            source.transform.position=Finite(emitterPosition)?emitterPosition:car.position;
            source.volume=model.Gain;
            if(!model.Playing){Silence();return;}
            if(model.Paused) {
                if(started&&!paused){source.Pause();paused=true;}
                return;
            }
            int target=Math.Max(0,Math.Min(sampleCount-1,(int)Math.Round(model.Position*sampleRate)));
            // Textures shorter than the animation finish once, without replaying
            // their final sample at every subsequent transformation frame.
            if(model.Position*sampleRate>=sampleCount){Silence();return;}
            if(!started) {
                source.Play();source.timeSamples=target;started=true;paused=false;
            } else if(paused) {
                source.timeSamples=target;source.UnPause();paused=false;
                if(!source.isPlaying){source.Play();source.timeSamples=target;}
            } else if(!source.isPlaying) {
                // Unity's independent audio clock can finish during a frame
                // hitch while the bounded transformation clock is still live.
                source.Play();source.timeSamples=target;
            } else if(Math.Abs((long)source.timeSamples-target)>sampleRate*.05f)source.timeSamples=target;
        } catch(Exception e){Release();log?.Invoke("Transformer deployment disabled: "+e.Message);}
    }
    void Silence(){if(source!=null){if(started)source.Stop();source.volume=0;}started=paused=false;}
    public void Stop(){if(disposed)return;Silence();model?.Reset();}
    void Release() {
        Silence();if(source!=null)source.clip=null;
        if(root!=null)UnityEngine.Object.Destroy(root);
        if(clip!=null)UnityEngine.Object.Destroy(clip);
        root=null;clip=null;source=null;model=null;
    }
    public void Dispose(){if(disposed)return;disposed=true;Release();}
    static bool Finite(Vector3 value)=>Finite(value.x)&&Finite(value.y)&&Finite(value.z);
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
}
}
