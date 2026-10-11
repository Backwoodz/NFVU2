using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace Rachel350Z {
// Original local Rust meshes/samples. Mount, firing, recoil and effects are NFVU2.
public sealed class VehicleTurret : IDisposable {
    const float Range=150;
    public float AutomaticRange {get;set;}=TurretAutoRules.Range;
    public readonly TurretState State=new TurretState();
    readonly List<Mesh> meshes=new List<Mesh>();
    readonly List<AudioClip> clips=new List<AudioClip>();
    readonly List<AudioSource> sources=new List<AudioSource>();
    readonly AudioClip[] shots=new AudioClip[3];
    Material[] materials;Texture2D[] textures;Material glow;
    Transform root,yaw,pitch,gun,muzzle,vehicle,laser,laserMount;
    LineRenderer tracer,flash,laserBeam,laserDot,laserCore;Light light;
    Material laserMaterial;
    TurretExplosion explosion;
    AudioSource servoAudio,actionAudio;
    TurretFireAudio fireAudio;
    AudioClip tail,reload,online;
    float flashLeft,yawAngle,pitchAngle;
    int shotIndex;bool enabled,disposed,pendingOnline;
    Character autoTarget;
    float searchLeft,scanTime;
    public bool Automatic {get;private set;}
    public bool Tracking=>Automatic&&autoTarget!=null;
    public bool Enabled=>enabled&&!disposed;
    public Vector3 AimPoint {get;private set;}
    public bool Obstructed {get;private set;}
    public bool Aligned {get;private set;}
    public bool LaserAvailable=>laser!=null;
    public bool ExplosiveAvailable=>explosion!=null;
    static Vector3 V(ModelVertex v)=>new Vector3(v.x,v.y,v.z);
    static Transform Node(string name,Transform parent,Vector3 position) {
        var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;
    }
    public IEnumerable<int> LoadSteps(GameObject car,MonoBehaviour local,string folder,ModelData model,Action<string> log,SpawnAudio prepared) {
        if(model.turretRig==null)throw new InvalidDataException("Missing turret rig");model.turretRig.Validate();
        vehicle=car.transform;
        // Seat the tripod into the curved roof; the former front foot overhung the windscreen.
        root=Node("Rust_AutoTurret_AK",vehicle,new Vector3(0,1.15f,-.32f));root.localScale=Vector3.one*.65f;
        yaw=Node("Yaw",root,V(model.turretRig.yawPivot));
        pitch=Node("Pitch",yaw,V(model.turretRig.pitchOffset));gun=Node("AK_Recoil",pitch,Vector3.zero);
        muzzle=Node("Muzzle",gun,V(model.turretRig.muzzle));
        laserMount=Node("Rust_LaserModule",gun,Vector3.zero);
        if(model.turretRig.laser.HasValue)laser=Node("LaserEmitter",gun,V(model.turretRig.laser.Value));
        foreach(var step in VehicleMaterials.CreateForModelSteps(local,model,log,(m,t)=>{materials=m;textures=t;}))yield return step;
        foreach(var p in model.parts) {
            string role=p.name.Split('|')[0];
            var parent=role=="base"?root:role=="yaw"?yaw:role=="pitch"?pitch:role=="gun"?gun:role=="laser"?laserMount:null;
            if(parent==null)throw new InvalidDataException("Unknown turret mesh role");
            var t=Node(p.name,parent,Vector3.zero);
            var mesh=new Mesh{name=p.name};meshes.Add(mesh);
            mesh.vertices=p.vertices.Select(V).ToArray();mesh.triangles=p.indices;mesh.normals=p.normals.Select(V).ToArray();
            mesh.uv=p.uv.Select(v=>new Vector2(v.u,v.v)).ToArray();mesh.colors=Enumerable.Repeat(Color.white,p.vertices.Length).ToArray();
            mesh.RecalculateTangents();mesh.RecalculateBounds();
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=materials[p.material];
            yield return 0;
        }
        var mixer=Resources.FindObjectsOfTypeAll<AudioMixerGroup>().FirstOrDefault(g=>g.name.Equals("SFX",StringComparison.OrdinalIgnoreCase));
        fireAudio=new TurretFireAudio(()=>Source(muzzle,mixer));actionAudio=Source(pitch,mixer);servoAudio=Source(yaw,mixer);servoAudio.loop=true;
        for(int i=0;i<3;i++){shots[i]=Clip(prepared,folder,"shot"+(i+1)+".wav");yield return 0;}
        tail=Clip(prepared,folder,"tail.wav");yield return 0;reload=Clip(prepared,folder,"reload.wav");yield return 0;online=Clip(prepared,folder,"online.wav");yield return 0;servoAudio.clip=Clip(prepared,folder,"servo.wav");yield return 0;
        glow=VehicleMaterials.CreateGlowMaterial(Texture2D.whiteTexture);
        tracer=Line("Tracer",root,.013f);flash=Line("MuzzleFlash",muzzle,.07f);
        laserBeam=Line("LaserBeam",root,.005f);laserBeam.startColor=new Color(1,.015f,.01f,.5f);laserBeam.endColor=new Color(1,.01f,.01f,.25f);
        laserDot=Line("LaserDot",root,.028f);laserDot.startColor=laserDot.endColor=new Color(1,.02f,.01f,1);laserDot.endWidth=.028f;
        laserCore=Line("LaserCore",root,.008f);
        laserMaterial=new Material(glow){name="Rust_LaserGlow"};
        laserBeam.sharedMaterial=laserDot.sharedMaterial=laserCore.sharedMaterial=laserMaterial;
        if(File.Exists(Path.Combine(folder,"explosion.wav")))explosion=new TurretExplosion(Clip(prepared,folder,"explosion.wav"),mixer,log);
        light=muzzle.gameObject.AddComponent<Light>();light.color=new Color(1,.65f,.22f);light.range=3;light.intensity=0;light.shadows=LightShadows.None;
        root.gameObject.SetActive(false);
        log?.Invoke("350Z Rust turret: "+model.parts.Length+" original mesh sections and "+clips.Count+" original sound clips.");
    }
    AudioClip Clip(SpawnAudio prepared,string folder,string name) {
        var data=prepared.FileData(Path.Combine(folder,name));
        var clip=AudioClip.Create("Rust_"+name,data.Samples.Length/data.Channels,data.Channels,data.SampleRate,false);clips.Add(clip);
        if(!clip.SetData(data.Samples,0))throw new InvalidDataException("Could not upload "+name);return clip;
    }
    AudioSource Source(Transform parent,AudioMixerGroup mixer) {
        var source=parent.gameObject.AddComponent<AudioSource>();sources.Add(source);
        source.playOnAwake=false;source.spatialBlend=1;source.minDistance=5;source.maxDistance=100;
        source.rolloffMode=AudioRolloffMode.Linear;source.dopplerLevel=0;source.outputAudioMixerGroup=mixer;return source;
    }
    LineRenderer Line(string name,Transform parent,float width) {
        var line=Node(name,parent,Vector3.zero).gameObject.AddComponent<LineRenderer>();
        line.sharedMaterial=glow;line.useWorldSpace=true;line.positionCount=2;line.startWidth=width;line.endWidth=width*.2f;
        line.startColor=new Color(1,.72f,.22f,.9f);line.endColor=new Color(1,.3f,.05f,0);line.enabled=false;return line;
    }
    // Trace the actual bore, retaining the car's hull as a blocker.
    bool Ray(Vector3 origin,Vector3 direction,Character driver,out RaycastHit closest,bool ignoreVehicle=false) {
        return TurretTargeting.Ray(origin,direction,Range,vehicle,driver,out closest,ignoreVehicle);
    }
    bool VisibleEnemy(Character candidate,Character owner) {
        return TurretTargeting.VisibleEnemy(candidate,owner,root,pitch.position,vehicle,AutomaticRange);
    }
    Vector3 AutomaticAim(float dt,Character owner) {
        scanTime+=dt;searchLeft-=dt;
        if(!VisibleEnemy(autoTarget,owner))autoTarget=null;
        if(autoTarget==null&&searchLeft<=0) {
            searchLeft=.25f;float nearest=float.PositiveInfinity;
            foreach(var candidate in Character.GetAllCharacters()) {
                if(candidate==null)continue;
                float distance=(candidate.transform.position-pitch.position).sqrMagnitude;
                if(distance<nearest&&VisibleEnemy(candidate,owner)){autoTarget=candidate;nearest=distance;}
            }
        }
        return autoTarget!=null?autoTarget.GetCenterPoint():yaw.position+root.TransformDirection(Quaternion.Euler(0,TurretAutoRules.ScanYaw(scanTime),0)*Vector3.forward)*TurretAutoRules.Range;
    }
    public void Tick(bool requested,bool active,bool held,Character driver,float volume,bool laserEnabled,bool explosive,float laserStrength,Color laserColor,bool automatic=false,Func<bool> explosiveShot=null,bool occupied=false,float damageMultiplier=1,Action<RaycastHit,Character> elementalHit=null,Color? projectileColor=null) {
        if(disposed)return;
        float dt=Mathf.Clamp(Time.deltaTime,0,.1f);
        volume=float.IsNaN(volume)||float.IsInfinity(volume)?0:Mathf.Clamp01(volume);
        foreach(var s in sources)s.volume=volume;
        fireAudio.SetVolume(volume);
        servoAudio.volume=volume*.3f;
        if(enabled!=requested) {
            enabled=requested;root.gameObject.SetActive(enabled);State.Step(0,false,false);
            pendingOnline=enabled;
        }
        active=active&&enabled&&Camera.main!=null&&driver!=null;
        if(driver!=null&&driver.IsDead())active=false;
        fireAudio.SetForeground(active&&occupied);
        actionAudio.priority=active&&occupied?32:128;
        servoAudio.priority=160;
        Automatic=active&&automatic;
        if(!Automatic){autoTarget=null;searchLeft=0;scanTime=0;}
        explosion?.Tick(active,volume);
        laserMount.gameObject.SetActive(laserEnabled&&LaserAvailable);
        if(!active) {
            State.Step(dt,false,held,automatic);foreach(var s in sources)s.Stop();
            flashLeft=0;flash.enabled=tracer.enabled=false;light.intensity=0;
            laserBeam.enabled=laserDot.enabled=laserCore.enabled=false;
            gun.localPosition=Vector3.zero;gun.localRotation=Quaternion.identity;return;
        }
        var camera=Camera.main;
        if(pendingOnline){actionAudio.PlayOneShot(online,.7f);pendingOnline=false;}
        // Raised aiming camera and an upper viewport target keep the car below
        // the sightline. Resolve the world target first, then converge the bore.
        Vector3 target;
        if(Automatic)target=AutomaticAim(dt,driver);
        else {
            var sightRay=camera.ViewportPointToRay(new Vector3(TurretAimSettings.ViewX,TurretAimSettings.ViewY,0));
            target=Ray(sightRay.origin,sightRay.direction,driver,out var sight,true)?sight.point:sightRay.GetPoint(Range);
        }
        var aim=root.InverseTransformDirection(target-yaw.position);
        float nextYaw=Mathf.MoveTowardsAngle(yawAngle,Mathf.Atan2(aim.x,aim.z)*Mathf.Rad2Deg,240*dt);
        yaw.localRotation=Quaternion.Euler(0,nextYaw,0);
        var localAim=yaw.InverseTransformDirection(target-pitch.position);
        float nextPitch=Mathf.MoveTowardsAngle(pitchAngle,Mathf.Clamp(-Mathf.Atan2(localAim.y,new Vector2(localAim.x,localAim.z).magnitude)*Mathf.Rad2Deg,-65,55),180*dt);
        bool moving=Mathf.Abs(Mathf.DeltaAngle(yawAngle,nextYaw))+Mathf.Abs(Mathf.DeltaAngle(pitchAngle,nextPitch))>.04f;
        yawAngle=nextYaw;pitchAngle=nextPitch;pitch.localRotation=Quaternion.Euler(pitchAngle,0,0);
        if(moving&&volume>0){if(!servoAudio.isPlaying)servoAudio.Play();}else servoAudio.Stop();
        // Recoil is cosmetic; aiming/damage use the mount's pre-recoil bore.
        gun.localPosition=Vector3.zero;gun.localRotation=Quaternion.identity;
        bool hit=Ray(muzzle.position,pitch.forward,Automatic?null:driver,out var impact);
        AimPoint=hit?impact.point:muzzle.position+pitch.forward*Range;
        Obstructed=hit&&impact.collider.transform.IsChildOf(vehicle);
        Aligned=Vector3.Angle(pitch.forward,target-muzzle.position)<2&&!Obstructed;
        flashLeft=Mathf.Max(0,flashLeft-dt);
        var shotTint=projectileColor??new Color(1,.72f,.22f,.9f);
        tracer.startColor=tracer.endColor=shotTint;
        bool trigger=Automatic?TurretTargeting.CanFire(autoTarget,driver,Aligned,hit,impact.collider):held;
        if(State.Step(dt,true,trigger,Automatic)) {
            bool explosiveRound=explosiveShot!=null?explosiveShot():explosive;
            fireAudio.Fire(shots[shotIndex++%shots.Length],tail);
            flashLeft=.045f;tracer.SetPosition(0,muzzle.position);tracer.SetPosition(1,AimPoint);
            if(hit&&!Obstructed){elementalHit?.Invoke(impact,driver);TurretDamage.Bullet(impact,pitch.forward,driver,damageMultiplier);}
            if(hit&&!Obstructed&&explosiveRound)explosion?.Impact(impact,driver,volume,damageMultiplier);
        }
        if(State.ReloadStarted)actionAudio.PlayOneShot(reload,.85f);
        tracer.enabled=flash.enabled=flashLeft>0;light.intensity=flashLeft>0?2:0;
        gun.localPosition=Vector3.back*(State.Recoil*.045f);gun.localRotation=Quaternion.Euler(-State.Recoil*3,0,0);
        flash.SetPosition(0,muzzle.position);flash.SetPosition(1,muzzle.position+muzzle.forward*.24f);
        laserBeam.enabled=laserCore.enabled=laserEnabled&&LaserAvailable;
        laserDot.enabled=false;
        if(laserBeam.enabled) {
            float strength=float.IsNaN(laserStrength)||float.IsInfinity(laserStrength)?2:Mathf.Clamp(laserStrength,.25f,4);
            float width=Mathf.Sqrt(strength);
            laserBeam.startWidth=laserBeam.endWidth=.018f*width;
            laserCore.startWidth=laserCore.endWidth=.006f*width;
            laserDot.startWidth=laserDot.endWidth=.055f*width;
            laserColor.a=.85f;laserBeam.startColor=laserBeam.endColor=laserColor;
            var core=Color.Lerp(laserColor,Color.white*Mathf.Max(laserColor.r,Mathf.Max(laserColor.g,laserColor.b)),.65f);core.a=1;laserCore.startColor=laserCore.endColor=core;
            laserColor.a=1;laserDot.startColor=laserDot.endColor=laserColor;
            var tint=new Color(strength,strength,strength,1);
            foreach(var property in new[]{"_TintColor","_Color"})if(laserMaterial.HasColor(property))laserMaterial.SetColor(property,tint);
            bool laserHit=Ray(laser.position,(AimPoint-laser.position).normalized,Automatic?null:driver,out var laserImpact);
            Vector3 end=laserHit?laserImpact.point+laserImpact.normal*.005f:AimPoint;
            laserBeam.SetPosition(0,laser.position);laserBeam.SetPosition(1,end);
            laserCore.SetPosition(0,laser.position);laserCore.SetPosition(1,end);
            laserDot.enabled=laserHit;laserDot.SetPosition(0,end-camera.transform.right*.028f*width);laserDot.SetPosition(1,end+camera.transform.right*.028f*width);
        }
    }
    public void Dispose() {
        if(disposed)return;disposed=true;
        explosion?.Dispose();explosion=null;
        foreach(var s in sources)if(s!=null){s.Stop();s.clip=null;}
        if(root!=null)UnityEngine.Object.Destroy(root.gameObject);
        foreach(var m in meshes)if(m!=null)UnityEngine.Object.Destroy(m);
        foreach(var c in clips)if(c!=null)UnityEngine.Object.Destroy(c);
        if(materials!=null)foreach(var m in materials)if(m!=null)UnityEngine.Object.Destroy(m);
        if(textures!=null)foreach(var t in textures)if(t!=null)UnityEngine.Object.Destroy(t);
        if(glow!=null)UnityEngine.Object.Destroy(glow);
        if(laserMaterial!=null)UnityEngine.Object.Destroy(laserMaterial);
    }
}
}
