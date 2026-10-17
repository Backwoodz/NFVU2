using System;
using System.Collections.Generic;
using UnityEngine;
namespace Rachel350Z {
internal sealed partial class TransformerVisual {
    float mountCrouch;
    Matrix4x4[] mountSource,mountDelta;

    // Called before Evaluate. Only the display bends: the root, collision hull
    // and both planted boot poses keep their existing world positions.
    public void SetMountPose(float progress,bool entering) {
        if(float.IsNaN(progress)||float.IsInfinity(progress)){ClearMountPose();return;}
        float p=Mathf.Clamp01(progress);
        if(entering)
            mountCrouch=0; // The climber needs stationary real contacts.
        else
            mountCrouch=.45f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.22f,p))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.22f,.4f,p)));
    }
    public void ClearMountPose(){mountCrouch=0;}

    // Evaluate always rebuilds pose from the ordinary gait before this overlay;
    // repeated frames therefore cannot accumulate crouch or knee rotations.
    void ApplyMountPose(float progress) {
        if(progress<1||mountCrouch<=.00001f)return;
        if(mountSource==null){mountSource=new Matrix4x4[pose.Length];mountDelta=new Matrix4x4[pose.Length];}
        Array.Copy(pose,mountSource,pose.Length);
        int pelvis=ids["pelvis"],thorax=ids["thorax"];
        var shift=Vector3.down*(.18f*mountCrouch);
        var lower=Matrix4x4.Translate(shift);
        var upper=Around(Position(mountSource[thorax])+shift,Quaternion.AngleAxis(3*mountCrouch,Vector3.right))*lower;
        var anchors=new Dictionary<int,Matrix4x4>{{pelvis,lower*mountSource[pelvis]},{thorax,upper*mountSource[thorax]}};
        for(int side=0;side<2;side++) {
            string suffix=side==0?"L":"R";
            int thigh=ids["thigh."+suffix],shin=ids["shin."+suffix],foot=ids["foot."+suffix];
            var oldHip=Position(mountSource[thigh]);var oldKnee=Position(mountSource[shin]);var ankle=Position(mountSource[foot]);
            var hip=oldHip+shift;
            float l1=Vector3.Distance(oldHip,oldKnee),l2=Vector3.Distance(oldKnee,ankle);
            var direction=(ankle-hip).normalized;
            float distance=Mathf.Clamp(Vector3.Distance(ankle,hip),Mathf.Abs(l1-l2)+.001f,l1+l2-.001f);
            float along=(l1*l1-l2*l2+distance*distance)/(2*distance);
            // Retain the current knee's bend plane, including slope adaptation.
            var bend=Vector3.ProjectOnPlane(oldKnee-oldHip,direction).normalized;
            if(bend.sqrMagnitude<.001f)bend=Vector3.ProjectOnPlane(Vector3.forward,direction).normalized;
            var knee=hip+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));
            anchors[thigh]=Matrix4x4.TRS(hip,Quaternion.FromToRotation(oldKnee-oldHip,knee-hip)*mountSource[thigh].rotation,Vector3.one);
            anchors[shin]=Matrix4x4.TRS(knee,Quaternion.FromToRotation(ankle-oldKnee,ankle-knee)*mountSource[shin].rotation,Vector3.one);
            anchors[foot]=mountSource[foot];
        }
        for(int i=0;i<pose.Length;i++) {
            int parent=gaitParent[i];string name=asset.Manifest.nodes[i].name;
            // Walk parks this knee cassette on the thorax once its carrier is
            // deployed. Its assistance motion must use that same live mount.
            if(name.StartsWith("panel.floor0_")&&name.Contains("_fold0_"))parent=thorax;
            if(anchors.TryGetValue(i,out var anchor))pose[i]=anchor;
            else if(parent>=0)
                pose[i]=gaitTranslate[i]?Matrix4x4.Translate(Position(pose[parent])-Position(mountSource[parent]))*mountSource[i]:mountDelta[parent]*mountSource[i];
            mountDelta[i]=pose[i]*mountSource[i].inverse;
            delta[i]=pose[i]*neutral[i].inverse;
        }
        foreach(var node in ids) {
            if(!node.Key.StartsWith("object:V18 connected sleeve 0 - "))continue;
            string role=node.Key.Substring(node.Key.IndexOf(" - ",StringComparison.Ordinal)+3);
            if(!ids.TryGetValue(role.StartsWith("neon.")?role:"panel."+role,out int end))continue;
            int parent=asset.Manifest.nodes[end].parent;if(parent<0)continue;
            var p=mountDelta[parent].MultiplyPoint3x4(Position(mountSource[node.Value]));
            pose[node.Value].SetColumn(3,new Vector4(p.x,p.y,p.z,1));
        }
        ReconnectSleeves();
    }
}
}
