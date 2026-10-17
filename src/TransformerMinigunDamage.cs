using System;
using UnityEngine;
namespace Rachel350Z {
// Native direct-target damage, optionally followed by the existing impact
// effect/blast. Observe combined health loss only after both have completed.
public static class TransformerMinigunDamage {
    public static TransformerMinigunHit Bullet(RaycastHit impact,Vector3 direction,Character driver,Action additionalImpact=null) {
        var collider=impact.collider;
        if(collider==null||driver==null||driver.IsDead())return default(TransformerMinigunHit);
        var enemy=collider.GetComponentInParent<Character>();
        if(collider.GetComponentInParent<Piece>()!=null||collider.GetComponentInParent<WearNTear>()!=null
            ||enemy==driver||enemy!=null&&(enemy.IsPlayer()||enemy.IsTamed()||enemy.IsDead()||!BaseAI.IsEnemy(enemy,driver)))enemy=null;
        if(enemy==null){additionalImpact?.Invoke();return default(TransformerMinigunHit);}
        float before=enemy.GetHealth(),maximum=enemy.GetMaxHealth();string name=enemy.GetHoverName();
        var hit=new HitData{m_point=impact.point,m_dir=direction,m_hitCollider=collider,m_pushForce=.2f,
            m_blockable=true,m_dodgeable=true,m_backstabBonus=1,m_staggerMultiplier=0,m_ranged=true};
        hit.SetAttacker(driver);hit.m_damage.m_pierce=TransformerMinigunCombatRules.BulletDamage;
        enemy.Damage(hit);
        additionalImpact?.Invoke();
        // The local single-player owner processes RPC_Damage synchronously.
        // Report the observed native result after block/resistance/difficulty.
        float after=enemy==null||enemy.IsDead()?0:enemy.GetHealth();
        return new TransformerMinigunHit{Name=name,Health=after,MaximumHealth=maximum,Damage=Mathf.Max(0,before-after)};
    }
}
}
