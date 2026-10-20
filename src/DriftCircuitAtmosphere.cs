using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rachel350Z {
// Local scenic models, never living Characters or saved world objects. Native
// prefabs are copied under an inactive parent and stripped before activation.
internal sealed class DriftCircuitAtmosphere {
    readonly Action<string> log;
    readonly List<GameObject> spectators=new List<GameObject>(),lamps=new List<GameObject>();
    readonly List<Light> lights=new List<Light>();
    readonly List<Mesh> meshes=new List<Mesh>();
    readonly List<Material> materials=new List<Material>();
    GameObject root;
    object world;
    ZNetScene scene;
    GameObject actorPrefab,postPrefab;
    Material surfaceTemplate;
    float nextVisibility;
    bool failed;
    public DriftCircuitAtmosphere(Action<string> log){this.log=log;}
    public void Tick(Vector3 viewer,object currentWorld) {
        if(currentWorld==null||ZNetScene.instance==null){Clear();return;}
        if(!ReferenceEquals(currentWorld,world)||scene!=ZNetScene.instance){Clear();world=currentWorld;scene=ZNetScene.instance;}
        if(failed)return;
        try {
            if(root==null) {
                actorPrefab=scene.GetPrefab("Greydwarf");postPrefab=scene.GetPrefab("wood_pole2");
                if(actorPrefab==null||postPrefab==null)return;
                root=new GameObject("NFVU2 Drift Venue Atmosphere");root.hideFlags=HideFlags.DontSave;
                CreateStartLine();
            }
            // Bounded main-thread construction: no burst of dozens of skinned meshes.
            for(int budget=0;budget<2;budget++) {
                if(spectators.Count<DriftCircuitAtmospherePlan.Spectators.Count)CreateSpectator(spectators.Count);
                else if(lamps.Count<DriftCircuitAtmospherePlan.Lamps.Count)CreateLamp(lamps.Count);
                else break;
            }
            if(Time.unscaledTime<nextVisibility)return;
            nextVisibility=Time.unscaledTime+.25f;
            for(int i=0;i<spectators.Count;i++) {
                var p=DriftCircuitAtmospherePlan.Spectators[i];float dx=p.X-viewer.x,dz=p.Z-viewer.z;
                bool visible=dx*dx+dz*dz<DriftCircuitAtmospherePlan.ActorDistance*DriftCircuitAtmospherePlan.ActorDistance;
                spectators[i].SetActive(visible);
            }
            int[] nearest=DriftCircuitAtmospherePlan.NearestLights(viewer.x,viewer.z);
            for(int i=0;i<lamps.Count;i++) {
                var p=DriftCircuitAtmospherePlan.Lamps[i];float dx=p.X-viewer.x,dz=p.Z-viewer.z;
                lamps[i].SetActive(dx*dx+dz*dz<155*155);
                bool enabled=false;foreach(int candidate in nearest)if(candidate==i){enabled=true;break;}
                lights[i].enabled=enabled;
            }
        } catch(Exception error) {
            Clear();world=currentWorld;scene=ZNetScene.instance;failed=true;
            log?.Invoke("Drift venue atmosphere disabled: "+error.GetType().Name+": "+error.Message);
        }
    }
    GameObject Holder(string name,Vector3 position,Quaternion rotation) {
        var go=new GameObject(name);go.SetActive(false);go.transform.SetParent(root.transform,false);
        go.transform.SetPositionAndRotation(position,rotation);return go;
    }
    static GameObject ScenicClone(GameObject source,Transform parent) {
        // Source and all its scripts remain unchanged. The inactive hierarchy
        // prevents cloned gameplay Awake/OnEnable from running or creating ZDOs.
        var clone=UnityEngine.Object.Instantiate(source,parent,false);
        clone.transform.localPosition=Vector3.zero;
        clone.transform.localRotation=Quaternion.identity;
        foreach(var component in clone.GetComponentsInChildren<Component>(true)) {
            if(component==null||component is Transform||component is Renderer||component is MeshFilter||component is Animator||component is LODGroup)continue;
            if(component is Behaviour behaviour)behaviour.enabled=false;
            if(component is Collider collider)collider.enabled=false;
            if(component is ParticleSystem particles)particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            UnityEngine.Object.DestroyImmediate(component);
        }
        foreach(var renderer in clone.GetComponentsInChildren<Renderer>(true)) {
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            renderer.receiveShadows=false;
        }
        // A particle renderer without its removed system has no decorative role.
        foreach(var renderer in clone.GetComponentsInChildren<ParticleSystemRenderer>(true))UnityEngine.Object.DestroyImmediate(renderer);
        return clone;
    }
    void CreateSpectator(int index) {
        var p=DriftCircuitAtmospherePlan.Spectators[index];
        var source=actorPrefab.GetComponentInChildren<Animator>(true);
        if(source==null||source.runtimeAnimatorController==null)throw new InvalidOperationException("Native Greydwarf idle animator unavailable");
        var holder=Holder("Greydwarf spectator "+index,new Vector3(p.X,DriftWorldRules.GroundHeight+p.Y,p.Z),Quaternion.Euler(0,p.Yaw,0));
        var clone=ScenicClone(source.gameObject,holder.transform);
        var animator=clone.GetComponent<Animator>();
        animator.applyRootMotion=false;animator.fireEvents=false;animator.logWarnings=false;
        // Sample once before distance/renderer culling. A newly cloned rig can
        // still contain its reference pose with feet below the terrain.
        animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        holder.SetActive(true);animator.Rebind();animator.Update(0);
        foreach(var parameter in animator.parameters) {
            if(parameter.type==AnimatorControllerParameterType.Float&&
                (parameter.name=="forward_speed"||parameter.name=="sideway_speed"||parameter.name=="turn_speed"))animator.SetFloat(parameter.nameHash,0);
            if(parameter.type==AnimatorControllerParameterType.Bool)
                if(parameter.name=="inWater"||parameter.name=="dead"||parameter.name=="wakeup"||parameter.name=="george_vibing")animator.SetBool(parameter.nameHash,false);
        }
        // Initial native state stays idle; different speeds and phase remove the
        // synchronized army look. Animation events cannot call combat/footsteps.
        animator.speed=.82f+(index%5)*.07f;
        // Align to a neutral, stationary native idle exactly once. The later
        // dancing animation can lift its feet naturally without being re-pivoted.
        int neutral=Animator.StringToHash("Base Layer.Movement");
        if(animator.HasState(0,neutral)) {
            animator.Play(neutral,0,0);animator.Update(0);
            float feet=float.PositiveInfinity;
            foreach(var bone in clone.GetComponentsInChildren<Transform>(true))
                if(bone.name=="l_foot"||bone.name=="r_foot"||bone.name=="l_foot_end"||bone.name=="r_foot_end")feet=Math.Min(feet,bone.position.y);
            if(!float.IsInfinity(feet))clone.transform.position+=Vector3.up*(holder.transform.position.y-feet);
        }
        foreach(var parameter in animator.parameters)
            if(parameter.type==AnimatorControllerParameterType.Bool&&parameter.name=="george_vibing")animator.SetBool(parameter.nameHash,index%4==0);
        int state=Animator.StringToHash(index%4==0?"Base Layer.George Vibing":"Base Layer.Movement");
        if(animator.HasState(0,state))animator.Play(state,0,p.Phase);
        else animator.Play(0,0,p.Phase);
        animator.Update(0);
        // Match the native prefab: keep the animation clock advancing when a
        // renderer is culled. Distant actors are disabled as whole hierarchies.
        animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
        spectators.Add(holder);
    }
    Material Surface(string name,Color color,float emission) {
        // Valheim's bundled shaders are not necessarily registered under
        // Shader.Find("Standard"). Reuse an actual loaded opaque piece shader,
        // with the same neutral textures as the proven vehicle material path.
        if(surfaceTemplate==null) {
            var templates=VehicleMaterials.Create(null,message=>{});
            materials.AddRange(templates);surfaceTemplate=templates[0];
        }
        var material=new Material(surfaceTemplate){name=name,hideFlags=HideFlags.DontSave};
        foreach(string property in new[]{"_Color","_BaseColor","_SkinColor"})
            if(material.HasColor(property))material.SetColor(property,color);
        foreach(string property in new[]{"_Glossiness","_Smoothness"})
            if(material.HasFloat(property))material.SetFloat(property,.08f);
        if(material.HasFloat("_Metallic"))material.SetFloat("_Metallic",0);
        VehicleMaterials.SetEmission(material,color*emission);materials.Add(material);return material;
    }
    void CreateStartLine() {
        var white=Surface("Drift start chalk",new Color(.88f,.84f,.69f),.65f);
        var dark=Surface("Drift start charcoal",new Color(.055f,.06f,.055f),.1f);
        for(int parity=0;parity<2;parity++) {
            var vertices=new List<Vector3>();var indices=new List<int>();
            for(int row=0;row<2;row++)for(int col=0;col<16;col++) {
                if((row+col)%2!=parity)continue;
                float x=DriftCircuitAtmospherePlan.LineX-.8f+row*.8f;
                float z=DriftCircuitAtmospherePlan.LineZ-8+col;
                float y=DriftWorldRules.GroundHeight+.045f;int first=vertices.Count;
                vertices.AddRange(new[]{new Vector3(x,y,z),new Vector3(x,y,z+1),new Vector3(x+.8f,y,z+1),new Vector3(x+.8f,y,z)});
                indices.AddRange(new[]{first,first+1,first+2,first,first+2,first+3});
            }
            var mesh=new Mesh{name="Drift ground checker "+parity};mesh.vertices=vertices.ToArray();mesh.triangles=indices.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
            var go=new GameObject("Start finish ground marking "+parity);go.transform.SetParent(root.transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=parity==0?white:dark;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
    }
    void CreateLamp(int index) {
        var p=DriftCircuitAtmospherePlan.Lamps[index];
        var holder=Holder("Drift amber course lantern "+index,new Vector3(p.X,DriftWorldRules.GroundHeight+1,p.Z),Quaternion.identity);
        ScenicClone(postPrefab,holder.transform);
        var glow=GameObject.CreatePrimitive(PrimitiveType.Cube);glow.name="Amber lantern core";
        UnityEngine.Object.DestroyImmediate(glow.GetComponent<Collider>());
        glow.transform.SetParent(holder.transform,false);glow.transform.localPosition=Vector3.up*1.15f;glow.transform.localScale=new Vector3(.18f,.28f,.18f);
        Material amber=materials.Find(m=>m.name=="Drift lantern amber");
        if(amber==null)amber=Surface("Drift lantern amber",new Color(1,.42f,.08f),3.5f);
        var renderer=glow.GetComponent<Renderer>();renderer.sharedMaterial=amber;renderer.shadowCastingMode=ShadowCastingMode.Off;
        var light=glow.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.66f,.3f);
        light.intensity=2.5f;light.range=30;light.shadows=LightShadows.None;light.enabled=false;
        // Light sources are culled separately from cheap emissive wayfinding.
        lamps.Add(holder);lights.Add(light);
    }
    public void Clear() {
        if(root!=null){root.SetActive(false);UnityEngine.Object.Destroy(root);}
        foreach(var mesh in meshes)UnityEngine.Object.Destroy(mesh);
        foreach(var material in materials)UnityEngine.Object.Destroy(material);
        spectators.Clear();lamps.Clear();lights.Clear();meshes.Clear();materials.Clear();
        root=null;actorPrefab=null;postPrefab=null;surfaceTemplate=null;world=null;scene=null;nextVisibility=0;failed=false;
    }
}
}
