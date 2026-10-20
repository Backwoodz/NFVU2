using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rachel350Z {
// Native Greydwarf visual rigs only: no Character, AI, physics, ZDO, loot or
// animation events. Construction and animation stay bounded near the start.
internal sealed class ParkadeStartSpectators {
    readonly Action<string> log;
    readonly List<GameObject> actors=new List<GameObject>();
    GameObject root;
    object world;
    ZNetScene scene;
    Material[] stoneMaterials;
    float nextVisibility;
    bool failed;
    internal ParkadeStartSpectators(Action<string> log){this.log=log;}
    internal void Tick(Vector3 viewer,object currentWorld) {
        if(currentWorld==null||ZNetScene.instance==null){Clear();return;}
        if(!ReferenceEquals(currentWorld,world)||scene!=ZNetScene.instance) {
            Clear();world=currentWorld;scene=ZNetScene.instance;
        }
        if(failed)return;
        try {
            float dx=viewer.x-ParkadeLayout.StartX,dz=viewer.z-ParkadeLayout.StartZ;
            bool nearby=dx*dx+dz*dz<ParkadeStartSpectatorPlan.ActorDistance*ParkadeStartSpectatorPlan.ActorDistance;
            if(nearby&&actors.Count<ParkadeStartSpectatorPlan.Spots.Count) {
                var prefab=scene.GetPrefab("Greydwarf");
                if(prefab==null)return;
                if(stoneMaterials==null) {
                    // Use the neutral opaque material path already rendered by
                    // the vehicle, start marking and marquee. A prefab's first
                    // renderer need not be its solid stone surface, and copying
                    // its raw material misses the native piece's setup.
                    stoneMaterials=VehicleMaterials.Create(null,message=>{});
                    var stone=stoneMaterials[0];stone.name="Parkade viewing plinth stone";stone.hideFlags=HideFlags.DontSave;
                    var color=new Color(.23f,.25f,.26f);
                    foreach(string property in new[]{"_Color","_BaseColor","_SkinColor"})
                        if(stone.HasColor(property))stone.SetColor(property,color);
                    foreach(string property in new[]{"_Glossiness","_Smoothness"})
                        if(stone.HasFloat(property))stone.SetFloat(property,.08f);
                    if(stone.HasFloat("_Metallic"))stone.SetFloat("_Metallic",0);
                    VehicleMaterials.SetEmission(stone,color*.05f);
                }
                if(root==null)root=new GameObject("NFVU2 Parkade Start Spectators"){hideFlags=HideFlags.DontSave};
                // One native skinned rig per frame, never a crowd-sized burst.
                CreateSpectator(prefab,actors.Count);
            }
            if(Time.unscaledTime<nextVisibility)return;
            nextVisibility=Time.unscaledTime+.25f;
            for(int i=0;i<actors.Count;i++) {
                var p=ParkadeStartSpectatorPlan.Spots[i];dx=p.X-viewer.x;dz=p.Z-viewer.z;
                bool visible=dx*dx+dz*dz<ParkadeStartSpectatorPlan.ActorDistance*ParkadeStartSpectatorPlan.ActorDistance;
                if(actors[i].activeSelf!=visible)actors[i].SetActive(visible);
            }
        } catch(Exception error) {
            Clear();world=currentWorld;scene=ZNetScene.instance;failed=true;
            log?.Invoke("Parkade start spectators disabled: "+error.GetType().Name+": "+error.Message);
        }
    }
    void CreateSpectator(GameObject prefab,int index) {
        var source=prefab.GetComponentInChildren<Animator>(true);
        if(source==null||source.runtimeAnimatorController==null)throw new InvalidOperationException("Native Greydwarf idle animator unavailable");
        var p=ParkadeStartSpectatorPlan.Spots[index];
        var holder=new GameObject("Parkade Greydwarf spectator "+index){hideFlags=HideFlags.DontSave};
        holder.SetActive(false);holder.transform.SetParent(root.transform,false);
        holder.transform.SetPositionAndRotation(new Vector3(p.X,p.Y,p.Z),Quaternion.Euler(0,p.Yaw,0));
        var platform=GameObject.CreatePrimitive(PrimitiveType.Cube);platform.name="Local stone viewing plinth";platform.hideFlags=HideFlags.DontSave;
        platform.SetActive(false);UnityEngine.Object.DestroyImmediate(platform.GetComponent<Collider>());
        platform.transform.SetParent(holder.transform,false);
        float height=p.Y-p.GroundY;
        platform.transform.localPosition=new Vector3(0,-height*.5f,0);
        platform.transform.localScale=new Vector3(1.8f,height,1.8f);
        var platformRenderer=platform.GetComponent<MeshRenderer>();platformRenderer.sharedMaterial=stoneMaterials[0];
        platformRenderer.shadowCastingMode=ShadowCastingMode.Off;platformRenderer.receiveShadows=false;platform.SetActive(true);
        // Clone only the animation hierarchy under an inactive parent. Native
        // gameplay scripts are removed before any Awake/OnEnable can run.
        var clone=UnityEngine.Object.Instantiate(source.gameObject,holder.transform,false);
        clone.transform.localPosition=Vector3.zero;clone.transform.localRotation=Quaternion.identity;
        foreach(var component in clone.GetComponentsInChildren<Component>(true)) {
            if(component==null||component is Transform||component is Renderer||component is MeshFilter||component is Animator||component is LODGroup)continue;
            if(component is Behaviour behaviour)behaviour.enabled=false;
            if(component is Collider collider)collider.enabled=false;
            if(component is ParticleSystem particles)particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            UnityEngine.Object.DestroyImmediate(component);
        }
        foreach(var renderer in clone.GetComponentsInChildren<ParticleSystemRenderer>(true))UnityEngine.Object.DestroyImmediate(renderer);
        foreach(var renderer in clone.GetComponentsInChildren<Renderer>(true)) {
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        var animator=clone.GetComponent<Animator>();
        animator.applyRootMotion=false;animator.fireEvents=false;animator.logWarnings=false;
        animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        holder.SetActive(true);animator.Rebind();
        foreach(var parameter in animator.parameters) {
            if(parameter.type==AnimatorControllerParameterType.Float&&
                (parameter.name=="forward_speed"||parameter.name=="sideway_speed"||parameter.name=="turn_speed"))animator.SetFloat(parameter.nameHash,0);
            if(parameter.type==AnimatorControllerParameterType.Bool&&
                (parameter.name=="inWater"||parameter.name=="dead"||parameter.name=="wakeup"||parameter.name=="george_vibing"))animator.SetBool(parameter.nameHash,false);
        }
        int idle=Animator.StringToHash("Base Layer.Movement");
        if(!animator.HasState(0,idle))throw new InvalidOperationException("Native Greydwarf idle state unavailable");
        animator.Play(idle,0,0);animator.Update(0);
        float feet=float.PositiveInfinity;
        foreach(var bone in clone.GetComponentsInChildren<Transform>(true))
            if(bone.name=="l_foot"||bone.name=="r_foot"||bone.name=="l_foot_end"||bone.name=="r_foot_end")feet=Math.Min(feet,bone.position.y);
        if(float.IsInfinity(feet))throw new InvalidOperationException("Native Greydwarf feet unavailable");
        // Align the neutral pose once; idle/dance foot lifts remain natural.
        clone.transform.position+=Vector3.up*(p.Y-feet);
        bool canDance=false;
        foreach(var parameter in animator.parameters)
            if(parameter.type==AnimatorControllerParameterType.Bool&&parameter.name=="george_vibing") {
                animator.SetBool(parameter.nameHash,p.Dancing);canDance=p.Dancing;
            }
        int state=canDance?Animator.StringToHash("Base Layer.George Vibing"):idle;
        if(!animator.HasState(0,state))state=idle;
        animator.speed=.82f+(index%5)*.07f;animator.Play(state,0,p.Phase);animator.Update(0);
        animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
        actors.Add(holder);
    }
    internal void Clear() {
        if(root!=null){root.SetActive(false);UnityEngine.Object.Destroy(root);}
        if(stoneMaterials!=null)foreach(var material in stoneMaterials)if(material!=null)UnityEngine.Object.Destroy(material);
        actors.Clear();root=null;world=null;scene=null;stoneMaterials=null;failed=false;nextVisibility=0;
    }
}
}
