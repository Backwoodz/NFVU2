using System.Collections.Generic;
using UnityEngine;

namespace Rachel350Z {
// Valheim owns resistance, destruction, falling logs, rock sections and drops.
// Never dispatch arbitrary IDestructible hits: buildings and dungeon props are excluded.
public static class TurretDamage {
    public static void Bullet(RaycastHit impact,Vector3 direction,Character driver,float multiplier=1) {
        if(driver==null||impact.collider==null||Protected(impact.collider))return;
        var target=impact.collider.GetComponentInParent<Character>();
        if(!Enemy(target,driver))return;
        var hit=Hit(impact.collider,impact.point,direction,driver);
        hit.m_pushForce=3;hit.m_blockable=true;hit.m_dodgeable=true;
        hit.m_damage.m_pierce=TurretCombatRules.BulletDamage*multiplier;target.Damage(hit);
    }
    public static void Blast(RaycastHit impact,Character driver,float multiplier=1) {
        if(driver==null||impact.collider==null)return;
        Vector3 origin=impact.point+impact.normal*.05f;
        // One hit per creature/tree; mineable rocks instead have independently damaged sections.
        var targets=new Dictionary<Component,Collider>();
        Add(targets,impact.collider,origin);
        foreach(var collider in Physics.OverlapSphere(origin,TurretBlastRules.Radius,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            Add(targets,collider,origin);
        foreach(var entry in targets) {
            var collider=entry.Value;
            if(collider==null)continue;
            Vector3 point=collider.ClosestPoint(origin);
            float distance=Vector3.Distance(point,origin);
            if(distance>=TurretBlastRules.Radius)continue;
            if(Physics.Linecast(origin,point,out var cover,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)
                &&Key(cover.collider)!=entry.Key)continue;
            var hit=Hit(collider,point,(point-origin).normalized,driver);
            var character=entry.Key as Character;
            if(character!=null) {
                if(!Enemy(character,driver))continue;
                float damage=TurretBlastRules.Damage(distance)*multiplier;
                hit.m_pushForce=7;hit.m_blockable=true;hit.m_dodgeable=true;
                hit.m_damage.m_blunt=damage*.8f;hit.m_damage.m_fire=damage*.2f;
                character.Damage(hit);
            } else {
                float damage=TurretBlastRules.EnvironmentDamage(distance);
                hit.m_toolTier=TurretCombatRules.ExplosiveToolTier;
                var rock=collider.GetComponentInParent<MineRock>();
                var rock5=collider.GetComponentInParent<MineRock5>();
                if(rock!=null||rock5!=null) {
                    hit.m_damage.m_pickaxe=damage;
                    if(rock!=null)rock.Damage(hit);else rock5.Damage(hit);
                } else {
                    hit.m_damage.m_chop=damage;
                    ((IDestructible)entry.Key).Damage(hit);
                }
            }
        }
    }
    static HitData Hit(Collider collider,Vector3 point,Vector3 direction,Character driver) {
        var hit=new HitData {m_point=point,m_dir=direction,m_hitCollider=collider};
        hit.SetAttacker(driver);return hit;
    }
    static bool Enemy(Character target,Character driver)=>target!=null&&target!=driver
        &&!target.IsPlayer()&&!target.IsTamed()&&!target.IsDead()&&BaseAI.IsEnemy(target,driver);
    static bool Protected(Collider collider)=>collider.GetComponentInParent<Piece>()!=null
        ||collider.GetComponentInParent<WearNTear>()!=null;
    static Component Key(Collider collider) {
        if(collider==null||Protected(collider))return null;
        var character=collider.GetComponentInParent<Character>();if(character!=null)return character;
        var tree=collider.GetComponentInParent<TreeBase>();if(tree!=null)return tree;
        var log=collider.GetComponentInParent<TreeLog>();if(log!=null)return log;
        if(collider.GetComponentInParent<MineRock>()!=null||collider.GetComponentInParent<MineRock5>()!=null)return collider;
        var plant=collider.GetComponentInParent<Destructible>();
        return plant!=null&&plant.GetDestructibleType()==DestructibleType.Tree?plant:null;
    }
    static void Add(Dictionary<Component,Collider> targets,Collider collider,Vector3 origin) {
        var key=Key(collider);if(key==null)return;
        if(!targets.TryGetValue(key,out var previous)
            ||Vector3.Distance(collider.ClosestPoint(origin),origin)<Vector3.Distance(previous.ClosestPoint(origin),origin))
            targets[key]=collider;
    }
}
}
