using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Rachel350Z {
// Uses the live character's chair animation and a small procedural limb/entry layer.
// Does not instantiate a Player clone or change equipment, inventory or network ownership.
internal sealed partial class VehicleCabinDriver:IDisposable {
    public readonly CabinTransition Motion=new CabinTransition();
    readonly Transform player,car,visual,hips;
    readonly Animator animator;
    readonly VehicleInterior interior;
    readonly Vector3 visualPosition,visualScale;
    readonly Quaternion visualRotation;
    readonly AnimatorCullingMode culling;
    readonly bool rootMotion;
    readonly Dictionary<string,float> floats=new Dictionary<string,float>();
    readonly Dictionary<string,bool> bools=new Dictionary<string,bool>();
    readonly Dictionary<string,Transform> bones;
    readonly Queue<Vector3> approach=new Queue<Vector3>();
    readonly List<Tuple<Renderer,bool>> equipment=new List<Tuple<Renderer,bool>>();
    readonly List<Tuple<SkinnedMeshRenderer,bool>> skins=new List<Tuple<SkinnedMeshRenderer,bool>>();
    Vector3 outside,exitWorld,standingLocal;
    Quaternion standingRotation;
    bool disposed;
    float windowRest;
    readonly Dictionary<string,Vector3> transformerStart=new Dictionary<string,Vector3>();
    public void BeginTransformer() {
        transformerStart.Clear();
        foreach(string name in new[]{"Hips","LeftHand","RightHand","LeftFoot","RightFoot","LeftLeg","RightLeg","LeftForeArm","RightForeArm"})
            if(bones.TryGetValue(name,out var bone))transformerStart[name]=car.InverseTransformPoint(bone.position);
    }
    public bool Failed {get;private set;}
    public Vector3 ExitPosition=>exitWorld;
    public void SetExitPosition(Vector3 point){exitWorld=point;}
    public VehicleCabinDriver(MonoBehaviour local,Transform car,VehicleInterior interior,Vector3 outsideWorld) {
        player=local.transform;this.car=car;this.interior=interior;
        visual=player.Find("Visual");
        animator=visual!=null?visual.GetComponentInChildren<Animator>():null;
        if(animator==null)throw new InvalidOperationException("Player animator unavailable for cabin pose");
        bones=visual.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
        if(!bones.TryGetValue("Hips",out hips))throw new InvalidOperationException("Player hip bone unavailable");
        visualPosition=visual.localPosition;visualScale=visual.localScale;visualRotation=visual.localRotation;
        culling=animator.cullingMode;rootMotion=animator.applyRootMotion;
        foreach(var p in animator.parameters) {
            if(p.type==AnimatorControllerParameterType.Bool && (p.name=="attach_chair"||p.name=="inWater"||p.name=="onGround"))bools[p.name]=animator.GetBool(p.name);
            if(p.type==AnimatorControllerParameterType.Float && (p.name=="forward_speed"||p.name=="sideway_speed"||p.name=="turn_speed"))floats[p.name]=animator.GetFloat(p.name);
        }
        if(!bools.ContainsKey("attach_chair"))throw new InvalidOperationException("Player chair animation parameter unavailable");
        animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.applyRootMotion=false;
        // Held items would pass through the wheel. Preserve their visibility without unequipping.
        foreach(var name in new[]{"LeftHand_Attach","RightHand_Attach"})if(bones.TryGetValue(name,out var attach))
            foreach(var r in attach.GetComponentsInChildren<Renderer>(true)){equipment.Add(Tuple.Create(r,r.enabled));r.enabled=false;}
        foreach(var r in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true)){skins.Add(Tuple.Create(r,r.updateWhenOffscreen));r.updateWhenOffscreen=true;}
        outside=car.InverseTransformPoint(outsideWorld);exitWorld=outsideWorld;
        standingLocal=car.InverseTransformPoint(player.position);standingRotation=Quaternion.Inverse(car.rotation)*player.rotation;
        // Route a character on the passenger side around the nearest end of the car.
        if(standingLocal.x>-.98f) {
            float end=standingLocal.z>=0?2.7f:-2.7f;
            approach.Enqueue(new Vector3(standingLocal.x,outside.y,end));
            approach.Enqueue(new Vector3(outside.x,outside.y,end));
        }
        approach.Enqueue(outside);
    }
    void Bool(string name,bool value){if(bools.ContainsKey(name))animator.SetBool(name,value);}
    void Float(string name,float value){if(floats.ContainsKey(name))animator.SetFloat(name,value);}
    public bool BeginExit(Vector3 destination) {
        if(!Motion.BeginExit())return false;
        exitWorld=destination;outside=car.InverseTransformPoint(destination);return true;
    }
    public void Tick(float dt,bool blocked,ShowroomState doors) {
        if(disposed)return;
        Motion.Step(dt,blocked,doors.Progress[1]);
        Motion.ApplyDoorRequest(doors);
        float blend=Motion.SeatBlend;
        Bool("attach_chair",blend>.12f);Bool("inWater",false);Bool("onGround",true);
        Float("sideway_speed",0);Float("turn_speed",0);Float("forward_speed",0);
        visual.localPosition=visualPosition;visual.localRotation=visualRotation;
        visual.localScale=visualScale*Mathf.Lerp(1,CabinAppearance.DriverScale,blend);
        if(Motion.Phase==CabinPhase.Approach) {
            if(!blocked&&approach.Count>0) {
                var target=approach.Peek();var next=Vector3.MoveTowards(standingLocal,target,Mathf.Min(dt,.1f)*1.65f);
                var world=car.TransformPoint(next);
                var ground=Physics.RaycastAll(world+Vector3.up*1.5f,Vector3.down,3,~0,QueryTriggerInteraction.Ignore)
                    .Where(h=>!h.collider.transform.IsChildOf(player)&&!h.collider.transform.IsChildOf(car)).OrderBy(h=>h.distance).ToArray();
                if(ground.Length==0||ground[0].normal.y<.65f){Failed=true;return;}
                world.y=ground[0].point.y+.06f;next=car.InverseTransformPoint(world);
                if(!ClearCapsule(world)){Failed=true;return;}
                var direction=target-standingLocal;direction.y=0;
                if(direction.sqrMagnitude>.0001f)standingRotation=Quaternion.RotateTowards(standingRotation,Quaternion.LookRotation(direction),dt*360);
                standingLocal=next;Float("forward_speed",1.65f);
                var remaining=next-target;remaining.y=0;if(remaining.magnitude<.025f)approach.Dequeue();
            }
            player.SetPositionAndRotation(car.TransformPoint(standingLocal),car.rotation*standingRotation);
            if(approach.Count==0)Motion.Arrive();return;
        }
        // Turn into the doorway and lower into the seat while the native chair pose blends.
        var seatRoot=VehicleInterior.Seat-new Vector3(0,.7f,0);
        player.SetPositionAndRotation(car.TransformPoint(Vector3.Lerp(outside,seatRoot,blend)),
            car.rotation*Quaternion.Slerp(Quaternion.Euler(0,Motion.Phase==CabinPhase.Exit||Motion.Phase==CabinPhase.Complete?-90:90,0),Quaternion.identity,blend));
        if(blend>0) {
            // Align the actual pelvis, allowing for gender, equipment and the native chair pose.
            visual.position+= (car.TransformPoint(VehicleInterior.Seat)-hips.position)*blend;
            if(bones.TryGetValue("Head",out var head)) {
                float headHeight=car.InverseTransformPoint(head.position).y;
                float scale=visual.lossyScale.y/car.lossyScale.y;
                visual.position-=car.up*CabinAppearance.RoofLowering(headHeight,scale)*blend;
            }
            // Withdraw the resting arm before opening the door; never follow a scissor door upwards.
            float restTarget=Motion.CanDrive&&!doors.Open[1]&&doors.Progress[1]<.03f?1:0;
            float poseDt=blocked&&!Motion.CanDrive?0:Mathf.Min(dt,.1f);
            windowRest=Mathf.MoveTowards(windowRest,restTarget,poseDt*5);
            var lap=new Vector3(-.50f,.52f,-.08f);
            var sill=new Vector3(-.78f,.82f,.08f);
            var leftPole=Vector3.Lerp(new Vector3(-.6f,.55f,-.25f),new Vector3(-.79f,.83f,-.28f),windowRest);
            Limb("LeftArm","LeftForeArm","LeftHand",car.TransformPoint(Vector3.Lerp(lap,sill,windowRest)),leftPole,blend);
            Limb("RightArm","RightForeArm","RightHand",interior.RightGrip,new Vector3(-.05f,.65f,-.05f),blend);
            AimHand("RightHand","RightHandMiddle1",interior.RightHandForward,blend);
            AimHand("LeftHand","LeftHandMiddle1",car.forward,blend*windowRest);
            Limb("LeftUpLeg","LeftLeg","LeftFoot",car.TransformPoint(new Vector3(-.46f,.22f,.59f)),new Vector3(-.48f,.46f,.1f),blend);
            Limb("RightUpLeg","RightLeg","RightFoot",car.TransformPoint(new Vector3(-.30f,.22f,.59f)),new Vector3(-.27f,.46f,.1f),blend);
        }
    }
    void AimHand(string handName,string fingerName,Vector3 direction,float blend) {
        if(!bones.TryGetValue(handName,out var hand)||!bones.TryGetValue(fingerName,out var finger))return;
        var current=finger.position-hand.position;
        if(current.sqrMagnitude<.00001f)return;
        hand.rotation=Quaternion.Slerp(hand.rotation,Quaternion.FromToRotation(current,direction)*hand.rotation,blend);
    }
    public void TickTransformer(TransformerVisual robot,float progress) {
        if(disposed)return;
        Bool("attach_chair",true);Bool("inWater",false);Bool("onGround",true);
        Float("forward_speed",0);Float("sideway_speed",0);Float("turn_speed",0);
        visual.localPosition=visualPosition;visual.localRotation=visualRotation;
        visual.localScale=visualScale*CabinAppearance.DriverScale;
        player.SetPositionAndRotation(car.TransformPoint(VehicleInterior.Seat-new Vector3(0,.7f,0)),car.rotation);
        var pelvis=robot.Node("target:NEW - REFERENCE pelvis").position;
        // Match the actual installed cabin seat at frame zero, then hand off to
        // the approved nape trajectory after the shell starts opening.
        float transfer=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.16f,progress));
        if(transformerStart.TryGetValue("Hips",out var initialHips))pelvis=Vector3.Lerp(car.TransformPoint(initialHips),pelvis,transfer);
        visual.position+=pelvis-hips.position;
        foreach(var pair in new[]{Tuple.Create("Left","R"),Tuple.Create("Right","L")}) {
            string side=pair.Item1,reference=pair.Item2;
            var foot=robot.Node("target:NEW - REFERENCE boot "+reference).position;
            var hand=robot.Node("target:NEW - REFERENCE hand "+reference).position;
            var knee=car.InverseTransformPoint(robot.Node("rider.calf."+reference).position);
            var elbow=car.InverseTransformPoint(robot.Node("rider.forearm."+reference).position);
            float limbs=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.12f,progress));
            if(transformerStart.TryGetValue(side+"Foot",out var initialFoot))foot=Vector3.Lerp(car.TransformPoint(initialFoot),foot,limbs);
            if(transformerStart.TryGetValue(side+"Hand",out var initialHand))hand=Vector3.Lerp(car.TransformPoint(initialHand),hand,limbs);
            if(transformerStart.TryGetValue(side+"Leg",out var initialKnee))knee=Vector3.Lerp(initialKnee,knee,limbs);
            if(transformerStart.TryGetValue(side+"ForeArm",out var initialElbow))elbow=Vector3.Lerp(initialElbow,elbow,limbs);
            Limb(side+"UpLeg",side+"Leg",side+"Foot",foot,knee,1);
            Limb(side+"Arm",side+"ForeArm",side+"Hand",hand,elbow,1);
            AimHand(side+"Hand",side+"HandMiddle1",car.forward,1);
        }
        windowRest=0;
    }
    // Two-bone IK keeps limb lengths unchanged; pole positions control knee/elbow bend.
    void Limb(string upperName,string lowerName,string endName,Vector3 target,Vector3 pole,float blend) {
        if(!bones.TryGetValue(upperName,out var a)||!bones.TryGetValue(lowerName,out var b)||!bones.TryGetValue(endName,out var end))return;
        var startA=a.rotation;var startB=b.rotation;var endRotation=end.rotation;
        float first=Vector3.Distance(a.position,b.position),second=Vector3.Distance(b.position,end.position);
        Vector3 direction=target-a.position;float distance=Mathf.Clamp(direction.magnitude,Mathf.Abs(first-second)+.001f,first+second-.001f);
        if(first<.001f||second<.001f||direction.sqrMagnitude<.00001f)return;
        direction.Normalize();var bend=Vector3.ProjectOnPlane(car.TransformPoint(pole)-a.position,direction).normalized;
        float along=(first*first-second*second+distance*distance)/(2*distance);
        Vector3 knee=a.position+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,first*first-along*along));
        a.rotation=Quaternion.FromToRotation(b.position-a.position,knee-a.position)*a.rotation;
        b.rotation=Quaternion.FromToRotation(end.position-b.position,target-b.position)*b.rotation;
        var finalA=a.rotation;var finalB=b.rotation;
        a.rotation=Quaternion.Slerp(startA,finalA,blend);b.rotation=Quaternion.Slerp(startB,finalB,blend);end.rotation=endRotation;
    }
    bool ClearCapsule(Vector3 foot) {
        return !Physics.OverlapCapsule(foot+Vector3.up*.32f,foot+Vector3.up*1.5f,.27f,~0,QueryTriggerInteraction.Ignore)
            .Any(c=>!c.transform.IsChildOf(player)&&!c.transform.IsChildOf(car));
    }
    public void Dispose() {
        if(disposed)return;disposed=true;
        if(animator!=null) {
            foreach(var p in bools)animator.SetBool(p.Key,p.Value);
            foreach(var p in floats)animator.SetFloat(p.Key,p.Value);
            animator.cullingMode=culling;animator.applyRootMotion=rootMotion;
        }
        if(visual!=null){visual.localPosition=visualPosition;visual.localRotation=visualRotation;visual.localScale=visualScale;}
        foreach(var item in equipment)if(item.Item1!=null)item.Item1.enabled=item.Item2;
        foreach(var item in skins)if(item.Item1!=null)item.Item1.updateWhenOffscreen=item.Item2;
    }
}
}
