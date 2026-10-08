using System;

namespace Rachel350Z {
// One continuous music stream; opening the cabin changes its exterior mix
// and moves its apparent origin towards the open door(s) or hatch.
public readonly struct RadioAcousticMix {
    public readonly float Gain,Cutoff,X,Y,Z;
    public RadioAcousticMix(float gain,float cutoff,float x,float y,float z) {
        Gain=gain;Cutoff=cutoff;X=x;Y=y;Z=z;
    }
}
public static class RadioAcoustics {
    static float Unit(float value)=>float.IsNaN(value)||float.IsInfinity(value)?0:Math.Max(0,Math.Min(1,value));
    public static RadioAcousticMix Evaluate(float outside,float leftDoor,float rightDoor,float hatch) {
        outside=Unit(outside);leftDoor=Unit(leftDoor)*.65f;rightDoor=Unit(rightDoor)*.65f;hatch=Unit(hatch);
        float opening=1-(1-leftDoor)*(1-rightDoor)*(1-hatch);
        float gain=1+outside*(.3f+.55f*opening-1);
        float cutoff=22000+outside*(1400+10600*opening-22000);
        // Local Unity left is -X. Door openings stay at the body apertures,
        // irrespective of normal, suicide or scissor hinge animation.
        float weight=leftDoor+rightDoor+hatch;
        float x=0,z=-.25f;
        if(weight>0) {
            float blend=outside*opening;
            x=blend*.78f*(rightDoor-leftDoor)/weight;
            z+=blend*((.25f*(leftDoor+rightDoor)-1.45f*hatch)/weight-z);
        }
        return new RadioAcousticMix(gain,cutoff,x,.65f,z);
    }
}
}
