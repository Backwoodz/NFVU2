using UnityEngine;
namespace Rachel350Z {
internal sealed partial class VehicleCabinDriver {
    // The caller restores its captured bone basis every tick before applying
    // this layer. The additive spine bends therefore cannot feed back into IK.
    void ApplyTransformerFlipTorso(TransformerMountSample sample,Quaternion body) {
        if(robotMount.Entering||sample.SeatBlend>0)return;
        var shape=TransformerFlipShape.Sample(sample);var axis=body*Vector3.right;
        if(bones.TryGetValue("Hips",out var hip))hip.rotation=Quaternion.AngleAxis(6*sample.Tuck,axis)*hip.rotation;
        int count=0;foreach(string name in new[]{"Spine","Spine1","Spine2"})if(bones.ContainsKey(name))count++;
        if(count>0)foreach(string name in new[]{"Spine","Spine1","Spine2"})
            if(bones.TryGetValue(name,out var spine))spine.rotation=Quaternion.AngleAxis(shape.SpineDegrees/count,axis)*spine.rotation;
        if(bones.TryGetValue("Neck",out var neck))neck.rotation=Quaternion.AngleAxis(shape.HeadDegrees*.5f,axis)*neck.rotation;
        if(bones.TryGetValue("Head",out var head))head.rotation=Quaternion.AngleAxis(shape.HeadDegrees*.5f,axis)*head.rotation;
    }
    bool TryTransformerFlipTargets(TransformerMountSample sample,Vector3 pelvis,Quaternion body,bool right,
        out Vector3 foot,out Vector3 hand,out Vector3 knee,out Vector3 elbow) {
        foot=hand=knee=elbow=Vector3.zero;
        if(robotMount.Entering||sample.SeatBlend>0)return false;
        string side=right?"Right":"Left";float sign=right?1:-1;
        if(!bones.TryGetValue(side+"UpLeg",out var thigh)||!bones.TryGetValue(side+"Leg",out var shin)||!bones.TryGetValue(side+"Foot",out var boot)
            ||!bones.TryGetValue(side+"Arm",out var arm)||!bones.TryGetValue(side+"ForeArm",out var forearm)||!bones.TryGetValue(side+"Hand",out var palm))return false;
        float thighLength=Vector3.Distance(thigh.position,shin.position),shinLength=Vector3.Distance(shin.position,boot.position);
        var shape=TransformerFlipShape.Sample(sample);
        shape.Leg(thighLength,shinLength,out float kneeUp,out float kneeForward,out float footUp,out float footForward);
        knee=thigh.position+body*new Vector3(sign*.025f*sample.Tuck,kneeUp,kneeForward);
        foot=thigh.position+body*new Vector3(0,footUp,footForward);
        float armLength=Vector3.Distance(arm.position,forearm.position)+Vector3.Distance(forearm.position,palm.position);
        var balanced=arm.position+body*new Vector3(sign*.21f,-armLength*.72f,.12f);
        var overhead=arm.position+body*new Vector3(sign*.06f,armLength*.92f,.06f);
        // Palms sweep from overhead to the folded upper shins, then release
        // as the legs open for landing. Keep targets inside measured arm reach.
        var tucked=knee+body*new Vector3(sign*.025f,-.06f,-.07f);
        hand=Vector3.Lerp(Vector3.Lerp(balanced,overhead,shape.ArmRaise),tucked,sample.Tuck);
        hand=arm.position+Vector3.ClampMagnitude(hand-arm.position,armLength*.97f);
        elbow=arm.position+body*new Vector3(sign*.48f,-.10f,.12f+.22f*sample.Tuck);
        return true;
    }
}
}
