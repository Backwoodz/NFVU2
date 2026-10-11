using System;
using UnityEngine;
namespace Rachel350Z {
// PlayOneShot stacks a separate voice for every call. A three-second tail at
// 450 rpm used over twenty voices alone. Keep full shot transients plus two tails.
public sealed class TurretFireAudio {
    readonly AudioSource[] shots=new AudioSource[4],tails=new AudioSource[2];
    int nextShot,nextTail;
    public TurretFireAudio(Func<AudioSource> create) {
        for(int i=0;i<shots.Length;i++)shots[i]=create();
        for(int i=0;i<tails.Length;i++)tails[i]=create();
        SetForeground(false);
    }
    public void SetForeground(bool occupied) {
        // Lower numbers win when Unity exhausts its real audio voices. Keep the
        // occupied car's shot attacks ahead of ordinary crowd SFX (usually 128),
        // with music (0) still first. Parked turrets return to ordinary priority.
        foreach(var source in shots)source.priority=occupied?16:128;
        foreach(var source in tails)source.priority=occupied?80:160;
    }
    public void SetVolume(float volume) {
        foreach(var source in shots)source.volume=volume;
        foreach(var source in tails)source.volume=volume*.4f;
    }
    public void Fire(AudioClip shot,AudioClip tail) {
        Play(shots[nextShot],shot);nextShot=(nextShot+1)%shots.Length;
        Play(tails[nextTail],tail);nextTail=(nextTail+1)%tails.Length;
    }
    static void Play(AudioSource source,AudioClip clip){source.Stop();source.clip=clip;source.Play();}
}
}
