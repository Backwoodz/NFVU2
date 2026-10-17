using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Rachel350Z {
// Rust supplies the original weapon meshes/audio. Shoulder articulation,
// bracket, ammo and Valheim hitscan are authored locally by NFVU2.
public sealed partial class TransformerMinigun:IDisposable {
    public readonly TransformerMinigunModel State=new TransformerMinigunModel();
    public readonly TransformerMinigunFeedback Feedback=new TransformerMinigunFeedback();
    readonly List<Mesh> meshes=new List<Mesh>();
    Material[] materials;Texture2D[] textures;Material bracketMaterial,glow;
    Transform root,yaw,pitch,barrels,muzzle,vehicle;
    readonly LineRenderer[] tracers=new LineRenderer[2];
    readonly float[] tracerLeft=new float[2];
    readonly Collider[] boreContacts=new Collider[32];
    LineRenderer flash;Light muzzleLight;
    TransformerMinigunRig rig;
    float yawAngle,pitchAngle,barrelAngle,flashLeft;
    int nextTracer;
    bool enabled,disposed;
    public bool Enabled=>enabled&&!disposed;
    public Vector3 AimPoint {get;private set;}
    public bool Obstructed {get;private set;}
    public bool Aligned {get;private set;}
    static Vector3 V(ModelVertex value)=>new Vector3(value.x,value.y,value.z);
    static Transform Node(string name,Transform parent,Vector3 position) {
        var result=new GameObject(name).transform;result.SetParent(parent,false);result.localPosition=position;return result;
    }
    public IEnumerable<int> LoadSteps(GameObject car,MonoBehaviour local,Transform shoulderMount,string folder,ModelData model,
        TransformerMinigunRig preparedRig,Action<string> log,SpawnAudio prepared) {
        if(disposed||root!=null)throw new InvalidOperationException("Minigun is already loaded or disposed");
        if(car==null||shoulderMount==null||model?.parts==null||preparedRig==null)throw new InvalidDataException("Minigun model, rig or shoulder mount unavailable");
        preparedRig.Validate();rig=preparedRig;vehicle=car.transform;
        root=Node("TransformerMinigun",shoulderMount,Vector3.zero);root.localScale=Vector3.one*1.4f;
        root.gameObject.SetActive(false);
        yaw=Node("MinigunYaw",root,Vector3.zero);pitch=Node("MinigunPitch",yaw,Vector3.zero);
        barrels=Node("RustRotatingBarrels",pitch,V(rig.barrelPivot));muzzle=Node("MinigunMuzzle",pitch,V(rig.muzzle));
        foreach(var step in VehicleMaterials.CreateForModelSteps(local,model,log,(m,t)=>{materials=m;textures=t;}))yield return step;
        bool haveBody=false,haveBarrels=false;
        foreach(var part in model.parts) {
            string role=part.name.Split('|')[0];
            if(role!="body"&&role!="barrels")throw new InvalidDataException("Unknown minigun mesh role");
            haveBody|=role=="body";haveBarrels|=role=="barrels";
            var node=Node(part.name,role=="barrels"?barrels:pitch,Vector3.zero);
            var pivot=role=="barrels"?V(rig.barrelPivot):Vector3.zero;
            var mesh=new Mesh{name=part.name};meshes.Add(mesh);
            mesh.vertices=part.vertices.Select(v=>V(v)-pivot).ToArray();mesh.triangles=part.indices;
            mesh.normals=part.normals.Select(V).ToArray();mesh.uv=part.uv.Select(v=>new Vector2(v.u,v.v)).ToArray();
            mesh.colors=Enumerable.Repeat(Color.white,part.vertices.Length).ToArray();mesh.RecalculateTangents();mesh.RecalculateBounds();
            node.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;node.gameObject.AddComponent<MeshRenderer>().sharedMaterial=materials[part.material];
            yield return 0;
        }
        if(!haveBody||!haveBarrels)throw new InvalidDataException("Minigun requires original body and rotating barrel meshes");
        bracketMaterial=new Material(materials[0]){name="NFVU2 shoulder minigun mount"};
        foreach(var property in new[]{"_Color","_BaseColor"})if(bracketMaterial.HasColor(property))bracketMaterial.SetColor(property,new Color(.12f,.14f,.15f,1));
        foreach(var property in new[]{"_MainTex","_BaseMap"})if(bracketMaterial.HasTexture(property))bracketMaterial.SetTexture(property,Texture2D.whiteTexture);
        VehicleMaterials.SetEmission(bracketMaterial,Color.black);
        Cylinder("NFVU2 shoulder bearing",root,new Vector3(0,-.12f,0),.13f,.14f,Quaternion.identity);
        Cylinder("NFVU2 yaw ring",yaw,new Vector3(0,-.04f,0),.11f,.05f,Quaternion.identity);
        Cylinder("NFVU2 pitch hinge",pitch,new Vector3(0,-.015f,0),.065f,.30f,Quaternion.Euler(0,0,90));
        glow=VehicleMaterials.CreateGlowMaterial(Texture2D.whiteTexture);
        CreateHeat(model);
        for(int i=0;i<tracers.Length;i++)tracers[i]=Line("MinigunTracer"+i,root,.009f);
        flash=Line("MinigunFlash",muzzle,.075f);
        muzzleLight=muzzle.gameObject.AddComponent<Light>();muzzleLight.color=new Color(1,.65f,.20f);muzzleLight.range=2.8f;muzzleLight.intensity=0;muzzleLight.shadows=LightShadows.None;
        CreateLaser();
        foreach(var step in LoadAudio(folder,prepared,log))yield return step;
        LoadExplosiveEffects(folder,prepared,log);yield return 0;
        log?.Invoke("Transformer: original Rust minigun on authored shoulder mount; 1000 rounds, 600 rpm, 1.5s spin-up.");
    }
    void Cylinder(string name,Transform parent,Vector3 position,float radius,float height,Quaternion rotation) {
        const int sides=16;var vertices=new Vector3[sides*2+2];var triangles=new int[sides*12];int at=0;
        for(int i=0;i<sides;i++) {
            float angle=i*Mathf.PI*2/sides;
            vertices[i]=new Vector3(Mathf.Cos(angle)*radius,-height*.5f,Mathf.Sin(angle)*radius);
            vertices[i+sides]=vertices[i]+Vector3.up*height;
            int next=(i+1)%sides;
            foreach(int index in new[]{i,i+sides,next,next,i+sides,next+sides,sides*2,i,next,sides*2+1,next+sides,i+sides})triangles[at++]=index;
        }
        vertices[sides*2]=Vector3.down*(height*.5f);vertices[sides*2+1]=Vector3.up*(height*.5f);
        var mesh=new Mesh{name=name};meshes.Add(mesh);mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
        var node=Node(name,parent,position);node.localRotation=rotation;
        node.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;node.gameObject.AddComponent<MeshRenderer>().sharedMaterial=bracketMaterial;
    }
    LineRenderer Line(string name,Transform parent,float width) {
        var line=Node(name,parent,Vector3.zero).gameObject.AddComponent<LineRenderer>();line.sharedMaterial=glow;
        line.useWorldSpace=true;line.positionCount=2;line.startWidth=width;line.endWidth=width*.2f;
        line.startColor=new Color(1,.75f,.25f,.85f);line.endColor=new Color(1,.35f,.05f,0);line.enabled=false;return line;
    }
    public void Tick(bool requested,bool active,bool held,Character driver,float volume,bool reload=false,Vector3? aimWorldPoint=null) {
        if(disposed||root==null)return;
        HideLaser();
        enabled=requested;root.gameObject.SetActive(enabled);
        float raw=Time.deltaTime;
        bool allowed=active&&enabled&&driver!=null&&!driver.IsDead()&&Finite(raw)&&raw>0&&(aimWorldPoint.HasValue||Camera.main!=null);
        float dt=Finite(raw)?Mathf.Clamp(raw,0,.1f):0;
        Feedback.Step(dt,allowed);
        volume=Finite(volume)?Mathf.Clamp01(volume):0;
        explosion?.Tick(allowed,volume);
        if(!allowed) {
            TickHeat(dt,0);State.Step(dt,false,held);UpdateAudio(false,false,0,false,volume);ClearEffects();Aligned=Obstructed=false;return;
        }
        Vector3 target;
        if(aimWorldPoint.HasValue&&Finite(aimWorldPoint.Value))target=aimWorldPoint.Value;
        else {
            if(Camera.main==null){TickHeat(dt,0);State.Stop();UpdateAudio(false,false,0,false,volume);ClearEffects();return;}
            var sight=Camera.main.ViewportPointToRay(new Vector3(TransformerAimSettings.ViewX,TransformerAimSettings.ViewY,0));
            target=TurretTargeting.Ray(sight.origin,sight.direction,TransformerMinigunCombatRules.Range,vehicle,driver,out var cover,true)?cover.point:sight.GetPoint(TransformerMinigunCombatRules.Range);
        }
        var aim=root.InverseTransformDirection(target-yaw.position);
        yawAngle=Mathf.MoveTowardsAngle(yawAngle,Mathf.Atan2(aim.x,aim.z)*Mathf.Rad2Deg,240*dt);yaw.localRotation=Quaternion.Euler(0,yawAngle,0);
        var local=yaw.InverseTransformDirection(target-pitch.position);
        pitchAngle=Mathf.MoveTowardsAngle(pitchAngle,Mathf.Clamp(-Mathf.Atan2(local.y,new Vector2(local.x,local.z).magnitude)*Mathf.Rad2Deg,-65,55),180*dt);
        pitch.localRotation=Quaternion.Euler(pitchAngle,0,0);
        // A muzzle can rotate INSIDE the robot's hull or through a wall. Unity
        // rays starting inside a collider do not report that enclosing collider,
        // so verify the short path along the actual barrel before tracing out.
        var bore=muzzle.position-pitch.position;
        bool barrelBlocked=TurretTargeting.Ray(pitch.position,bore.normalized,bore.magnitude,vehicle,driver,out var impact);
        bool hit=barrelBlocked||TurretTargeting.Ray(muzzle.position,pitch.forward,TransformerMinigunCombatRules.Range,vehicle,driver,out impact);
        AimPoint=hit?impact.point:muzzle.position+pitch.forward*TransformerMinigunCombatRules.Range;
        Obstructed=barrelBlocked||hit&&impact.collider!=null&&impact.collider.transform.IsChildOf(vehicle);
        // A barrel can begin inside a wall, which neither ray reports. Check
        // its physical segment including the pivot before dispatching damage.
        int contacts=Physics.OverlapCapsuleNonAlloc(pitch.position,muzzle.position,.025f,boreContacts,
            Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        if(contacts==boreContacts.Length)Obstructed=true;
        for(int i=0;i<contacts;i++) {
            var collider=boreContacts[i];
            if(collider!=null&&!collider.transform.IsChildOf(driver.transform)){Obstructed=true;break;}
        }
        Aligned=Vector3.Angle(pitch.forward,target-muzzle.position)<2&&!Obstructed;
        int shots=State.Step(dt,true,held,reload);
        TickHeat(dt,shots);
        UpdateAudio(true,State.Firing||shots>0,State.Spin,State.ReloadStarted,volume);
        barrelAngle=(barrelAngle+State.Spin*1800*dt)%360;barrels.localRotation=Quaternion.AngleAxis(barrelAngle,V(rig.barrelAxis));
        for(int i=0;i<tracers.Length;i++){tracerLeft[i]=Mathf.Max(0,tracerLeft[i]-dt);tracers[i].enabled=tracerLeft[i]>0;}
        flashLeft=Mathf.Max(0,flashLeft-dt);
        for(int i=0;i<shots;i++) {
            var tracer=tracers[nextTracer];tracer.SetPosition(0,muzzle.position);tracer.SetPosition(1,AimPoint);tracer.enabled=true;tracerLeft[nextTracer]=.035f;nextTracer=(nextTracer+1)%tracers.Length;
            flashLeft=.035f;
            if(hit&&!Obstructed)ImpactRound(impact,pitch.forward,driver,volume);
        }
        flash.enabled=flashLeft>0;muzzleLight.intensity=flashLeft>0?1.5f:0;
        flash.SetPosition(0,muzzle.position);flash.SetPosition(1,muzzle.position+pitch.forward*.25f);
        laserReady=true;laserDriver=driver;
    }
    void ClearEffects(){HideLaser();flashLeft=0;for(int i=0;i<tracers.Length;i++){tracerLeft[i]=0;if(tracers[i]!=null)tracers[i].enabled=false;}if(flash!=null)flash.enabled=false;if(muzzleLight!=null)muzzleLight.intensity=0;}
    public void Stop(){State.Stop();Feedback.Reset();UpdateAudio(false,false,0,false,0);explosion?.Tick(false,0);ClearEffects();}
    public void Dispose() {
        if(disposed)return;disposed=true;Stop();DisposeExplosiveEffects();DisposeAudio();DisposeLaser();DisposeHeat();
        if(root!=null){root.gameObject.SetActive(false);UnityEngine.Object.Destroy(root.gameObject);}root=null;
        foreach(var mesh in meshes)if(mesh!=null)UnityEngine.Object.Destroy(mesh);
        if(materials!=null)foreach(var material in materials)if(material!=null)UnityEngine.Object.Destroy(material);
        if(textures!=null)foreach(var texture in textures)if(texture!=null)UnityEngine.Object.Destroy(texture);
        if(bracketMaterial!=null)UnityEngine.Object.Destroy(bracketMaterial);if(glow!=null)UnityEngine.Object.Destroy(glow);
    }
    static bool Finite(Vector3 value)=>Finite(value.x)&&Finite(value.y)&&Finite(value.z);
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
}
}
