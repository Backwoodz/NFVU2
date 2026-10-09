using UnityEngine;
namespace Rachel350Z {
// Shared by acquisition and the actual muzzle trace. Friendly actors are
// blockers, even though they can never become automatic targets.
public static class TurretTargeting {
    public static bool Enemy(Character candidate,Character owner) =>
        owner!=null&&candidate!=null&&candidate!=owner&&!candidate.IsPlayer()&&!candidate.IsTamed()&&!candidate.IsDead()&&BaseAI.IsEnemy(candidate,owner);
    public static bool Ray(Vector3 origin,Vector3 direction,float range,Transform vehicle,Character ignoredDriver,out RaycastHit closest,bool ignoreVehicle=false) {
        closest=default(RaycastHit);bool found=false;
        foreach(var hit in Physics.RaycastAll(origin,direction,range,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)) {
            if(hit.collider==null||(ignoredDriver!=null&&hit.collider.transform.IsChildOf(ignoredDriver.transform)))continue;
            if(ignoreVehicle&&hit.collider.transform.IsChildOf(vehicle))continue;
            if(!found||hit.distance<closest.distance){closest=hit;found=true;}
        }
        return found;
    }
    public static bool VisibleEnemy(Character candidate,Character owner,Transform root,Vector3 origin,Transform vehicle) {
        if(!Enemy(candidate,owner))return false;
        Vector3 delta=candidate.GetCenterPoint()-origin;
        var aim=root.InverseTransformDirection(delta);
        float bearing=Mathf.Atan2(aim.x,aim.z)*Mathf.Rad2Deg;
        float elevation=-Mathf.Atan2(aim.y,new Vector2(aim.x,aim.z).magnitude)*Mathf.Rad2Deg;
        if(!TurretAutoRules.InSector(delta.magnitude,bearing,elevation))return false;
        return Ray(origin,delta.normalized,TurretAutoRules.Range,vehicle,null,out var hit)&&hit.collider.GetComponentInParent<Character>()==candidate;
    }
    public static bool CanFire(Character target,Character owner,bool aligned,bool hit,Collider firstCollider) =>
        Enemy(target,owner)&&aligned&&hit&&firstCollider!=null&&firstCollider.GetComponentInParent<Character>()==target;
}
}
