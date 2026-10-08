using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rachel350Z {
// Original NFS smoke atlas; emitter behaviour is authored for this Unity mod.
public sealed class VehicleBurnoutEffects : IDisposable {
    readonly ParticleSystem[] systems=new ParticleSystem[2];
    readonly bool[] emitting=new bool[2];
    WheelCollider[] wheels;Transform[] visuals;
    GameObject root;Texture2D texture;Material material;
    bool disposed;
    public static VehicleBurnoutEffects Create(GameObject car,ModelData model,WheelCollider[] wheels,Transform[] visuals,Action<string> log) {
        var result=new VehicleBurnoutEffects();
        try {result.Initialize(car,model,wheels,visuals);return result;}
        catch(Exception e){result.Dispose();log?.Invoke("350Z tyre smoke unavailable: "+e.Message);return null;}
    }
    void Initialize(GameObject car,ModelData model,WheelCollider[] carWheels,Transform[] wheelVisuals) {
        wheels=carWheels;visuals=wheelVisuals;
        if(model.nitroPurgeTexture<0)throw new InvalidOperationException("Original smoke atlas missing.");
        var data=model.textures[model.nitroPurgeTexture];
        texture=new Texture2D(data.width,data.height,TextureFormat.RGBA32,false){name="350Z_TyreSmoke",wrapMode=TextureWrapMode.Clamp};
        var pixels=new byte[data.rgba.Length];
        for(int y=0;y<data.height;y++)for(int x=0;x<data.width;x++) {
            int src=(y*data.width+x)*4,dst=((data.height-1-y)*data.width+x)*4;
            pixels[dst]=pixels[dst+1]=pixels[dst+2]=255;pixels[dst+3]=data.rgba[src+3];
        }
        texture.SetPixelData<byte>(pixels,0);texture.Apply(false,true);
        var shader=Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        material=shader!=null&&shader.isSupported?new Material(shader):VehicleMaterials.CreateGlowMaterial(texture);
        material.name="350Z_TyreSmoke";material.SetTexture("_MainTex",texture);material.renderQueue=3000;
        foreach(var property in new[]{"_TintColor","_Color","_BaseColor"})
            if(material.HasColor(property))material.SetColor(property,new Color(.65f,.65f,.65f,.65f));
        root=new GameObject("350Z_TyreSmoke");root.transform.SetParent(car.transform,false);
        for(int i=0;i<2;i++) {
            var obj=new GameObject("RearTyreSmoke"+i);obj.transform.SetParent(root.transform,false);
            var ps=obj.AddComponent<ParticleSystem>();systems[i]=ps;ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.playOnAwake=false;main.loop=true;main.maxParticles=100;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=new ParticleSystem.MinMaxCurve(.7f,1.4f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(.3f,.85f);main.startSize=new ParticleSystem.MinMaxCurve(.2f,.38f);
            main.startRotation=new ParticleSystem.MinMaxCurve(0,6.28f);main.gravityModifier=-.025f;
            var emission=ps.emission;emission.rateOverTime=55;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=28;shape.radius=.07f;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.3f,1,2.6f));
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.65f,.12f),new GradientAlphaKey(0,1)});
            var color=ps.colorOverLifetime;color.enabled=true;color.color=gradient;
            var sheet=ps.textureSheetAnimation;sheet.enabled=true;sheet.numTilesX=sheet.numTilesY=2;
            sheet.startFrame=new ParticleSystem.MinMaxCurve(0,1);sheet.frameOverTime=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,0,1,1));
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
    }
    public void Tick(bool burnout,bool blocked,bool drift=false) {
        if(disposed)return;
        for(int i=0;i<systems.Length;i++) {
            var ps=systems[i];if(ps==null)continue;
            bool active=false;var wheel=wheels[i+2];
            if(!blocked&&(burnout||drift)&&wheel.GetGroundHit(out var hit)&&
                (burnout?Mathf.Abs(wheel.rpm)>40:Mathf.Abs(hit.sidewaysSlip)>.3f||Mathf.Abs(hit.forwardSlip)>.5f)) {
                wheel.GetWorldPose(out var hub,out var rotation);
                ps.transform.position=hit.point+(visuals[i+2].position-hub)+Vector3.up*.035f;
                ps.transform.rotation=Quaternion.LookRotation(Vector3.up,Vector3.forward);active=true;
            }
            if(blocked){ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);emitting[i]=false;}
            else if(active!=emitting[i]) {
                if(active)ps.Play(false);else ps.Stop(false,ParticleSystemStopBehavior.StopEmitting);
                emitting[i]=active;
            }
        }
    }
    public void Dispose() {
        if(disposed)return;disposed=true;
        foreach(var ps in systems)if(ps!=null)ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
        if(root!=null)UnityEngine.Object.Destroy(root);
        if(material!=null)UnityEngine.Object.Destroy(material);
        if(texture!=null)UnityEngine.Object.Destroy(texture);
    }
}
}
