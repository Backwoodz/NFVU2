using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Rachel350Z {
// Reusable local impact effects; original Rust sound, NFVU2 particles and damage.
public sealed class TurretExplosion : IDisposable {
    readonly GameObject[] emitters=new GameObject[12];
    readonly ParticleSystem[] particles=new ParticleSystem[12];
    readonly AudioSource[] sounds=new AudioSource[12];
    readonly Light[] lights=new Light[12];
    readonly float[] ages=new float[12];
    readonly Action<string> log;
    Texture2D texture;Material material;int next;bool terrainFailed;
    public TurretExplosion(AudioClip clip,AudioMixerGroup mixer,Action<string> log) {
        this.log=log;
        try {
            texture=new Texture2D(32,32,TextureFormat.RGBA32,false){name="NFVU2_BlastGlow"};
            var pixels=new Color[1024];
            for(int y=0;y<32;y++)for(int x=0;x<32;x++) {
                float a=Mathf.Clamp01(1-new Vector2((x-15.5f)/15.5f,(y-15.5f)/15.5f).magnitude);
                pixels[y*32+x]=new Color(1,1,1,a*a);
            }
            texture.SetPixels(pixels);texture.Apply();material=VehicleMaterials.CreateGlowMaterial(texture);
            for(int i=0;i<emitters.Length;i++) {
                var obj=new GameObject("NFVU2_ExplosiveImpact");emitters[i]=obj;
                var ps=obj.AddComponent<ParticleSystem>();particles[i]=ps;ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=ps.main;main.playOnAwake=false;main.loop=false;main.duration=1;main.maxParticles=60;
                main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=new ParticleSystem.MinMaxCurve(.15f,.65f);
                main.startSpeed=new ParticleSystem.MinMaxCurve(1,5);main.startSize=new ParticleSystem.MinMaxCurve(.06f,.35f);main.gravityModifier=.6f;
                var emission=ps.emission;emission.enabled=false;
                var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Hemisphere;shape.radius=.12f;
                var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(1,.8f,.2f),0),new GradientColorKey(new Color(.35f,.14f,.035f),1)},
                    new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});
                var color=ps.colorOverLifetime;color.enabled=true;color.color=gradient;
                ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;
                var audio=obj.AddComponent<AudioSource>();sounds[i]=audio;audio.clip=clip;audio.playOnAwake=false;audio.spatialBlend=1;
                audio.minDistance=5;audio.maxDistance=110;audio.rolloffMode=AudioRolloffMode.Linear;audio.dopplerLevel=0;audio.outputAudioMixerGroup=mixer;
                var light=obj.AddComponent<Light>();lights[i]=light;light.color=new Color(1,.55f,.12f);light.range=4;light.intensity=0;
            }
        }catch{Dispose();throw;}
    }
    public void Tick(bool active,float volume) {
        for(int i=0;i<emitters.Length;i++) {
            sounds[i].volume=volume;
            if(!active){sounds[i].Stop();particles[i].Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ages[i]=0;}
            ages[i]=Mathf.Max(0,ages[i]-Time.deltaTime);lights[i].intensity=ages[i]*18;
        }
    }
    public void Impact(RaycastHit impact,Character driver,float volume,float damageMultiplier=1) {
        int index=next++%emitters.Length;var obj=emitters[index];
        Vector3 point=impact.point+impact.normal*.05f;
        obj.transform.SetPositionAndRotation(point,Quaternion.LookRotation(impact.normal));
        particles[index].Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);particles[index].Play();particles[index].Emit(35);
        sounds[index].Stop();sounds[index].volume=volume;sounds[index].Play();ages[index]=.16f;
        TurretDamage.Blast(impact,driver,damageMultiplier);
        if(!terrainFailed)try {TurretTerrain.Excavate(impact);}
        catch(Exception e){terrainFailed=true;log?.Invoke("350Z crater deformation disabled: "+e.Message);}
    }
    public void Dispose() {
        foreach(var obj in emitters)if(obj!=null)UnityEngine.Object.Destroy(obj);
        if(material!=null)UnityEngine.Object.Destroy(material);
        if(texture!=null)UnityEngine.Object.Destroy(texture);
    }
}
}
