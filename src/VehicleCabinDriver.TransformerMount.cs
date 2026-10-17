using UnityEngine;
using System.Collections.Generic;
namespace Rachel350Z {
internal sealed partial class VehicleCabinDriver {
    readonly TransformerMountMotion robotMount=new TransformerMountMotion();
    readonly Vector3[] robotHands=new Vector3[2],robotFeet=new Vector3[2];
    readonly bool[] robotHandContact=new bool[2],robotFootContact=new bool[2];
    readonly Quaternion[] robotStandingFootRotation=new Quaternion[2];
    readonly Quaternion[] robotHandOrientation=new Quaternion[2],robotFootOrientation=new Quaternion[2];
    readonly float[] robotHandSupport=new float[2],robotFootSupport=new float[2];
    readonly Dictionary<Transform,Quaternion> robotLocalPose=new Dictionary<Transform,Quaternion>();
    Vector3 robotGround,robotReachOffset;
    Vector3 robotPathPelvis,robotPathVelocity;
    bool robotPathReady;
    Quaternion robotEntryRotation;
    public bool RobotTransitionActive=>robotMount.Active;
    public bool RobotTransitionComplete=>robotMount.Complete;
    public bool RobotTransitionEntering=>robotMount.Entering;
    public float RobotTransitionProgress=>robotMount.Progress;
    public Vector3 RobotExitPosition=>robotGround;
    public bool TryBeginRobotEntry(Vector3 startWorld) {
        if(disposed||!robotMount.Begin(true))return false;
        robotGround=startWorld;robotEntryRotation=player.rotation;
        for(int i=0;i<2;i++)if(bones.TryGetValue(i==0?"LeftFoot":"RightFoot",out var foot))robotStandingFootRotation[i]=Quaternion.Inverse(player.rotation)*foot.rotation;
        CaptureRobotMountPose();
        return true;
    }
    public bool TryBeginRobotExit(Vector3 landingWorld) {
        if(disposed||!robotMount.Begin(false))return false;
        robotGround=landingWorld;exitWorld=landingWorld;robotEntryRotation=player.rotation;CaptureRobotMountPose();return true;
    }
    void CaptureRobotMountPose() {
        robotLocalPose.Clear();robotReachOffset=robotPathVelocity=Vector3.zero;robotPathReady=false;
        // Only bones owned by this procedural layer. Keep native look/head motion.
        foreach(string name in new[]{"Hips","Spine","Spine1","Spine2","Chest","UpperChest","LeftArm","LeftForeArm","LeftHand","RightArm","RightForeArm","RightHand","LeftUpLeg","LeftLeg","LeftFoot","RightUpLeg","RightLeg","RightFoot"})
            if(bones.TryGetValue(name,out var bone))robotLocalPose[bone]=bone.localRotation;
        if(!robotMount.Entering)foreach(string name in new[]{"Neck","Head"})
            if(bones.TryGetValue(name,out var bone))robotLocalPose[bone]=bone.localRotation;
        for(int i=0;i<2;i++) {
            string side=i==0?"Left":"Right";
            if(bones.TryGetValue(side+"Hand",out var hand))robotHandOrientation[i]=hand.rotation;
            if(bones.TryGetValue(side+"Foot",out var foot))robotFootOrientation[i]=foot.rotation;
        }
    }
    void RebaseRobotMountPose(){foreach(var pose in robotLocalPose)if(pose.Key!=null)pose.Key.localRotation=pose.Value;}
    static Quaternion MountFacing(Transform vehicle) {
        var forward=Vector3.ProjectOnPlane(vehicle.forward,Vector3.up);
        return forward.sqrMagnitude>.0001f?Quaternion.LookRotation(forward,Vector3.up):Quaternion.identity;
    }
    static Vector3 MountPlatform(TransformerVisual robot)=>robot.MountStandPlatform;
    static Vector3 ClimbApproach(TransformerVisual robot,Transform vehicle,Vector3 ground) {
        var p=(robot.MountClimbFoot(0,false)+robot.MountClimbFoot(0,true))*.5f-MountFacing(vehicle)*Vector3.forward*.48f;
        p.y=ground.y;return p;
    }
    static Vector3 GroundApproachAt(TransformerVisual robot,Transform vehicle,Vector3 ground,float blend) {
        var end=ClimbApproach(robot,vehicle,ground);var yaw=MountFacing(vehicle);
        var local=Quaternion.Inverse(yaw)*(ground-vehicle.position);
        if(local.z< -1.3f)return Vector3.Lerp(ground,end,blend);
        // Approach from either side without walking straight through the chest.
        float side=local.x<0?-1:1;
        var endLocal=Quaternion.Inverse(yaw)*(end-vehicle.position);
        var a=vehicle.position+yaw*new Vector3(side*1.85f,local.y,local.z);
        var b=vehicle.position+yaw*new Vector3(side*1.85f,local.y,-2.1f);
        var c=vehicle.position+yaw*new Vector3(endLocal.x,local.y,-2.1f);
        float one=Vector3.Distance(ground,a),two=Vector3.Distance(a,b),three=Vector3.Distance(b,c),four=Vector3.Distance(c,end);
        float distance=(one+two+three+four)*Mathf.Clamp01(blend);
        if(distance<one)return Vector3.Lerp(ground,a,one>.001f?distance/one:1);distance-=one;
        if(distance<two)return Vector3.Lerp(a,b,two>.001f?distance/two:1);distance-=two;
        if(distance<three)return Vector3.Lerp(b,c,three>.001f?distance/three:1);distance-=three;
        return Vector3.Lerp(c,end,four>.001f?distance/four:1);
    }
    static Vector3 ClimbPelvis(TransformerVisual robot,Vector3 approach,int level) =>level<0?approach+Vector3.up*.92f:
        level>=robot.MountClimbLevels?MountPlatform(robot)+Vector3.up*.92f:robot.MountClimbPelvis(level);
    static Vector3 ClimbContact(TransformerVisual robot,Transform vehicle,Vector3 approach,int level,bool hand,bool right) {
        if(level<0)return approach+MountFacing(vehicle)*new Vector3((right?1:-1)*(hand?.35f:.14f),hand?.74f:.02f,hand?.08f:.03f);
        if(level>=robot.MountClimbLevels)return hand?robot.Node("target:NEW - REFERENCE hand "+(right?"L":"R")).position:robot.MountStandFoot(right);
        // Surface route index 0 is +vehicle X; the live avatar's Left is -X.
        // Invert only the mesh-route index, as already done by the seat refs.
        return hand?robot.MountClimbGrip(level,!right):robot.MountClimbFoot(level,!right);
    }
    static Vector3 ClimbNormal(TransformerVisual robot,Transform vehicle,int level,bool hand,bool right) {
        if(level<0||level>=robot.MountClimbLevels)return hand?-vehicle.forward:Vector3.up;
        return hand?robot.MountClimbGripNormal(level,!right):robot.MountClimbFootNormal(level,!right);
    }
    // Lower behind the head before sliding into the existing seat. In reverse,
    // withdraw behind it before standing; the seated boot refs are not a platform.
    static Vector3 BlendMountSeat(Vector3 standing,Vector3 seated,float blend) {
        var point=Vector3.Lerp(standing,seated,TransformerMountMotion.SeatForward(blend));
        point.y=Mathf.Lerp(standing.y,seated.y,TransformerMountMotion.SeatLower(blend));
        return point;
    }
    // Sample the same supported route for the player root and procedural pose.
    public static Vector3 RobotMountPelvisAt(TransformerVisual robot,Transform vehicle,Vector3 groundWorld,bool entering,float progress) {
        var s=TransformerMountMotion.Sample(progress,entering);
        var seated=robot.Node("target:NEW - REFERENCE pelvis").position;
        Vector3 path;
        if(entering) {
            var approach=ClimbApproach(robot,vehicle,groundWorld);
            if(progress<TransformerMountMotion.ClimbApproachEnd)path=GroundApproachAt(robot,vehicle,groundWorld,Mathf.SmoothStep(0,1,progress/TransformerMountMotion.ClimbApproachEnd))+Vector3.up*.92f;
            else {
                var c=TransformerMountMotion.Climb(progress,robot.MountClimbLevels+1);
                var from=ClimbPelvis(robot,approach,c.FromLevel);var to=ClimbPelvis(robot,approach,c.ToLevel);
                if(c.ToLevel==robot.MountClimbLevels-1) {
                    // On the wide final step, carry the hips with the boots.
                    // Holding the old rear offset/high stance here demanded
                    // a metre-long diagonal leg before the other foot moved.
                    var oldLeft=ClimbContact(robot,vehicle,approach,c.FromLevel,false,false);
                    var oldRight=ClimbContact(robot,vehicle,approach,c.FromLevel,false,true);
                    var left=Vector3.Lerp(oldLeft,ClimbContact(robot,vehicle,approach,c.ToLevel,false,false),c.LeftFoot);
                    var right=Vector3.Lerp(oldRight,ClimbContact(robot,vehicle,approach,c.ToLevel,false,true),c.RightFoot);
                    var support=(left+right)*.5f;var oldSupport=(oldLeft+oldRight)*.5f;
                    path=support+(from-oldSupport)*(1-c.RightHand);
                    float stance=.68f+.14f*Mathf.SmoothStep(0,1,(c.Pull-.72f)/.28f);
                    path.y=Mathf.Lerp(from.y,support.y+stance,c.RightHand);
                } else if(c.ToLevel>=robot.MountClimbLevels-2) {
                    // Raise the torso behind the lip while reaching. Only then
                    // carry it across with the alternating boot transfers.
                    float clear=Mathf.Max(from.y,robot.MountClimbMantleHeight);
                    float across=(c.LeftFoot+c.RightFoot)*.5f;
                    path=Vector3.Lerp(from,to,across);
                    path.y=Mathf.Lerp(from.y,clear,c.RightHand)+Mathf.Max(0,to.y-clear)*c.Pull;
                    if(to.y<clear)path.y=Mathf.Lerp(path.y,to.y,Mathf.SmoothStep(0,1,(c.Pull-.72f)/.28f));
                } else path=Vector3.Lerp(from,to,c.Pull);
                path=robot.MountClimbSafePelvis(path,.26f);
            }
        } else {
            var takeoff=MountPlatform(robot)+Vector3.up*.92f;var landing=groundWorld+Vector3.up*.92f;
            path=Vector3.Lerp(takeoff,landing,s.Travel);
            path.y=TransformerDismountArc.Height(s.Travel,takeoff.y,landing.y);
        }
        return BlendMountSeat(path,seated,s.SeatBlend);
    }
    static float ContactArc(float blend)=>blend<=0||blend>=1?0:Mathf.Sin(Mathf.PI*blend);
    Vector3 WalkingFoot(TransformerVisual robot,bool right,float progress) {
        const int steps=8;
        float phase=Mathf.Clamp01(progress/TransformerMountMotion.ClimbApproachEnd)*steps,first=right?1:0;
        var offset=MountFacing(car)*new Vector3(right?.14f:-.14f,.02f,.03f);
        if(phase<first)return robotGround+offset;
        int step=Mathf.Min(steps-1,(int)(Mathf.Max(0,phase-first)/2)*2+(int)first);
        float from=step<=1?0:step/(float)steps,to=Mathf.Min(1,(step+2)/(float)steps);
        float swing=Mathf.SmoothStep(0,1,phase-step);
        return Vector3.Lerp(GroundApproachAt(robot,car,robotGround,from),GroundApproachAt(robot,car,robotGround,to),swing)+offset+Vector3.up*(.10f*ContactArc(swing));
    }
    void ClimbingTargets(TransformerVisual robot,float progress,Vector3 pelvis,Quaternion body) {
        var approach=ClimbApproach(robot,car,robotGround);
        var c=TransformerMountMotion.Climb(progress,robot.MountClimbLevels+1);
        for(int i=0;i<2;i++) {
            bool right=i==1;
            float hand=right?c.RightHand:c.LeftHand,foot=right?c.RightFoot:c.LeftFoot;
            robotHands[i]=Vector3.Lerp(ClimbContact(robot,car,approach,c.FromLevel,true,right),ClimbContact(robot,car,approach,c.ToLevel,true,right),hand)
                +Vector3.up*(.07f*ContactArc(hand))-car.forward*(.06f*ContactArc(hand));
            robotFeet[i]=Vector3.Lerp(ClimbContact(robot,car,approach,c.FromLevel,false,right),ClimbContact(robot,car,approach,c.ToLevel,false,right),foot)
                +Vector3.up*(.10f*ContactArc(foot))-car.forward*(.12f*ContactArc(foot));
            robotHandContact[i]=hand<=0||hand>=1;robotFootContact[i]=foot<=0||foot>=1;
            robotHandSupport[i]=1-ContactArc(hand);robotFootSupport[i]=1-ContactArc(foot);
            // At the upper mantle the low rail cannot be held below the boots.
            // Feet take the load while hands release, balance, then find the seat.
            // Release the low spoiler rail on the intermediate ROOF mantle,
            // before it would drag the shoulders down through the hatch.
            int mantle=robot.MountClimbLevels-2;
            float release=c.ToLevel>mantle?1:c.ToLevel==mantle?Mathf.SmoothStep(0,1,c.Pull/.35f):0;
            if(release>0) {
                var balance=pelvis+body*new Vector3(right?.36f:-.36f,.02f,.15f);
                robotHands[i]=Vector3.Lerp(robotHands[i],balance,release);robotHandContact[i]=false;
                robotHandSupport[i]*=1-release;
            }
        }
    }
    Vector3 ReachCorrection(string upper,string lower,string end,Vector3 target) {
        if(!bones.TryGetValue(upper,out var a)||!bones.TryGetValue(lower,out var b)||!bones.TryGetValue(end,out var e))return Vector3.zero;
        float reach=(Vector3.Distance(a.position,b.position)+Vector3.Distance(b.position,e.position))*.985f;
        var delta=target-a.position;float distance=delta.magnitude;
        return distance>reach&&distance>.001f?delta*((distance-reach)/distance):Vector3.zero;
    }
    Vector3 MeasureClimbReach() {
        // Fit the pelvis to planted contacts using measured, unscaled bone lengths.
        var origin=visual.position;
        for(int pass=0;pass<8;pass++) {
            Vector3 correction=Vector3.zero;float weight=0;
            for(int i=0;i<2;i++) {
                string side=i==0?"Left":"Right";
                correction+=ReachCorrection(side+"Arm",side+"ForeArm",side+"Hand",robotHands[i])*robotHandSupport[i];weight+=robotHandSupport[i];
                correction+=ReachCorrection(side+"UpLeg",side+"Leg",side+"Foot",robotFeet[i])*robotFootSupport[i];weight+=robotFootSupport[i];
            }
            if(weight<.001f||correction.sqrMagnitude<.000001f)break;
            var proposed=visual.position+correction*(.9f/weight);
            visual.position=origin+Vector3.ClampMagnitude(proposed-origin,.28f);
        }
        var result=visual.position-origin;visual.position=origin;return result;
    }
    void AimClimbPalm(string side,Vector3 surfaceNormal,float blend) {
        var fingers=Vector3.ProjectOnPlane(car.up,surfaceNormal);
        if(fingers.sqrMagnitude<.01f)fingers=Vector3.ProjectOnPlane(car.forward,surfaceNormal);
        AimHand(side+"Hand",side+"HandMiddle1",fingers.normalized,blend);
        if(!bones.TryGetValue(side+"Hand",out var hand)||!bones.TryGetValue(side+"HandIndex1",out var index)
            ||!bones.TryGetValue(side+"HandPinky1",out var pinky)||!bones.TryGetValue(side+"HandMiddle1",out var middle))return;
        var axis=(middle.position-hand.position).normalized;
        var palm=Vector3.Cross(index.position-hand.position,pinky.position-hand.position)*(side=="Left"?1:-1);
        palm=Vector3.ProjectOnPlane(palm,axis);var toward=Vector3.ProjectOnPlane(-surfaceNormal,axis);
        if(palm.sqrMagnitude>.000001f&&toward.sqrMagnitude>.000001f)
            hand.rotation=Quaternion.Slerp(hand.rotation,Quaternion.FromToRotation(palm,toward)*hand.rotation,blend);
    }
    public bool TickRobotTransition(TransformerVisual robot,float dt,bool blocked) {
        if(disposed||robot==null||!robotMount.Started)return false;
        robotMount.Step(dt,blocked);
        float poseDt=blocked||float.IsNaN(dt)||float.IsInfinity(dt)?0:Mathf.Clamp(dt,0,.05f);
        var s=robotMount.Pose;var facing=MountFacing(car);
        if(robotMount.Entering)facing=Quaternion.Slerp(robotEntryRotation,facing,Mathf.SmoothStep(0,1,s.Progress/TransformerMountMotion.ClimbApproachEnd));
        var pathPelvis=RobotMountPelvisAt(robot,car,robotGround,robotMount.Entering,s.Progress);
        if(robotMount.Entering) {
            if(!robotPathReady){robotPathPelvis=pathPelvis;robotPathReady=true;}
            else if(poseDt>0)robotPathPelvis=Vector3.SmoothDamp(robotPathPelvis,pathPelvis,ref robotPathVelocity,.075f,6,poseDt);
            pathPelvis=robotPathPelvis;
            if(s.Progress>=TransformerMountMotion.ClimbApproachEnd&&s.SeatBlend<=0) {
                pathPelvis=robot.MountClimbSafePelvis(pathPelvis,.26f);robotPathPelvis=pathPelvis;
            }
        }
        player.SetPositionAndRotation(pathPelvis-Vector3.up*.92f,facing);
        Bool("attach_chair",s.SeatBlend>.5f);Bool("inWater",false);Bool("onGround",s.Travel<=0||s.Travel>=1);
        Float("forward_speed",0);Float("sideway_speed",0);Float("turn_speed",0);
        RebaseRobotMountPose();
        visual.localPosition=visualPosition;visual.localRotation=visualRotation;
        visual.localScale=visualScale*Mathf.Lerp(1,CabinAppearance.DriverScale,s.SeatBlend);
        // Keep the chest outside the rear panel instead of leaning into it.
        float lean=robotMount.Entering?-3*Mathf.SmoothStep(0,1,(s.Progress-TransformerMountMotion.ClimbApproachEnd)/.08f)
            *(1-Mathf.SmoothStep(0,1,(s.Progress-.76f)/.14f))*(1-s.SeatBlend):s.PitchDegrees;
        var body=Quaternion.Slerp(facing*Quaternion.Euler(lean,0,0),car.rotation,s.SeatBlend);
        visual.rotation=body*visualRotation;
        var pelvis=pathPelvis-Vector3.up*(.20f*s.Crouch);
        float alignment=robotMount.Entering?Mathf.SmoothStep(0,1,s.Progress/.10f):1-Mathf.SmoothStep(0,1,(s.Progress-.90f)/.10f);
        visual.position+=(pelvis-hips.position)*alignment;
        pelvis=hips.position;
        if(!robotMount.Entering)ApplyTransformerFlipTorso(s,body);
        bool climbing=robotMount.Entering&&s.Progress>=TransformerMountMotion.ClimbApproachEnd;
        if(climbing) {
            ClimbingTargets(robot,s.Progress,pelvis,body);
            if(s.SeatBlend<=0&&poseDt>0) {
                var measured=MeasureClimbReach();
                var filtered=Vector3.Lerp(robotReachOffset,measured,1-Mathf.Exp(-12*poseDt));
                robotReachOffset=Vector3.MoveTowards(robotReachOffset,filtered,.9f*poseDt);
            }
            visual.position+=robotReachOffset*(1-s.SeatBlend);
            if(s.SeatBlend<=0)visual.position+=robot.MountClimbSafePelvis(hips.position,.26f)-hips.position;
            pelvis=hips.position;
        }
        for(int i=0;i<2;i++) {
            string side=i==0?"Left":"Right",reference=i==0?"R":"L";float sign=i==0?-1:1;
            var foot=pelvis+body*new Vector3(sign*.14f,-.90f+.46f*s.Tuck,.03f+.30f*s.Tuck)+Vector3.up*(.20f*s.Crouch*alignment);
            var hand=pelvis+body*new Vector3(sign*(.35f-.17f*s.Tuck),-.18f+.15f*s.Tuck,.08f+.30f*s.Tuck);
            if(climbing){foot=robotFeet[i];hand=robotHands[i];}
            else if(robotMount.Entering&&s.Progress<TransformerMountMotion.ClimbApproachEnd)foot=WalkingFoot(robot,i==1,s.Progress);
            var knee=pelvis+body*new Vector3(sign*.22f,-.25f,.65f);
            var elbow=pelvis+body*new Vector3(sign*.65f,.02f,-.12f);
            if(s.SeatBlend>0) {
                // Keep knees beside the helmet while withdrawing the seated
                // boots, rather than bending both legs through its centre.
                float around=Mathf.SmoothStep(0,1,s.SeatBlend/.7f);
                knee=pelvis+facing*new Vector3(sign*Mathf.Lerp(.22f,.62f,around),-.25f,Mathf.Lerp(.65f,-.10f,around));
            }
            if(climbing) {
                // Use each real hip and the body's sagittal hinge. An outward,
                // rearward world-space pole made knees fold sideways/backwards.
                // Keep pelvis clearance and planted contacts independent of it.
                if(bones.TryGetValue(side+"UpLeg",out var thigh)) {
                    var leg=Quaternion.Inverse(body)*(foot-thigh.position);
                    TransformerClimbLeg.PoleOffset(leg.y,leg.z,out float poleUp,out float poleForward);
                    knee=thigh.position+body*new Vector3(0,poleUp,poleForward);
                }
                elbow=pelvis+facing*new Vector3(sign*.58f,.10f,-.12f);
            }
            if(!robotMount.Entering&&TryTransformerFlipTargets(s,pelvis,body,i==1,out var flipFoot,out var flipHand,out var flipKnee,out var flipElbow)) {
                foot=flipFoot;hand=flipHand;knee=flipKnee;elbow=flipElbow;
            }
            var seatedFoot=robot.Node("target:NEW - REFERENCE boot "+reference).position;
            var seatedHand=robot.Node("target:NEW - REFERENCE hand "+reference).position;
            knee=Vector3.Lerp(knee,robot.Node("rider.calf."+reference).position,s.SeatBlend);
            elbow=Vector3.Lerp(elbow,robot.Node("rider.forearm."+reference).position,s.SeatBlend);
            Limb(side+"UpLeg",side+"Leg",side+"Foot",Vector3.Lerp(foot,seatedFoot,s.SeatBlend),car.InverseTransformPoint(knee),1);
            Limb(side+"Arm",side+"ForeArm",side+"Hand",Vector3.Lerp(hand,seatedHand,s.SeatBlend),car.InverseTransformPoint(elbow),1);
            if(climbing) {
                var contact=TransformerMountMotion.Climb(s.Progress,robot.MountClimbLevels+1);
                float handMove=i==0?contact.LeftHand:contact.RightHand,footMove=i==0?contact.LeftFoot:contact.RightFoot;
                if(robotHandContact[i]) {
                    var normal=Vector3.Slerp(ClimbNormal(robot,car,contact.FromLevel,true,i==1),ClimbNormal(robot,car,contact.ToLevel,true,i==1),handMove);
                    AimClimbPalm(side,normal,1-s.SeatBlend);
                }
                if(robotFootContact[i]&&bones.TryGetValue(side+"Foot",out var boot)) {
                    var normal=Vector3.Slerp(ClimbNormal(robot,car,contact.FromLevel,false,i==1),ClimbNormal(robot,car,contact.ToLevel,false,i==1),footMove);
                    boot.rotation=Quaternion.Slerp(boot.rotation,Quaternion.FromToRotation(Vector3.up,normal)*facing*robotStandingFootRotation[i],1-s.SeatBlend);
                }
            }
            if(robotMount.Entering) {
                float follow=1-Mathf.Exp(-30*poseDt);
                if(bones.TryGetValue(side+"Hand",out var wrist)) {
                    robotHandOrientation[i]=climbing?Quaternion.Slerp(robotHandOrientation[i],wrist.rotation,follow):wrist.rotation;
                    wrist.rotation=robotHandOrientation[i];
                }
                if(bones.TryGetValue(side+"Foot",out var ankle)) {
                    robotFootOrientation[i]=climbing?Quaternion.Slerp(robotFootOrientation[i],ankle.rotation,follow):ankle.rotation;
                    ankle.rotation=robotFootOrientation[i];
                }
            }
        }
        windowRest=0;
        if(robotMount.Complete) {
            if(robotMount.Entering){Motion.SitImmediately();TickTransformer(robot,1);}
            else {
                player.SetPositionAndRotation(robotGround,facing);
                visual.localPosition=visualPosition;visual.localRotation=visualRotation;visual.localScale=visualScale;
                Bool("attach_chair",false);Bool("onGround",true);
            }
        }
        return true;
    }
}
}
