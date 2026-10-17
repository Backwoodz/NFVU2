using UnityEngine;
namespace Rachel350Z {
// Native single-target damage only. No IDestructible, terrain, blast, skill
// scaling or recovered Rust damage code is dispatched by this adapter.
public static class TransformerMinigunDamage {
    public static void Bullet(RaycastHit impact,Vector3 direction,Character driver) {
        var collider=impact.collider;
        if(collider==null||driver==null||driver.IsDead()||collider.GetComponentInParent<Piece>()!=null||collider.GetComponentInParent<WearNTear>()!=null)return;
        var enemy=collider.GetComponentInParent<Character>();
        if(enemy==null||enemy==driver||enemy.IsPlayer()||enemy.IsTamed()||enemy.IsDead()||!BaseAI.IsEnemy(enemy,driver))return;
        var hit=new HitData{m_point=impact.point,m_dir=direction,m_hitCollider=collider,m_pushForce=.2f,
            m_blockable=true,m_dodgeable=true,m_backstabBonus=1,m_staggerMultiplier=0};
        hit.SetAttacker(driver);hit.m_damage.m_pierce=TransformerMinigunCombatRules.BulletDamage;
        enemy.Damage(hit);
    }
}
}
