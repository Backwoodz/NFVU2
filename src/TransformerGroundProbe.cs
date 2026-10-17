using System;
using UnityEngine;

namespace Rachel350Z {
// Valheim's WaterSurface has a non-trigger BoxCollider on the Water layer.
// IgnoreTrigger alone therefore cannot distinguish the lake from its bed.
internal sealed class TransformerGroundProbe {
    public static readonly int SolidMask=LayerMask.GetMask("Default","static_solid","Default_small","piece","terrain","blocker","vehicle");
    static readonly int fluidMask=LayerMask.GetMask("Water","water","WaterVolume","Liquid","liquid");
    // Actor collision remains as broad as before. The native ground allowlist
    // is only for downward support, not for phasing through nearby enemies.
    public static readonly int MovementMask=TransformerImpact.CollisionMask&~fluidMask;
    readonly RaycastHit[] hits=new RaycastHit[48];
    readonly Func<Collider,bool> own;
    public TransformerGroundProbe(Func<Collider,bool> own){this.own=own;}
    public static bool IsFluid(Collider collider) {
        if(collider==null)return false;
        return (fluidMask&(1<<collider.gameObject.layer))!=0||collider.GetComponentInParent<WaterVolume>()!=null
            ||collider.GetComponentInParent<LiquidVolume>()!=null||collider.GetComponentInParent<LiquidSurface>()!=null;
    }
    public bool Sample(Vector3 point,out float height,out Vector3 normal,out Collider support) {
        height=point.y;normal=Vector3.up;support=null;
        if(!Finite(point.x)||!Finite(point.y)||!Finite(point.z))return false;
        // The rounded terrain grid is only a ray-length hint. It is never a
        // stand-in for an actual loaded terrain triangle or bridge collider.
        float hint=point.y;var map=Heightmap.FindHeightmap(point);
        bool terrain=map!=null&&map.GetWorldHeight(point,out hint)&&Finite(hint);
        float top=Mathf.Max(point.y,terrain?hint:point.y)+1.8f;
        int count=Physics.RaycastNonAlloc(new Vector3(point.x,top,point.z),Vector3.down,hits,terrain?Mathf.Max(8,top-hint+2):128,SolidMask,QueryTriggerInteraction.Ignore);
        if(count==0||count==hits.Length)return false;
        float closest=float.PositiveInfinity;
        for(int i=0;i<count;i++) {
            var hit=hits[i];var collider=hit.collider;
            if(collider==null||own!=null&&own(collider)||IsFluid(collider)||TransformerImpact.IgnoreMovementCollider(collider)
                ||collider.GetComponentInParent<Character>()!=null||!Finite(hit.distance)||hit.distance<0||hit.distance>=closest
                ||!Finite(hit.point.y)||!Finite(hit.normal.y)||hit.normal.y<.1f)continue;
            closest=hit.distance;height=hit.point.y;normal=hit.normal;support=collider;
        }
        return support!=null;
    }
    // The same vertical convergence runs when horizontal movement is denied.
    // A shoreline/deep-water stop must not freeze a kinematic body in mid-air.
    public static float FollowHeight(float current,float floor,float dt) {
        if(!Finite(current)||!Finite(floor)||!Finite(dt)||dt<=0)return current;
        return Mathf.MoveTowards(current,floor,dt*18);
    }
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
}
}
