using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace Rachel350Z {
// Optional sustained reactor texture. Progress is the deployed robot fraction
// in both directions; the loop follows the visible core, not engine ignition.
public sealed class TransformerReactorAudio : IDisposable {
    public const float LayerGain=.75f;
    readonly Transform car;
    readonly Action<string> log;
    GameObject root;
    AudioClip clip;
    AudioSource source;
    bool started,paused,disposed;
    public TransformerReactorAudio(GameObject vehicle,WavData prepared,Action<string> log) {
        car=vehicle.transform;this.log=log;
        if(prepared==null){log?.Invoke("Transformer reactor unavailable: optional audio/transformer/reactor.wav missing.");return;}
        try {
            if(prepared.Channels!=1||prepared.SampleRate<8000||prepared.SampleRate>96000||prepared.Samples==null||prepared.Samples.Length<prepared.SampleRate/4||prepared.Samples.Length>prepared.SampleRate*12)
                throw new InvalidOperationException("Expected bounded mono reactor audio");
            foreach(float sample in prepared.Samples)if(!Finite(sample)||Math.Abs(sample)>1)throw new InvalidOperationException("Invalid reactor sample");
            clip=AudioClip.Create("Transformer_Reactor",prepared.Samples.Length,1,prepared.SampleRate,false);
            if(!clip.SetData(prepared.Samples,0))throw new InvalidOperationException("Could not upload reactor samples");
            root=new GameObject("Transformer_Reactor");root.transform.SetParent(car,false);
            source=root.AddComponent<AudioSource>();
            source.playOnAwake=false;source.loop=true;source.clip=clip;source.pitch=1;source.volume=0;source.priority=90;
            source.spatialBlend=1;source.minDistance=5;source.maxDistance=55;
            source.rolloffMode=AudioRolloffMode.Linear;source.dopplerLevel=0;source.spread=0;
            source.outputAudioMixerGroup=Resources.FindObjectsOfTypeAll<AudioMixerGroup>().FirstOrDefault(g=>g.name.Equals("SFX",StringComparison.OrdinalIgnoreCase));
            log?.Invoke("Transformer reactor: local continuous core texture follows visible deployment and parked robot form.");
        } catch(Exception e){Release();log?.Invoke("Transformer reactor unavailable: "+e.Message);}
    }
    public bool Owns(AudioSource candidate)=>source!=null&&ReferenceEquals(source,candidate);
    public void Tick(float dt,bool active,bool returning,float progress,bool blocked,float volume,Vector3 emitterPosition) {
        if(disposed||source==null)return;
        try {
            if(!active||!Finite(progress)||progress<=.55f){Silence();return;}
            source.transform.position=Finite(emitterPosition)?emitterPosition:car.position;
            // A single phase envelope fades the core up when deployed and down
            // while returning. Never infer direction from elapsed wall time.
            float phase=Math.Max(0,Math.Min(1,(progress-.55f)/.45f));
            float master=Finite(volume)?Math.Max(0,Math.Min(1,volume)):0;
            source.volume=phase*phase*(3-2*phase)*LayerGain*master;
            if(blocked||!Finite(dt)||dt<=0) {
                source.volume=0;
                if(started&&!paused){source.Pause();paused=true;}
                return;
            }
            if(!started){source.Play();started=true;paused=false;}
            else if(paused){source.UnPause();paused=false;if(!source.isPlaying)source.Play();}
            else if(!source.isPlaying)source.Play();
        } catch(Exception e){Release();log?.Invoke("Transformer reactor disabled: "+e.Message);}
    }
    void Silence(){if(source!=null){if(started)source.Stop();source.volume=0;}started=paused=false;}
    public void Stop(){if(!disposed)Silence();}
    void Release() {
        Silence();if(source!=null)source.clip=null;
        if(root!=null)UnityEngine.Object.Destroy(root);
        if(clip!=null)UnityEngine.Object.Destroy(clip);
        root=null;clip=null;source=null;
    }
    public void Dispose(){if(disposed)return;disposed=true;Release();}
    static bool Finite(Vector3 value)=>Finite(value.x)&&Finite(value.y)&&Finite(value.z);
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
}
}
