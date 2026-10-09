using System;
using UnityEngine;
namespace Rachel350Z {
// Raise the suspension anchors relative to the hull: gravity settles the body
// lower while wheels, suspension travel, springs and visual attachments agree.
public sealed class VehicleRideHeight {
    readonly Rigidbody body;
    readonly WheelCollider[] wheels;
    readonly float[] originalY;
    float applied;
    public VehicleRideHeight(Rigidbody body,WheelCollider[] wheels) {
        this.body=body;this.wheels=wheels;originalY=new float[wheels.Length];
        for(int i=0;i<wheels.Length;i++)originalY[i]=wheels[i].transform.localPosition.y;
    }
    public void Apply(float centimetres) {
        float next=float.IsNaN(centimetres)||float.IsInfinity(centimetres)?0:Math.Max(0,Math.Min(12,centimetres))*.01f;
        if(next==applied)return;
        for(int i=0;i<wheels.Length;i++) {
            var pos=wheels[i].transform.localPosition;pos.y=originalY[i]+next;wheels[i].transform.localPosition=pos;
        }
        applied=next;body.WakeUp();
    }
}
}
