using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
namespace Rachel350Z {
public sealed partial class TransformerMinigun {
    TurretExplosion explosion;
    public bool ExplosiveRounds {get;set;}=true;
    public bool ExplosiveAvailable=>explosion!=null;
    void LoadExplosiveEffects(string folder,SpawnAudio prepared,Action<string> log) {
        AudioClip clip=null;
        try {
            var data=prepared?.FileData(Path.Combine(Path.GetDirectoryName(folder),"turret","explosion.wav"));
            if(data!=null) {
                if(data.Channels<1||data.Channels>2||data.SampleRate<8000||data.SampleRate>96000
                    ||data.Samples==null||data.Samples.Length==0||data.Samples.Length>(long)data.SampleRate*data.Channels*15)
                    throw new InvalidDataException("Invalid bounded explosion sample");
                clip=AudioClip.Create("RustMinigun_explosion",data.Samples.Length/data.Channels,data.Channels,data.SampleRate,false);
                if(!clip.SetData(data.Samples,0))throw new InvalidDataException("Could not upload explosion sample");
                audioClips["explosion"]=clip;
            }
        } catch(Exception error) {
            if(clip!=null)UnityEngine.Object.Destroy(clip);clip=null;
            log?.Invoke("Minigun explosion sound unavailable: "+error.Message);
        }
        var mixer=Resources.FindObjectsOfTypeAll<AudioMixerGroup>().FirstOrDefault(g=>g.name.Equals("SFX",StringComparison.OrdinalIgnoreCase));
        try {explosion=new TurretExplosion(clip,mixer,log);}
        catch(Exception error){log?.Invoke("Minigun explosive effects unavailable: "+error.Message);}
    }
    void ImpactRound(RaycastHit impact,Vector3 direction,Character driver,float volume) {
        Action additional=ExplosiveRounds&&explosion!=null?(Action)(()=>explosion.Impact(impact,driver,volume)):null;
        var result=TransformerMinigunDamage.Bullet(impact,direction,driver,additional);
        Feedback.Observe(result,Vector3.Distance(muzzle.position,impact.point));
    }
    void DisposeExplosiveEffects(){explosion?.Dispose();explosion=null;}
}
}
