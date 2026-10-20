using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rachel350Z {
// A local visual rig, never a Character. The native controller is sampled once
// for its idle pose and disabled before our deterministic two-bone posing.
internal sealed class ParkadeRaceStarter {
    readonly Action<string> log;
    GameObject holder,visual;
    object world;
    ZNetScene scene;
    Transform rootBone,spine;
    Transform[] bones;
    Vector3[] positions;
    Quaternion[] rotations;
    Limb leftArm,rightArm,leftLeg,rightLeg;
    Vector3 start,across;
    Quaternion facing,escapeFacing;
    bool failed;
    internal bool Ready=>holder!=null&&bones!=null&&!failed;
    internal bool Failed=>failed;
    internal ParkadeRaceStarter(Action<string> log=null){this.log=log;}

    internal bool EnsureReady(object currentWorld) {
        if(currentWorld==null||ZNetScene.instance==null){Clear();return false;}
        if(!ReferenceEquals(world,currentWorld)||scene!=ZNetScene.instance){Clear();world=currentWorld;scene=ZNetScene.instance;}
        if(Ready)return true;
        if(failed)return false;
        var prefab=scene.GetPrefab("Greydwarf");
        if(prefab==null)return false;
        try {
            var native=prefab.GetComponentInChildren<Animator>(true);
            if(native==null||native.runtimeAnimatorController==null)throw new InvalidOperationException("Native Greydwarf starter animator unavailable");
            // The last navigation segment has a different tangent from the
            // first. Anchor to the actual start plane so the lateral escape
            // stays inside the existing wall rather than cutting through it.
            var course=ParkadeLayout.AtDistance(0);
            var forward=new Vector3(course.TangentX,0,course.TangentZ).normalized;
            across=new Vector3(-forward.z,0,forward.x);
            start=new Vector3(course.X,0,course.Z)+forward*ParkadeStarterPose.StartAhead;
            start.y=ParkadeLayout.TerrainHeightAt(start.x,start.z)+.025f;
            facing=Quaternion.LookRotation(-forward);escapeFacing=Quaternion.LookRotation(across);
            holder=new GameObject("NFVU2 Parkade Greydwarf Race Starter"){hideFlags=HideFlags.DontSave};
            holder.SetActive(false);holder.transform.SetPositionAndRotation(start,facing);
            visual=UnityEngine.Object.Instantiate(native.gameObject,holder.transform,false);
            visual.name="Greydwarf starter visual";visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;
            foreach(var component in visual.GetComponentsInChildren<Component>(true)) {
                if(component==null||component is Transform||component is Renderer||component is MeshFilter||component is Animator||component is LODGroup)continue;
                if(component is Behaviour behaviour)behaviour.enabled=false;
                if(component is Collider collider)collider.enabled=false;
                if(component is ParticleSystem particles)particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                UnityEngine.Object.DestroyImmediate(component);
            }
            foreach(var particle in visual.GetComponentsInChildren<ParticleSystemRenderer>(true))UnityEngine.Object.DestroyImmediate(particle);
            foreach(var renderer in visual.GetComponentsInChildren<Renderer>(true)) {
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                if(renderer is SkinnedMeshRenderer skin)skin.updateWhenOffscreen=true;
            }
            var animator=visual.GetComponent<Animator>();
            animator.fireEvents=false;animator.applyRootMotion=false;animator.logWarnings=false;
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            holder.SetActive(true);animator.Rebind();
            foreach(var parameter in animator.parameters) {
                if(parameter.type==AnimatorControllerParameterType.Float)animator.SetFloat(parameter.nameHash,0);
                if(parameter.type==AnimatorControllerParameterType.Bool)animator.SetBool(parameter.nameHash,false);
            }
            int idle=Animator.StringToHash("Base Layer.Movement");
            if(!animator.HasState(0,idle))throw new InvalidOperationException("Native Greydwarf starter idle state unavailable");
            animator.Play(idle,0,0);animator.Update(0);animator.enabled=false;
            bones=visual.GetComponentsInChildren<Transform>(true);
            float feet=float.PositiveInfinity;
            foreach(var bone in bones)if(bone.name=="l_foot"||bone.name=="r_foot"||bone.name=="l_foot_end"||bone.name=="r_foot_end")feet=Math.Min(feet,bone.position.y);
            if(float.IsInfinity(feet))throw new InvalidOperationException("Native Greydwarf starter feet unavailable");
            visual.transform.position+=Vector3.up*(start.y-feet);
            rootBone=Find("root");spine=Find("spine1");
            leftArm=new Limb(Find("l_arm1"),Find("l_arm2"),Find("l_hand"),holder.transform);
            rightArm=new Limb(Find("r_arm1"),Find("r_arm2"),Find("r_hand"),holder.transform);
            leftLeg=new Limb(Find("l_leg1"),Find("l_leg2"),Find("l_foot"),holder.transform);
            rightLeg=new Limb(Find("r_leg1"),Find("r_leg2"),Find("r_foot"),holder.transform);
            positions=new Vector3[bones.Length];rotations=new Quaternion[bones.Length];
            for(int i=0;i<bones.Length;i++){positions[i]=bones[i].localPosition;rotations[i]=bones[i].localRotation;}
            Tick(3,0,false);return true;
        } catch(Exception error) {
            Clear();world=currentWorld;scene=ZNetScene.instance;failed=true;
            log?.Invoke("Parkade race starter unavailable: "+error.GetBaseException().Message);return false;
        }
    }
    Transform Find(string name) {
        foreach(var bone in bones)if(bone.name==name)return bone;
        throw new InvalidOperationException("Native Greydwarf starter bone unavailable: "+name);
    }
    internal void Tick(float countdownRemaining,float elapsedSinceGo,bool started) {
        if(!Ready)return;
        var pose=ParkadeStarterPose.At(countdownRemaining,elapsedSinceGo,started);
        var position=start+across*pose.Across;
        position.y=ParkadeLayout.TerrainHeightAt(position.x,position.z)+.025f;
        holder.transform.SetPositionAndRotation(position,Quaternion.Slerp(facing,escapeFacing,pose.Turn));
        // Always restore the same sampled idle before solving. Repeated paused
        // ticks and jumps in frame rate cannot accumulate bone rotations.
        for(int i=0;i<bones.Length;i++){bones[i].localPosition=positions[i];bones[i].localRotation=rotations[i];}
        var tr=holder.transform;
        rootBone.position+=Vector3.up*(-.34f*pose.Crouch+.035f*pose.Run*Math.Abs((float)Math.Sin(pose.Stride)));
        spine.rotation=Quaternion.AngleAxis(14*pose.Crouch+8*pose.Run,tr.right)*spine.rotation;
        PoseLeg(leftLeg,-1,pose,tr);PoseLeg(rightLeg,1,pose,tr);
        PoseArm(leftArm,-1,pose,tr);PoseArm(rightArm,1,pose,tr);
    }
    static void PoseArm(Limb limb,float side,ParkadeStarterPose pose,Transform tr) {
        var down=tr.TransformPoint(limb.NeutralEnd);
        var up=limb.Upper.position+tr.up*(limb.Reach*.88f)+tr.right*(side*limb.Reach*.23f)+tr.forward*.1f;
        var hand=Vector3.Lerp(down,up,pose.Raise);
        hand+=tr.up*(-.2f*pose.Crouch);
        var running=limb.Upper.position-tr.up*(limb.Reach*.68f)+tr.right*(side*.13f)+tr.forward*((float)Math.Sin(pose.Stride)*side*.36f+.08f);
        hand=Vector3.Lerp(hand,running,pose.Run);
        Solve(limb,hand,tr.forward+tr.right*side*.5f);
    }
    static void PoseLeg(Limb limb,float side,ParkadeStarterPose pose,Transform tr) {
        float phase=pose.Stride+(side>0?(float)Math.PI:0);
        var foot=tr.TransformPoint(limb.NeutralEnd);
        foot+=tr.forward*((float)Math.Cos(phase)*.3f*pose.Run)+tr.up*(Math.Max(0,(float)Math.Sin(phase))*.2f*pose.Run);
        Solve(limb,foot,tr.forward);
        limb.End.rotation=tr.rotation*limb.NeutralEndRotation;
    }
    // Analytic two-bone IK in world space avoids assumptions about the imported
    // Blender rig's local axes and its 50x armature scale.
    static void Solve(Limb limb,Vector3 target,Vector3 bend) {
        Vector3 origin=limb.Upper.position,delta=target-origin;
        float length=Math.Max(.001f,delta.magnitude);
        var direction=delta/length;
        float distance=Math.Max(Math.Abs(limb.A-limb.B)+.001f,Math.Min(limb.A+limb.B-.001f,length));
        var plane=bend-direction*Vector3.Dot(bend,direction);
        if(plane.sqrMagnitude<.000001f)plane=Vector3.Cross(direction,Vector3.right);
        if(plane.sqrMagnitude<.000001f)plane=Vector3.Cross(direction,Vector3.forward);
        plane.Normalize();
        float along=(limb.A*limb.A-limb.B*limb.B+distance*distance)/(2*distance);
        float height=(float)Math.Sqrt(Math.Max(0,limb.A*limb.A-along*along));
        var joint=origin+direction*along+plane*height;
        limb.Upper.rotation=Quaternion.FromToRotation(limb.Lower.position-origin,joint-origin)*limb.Upper.rotation;
        limb.Lower.rotation=Quaternion.FromToRotation(limb.End.position-limb.Lower.position,origin+direction*distance-limb.Lower.position)*limb.Lower.rotation;
    }
    sealed class Limb {
        internal readonly Transform Upper,Lower,End;
        internal readonly float A,B,Reach;
        internal readonly Vector3 NeutralEnd;
        internal readonly Quaternion NeutralEndRotation;
        internal Limb(Transform upper,Transform lower,Transform end,Transform holder) {
            Upper=upper;Lower=lower;End=end;A=Vector3.Distance(upper.position,lower.position);B=Vector3.Distance(lower.position,end.position);Reach=A+B;
            if(A<.05f||B<.05f)throw new InvalidOperationException("Unexpected Greydwarf starter limb proportions");
            NeutralEnd=holder.InverseTransformPoint(end.position);NeutralEndRotation=Quaternion.Inverse(holder.rotation)*end.rotation;
        }
    }
    internal void Clear() {
        if(holder!=null){holder.SetActive(false);UnityEngine.Object.Destroy(holder);}
        holder=visual=null;world=null;scene=null;bones=null;positions=null;rotations=null;rootBone=spine=null;
        leftArm=rightArm=leftLeg=rightLeg=null;failed=false;
    }
}
}
