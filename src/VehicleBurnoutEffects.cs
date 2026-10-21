using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rachel350Z {
public struct TyreSmokeContact {
    public bool Grounded,Sliding;
    public Vector3 Point,Hub,Normal;
    public float Rpm,Load;
}
// A short wheel boundary layer feeds a detached, world-space plume. All heat and
// flow here are visual proxies; no forces, tyre grip or game Smoke objects.
public sealed class VehicleBurnoutEffects : IDisposable {
    const int Capacity=1400;
    sealed class Plume {
        public readonly TyreSmokeModel Heat=new TyreSmokeModel();
        public readonly ParticleSystem.Particle[] Particles=new ParticleSystem.Particle[Capacity];
        public readonly Puff[] Puffs=new Puff[Capacity];
        public readonly System.Random CloudRandom,WispRandom;
        public Plume(int index){CloudRandom=new System.Random(3502+index*17);WispRandom=new System.Random(4502+index*17);}
        public ParticleSystem System;
        public int Count,WispCount;
        public float CloudCredit,WispCredit;
        public Source Previous;
        public bool HasPrevious;
    }
    struct Source {
        public Vector3 Point,Hub,Normal,Forward,Axle,Velocity;
        public float Radius,Rpm,Load,Slip;
        public bool Grounded;
    }
    struct Puff {
        public Vector3 Hub,Normal,Forward,Axle,Velocity,Ground;
        public float Age,Life,Orbit,Roll,Phase,Width,Radius,Size,Alpha,Spin,Seed;
        public bool Wisp;
    }
    readonly Plume[] plumes={new Plume(0),new Plume(1)};
    WheelCollider[] wheels;Transform[] visuals;Transform chassis;Rigidbody body;
    GameObject root;Texture2D texture;Material material;
    bool disposed,animated;
    float clock;
    public static VehicleBurnoutEffects Create(GameObject car,ModelData model,WheelCollider[] wheels,Transform[] visuals,Action<string> log) {
        var result=new VehicleBurnoutEffects();
        try {result.Initialize(car,model,wheels,visuals);return result;}
        catch(Exception e){result.Dispose();log?.Invoke("350Z tyre smoke unavailable: "+e.Message);return null;}
    }
    void Initialize(GameObject car,ModelData model,WheelCollider[] carWheels,Transform[] wheelVisuals) {
        wheels=carWheels;visuals=wheelVisuals;chassis=car.transform;body=car.GetComponent<Rigidbody>();
        // Copy only a renderer material, never a native emitter/Smoke script. This
        // native 64-frame smoke sheet already supports environment light and fog.
        foreach(var candidate in Resources.FindObjectsOfTypeAll<Material>()) {
            if(candidate.name!="slowwispysmoke"||candidate.shader==null||!candidate.shader.isSupported||candidate.shader.name!="Lux Lit Particles/ Bumped"||candidate.mainTexture==null)continue;
            material=new Material(candidate);animated=true;break;
        }
        if(material==null)CreateFallback(model);
        material.name="350Z_TyreSmoke";material.renderQueue=3000;
        SetFloat("_EnableEmission",0);SetFloat("_AddLightsPerPixel",0);SetFloat("_ShadowsPerPixel",0);
        SetFloat("_AlphaInfluence",1);SetFloat("_InvFade",2.5f);
        root=new GameObject("350Z_TyreSmoke");root.transform.SetParent(car.transform,false);
        foreach(var plume in plumes) {
            var obj=new GameObject("RearTyrePlume");obj.transform.SetParent(root.transform,false);
            var ps=obj.AddComponent<ParticleSystem>();plume.System=ps;
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.playOnAwake=false;main.loop=true;main.maxParticles=Capacity;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.startSpeed=0;
            main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            var emission=ps.emission;emission.enabled=false;
            var shape=ps.shape;shape.enabled=false;
            {
                var sheet=ps.textureSheetAnimation;sheet.enabled=true;sheet.numTilesX=sheet.numTilesY=8;
                if(!animated)sheet.numTilesX=sheet.numTilesY=2;
                sheet.startFrame=new ParticleSystem.MinMaxCurve(0,1);
                sheet.frameOverTime=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,0,1,1));
                sheet.cycleCount=2;
            }
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;
            renderer.sortMode=ParticleSystemSortMode.Distance;renderer.maxParticleSize=.35f;
            renderer.SetActiveVertexStreams(animated?new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Normal,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV,ParticleSystemVertexStream.UV2,ParticleSystemVertexStream.AnimBlend}:new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV});
            // We own age and movement, including pause. The native renderer still
            // draws paused systems, without integrating their particles twice.
            if(obj.activeInHierarchy){ps.Play(false);ps.Pause(false);}
        }
    }
    void SetFloat(string name,float value){if(material.HasProperty(name))material.SetFloat(name,value);}
    void CreateFallback(ModelData model) {
        if(model==null||model.nitroPurgeTexture<0)throw new InvalidOperationException("Smoke textures unavailable.");
        var data=model.textures[model.nitroPurgeTexture];
        texture=new Texture2D(data.width,data.height,TextureFormat.RGBA32,false){name="350Z_TyreSmoke",wrapMode=TextureWrapMode.Clamp};
        var pixels=new byte[data.rgba.Length];
        for(int y=0;y<data.height;y++)for(int x=0;x<data.width;x++) {
            int src=(y*data.width+x)*4,dst=((data.height-1-y)*data.width+x)*4;
            pixels[dst]=pixels[dst+1]=pixels[dst+2]=255;pixels[dst+3]=data.rgba[src+3];
        }
        texture.SetPixelData<byte>(pixels,0);texture.Apply(false,true);
        var shader=Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if(shader==null||!shader.isSupported)throw new InvalidOperationException("Alpha smoke shader unavailable.");
        material=new Material(shader);material.mainTexture=texture;
        if(material.HasProperty("_TintColor"))material.SetColor("_TintColor",new Color(.65f,.65f,.65f,.65f));
    }
    Source ReadSource(int index,Func<int,TyreSmokeContact> provider) {
        var wheel=wheels[index];var c=new TyreSmokeContact();
        if(provider!=null)c=provider(index);
        else if(wheel.GetGroundHit(out var hit)) {
            wheel.GetWorldPose(out var hub,out var rotation);
            c=new TyreSmokeContact{Grounded=true,Point=hit.point,Hub=hub,Normal=hit.normal,Rpm=wheel.rpm,Load=hit.force};
        }
        var normal=c.Normal.sqrMagnitude>.5f?c.Normal.normalized:chassis.up;
        var forward=Vector3.ProjectOnPlane(chassis.forward,normal).normalized;
        var axle=Vector3.Cross(normal,forward).normalized;
        float radius=Mathf.Max(.1f,wheel.radius);
        var point=c.Grounded?c.Point+(visuals[index].position-c.Hub):visuals[index].position-normal*radius;
        Vector3 velocity=body!=null?body.GetPointVelocity(visuals[index].position):Vector3.zero;
        float longitudinal=Vector3.Dot(velocity,forward),side=Vector3.Dot(velocity,axle);
        float slip=c.Rpm*(Mathf.PI/30)*radius-longitudinal;
        return new Source{Grounded=c.Grounded,Point=point,Hub=point+normal*radius,Normal=normal,Forward=forward,Axle=axle,Velocity=velocity,
            Radius=radius,Rpm=c.Rpm,Load=c.Load,Slip=Mathf.Sqrt(slip*slip+side*side)};
    }
    public void Tick(bool burnout,bool blocked,bool drift=false,Func<int,TyreSmokeContact> contactProvider=null) {
        if(disposed||Time.deltaTime<=0)return;
        Advance(Time.deltaTime,burnout||drift,!blocked,contactProvider,EnvMan.instance!=null?EnvMan.instance.GetWindForce()*1.4f:Vector3.zero);
    }
    // Explicit clock also lets native fixtures verify identical motion at different frame rates.
    internal void Advance(float dt,bool enabled,bool allowed,Func<int,TyreSmokeContact> provider,Vector3 wind) {
        if(disposed||dt<=0)return;
        if(float.IsNaN(dt)||float.IsInfinity(dt)){foreach(var p in plumes)Clear(p);return;}
        if(!Finite(wind))wind=Vector3.zero;
        dt=Mathf.Min(dt,.1f);clock+=dt;
        for(int wheel=0;wheel<plumes.Length;wheel++) {
            var plume=plumes[wheel];var source=ReadSource(wheel+2,provider);
            if(!Finite(source.Hub)||!Finite(source.Normal)||!Finite(source.Velocity)||float.IsNaN(source.Rpm)||float.IsInfinity(source.Rpm)){Clear(plume);continue;}
            if(plume.HasPrevious&&(source.Hub-plume.Previous.Hub).sqrMagnitude>Mathf.Pow(8+source.Velocity.magnitude*dt*2,2))Clear(plume);
            if(!plume.HasPrevious){plume.Previous=source;plume.HasPrevious=true;}
            int steps=Mathf.CeilToInt(dt/.025f);float step=dt/steps;
            for(int s=0;s<steps;s++) {
                var interpolated=source;float fraction=(s+1f)/steps;
                interpolated.Hub=Vector3.Lerp(plume.Previous.Hub,source.Hub,fraction);
                interpolated.Point=Vector3.Lerp(plume.Previous.Point,source.Point,fraction);
                plume.Heat.Step(step,allowed&&enabled&&source.Grounded,source.Slip,source.Load);
                Move(plume,step,wind,interpolated,allowed&&enabled&&source.Grounded);
                if(allowed&&enabled&&source.Grounded&&plume.Heat.Density>0) {
                    Emit(plume,interpolated,step,false);Emit(plume,interpolated,step,true);
                }else plume.CloudCredit=plume.WispCredit=0;
            }
            if(plume.System.gameObject.activeInHierarchy&&!plume.System.isPaused){plume.System.Play(false);plume.System.Pause(false);}
            plume.System.SetParticles(plume.Particles,plume.Count);
            plume.Previous=source;
        }
    }
    void Emit(Plume plume,Source source,float dt,bool wisp) {
        var random=wisp?plume.WispRandom:plume.CloudRandom;
        float credit=(wisp?plume.WispCredit:plume.CloudCredit)+dt*(wisp?plume.Heat.WispRate:plume.Heat.CloudRate);
        int count=(int)credit;credit-=count;
        if(wisp)plume.WispCredit=credit;else plume.CloudCredit=credit;
        for(int n=0;n<count&&plume.Count<Capacity;n++) {
            // Reserve room for the tyre mantle even when a long-lived cloud is
            // already dense. The total per-wheel allocation remains unchanged.
            if(wisp?plume.WispCount>=350:plume.Count-plume.WispCount>=1050)break;
            // Sub-frame births fill a moving trajectory, rather than equally
            // spaced clumps at the current contact point.
            var hub=source.Hub-source.Velocity*(dt*(1-(n+.5f)/count));
            float life=wisp?Range(random,.65f,1.05f):Range(random,3.6f,6.2f);
            float roll=TyreSmokeModel.Entrainment(source.Rpm);
            // The dense plume sheds early. Shoulder wisps instead reveal the
            // wheel's boundary layer all the way over its crown before peeling
            // off. Limit the arc so smoke never forms a rigid rotating ring.
            float orbit=wisp?Mathf.Min(Range(random,.30f,.42f),Range(random,3.7f,4.5f)/Mathf.Max(.1f,Mathf.Abs(roll))):Range(random,.10f,.22f);
            var puff=new Puff{Hub=hub,Normal=source.Normal,Forward=source.Forward,Axle=source.Axle,Velocity=source.Velocity,
                Ground=source.Point,Life=life,Orbit=orbit,Roll=roll,Phase=Range(random,-.08f,.08f),
                // Smoke inside the 28-cm tyre silhouette is hidden by rubber.
                // Put small rolling wisps just outside either shoulder instead.
                Width=wisp?(random.Next(2)==0?-1:1)*Range(random,.165f,.215f):Range(random,-.11f,.11f),
                Radius=source.Radius,Size=wisp?Range(random,.16f,.24f):Range(random,.19f,.32f),
                Alpha=(wisp?.48f:.29f)*Mathf.Lerp(.4f,1,plume.Heat.Density),Spin=Range(random,-45,45),Seed=Range(random,0,6.28f),Wisp=wisp};
            int i=plume.Count++;plume.Puffs[i]=puff;
            if(wisp)plume.WispCount++;
            plume.Particles[i]=new ParticleSystem.Particle{position=hub-source.Normal*source.Radius,velocity=Vector3.zero,
                startLifetime=life,remainingLifetime=life,startSize=0,startColor=new Color(1,1,1,0),rotation=Range(random,0,360),randomSeed=(uint)random.Next(1,int.MaxValue)};
        }
    }
    void Move(Plume plume,float dt,Vector3 wind,Source source,bool attached) {
        for(int i=0;i<plume.Count;i++) {
            var puff=plume.Puffs[i];var particle=plume.Particles[i];puff.Age+=dt;
            if(puff.Age>=puff.Life){if(puff.Wisp)plume.WispCount--;int last=--plume.Count;plume.Puffs[i]=plume.Puffs[last];plume.Particles[i]=plume.Particles[last];i--;continue;}
            // Losing the contact/source sheds the boundary layer immediately;
            // existing smoke must not follow an airborne or inactive wheel.
            if(!attached)puff.Orbit=Mathf.Min(puff.Orbit,puff.Age-dt);
            float theta=puff.Phase+puff.Roll*Mathf.Min(puff.Age,puff.Orbit);
            if(puff.Age<=puff.Orbit) {
                // The thin boundary layer travels with the actual wheel frame,
                // including a turning donut. Detached clouds remain world-space.
                var normal=puff.Wisp?source.Normal:puff.Normal;
                var forward=puff.Wisp?source.Forward:puff.Forward;
                var axle=puff.Wisp?source.Axle:puff.Axle;
                var radial=-normal*Mathf.Cos(theta)-forward*Mathf.Sin(theta);
                var tangent=normal*Mathf.Sin(theta)-forward*Mathf.Cos(theta);
                float peel=Mathf.SmoothStep(0,1,Mathf.Clamp01((puff.Age/puff.Orbit-.55f)/.45f));
                float radius=puff.Radius+.025f+(puff.Wisp?.065f*peel:0);
                var hub=puff.Wisp?source.Hub:puff.Hub+puff.Velocity*puff.Age;
                particle.position=hub+radial*radius+axle*(puff.Width+(puff.Wisp?Mathf.Sign(puff.Width)*.035f*peel:0));
                particle.velocity=(puff.Wisp?source.Velocity:puff.Velocity)*.42f+tangent*(puff.Roll*puff.Radius*(puff.Wisp?.42f:.25f))+normal*.18f+axle*(puff.Width*(puff.Wisp?1.5f:4));
            }else {
                float age=puff.Age-puff.Orbit;
                Vector3 p=particle.position;
                // Continuous, spatially coherent eddies. Newly shed smoke keeps
                // some tyre/wake momentum; older smoke approaches local wind.
                // Each component depends only on the other coordinates: this
                // divergence-free field rolls nearby puffs together instead of
                // applying unrelated random jitter to each billboard.
                float x=p.x*.8f-clock*.5f,y=p.y*.8f+clock*.6f,z=p.z*.8f+clock*.55f;
                var eddy=new Vector3(Mathf.Sin(z)+Mathf.Cos(y)*.5f,(Mathf.Sin(x)+Mathf.Cos(z))*.32f,Mathf.Sin(y)-Mathf.Cos(x));
                var target=wind+puff.Normal*(.10f+.14f*Mathf.Exp(-age*.4f))+eddy*(puff.Wisp?.25f:.65f);
                particle.velocity=Vector3.Lerp(particle.velocity,target,1-Mathf.Exp(-dt*(age<.6f?2.4f:1.3f)));
                particle.position+=particle.velocity*dt;
            }
            float cloudAge=Mathf.Max(0,puff.Age-puff.Orbit);
            float size=puff.Wisp?puff.Size*(1+puff.Age*.8f+cloudAge*2.2f):Mathf.Min(3.8f,puff.Size+cloudAge*(.44f+.10f*Mathf.Sin(puff.Seed))+Mathf.Sqrt(cloudAge)*.22f);
            // A local contact-plane guard keeps rolling eddies above the road.
            // Limit its extent: it is not a world/obstacle collision solver.
            if(Vector3.ProjectOnPlane(particle.position-puff.Ground,puff.Normal).sqrMagnitude<36) {
                float clearance=Vector3.Dot(particle.position-puff.Ground,puff.Normal);
                float floor=Mathf.Min(size*.18f,.18f);
                if(clearance<floor)particle.position+=puff.Normal*(floor-clearance);
            }
            float fadeIn=Mathf.Clamp01(puff.Age/(puff.Wisp?.065f:.13f));
            float fadeOut=Mathf.Clamp01((puff.Life-puff.Age)/(puff.Wisp?.20f:2.1f));
            float dilution=puff.Wisp?1:1/(1+cloudAge*.18f);
            float alpha=puff.Alpha*fadeIn*fadeOut*dilution;
            float shade=.91f+.07f*Mathf.Sin(puff.Seed);
            particle.startColor=new Color(shade,shade,shade,alpha);particle.startSize=size;
            particle.rotation+=puff.Spin*dt;particle.remainingLifetime=puff.Life-puff.Age;
            plume.Puffs[i]=puff;plume.Particles[i]=particle;
        }
    }
    static float Range(System.Random random,float min,float max)=>min+(max-min)*(float)random.NextDouble();
    static bool Finite(Vector3 v)=>!float.IsNaN(v.x)&&!float.IsInfinity(v.x)&&!float.IsNaN(v.y)&&!float.IsInfinity(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.z);
    static void Clear(Plume plume) {
        plume.Count=plume.WispCount=0;plume.CloudCredit=plume.WispCredit=0;plume.Heat.Reset();plume.HasPrevious=false;
        if(plume.System!=null)plume.System.Clear(false);
    }
    public void Dispose() {
        if(disposed)return;disposed=true;
        foreach(var plume in plumes)Clear(plume);
        if(root!=null)UnityEngine.Object.Destroy(root);
        if(material!=null)UnityEngine.Object.Destroy(material);
        if(texture!=null)UnityEngine.Object.Destroy(texture);
    }
}
}
