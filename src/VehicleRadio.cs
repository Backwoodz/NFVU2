using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Rachel350Z {
public sealed class VehicleRadio : IDisposable {
    MonoBehaviour owner;
    GameObject root;
    AudioSource source;
    AudioLowPassFilter cabinFilter;
    AudioClip clip;
    Coroutine loading;
    UnityWebRequest request;
    RadioPlaybackState state;
    ValheimMusicDucking musicDucking;
    string[] files=new string[0],titles=new string[0];
    Action<string> log;
    int loadedRevision=-1,loadingRevision=-1;
    bool disposed,sourcePaused,started;
    float gain,startedAt,outsideBlend;
    RadioBassTap bassTap;
    readonly BassMotion bassMotion=new BassMotion();
    int bassSequence;float bassStale;
    public float SubwooferOffset=>bassMotion.Offset;
    public int Count {get{return titles.Length;}}
    public int CurrentIndex {get{return state==null?-1:state.Index;}}
    public string CurrentTitle {get{return CurrentIndex>=0?titles[CurrentIndex]:"Keine Musik verfügbar";}}
    public IReadOnlyList<string> TrackTitles {get{return Array.AsReadOnly(titles);}}
    public bool Paused {get{return state!=null&&state.Paused;}}
    public string Status {get;private set;}="Keine Musik importiert";
    public static VehicleRadio Create(MonoBehaviour owner,GameObject car,Action<string> log) {
        var radio=new VehicleRadio {owner=owner,log=log};
        try {radio.Initialize(car);return radio;}
        catch(Exception e){radio.Dispose();log("350Z radio unavailable: "+e.Message);return null;}
    }
    void Initialize(GameObject car) {
        string folder=Path.Combine(Path.GetDirectoryName(typeof(VehicleRadio).Assembly.Location),"radio");
        var tracks=RadioPlaylist.Read(folder,log);
        files=tracks.Select(t=>t.Path).ToArray();titles=tracks.Select(t=>t.Title).ToArray();state=new RadioPlaybackState(files.Length);
        root=new GameObject("350Z_Radio");root.transform.SetParent(car.transform,false);
        source=root.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=false;
        source.spatialBlend=0;source.dopplerLevel=0;source.volume=0;source.priority=128;
        source.minDistance=2;source.maxDistance=28;source.rolloffMode=AudioRolloffMode.Linear;
        cabinFilter=root.AddComponent<AudioLowPassFilter>();cabinFilter.cutoffFrequency=22000;
        bassTap=root.AddComponent<RadioBassTap>();bassTap.SampleRate=AudioSettings.outputSampleRate;
        musicDucking=new ValheimMusicDucking(log);
        Status=Count>0?"Bereit":"Keine Musik importiert";
        log("350Z radio ready: "+Count+" local tracks (streamed stereo).");
    }
    public void Select(int index){if(!disposed&&state.Select(index))Invalidate();}
    public void Next(){if(!disposed){state.Move(1);Invalidate();}}
    public void Previous(){if(!disposed){state.Move(-1);Invalidate();}}
    public void TogglePause(){if(!disposed)state.TogglePause();}
    void Invalidate() {
        CancelLoad();ReleaseClip();Status=state.HasPlayable?"Bereit":"Keine abspielbare Musik";
    }
    public void Tick(bool driving,bool blocked,float volume,bool enabled=true,bool parkedRunning=false,
                     float leftDoorOpen=0,float rightDoorOpen=0,float hatchOpen=0) {
        UpdatePlayback(driving,blocked,volume,enabled,parkedRunning,leftDoorOpen,rightDoorOpen,hatchOpen);
        if(!disposed)musicDucking.Tick(driving&&source!=null&&source.isPlaying&&source.volume>.001f&&!blocked);
        if(!disposed&&bassTap!=null) {
            float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
            bassTap.SampleRate=AudioSettings.outputSampleRate;
            int sequence=bassTap.Sequence;bassStale=sequence!=bassSequence?0:bassStale+dt;bassSequence=sequence;
            bool playing=enabled&&(driving||parkedRunning)&&!blocked&&!Paused&&source!=null&&source.isPlaying&&volume>.001f&&bassStale<.2f;
            bassMotion.Step(dt,bassTap.Energy*Mathf.Clamp01(volume),playing);
        }
    }
    void UpdatePlayback(bool driving,bool blocked,float volume,bool enabled,bool parkedRunning,
                        float leftDoorOpen,float rightDoorOpen,float hatchOpen) {
        if(disposed||source==null)return;
        bool active=enabled&&(driving||parkedRunning)&&!blocked&&!state.Paused;
        float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
        outsideBlend=Mathf.MoveTowards(outsideBlend,driving?0:1,dt*3);
        source.spatialBlend=outsideBlend;
        var acoustics=RadioAcoustics.Evaluate(outsideBlend,leftDoorOpen,rightDoorOpen,hatchOpen);
        root.transform.localPosition=new Vector3(acoustics.X,acoustics.Y,acoustics.Z);
        cabinFilter.cutoffFrequency=acoustics.Cutoff;
        gain=blocked?0:Mathf.MoveTowards(gain,active?1:0,dt*3);
        source.volume=Mathf.Clamp01(volume)*gain*acoustics.Gain;
        if(!state.HasPlayable)return;
        if(loadedRevision!=state.Revision&&loadingRevision!=state.Revision&&active) {
            CancelLoad();ReleaseClip();loadingRevision=state.Revision;
            loading=owner.StartCoroutine(Load(state.Revision,files[state.Index]));
        }
        if(clip==null)return;
        if(!active&&gain<=.001f) {
            if(started&&!sourcePaused){source.Pause();sourcePaused=true;}
            Status=state.Paused?"Pausiert":"Bereit";return;
        }
        if(!active)return;
        if(!started){source.Play();started=true;startedAt=Time.unscaledTime;}
        else if(sourcePaused){source.UnPause();sourcePaused=false;startedAt=Time.unscaledTime;}
        else if(!source.isPlaying&&Time.unscaledTime-startedAt>.2f){Next();return;}
        Status="Wiedergabe";
    }
    IEnumerator Load(int revision,string path) {
        Status="Lädt …";
        request=UnityWebRequestMultimedia.GetAudioClip(new Uri(Path.GetFullPath(path)).AbsoluteUri,
            Path.GetExtension(path).Equals(".ogg",StringComparison.OrdinalIgnoreCase)?AudioType.OGGVORBIS:AudioType.WAV);
        ((DownloadHandlerAudioClip)request.downloadHandler).streamAudio=true;
        request.timeout=30;
        yield return request.SendWebRequest();
        AudioClip loaded=null;string error=null;
        try {
            if(request.result!=UnityWebRequest.Result.Success)error=request.error;
            else {loaded=DownloadHandlerAudioClip.GetContent(request);if(loaded==null||loaded.samples<=0)error="Empty audio clip";}
        }catch(Exception e){error=e.Message;}
        request.Dispose();request=null;loading=null;loadingRevision=-1;
        if(disposed||!state.IsCurrent(revision)){if(loaded!=null)UnityEngine.Object.Destroy(loaded);yield break;}
        if(error!=null) {
            if(loaded!=null)UnityEngine.Object.Destroy(loaded);
            log("350Z radio skipped "+Path.GetFileName(path)+": "+error);
            state.MarkFailed(revision);Status=state.HasPlayable?"Titel übersprungen":"Keine abspielbare Musik";yield break;
        }
        clip=loaded;source.clip=clip;loadedRevision=revision;Status="Bereit";
    }
    void CancelLoad() {
        if(loading!=null&&owner!=null)owner.StopCoroutine(loading);
        loading=null;loadingRevision=-1;
        if(request!=null){request.Abort();request.Dispose();request=null;}
    }
    void ReleaseClip() {
        bassMotion.Reset();bassStale=1;if(bassTap!=null)bassTap.ResetRequested=true;
        if(source!=null){source.Stop();source.clip=null;}
        if(clip!=null)UnityEngine.Object.Destroy(clip);
        clip=null;loadedRevision=-1;started=false;sourcePaused=false;
    }
    public void Dispose() {
        if(disposed)return;disposed=true;CancelLoad();ReleaseClip();
        if(musicDucking!=null)musicDucking.Dispose();
        if(root!=null)UnityEngine.Object.Destroy(root);
    }
}
}
