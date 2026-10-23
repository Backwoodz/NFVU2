using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rachel350Z {
public struct TyreSmokeContact {
    public bool Grounded,Sliding;
    // Point and Hub must come from the same physics sample. ReadSource shifts
    // that pair to the rendered wheel; mixing in an interpolated Hub cancels
    // the wheel's motion between physics ticks.
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
        public int Count,WispCount,CloudBirths;
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
        public float SizeScale,Growth,Spread;
        public bool Wisp,Contact;
    }
    readonly Plume[] plumes={new Plume(0),new Plume(1)};
    readonly ParticleSystem.Particle[] renderParticles=new ParticleSystem.Particle[Capacity*2];
    ParticleSystem system;
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
        {
            // One renderer sorts both interpenetrating tyre plumes together.
            // Separate renderer bounds can exchange their whole draw order as
            // the camera moves, making otherwise frozen smoke flash.
            var obj=new GameObject("RearTyrePlumes");obj.transform.SetParent(root.transform,false);
            var ps=obj.AddComponent<ParticleSystem>();system=ps;
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.playOnAwake=false;main.loop=true;main.maxParticles=renderParticles.Length;
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
        if(float.IsNaN(dt)||float.IsInfinity(dt)){ClearAll();return;}
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
            plume.Previous=source;
        }
        // Preserve independent per-tyre simulation and budgets; only the native
        // render buffer is shared. Rebuild it even after a rejected source or
        // teleport so the other tyre survives without stale cleared particles.
        int count=0;
        foreach(var plume in plumes){Array.Copy(plume.Particles,0,renderParticles,count,plume.Count);count+=plume.Count;}
        if(system.gameObject.activeInHierarchy&&!system.isPaused){system.Play(false);system.Pause(false);}
        system.SetParticles(renderParticles,count);
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
            // Feed a direct contact-patch source from the existing cloud budget.
            // The remaining clouds still shed from the rotating boundary layer.
            bool contact=!wisp&&plume.CloudBirths++%3==0;
            // Sub-frame births fill a moving trajectory, rather than equally
            // spaced clumps at the current contact point.
            var hub=source.Hub-source.Velocity*(dt*(1-(n+.5f)/count));
            float life=wisp?Range(random,.65f,1.05f):Range(random,3.6f,6.2f);
            float roll=TyreSmokeModel.Entrainment(source.Rpm);
            // The dense plume sheds early. Shoulder wisps instead reveal the
            // wheel's boundary layer for roughly one full turn under strong
            // wheelspin. Vary the release and peel outward to keep it flowing;
            // the time cap still releases slowly rotating smoke promptly.
            float orbit=wisp?Mathf.Min(Range(random,.50f,.56f),Range(random,6.28f,6.65f)/Mathf.Max(.1f,Mathf.Abs(roll))):Range(random,.10f,.22f);
            if(contact)orbit=0;
            var puff=new Puff{Hub=hub,Normal=source.Normal,Forward=source.Forward,Axle=source.Axle,Velocity=source.Velocity,
                Ground=source.Point,Life=life,Orbit=orbit,Roll=roll,Phase=Range(random,-.08f,.08f),
                // Hug the 28-cm tyre shoulders; large side offsets and puffs
                // made the full-turn mantle look like a separate outer ring.
                Width=wisp?(random.Next(2)==0?-1:1)*Range(random,.140f,.160f):Range(random,-.11f,.11f),
                Radius=source.Radius,Size=wisp?Range(random,.11f,.16f):contact?Range(random,.16f,.23f):Range(random,.19f,.32f),
                Alpha=(wisp?.48f:contact?.42f:.37f)*Mathf.Lerp(.4f,1,plume.Heat.Density),Spin=Range(random,-45,45),Seed=Range(random,0,6.28f),Wisp=wisp,Contact=contact};
            int i=plume.Count++;
            if(wisp)plume.WispCount++;
            plume.Particles[i]=new ParticleSystem.Particle{position=hub-source.Normal*source.Radius,velocity=Vector3.zero,
                startLifetime=life,remainingLifetime=life,startSize=0,startColor=new Color(1,1,1,0),rotation=Range(random,0,360),randomSeed=(uint)random.Next(1,int.MaxValue)};
            if(!wisp) {
                var particle=plume.Particles[i];uint seed=particle.randomSeed;
                // Fixed birth variation breaks regular rows without frame-wise
                // jitter or consuming the mantle's independent random stream.
                puff.SizeScale=Mathf.Lerp(.78f,1.32f,Hash01(seed));
                puff.Growth=Mathf.Lerp(.32f,.66f,Hash01(seed^0xa511e9b3u));
                puff.Spread=Mathf.Lerp(-.65f,.65f,Hash01(seed^0x63d83595u));
                puff.Alpha*=Mathf.Lerp(.85f,1.10f,Hash01(seed^0xb5297a4du));
                if(!contact)puff.Phase+=Mathf.Lerp(-.24f,.24f,Hash01(seed^0x68e31da4u));
                var variedHub=source.Hub-source.Velocity*(dt*(1-(n+Hash01(seed^0x1b56c4e9u))/count));
                particle.position+=variedHub-hub;puff.Hub=variedHub;
                plume.Particles[i]=particle;
            }
            if(contact) {
                var particle=plume.Particles[i];
                particle.position+=source.Axle*puff.Width+source.Forward*(puff.Phase*.4f)+source.Normal*.035f;
                particle.velocity=source.Velocity*.2f+source.Normal*.2f-source.Forward*(Mathf.Sign(source.Rpm)*.4f)+source.Axle*(puff.Width*2);
                plume.Particles[i]=particle;
            }
            plume.Puffs[i]=puff;
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
                float radius=puff.Radius+(puff.Wisp?.012f+.030f*peel:.025f);
                var hub=puff.Wisp?source.Hub:puff.Hub+puff.Velocity*puff.Age;
                particle.position=hub+radial*radius+axle*(puff.Width+(puff.Wisp?Mathf.Sign(puff.Width)*.012f*peel:0));
                particle.velocity=(puff.Wisp?source.Velocity:puff.Velocity)*.42f+tangent*(puff.Roll*puff.Radius*(puff.Wisp?.42f:.25f))+normal*.18f+axle*(puff.Width*(puff.Wisp?1.5f:4));
            }else {
                float age=puff.Age-puff.Orbit;
                Vector3 p=particle.position;
                Vector3 target;
                if(puff.Wisp) {
                    // Preserve the accepted mantle and its short released tail.
                    float x=p.x*.8f-clock*.5f,y=p.y*.8f+clock*.6f,z=p.z*.8f+clock*.55f;
                    var eddy=new Vector3(Mathf.Sin(z)+Mathf.Cos(y)*.5f,(Mathf.Sin(x)+Mathf.Cos(z))*.32f,Mathf.Sin(y)-Mathf.Cos(x));
                    target=wind+puff.Normal*(.10f+.14f*Mathf.Exp(-age*.4f))+eddy*.25f;
                }else target=CloudFlow(puff,p,age,wind);
                particle.velocity=Vector3.Lerp(particle.velocity,target,1-Mathf.Exp(-dt*(age<.6f?2.4f:1.3f)));
                if(!puff.Wisp) {
                    // The rolling wake can travel beyond the short position
                    // guard below. Slow its descent smoothly near the birth
                    // contact plane instead of letting a fast trail dive into it.
                    float height=Vector3.Dot(p-puff.Ground,puff.Normal);
                    float descent=Vector3.Dot(particle.velocity,puff.Normal);
                    if(descent<0&&height<.5f)
                        particle.velocity-=puff.Normal*(descent*(1-Mathf.Clamp01((height-.10f)/.40f)));
                }
                particle.position+=particle.velocity*dt;
            }
            float cloudAge=Mathf.Max(0,puff.Age-puff.Orbit);
            float size=puff.Wisp?puff.Size*(1+puff.Age*.8f+cloudAge*2.2f):Mathf.Min(3.8f,(puff.Size+cloudAge*puff.Growth+Mathf.Sqrt(cloudAge)*.28f)*puff.SizeScale);
            // A local contact-plane guard keeps rolling eddies above the road.
            // Limit its extent: it is not a world/obstacle collision solver.
            if(Vector3.ProjectOnPlane(particle.position-puff.Ground,puff.Normal).sqrMagnitude<36) {
                float clearance=Vector3.Dot(particle.position-puff.Ground,puff.Normal);
                float floor=Mathf.Min(size*.18f,.18f);
                if(clearance<floor)particle.position+=puff.Normal*(floor-clearance);
            }
            float fadeIn=Mathf.Clamp01(puff.Age/(puff.Wisp?.065f:puff.Contact?.035f:.13f));
            float fadeOut=Mathf.Clamp01((puff.Life-puff.Age)/(puff.Wisp?.20f:2.1f));
            float dilution=puff.Wisp?1:1/(1+cloudAge*.12f);
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
        plume.Count=plume.WispCount=plume.CloudBirths=0;plume.CloudCredit=plume.WispCredit=0;plume.Heat.Reset();plume.HasPrevious=false;
    }
    static Vector3 CloudFlow(Puff puff,Vector3 position,float age,Vector3 wind) {
        // The free cloud retains its birth frame and a rolling wake in world
        // space. Never anchor released smoke to the current car/wheel pose.
        float wake=Mathf.Exp(-age*.65f),windBlend=1-Mathf.Exp(-age*.8f);
        var centre=puff.Hub+puff.Velocity*(puff.Orbit+.25f*(1-wake)/.65f)
            +wind*(age-windBlend/.8f)+puff.Normal*(.12f+age*.13f);
        var offset=position-centre;
        var planar=offset-puff.Axle*Vector3.Dot(offset,puff.Axle);
        float radius=.60f+age*.38f;
        float strength=3.8f*Mathf.Exp(-age*.55f)*Mathf.Exp(-planar.sqrMagnitude/(2*radius*radius));
        var rolling=Vector3.Cross(puff.Axle,planar)*(Mathf.Clamp(puff.Roll/4f,-1,1)*strength);
        // A shared smooth field in the stored driving frame adds small billows;
        // stable lateral birth variation opens the trail into a volume.
        float x=Vector3.Dot(offset,puff.Forward)*.9f-age*.4f;
        float y=Vector3.Dot(offset,puff.Normal)*.9f+age*.5f;
        float z=Vector3.Dot(offset,puff.Axle)*.9f+age*.35f;
        var eddy=puff.Forward*(Mathf.Sin(z)+Mathf.Cos(y)*.5f)
            +puff.Normal*((Mathf.Sin(x)+Mathf.Cos(z))*.32f)
            +puff.Axle*(Mathf.Sin(y)-Mathf.Cos(x));
        return puff.Velocity*(.25f*wake)+wind*windBlend+rolling
            +puff.Normal*(.10f+.14f*Mathf.Exp(-age*.4f))+eddy*.30f
            +puff.Axle*(puff.Spread*Mathf.Exp(-age*.55f));
    }
    static float Hash01(uint value) {
        unchecked {value^=value>>16;value*=0x7feb352du;value^=value>>15;value*=0x846ca68bu;value^=value>>16;}
        return (value&0x00ffffffu)/16777216f;
    }
    void ClearAll(){foreach(var plume in plumes)Clear(plume);if(system!=null)system.Clear(false);}
    public void Dispose() {
        if(disposed)return;disposed=true;
        ClearAll();
        if(root!=null)UnityEngine.Object.Destroy(root);
        if(material!=null)UnityEngine.Object.Destroy(material);
        if(texture!=null)UnityEngine.Object.Destroy(texture);
    }
}
}
