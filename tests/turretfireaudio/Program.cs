using System;
using System.Collections.Generic;
using System.Linq;
using Rachel350Z;
using UnityEngine;
namespace Rachel350Z {public struct ModelVertex {public float x,y,z;}}
namespace UnityEngine {
    public class AudioClip {public float Length;}
    public class AudioSource {
        public AudioClip clip;public float volume,Left;public int Starts,Cuts,priority=128;
        public void Stop(){if(Left>0)Cuts++;Left=0;}
        public void Play(){Left=clip.Length;Starts++;}
    }
}
static class Program {
    static int checks;
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);checks++;}
    static void Main() {
        var sources=new List<AudioSource>();
        var audio=new TurretFireAudio(()=>{var source=new AudioSource();sources.Add(source);return source;});
        var shot=new AudioClip{Length=.34f};var tail=new AudioClip{Length=3};
        audio.SetVolume(.8f);
        Check(sources.Count==6,"firing creates a fixed six-source budget");
        for(int i=0;i<1000;i++) {
            foreach(var source in sources)source.Left=Math.Max(0,source.Left-TurretState.ShotInterval);
            audio.Fire(shot,tail);
            Check(sources.Count(s=>s.Left>0)<=6,"continuous fire never accumulates unbounded voices");
        }
        Check(sources.Where(s=>s.clip==shot).Sum(s=>s.Starts)==1000,"every shot has its transient");
        Check(sources.Where(s=>s.clip==shot).All(s=>s.Cuts==0),"four slots preserve full .34-second transients at 450 rpm");
        Check(sources.Count(s=>s.clip==tail)==2&&sources.Where(s=>s.clip==tail).Sum(s=>s.Cuts)>0,"only the two oldest tails are recycled");
        Check(sources.Where(s=>s.clip==tail).All(s=>Math.Abs(s.volume-.32f)<.001f),"tails retain their quieter gain");
        var shotSources=sources.Where(s=>s.clip==shot).ToArray();var tailSources=sources.Where(s=>s.clip==tail).ToArray();
        int starts=sources.Sum(s=>s.Starts),cuts=sources.Sum(s=>s.Cuts);
        audio.SetForeground(true);
        Check(shotSources.All(s=>s.priority>0&&s.priority<80)&&tailSources.All(s=>s.priority>shotSources[0].priority&&s.priority<128),"occupied turret attacks outrank ordinary crowd audio, tails remain secondary, music remains first");
        audio.SetForeground(false);
        Check(shotSources.All(s=>s.priority==128)&&tailSources.All(s=>s.priority==160),"parked turret returns to ordinary priority");
        Check(sources.Sum(s=>s.Starts)==starts&&sources.Sum(s=>s.Cuts)==cuts,"entering or exiting changes priority without interrupting playback");
        audio.SetVolume(.25f);Check(sources.Where(s=>s.clip==shot).All(s=>s.volume==.25f)&&sources.Where(s=>s.clip==tail).All(s=>Math.Abs(s.volume-.1f)<.001f),"volume changes apply to existing shots and tails");
        foreach(var source in sources)source.Left=Math.Max(0,source.Left-3.01f);
        Check(sources.All(s=>s.Left==0),"all voices finish naturally after releasing fire");
        audio.Fire(shot,tail);foreach(var source in sources)source.Stop();
        Check(sources.All(s=>s.Left==0),"owning turret can stop all voices on pause or disposal");
        Console.WriteLine(checks+" turret audio budget checks passed (controlled playback, not audio-device verification).");
    }
}
