using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace Rachel350Z {
// Optional audio: worker-decoded samples in, Unity objects on the main thread.
// Six bounded voices retain short tails without PlayOneShot's unbounded stack.
public sealed class TransformerFootstepAudio : IDisposable {
    readonly List<AudioClip> clips=new List<AudioClip>();
    readonly List<AudioSource> sources=new List<AudioSource>();
    readonly List<float> gains=new List<float>();
    readonly List<Vector3> impacts=new List<Vector3>();
    readonly Transform car;
    readonly TransformerFootstepModel model;
    GameObject root;
    int nextLeft,nextRight;
    bool disposed;
    public int ClipCount=>clips.Count;
    public TransformerFootstepAudio(GameObject vehicle,IEnumerable<KeyValuePair<string,WavData>> samples,Action<string> log) {
        car=vehicle.transform;
        if(samples!=null)foreach(var entry in samples) {
            if(clips.Count>=16)break;
            AudioClip clip=null;
            try {
                var data=entry.Value;
                if(data==null||data.Channels!=1||data.SampleRate<8000||data.SampleRate>96000||data.Samples==null||data.Samples.Length==0||data.Samples.Length>data.SampleRate*3)
                    throw new InvalidOperationException("Expected bounded mono footstep audio");
                clip=AudioClip.Create("Transformer_"+entry.Key,data.Samples.Length,1,data.SampleRate,false);
                if(!clip.SetData(data.Samples,0))throw new InvalidOperationException("Could not upload footstep samples");
                clips.Add(clip);
            }catch(Exception e){if(clip!=null)UnityEngine.Object.Destroy(clip);log?.Invoke("Transformer step skipped: "+entry.Key+" ("+e.Message+")");}
        }
        model=new TransformerFootstepModel(clips.Count);
        if(clips.Count==0){log?.Invoke("Transformer footsteps unavailable: optional audio/transformer/step_*.wav missing or invalid.");return;}
        try {
            root=new GameObject("Transformer_Footsteps");root.transform.SetParent(car,false);
            var mixer=Resources.FindObjectsOfTypeAll<AudioMixerGroup>().FirstOrDefault(g=>g.name.Equals("SFX",StringComparison.OrdinalIgnoreCase));
            for(int i=0;i<6;i++) {
                var voice=new GameObject("Footstep_"+(i<3?"L":"R")+i);voice.transform.SetParent(root.transform,false);
                var source=voice.AddComponent<AudioSource>();sources.Add(source);gains.Add(0);impacts.Add(car.position);
                source.playOnAwake=false;source.loop=false;source.volume=0;source.priority=65;
                source.spatialBlend=1;source.minDistance=5;source.maxDistance=65;
                source.rolloffMode=AudioRolloffMode.Linear;source.dopplerLevel=0;source.spread=0;source.outputAudioMixerGroup=mixer;
            }
            log?.Invoke("Transformer footsteps: "+clips.Count+" local variants; actual foot contacts, shuffled without consecutive repeats.");
        }catch(Exception e){Dispose();log?.Invoke("Transformer footsteps unavailable: "+e.Message);}
    }
    public bool Owns(AudioSource source)=>sources.Contains(source);
    public void Tick(float dt,bool enabled,bool blocked,uint leftFootfalls,uint rightFootfalls,float runBlend,float masterVolume) {
        Tick(dt,enabled,blocked,leftFootfalls,rightFootfalls,runBlend,masterVolume,car.position,car.position);
    }
    public void Tick(float dt,bool enabled,bool blocked,uint leftFootfalls,uint rightFootfalls,float runBlend,float masterVolume,Vector3 leftWorld,Vector3 rightWorld) {
        if(disposed)return;
        model.Step(dt,enabled,blocked,leftFootfalls,rightFootfalls,runBlend,masterVolume);
        bool validTime=!float.IsNaN(dt)&&!float.IsInfinity(dt)&&dt>0;
        if(!enabled||blocked||!validTime){Silence();return;}
        float volume=float.IsNaN(masterVolume)||float.IsInfinity(masterVolume)?0:Mathf.Clamp01(masterVolume);
        for(int i=0;i<sources.Count;i++) {
            sources[i].volume=gains[i]*volume;
            // Sources belong to the car for teardown, but an impact's ringing
            // tail stays at its contact point as the robot moves away.
            sources[i].transform.position=impacts[i];
        }
        if(sources.Count!=6)return;
        if(model.Left.Active){Play(nextLeft,model.Left,leftWorld,volume);nextLeft=(nextLeft+1)%3;}
        if(model.Right.Active){Play(3+nextRight,model.Right,rightWorld,volume);nextRight=(nextRight+1)%3;}
    }
    void Play(int index,TransformerFootstepEvent step,Vector3 world,float volume) {
        var source=sources[index];source.Stop();source.clip=clips[step.Clip];
        impacts[index]=Finite(world)?world:car.position;source.transform.position=impacts[index];
        // Store the per-contact gain so live volume edits also affect tails.
        gains[index]=volume>0?step.Gain/volume:0;
        source.volume=step.Gain;source.pitch=step.Pitch;source.Play();
    }
    static bool Finite(Vector3 p)=>Finite(p.x)&&Finite(p.y)&&Finite(p.z);
    static bool Finite(float p)=>!float.IsNaN(p)&&!float.IsInfinity(p);
    void Silence(){foreach(var source in sources)if(source!=null)source.Stop();}
    public void Stop(){if(disposed)return;Silence();model.Reset();}
    public void Dispose() {
        if(disposed)return;disposed=true;Silence();
        foreach(var source in sources)if(source!=null)source.clip=null;
        if(root!=null)UnityEngine.Object.Destroy(root);
        foreach(var clip in clips)if(clip!=null)UnityEngine.Object.Destroy(clip);
        sources.Clear();gains.Clear();impacts.Clear();clips.Clear();model.Reset();
    }
}
}
