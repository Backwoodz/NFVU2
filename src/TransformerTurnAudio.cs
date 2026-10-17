using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace Rachel350Z {
// Optional worker-decoded textures. Owned Unity objects stay on the main thread.
// Three fixed voices give gentle overlap without an unbounded PlayOneShot stack.
public sealed class TransformerTurnAudio : IDisposable {
    readonly List<AudioClip> clips=new List<AudioClip>();
    readonly List<float> durations=new List<float>();
    readonly List<AudioSource> sources=new List<AudioSource>();
    readonly float[] ages=new float[3],lengths=new float[3];
    readonly Transform car;
    readonly TransformerTurnSoundModel model;
    GameObject root;
    int next;
    bool disposed;
    public int ClipCount=>clips.Count;
    public TransformerTurnAudio(GameObject vehicle,IEnumerable<KeyValuePair<string,WavData>> samples,Action<string> log) {
        car=vehicle.transform;
        if(samples!=null)foreach(var entry in samples) {
            if(clips.Count>=16)break;
            AudioClip clip=null;
            try {
                var data=entry.Value;
                if(data==null||data.Channels!=1||data.SampleRate<8000||data.SampleRate>96000||data.Samples==null||data.Samples.Length==0||data.Samples.Length>data.SampleRate*3)
                    throw new InvalidOperationException("Expected bounded mono turn audio");
                foreach(float sample in data.Samples)if(!Finite(sample))throw new InvalidOperationException("Invalid turn sample");
                clip=AudioClip.Create("Transformer_"+entry.Key,data.Samples.Length,1,data.SampleRate,false);
                if(!clip.SetData(data.Samples,0))throw new InvalidOperationException("Could not upload turn samples");
                clips.Add(clip);durations.Add((float)data.Samples.Length/data.SampleRate);
            }catch(Exception e){if(clip!=null)UnityEngine.Object.Destroy(clip);log?.Invoke("Transformer turn skipped: "+entry.Key+" ("+e.Message+")");}
        }
        model=new TransformerTurnSoundModel(durations.ToArray());
        if(clips.Count==0){log?.Invoke("Transformer turning unavailable: optional audio/transformer/turn_*.wav missing or invalid.");return;}
        try {
            root=new GameObject("Transformer_Turning");root.transform.SetParent(car,false);
            var mixer=Resources.FindObjectsOfTypeAll<AudioMixerGroup>().FirstOrDefault(g=>g.name.Equals("SFX",StringComparison.OrdinalIgnoreCase));
            for(int i=0;i<3;i++) {
                var voice=new GameObject("Turn_"+i);voice.transform.SetParent(root.transform,false);
                var source=voice.AddComponent<AudioSource>();sources.Add(source);
                source.playOnAwake=false;source.loop=false;source.volume=0;source.priority=75;
                source.spatialBlend=1;source.minDistance=5;source.maxDistance=55;
                source.rolloffMode=AudioRolloffMode.Linear;source.dopplerLevel=0;source.spread=0;source.outputAudioMixerGroup=mixer;
            }
            log?.Invoke("Transformer turning: "+clips.Count+" local variants driven by actual root rotation.");
        }catch(Exception e){Dispose();log?.Invoke("Transformer turning unavailable: "+e.Message);}
    }
    public bool Owns(AudioSource source)=>sources.Contains(source);
    public void Tick(float dt,bool enabled,bool blocked,float actualTurnDegreesPerSecond,float masterVolume,Vector3 worldEmitterPosition) {
        if(disposed)return;
        model.Step(dt,enabled,blocked,actualTurnDegreesPerSecond,masterVolume);
        if(model.ImmediateSilence){Silence();return;}
        Vector3 world=Finite(worldEmitterPosition)?worldEmitterPosition:car.position;
        for(int i=0;i<sources.Count;i++) {
            sources[i].transform.position=world;
            if(lengths[i]<=0)continue;
            ages[i]+=dt;
            if(ages[i]>=lengths[i]||model.Gain<=0){StopVoice(i);continue;}
            SetGain(i);
        }
        if(!model.Event.Active||sources.Count!=3)return;
        // Prefer a free voice. If optional textures are mismatched in length,
        // reuse the oldest voice rather than increase the number of sources.
        int index=next;
        for(int i=0;i<3;i++)if(lengths[(next+i)%3]<=0){index=(next+i)%3;break;}
        var source=sources[index];source.Stop();source.clip=clips[model.Event.Clip];
        source.pitch=model.Event.Pitch;source.transform.position=world;
        ages[index]=0;lengths[index]=durations[model.Event.Clip]/source.pitch;
        SetGain(index);source.Play();next=(index+1)%3;
    }
    void SetGain(int i) {
        float edge=Math.Min(Math.Min(1,ages[i]/.04f),Math.Min(1,(lengths[i]-ages[i])/.07f));
        sources[i].volume=model.Gain*Math.Max(0,edge);
    }
    void StopVoice(int i){sources[i].Stop();sources[i].volume=0;ages[i]=lengths[i]=0;}
    void Silence(){for(int i=0;i<sources.Count;i++)StopVoice(i);}
    public void Stop(){if(disposed)return;Silence();model.Reset();next=0;}
    public void Dispose() {
        if(disposed)return;disposed=true;Silence();
        foreach(var source in sources)if(source!=null)source.clip=null;
        if(root!=null)UnityEngine.Object.Destroy(root);
        foreach(var clip in clips)if(clip!=null)UnityEngine.Object.Destroy(clip);
        sources.Clear();clips.Clear();durations.Clear();model.Reset();
    }
    static bool Finite(Vector3 value)=>Finite(value.x)&&Finite(value.y)&&Finite(value.z);
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
}
}
