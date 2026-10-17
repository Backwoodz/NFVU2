using UnityEngine;
namespace Rachel350Z {
// Best-effort placement only: these probes never prevent boarding/dismounting.
internal static class TransformerMountClearance {
    static bool Own(Collider collider,Transform car,Transform player)=>collider==null
        ||car!=null&&collider.transform.IsChildOf(car)
        ||player!=null&&collider.transform.IsChildOf(player);
    static bool Ground(Transform car,Transform player,Vector3 point,out Vector3 foot) {
        foot=point;bool found=false;float height=point.y;
        var map=Heightmap.FindHeightmap(point);
        if(map!=null&&map.GetWorldHeight(point,out height)) {
            foot.y=height+.08f;found=true;
        }
        // Probe near the local ground, keeping overhead trees out of the search.
        float probeTop=Mathf.Max(point.y,height)+2;
        float nearest=float.PositiveInfinity;
        foreach(var hit in Physics.RaycastAll(new Vector3(point.x,probeTop,point.z),Vector3.down,
            Mathf.Max(8,probeTop-height+2),Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)) {
            if(Own(hit.collider,car,player)||hit.distance>=nearest||hit.normal.y<.05f
                ||hit.collider.GetComponentInParent<Character>()!=null)continue;
            nearest=hit.distance;foot=hit.point+Vector3.up*.08f;found=true;
        }
        return found;
    }
    static bool Clear(Transform car,Transform player,Vector3 foot) {
        var native=player!=null?player.GetComponent<CapsuleCollider>():null;
        float radius=.35f,bottom=.4f,top=1.5f;
        if(native!=null) {
            var scale=player.lossyScale;
            radius=Mathf.Max(radius,native.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z)));
            float half=Mathf.Max(radius,native.height*Mathf.Abs(scale.y)*.5f),center=native.center.y*scale.y;
            bottom=Mathf.Max(radius+.04f,center-half+radius);top=Mathf.Max(bottom,center+half-radius);
        }
        foreach(var collider in Physics.OverlapCapsule(foot+Vector3.up*bottom,foot+Vector3.up*top,radius,
            Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))if(!Own(collider,car,player))return false;
        return true;
    }
    public static Vector3 FindLanding(Transform car,Transform player) {
        if(car==null)return player!=null?player.position:Vector3.zero;
        var yaw=Quaternion.Euler(0,car.eulerAngles.y,0);
        var fallback=car.position+yaw*new Vector3(0,0,-3.5f)+Vector3.up*.15f;
        bool supported=false;
        // Prefer a free nearby landing, without making any candidate a gate.
        foreach(float distance in new[]{3.5f,4.2f,4.6f})foreach(float side in new[]{0f,-1.2f,1.2f}) {
            var candidate=car.position+yaw*new Vector3(side,0,-distance);
            if(!Ground(car,player,candidate,out var ground))continue;
            if(!supported){fallback=ground;supported=true;}
            if(Clear(car,player,ground))return ground;
        }
        return fallback;
    }
}
}
