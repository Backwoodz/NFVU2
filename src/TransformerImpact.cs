using System.Collections.Generic;
using UnityEngine;
namespace Rachel350Z {
// Robot-only contact damage. Native damage owns death, logs, drops and effects;
// car impacts and driving physics are deliberately unaffected.
internal sealed class TransformerImpact {
    static readonly int effectLayer=LayerMask.NameToLayer("effect");
    public static readonly int CollisionMask=effectLayer>=0?Physics.DefaultRaycastLayers&~(1<<effectLayer):Physics.DefaultRaycastLayers;
    readonly Collider[] overlaps=new Collider[64];
    readonly RaycastHit[] sight=new RaycastHit[32];
    readonly HashSet<int> visited=new HashSet<int>();
    readonly VehicleImpactCooldown cooldown=new VehicleImpactCooldown();
    float nextProbe;
    static Component Target(Collider collider) {
        if(collider==null||collider.GetComponentInParent<Piece>()!=null||collider.GetComponentInParent<WearNTear>()!=null)return null;
        var character=collider.GetComponentInParent<Character>();if(character!=null)return character;
        var tree=collider.GetComponentInParent<TreeBase>();if(tree!=null)return tree;
        var log=collider.GetComponentInParent<TreeLog>();if(log!=null)return log;
        var plant=collider.GetComponentInParent<Destructible>();
        return plant!=null&&plant.GetDestructibleType()==DestructibleType.Tree?plant:null;
    }
    public static bool PassThroughVegetation(Collider collider) {
        var target=Target(collider);
        return target!=null&&!(target is Character);
    }
    public static bool IgnoreMovementCollider(Collider collider) {
        if(collider==null)return false;
        if(collider.GetComponentInParent<Piece>()!=null||collider.GetComponentInParent<WearNTear>()!=null
            ||collider.GetComponentInParent<Character>()!=null)return false;
        // Native tree destruction creates independent loot and effect fragments.
        // Neither still has a TreeBase/TreeLog ancestor. They must never become
        // an obstacle or a raised support surface for this five-metre machine.
        return collider.GetComponentInParent<ItemDrop>()!=null||collider.gameObject.layer==effectLayer
            ||collider.GetComponentInParent<Gibber>()!=null||PassThroughVegetation(collider);
    }
    bool Reachable(Transform car,Character driver,Collider collider,Component target,Vector3 point) {
        var origin=car.position+Vector3.up*Mathf.Clamp(point.y-car.position.y,.5f,2);
        var direction=point-origin;float distance=direction.magnitude;
        if(distance<.01f)return true;
        int count=Physics.RaycastNonAlloc(origin,direction/distance,sight,distance,CollisionMask,QueryTriggerInteraction.Ignore);
        if(count==sight.Length)return false;
        for(int i=0;i<count;i++) {
            var c=sight[i].collider;
            if(c==null||c==collider||c.transform.IsChildOf(car)||c.transform.IsChildOf(driver.transform)
                ||IgnoreMovementCollider(c)||Target(c)==target)continue;
            // Other vegetation/creatures can be cleared together, but never hit
            // through a wall or rock just because the broad contact query sees it.
            if(Target(c)==null)return false;
        }
        return true;
    }
    public void Step(Transform car,Character driver,Vector3 next,Vector3 direction,float speed,float input) {
        // Intent allows a tree already touching the core to fall before speed
        // has built up. It never grants stationary character damage.
        if(driver==null||Mathf.Abs(input)<.1f||Time.time<nextProbe)return;
        nextProbe=Time.time+.12f;
        direction.y=0;direction.Normalize();
        var center=next+direction*.45f;
        int count=Physics.OverlapCapsuleNonAlloc(center+Vector3.up*.4f,center+Vector3.up*3.2f,1.15f,overlaps,CollisionMask,QueryTriggerInteraction.Ignore);
        visited.Clear();
        for(int i=0;i<count;i++) {
            var collider=overlaps[i];
            if(collider==null||collider.transform.IsChildOf(car)||collider.transform.IsChildOf(driver.transform))continue;
            var target=Target(collider);if(target==null)continue;
            int id=target.GetInstanceID();if(visited.Contains(id))continue;
            var character=target as Character;
            if(character!=null&&(speed<.7f||character==driver||character.IsPlayer()||character.IsTamed()||character.IsDead()
                ||character.IsBoss()||character.GetHeight()*Mathf.Abs(character.transform.lossyScale.y)>2.8f||character.GetRadius()>1.3f
                ||!BaseAI.IsEnemy(character,driver)))continue;
            var point=collider.ClosestPoint(car.position+Vector3.up*1.3f);
            var horizontal=point-car.position;horizontal.y=0;
            if(Vector3.Dot(horizontal,direction)<-.2f||!Reachable(car,driver,collider,target,point))continue;
            visited.Add(id);
            if(!cooldown.TryHit(id,Time.time))continue;
            var hit=new HitData {m_point=point,m_dir=direction,m_hitCollider=collider,m_toolTier=6,
                m_pushForce=character!=null?80:10,m_dodgeable=false,m_blockable=false,m_hitType=HitData.HitType.Impact};
            hit.SetAttacker(driver);
            if(character!=null) {
                hit.m_damage.m_blunt=Mathf.Clamp(100+speed*speed*3,100,350);character.Damage(hit);
            } else {
                // Every native tree species/tool tier, plus fallen trunks and
                // saplings. Rocks, buildings and miscellaneous props excluded.
                hit.m_damage.m_chop=2000;((IDestructible)target).Damage(hit);
            }
        }
    }
}
}
