using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
namespace Rachel350Z {
// NFVU2-authored mask and motion, not a recovered NFS exhaust effect.
internal sealed class VehicleExhaustSmoke:IDisposable {
    readonly List<ParticleSystem> systems=new List<ParticleSystem>();
    readonly List<ModelEffectEmitter> outlets=new List<ModelEffectEmitter>();
    ModelExhaustVariant[] variants;
    GameObject root;Texture2D texture;Material material;Transform car;
    int style=-1;bool emitting,cleared;
    public bool Owns(Transform transform)=>root!=null&&transform.IsChildOf(root.transform);
    public static VehicleExhaustSmoke Create(GameObject car,ModelData model,int style,Action<string> log) {
        var effect=new VehicleExhaustSmoke();
        try {effect.Initialize(car,model,style);return effect;}
        catch(Exception error){effect.Dispose();log?.Invoke("Idle exhaust smoke unavailable: "+error.Message);return null;}
    }
    void Initialize(GameObject vehicle,ModelData model,int selected) {
        car=vehicle.transform;variants=model.exhaustVariants;
        if(model.effectEmitters!=null)outlets.AddRange(model.effectEmitters.Where(e=>e.role=="exhaust"));
        if(outlets.Count==0)throw new InvalidOperationException("No exhaust outlet positions in model");
        texture=new Texture2D(32,32,TextureFormat.RGBA32,false){name="NFVU2 exhaust vapour mask",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
        var pixels=new Color[32*32];
        for(int y=0;y<32;y++)for(int x=0;x<32;x++) {
            float dx=(x-15.5f)/15.5f,dy=(y-15.5f)/15.5f,edge=Mathf.Clamp01(1-dx*dx-dy*dy);
            pixels[y*32+x]=new Color(1,1,1,edge*edge*(.82f+.18f*Mathf.Sin(x*.73f+Mathf.Sin(y*.51f))));
        }
        texture.SetPixels(pixels);texture.Apply(false,true);
        var shader=Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        material=shader!=null&&shader.isSupported?new Material(shader):VehicleMaterials.CreateGlowMaterial(texture);
        material.name="NFVU2 idle exhaust vapour";material.SetTexture("_MainTex",texture);material.renderQueue=3000;
        foreach(var key in new[]{"_TintColor","_Color","_BaseColor"})if(material.HasColor(key))material.SetColor(key,new Color(.72f,.74f,.76f,.5f));
        root=new GameObject("NFVU2_IdleExhaustSmoke");root.transform.SetParent(car,false);SetStyle(selected);
    }
    void CreateSystem() {
        var obj=new GameObject("IdleExhaustVapour");obj.transform.SetParent(root.transform,false);
        var ps=obj.AddComponent<ParticleSystem>();ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.playOnAwake=false;main.loop=true;main.maxParticles=24;
        main.simulationSpace=ParticleSystemSimulationSpace.World;main.scalingMode=ParticleSystemScalingMode.Local;
        main.startLifetime=new ParticleSystem.MinMaxCurve(.65f,.95f);main.startSpeed=new ParticleSystem.MinMaxCurve(.12f,.23f);
        main.startSize=new ParticleSystem.MinMaxCurve(.065f,.10f);main.startColor=Color.white;
        main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);main.gravityModifier=-.015f;
        var emission=ps.emission;emission.rateOverTime=0;
        var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=12;shape.radius=.012f;
        var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;
        velocity.x=new ParticleSystem.MinMaxCurve(-.015f,.015f);velocity.y=new ParticleSystem.MinMaxCurve(.10f,.16f);velocity.z=new ParticleSystem.MinMaxCurve(-.015f,.015f);
        var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.55f,1,2.5f));
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
            new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.25f,.14f),new GradientAlphaKey(.15f,.5f),new GradientAlphaKey(0,1)});
        var color=ps.colorOverLifetime;color.enabled=true;color.color=gradient;
        var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.renderMode=ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
        systems.Add(ps);
    }
    public void SetStyle(int selected) {
        if(style==selected)return;style=selected;
        var variant=variants==null?null:Array.Find(variants,v=>v.style==selected);
        if(variant!=null){outlets.Clear();outlets.AddRange(variant.emitters);}
        while(systems.Count<outlets.Count)CreateSystem();
        foreach(var ps in systems)ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
        emitting=false;
    }
    public void Tick(bool running,float speed,float throttle,bool blocked,TransformerVisual robot=null) {
        if(root==null)return;
        float rate=ExhaustSmokeRules.Rate(running,speed,throttle,blocked);
        if(blocked) {
            if(!cleared)foreach(var ps in systems)ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            emitting=false;cleared=true;return;
        }
        cleared=false;
        for(int i=0;i<systems.Count;i++) {
            var ps=systems[i];bool available=i<outlets.Count;ps.gameObject.SetActive(available);if(!available)continue;
            var outlet=outlets[i];var point=V(outlet.position);var direction=V(outlet.direction).normalized;
            if(robot!=null)robot.ExhaustOutlet(point,direction,out point,out direction);
            else {point=car.TransformPoint(point);direction=car.TransformDirection(direction);}
            ps.transform.SetPositionAndRotation(point+direction*.015f,Quaternion.LookRotation(direction,Mathf.Abs(Vector3.Dot(direction,Vector3.up))>.98f?Vector3.forward:Vector3.up));
            var emission=ps.emission;emission.rateOverTime=rate;
            if(rate>0){if(!emitting||!ps.isPlaying)ps.Play(false);}
            else if(emitting)ps.Stop(false,ParticleSystemStopBehavior.StopEmitting);
        }
        emitting=rate>0;
    }
    static Vector3 V(ModelVertex value)=>new Vector3(value.x,value.y,value.z);
    public void Dispose() {
        if(root!=null){root.SetActive(false);UnityEngine.Object.Destroy(root);}root=null;
        if(material!=null)UnityEngine.Object.Destroy(material);
        if(texture!=null)UnityEngine.Object.Destroy(texture);
        systems.Clear();outlets.Clear();
    }
}
public partial class Plugin {
    void TickExhaustSmoke() {
        if(current.exhaustSmoke==null)return;
        try {
            bool robot=Transforming;
            if(!robot)current.exhaustSmoke.SetStyle(exhaustStyle.Value);
            current.exhaustSmoke.Tick(idleExhaustSmoke.Value&&!Flying&&(robot||driving||ParkedRunning),
                robot?Mathf.Abs(current.transformer.Speed):body.linearVelocity.magnitude,driving?throttle:0,
                audioBlocked||menuOpen||!Application.isFocused||Time.timeScale<=0||current.worldStreaming?.Waiting==true,
                robot?current.transformer.Visual:null);
        }catch(Exception error) {
            current.exhaustSmoke.Dispose();current.exhaustSmoke=null;
            Logger.LogWarning("Idle exhaust smoke disabled: "+error);
        }
    }
}
}
