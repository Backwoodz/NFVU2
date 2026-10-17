using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
namespace Rachel350Z {
public sealed partial class TransformerMinigun {
    readonly Dictionary<string,AudioClip> audioClips=new Dictionary<string,AudioClip>();
    readonly Dictionary<AudioClip,float> audioPeaks=new Dictionary<AudioClip,float>();
    readonly List<AudioSource> audioSources=new List<AudioSource>();
    AudioSource motorLoop,motorUnpitchedLoop,fireLoop,mechLoop,lfeLoop,motorAction,fireAction,mechAction,lfeAction,reloadAction;
    bool motorWasActive,wasFiring;
    IEnumerable<int> LoadAudio(string folder,SpawnAudio prepared,Action<string> log) {
        var mixer=Resources.FindObjectsOfTypeAll<AudioMixerGroup>().FirstOrDefault(g=>g.name.Equals("SFX",StringComparison.OrdinalIgnoreCase));
        motorLoop=AudioVoice(mixer,true);fireLoop=AudioVoice(mixer,true);mechLoop=AudioVoice(mixer,true);lfeLoop=AudioVoice(mixer,true);
        motorAction=AudioVoice(mixer,false);fireAction=AudioVoice(mixer,false);mechAction=AudioVoice(mixer,false);lfeAction=AudioVoice(mixer,false);reloadAction=AudioVoice(mixer,false);
        motorUnpitchedLoop=AudioVoice(mixer,true);
        foreach(var name in new[]{"spin_start","spin_loop","spin_unpitched_loop","spin_release","spin_stop","fire_start","fire_loop","fire_tail","mech_loop","mech_tail","lfe_start","lfe_loop","lfe_tail","reload","deploy"}) {
            try {
                var data=prepared?.FileData(Path.Combine(folder,name+".wav"));
                if(data==null)continue;
                if(data.Channels<1||data.Channels>2||data.SampleRate<8000||data.SampleRate>96000||data.Samples==null||data.Samples.Length==0||data.Samples.Length>(long)data.SampleRate*data.Channels*15)
                    throw new InvalidDataException("Invalid bounded minigun sample");
                var samples=TransformerMinigunSoundLevels.Prepare(name,data.Samples,out float peak);
                var clip=AudioClip.Create("RustMinigun_"+name,data.Samples.Length/data.Channels,data.Channels,data.SampleRate,false);
                audioClips[name]=clip;
                if(!clip.SetData(samples,0))throw new InvalidDataException("Could not upload minigun sample");
                audioPeaks[clip]=peak;
            }catch(Exception error){
                if(audioClips.TryGetValue(name,out var failed)){UnityEngine.Object.Destroy(failed);audioClips.Remove(name);}
                log?.Invoke("Minigun optional sound skipped: "+name+" ("+error.Message+")");
            }
            yield return 0;
        }
        log?.Invoke("Minigun motor: +12 dB PCM headroom-limited to 0.97, early spin presence and native unpitched motor layer; ten fixed voices.");
    }
    AudioSource AudioVoice(AudioMixerGroup mixer,bool loop) {
        var source=muzzle.gameObject.AddComponent<AudioSource>();audioSources.Add(source);
        source.playOnAwake=false;source.loop=loop;source.priority=24;
        source.spatialBlend=1;source.minDistance=5;source.maxDistance=100;source.rolloffMode=AudioRolloffMode.Linear;
        source.dopplerLevel=0;source.outputAudioMixerGroup=mixer;return source;
    }
    void Play(AudioSource source,string name) {
        if(source==null)return;
        source.Stop();source.clip=audioClips.TryGetValue(name,out var clip)?clip:null;
        if(source.clip!=null)source.Play();
    }
    void Loop(AudioSource source,string name,bool enabled) {
        if(source==null)return;
        if(!enabled){source.Stop();return;}
        if(!audioClips.ContainsKey(name)){if(source.isPlaying)source.Stop();return;}
        if(!source.isPlaying)Play(source,name);
    }
    float ClipPeak(string name)=>audioClips.TryGetValue(name,out var clip)&&audioPeaks.TryGetValue(clip,out float peak)?peak:0;
    float PlayingPeak(AudioSource source)=>source!=null&&source.isPlaying&&source.clip!=null&&audioPeaks.TryGetValue(source.clip,out float peak)?peak:0;
    void UpdateAudio(bool active,bool firing,float spin,bool reloadStarted,float volume) {
        volume=TransformerMinigunSoundLevels.Master(volume);
        spin=TransformerMinigunSoundLevels.Master(spin);
        foreach(var source in audioSources)if(source!=null)source.volume=volume;
        if(!active) {
            foreach(var source in audioSources)if(source!=null)source.Stop();motorWasActive=wasFiring=false;return;
        }
        bool motor=spin>0;
        if(motor&&!motorWasActive)Play(motorAction,"spin_start");
        if(!firing&&wasFiring){Play(fireAction,"fire_tail");Play(mechAction,"mech_tail");Play(lfeAction,"lfe_tail");Play(motorAction,"spin_release");}
        if(!motor&&motorWasActive&&!reloadStarted)Play(motorAction,"spin_stop");
        if(firing&&!wasFiring){Play(fireAction,"fire_start");Play(lfeAction,"lfe_start");}
        // The final bullet's following release edge must not cut off the belt
        // reload. It has one dedicated bounded voice, rather than PlayOneShot.
        if(reloadStarted)Play(reloadAction,"reload");
        var motorLevels=TransformerMinigunSoundLevels.Motor(spin,volume,motor?ClipPeak("spin_loop"):0,motor?ClipPeak("spin_unpitched_loop"):0,PlayingPeak(motorAction));
        if(motorLoop!=null){motorLoop.pitch=.7f+.3f*spin;motorLoop.volume=motorLevels.Pitched;}
        if(motorUnpitchedLoop!=null){motorUnpitchedLoop.pitch=1;motorUnpitchedLoop.volume=motorLevels.Unpitched;}
        if(motorAction!=null)motorAction.volume=motorLevels.Action;
        if(mechLoop!=null)mechLoop.volume=volume*.45f;
        if(lfeLoop!=null)lfeLoop.volume=volume*.45f;
        Loop(motorLoop,"spin_loop",motor);Loop(motorUnpitchedLoop,"spin_unpitched_loop",motor);
        Loop(fireLoop,"fire_loop",firing);Loop(mechLoop,"mech_loop",firing);Loop(lfeLoop,"lfe_loop",firing);
        motorWasActive=motor;wasFiring=firing;
    }
    void DisposeAudio() {
        foreach(var source in audioSources)if(source!=null){source.Stop();source.clip=null;}
        foreach(var clip in audioClips.Values)if(clip!=null)UnityEngine.Object.Destroy(clip);
        audioSources.Clear();audioClips.Clear();audioPeaks.Clear();
    }
}
}
