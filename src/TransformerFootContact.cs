using UnityEngine;
namespace Rachel350Z {
internal sealed partial class TransformerVisual {
    readonly Vector3[] contactCenters=new Vector3[2],contactDirections=new Vector3[2];
    readonly Vector2[] contactSizes=new Vector2[2];
    bool contactsCached;
    // Cache the bottom of the actual rigid boot, rather than placing a print
    // below its ankle pivot or including armor high above the ground.
    void CacheFootContacts() {
        for(int side=0;side<2;side++) {
            int foot=ids["foot."+(side==0?"L":"R")];
            float low=float.PositiveInfinity;
            foreach(var point in solePoints[side])low=Mathf.Min(low,neutral[foot].MultiplyPoint3x4(point).y);
            var min=new Vector3(float.PositiveInfinity,low,float.PositiveInfinity);
            var max=new Vector3(float.NegativeInfinity,low,float.NegativeInfinity);
            foreach(var point in solePoints[side]) {
                var p=neutral[foot].MultiplyPoint3x4(point);
                if(p.y>low+.12f)continue;
                min.x=Mathf.Min(min.x,p.x);min.z=Mathf.Min(min.z,p.z);
                max.x=Mathf.Max(max.x,p.x);max.z=Mathf.Max(max.z,p.z);
            }
            contactCenters[side]=neutral[foot].inverse.MultiplyPoint3x4((min+max)*.5f);
            contactDirections[side]=neutral[foot].inverse.MultiplyVector(Vector3.forward);
            contactSizes[side]=new Vector2(max.x-min.x,max.z-min.z);
        }
        contactsCached=true;
    }
    public void FootContact(bool right,out Vector3 center,out Vector3 forward,out Vector2 size) {
        if(!contactsCached)CacheFootContacts();
        int side=right?1:0,foot=ids["foot."+(right?"R":"L")];
        var world=Root.transform.localToWorldMatrix*pose[foot];
        center=world.MultiplyPoint3x4(contactCenters[side]);
        forward=world.MultiplyVector(contactDirections[side]).normalized;
        var scale=Root.transform.lossyScale;
        size=new Vector2(contactSizes[side].x*Mathf.Abs(scale.x),contactSizes[side].y*Mathf.Abs(scale.z));
    }
}
}
