using System;
using UnityEngine;

namespace Rachel350Z {
// Uses Valheim's damage path for resistance, knockback, death, drops and effects.
public sealed class VehicleImpact : MonoBehaviour {
    readonly VehicleImpactCooldown cooldown=new VehicleImpactCooldown();
    readonly RaycastHit[] sweepHits=new RaycastHit[32];
    Rigidbody body;
    BoxCollider hull;
    Character driver;
    Vector3 velocity,contactDrive;
    Action<string> log;
    bool failed;
    public void Initialize(Rigidbody body,BoxCollider hull,Action<string> log) {
        this.body=body;this.hull=hull;this.log=log;
    }
    public void Step(MonoBehaviour seatedPlayer,bool active,float driveInput=0) {
        driver=active&&!failed?seatedPlayer as Character:null;
        velocity=contactDrive=Vector3.zero;
        // World teardown may destroy the rigidbody before the plugin exits.
        if(driver==null||body==null||hull==null){driver=null;return;}
        velocity=body.linearVelocity;
        velocity.y=0; // Falling onto a target is not a run-over.
        // Powered contact can clear a sapling that already stopped the hull.
        // Never use this intent for sweeps or character damage. The caller only
        // supplies drive input while driving on the ground without braking.
        contactDrive=transform.forward;contactDrive.y=0;
        contactDrive=driver!=null&&Mathf.Abs(driveInput)>.1f
            ?contactDrive.normalized*(driveInput>0?.35f:-.35f):Vector3.zero;
        if(driver==null||velocity.magnitude<VehicleImpactRules.VegetationMinSpeed)return;
        // One fixed step ahead, only the nearest solid surface. This allows light
        // vegetation to break before the hull comes to a complete stop against it.
        int count=Physics.BoxCastNonAlloc(transform.TransformPoint(hull.center),hull.size*.5f,
            velocity.normalized,sweepHits,transform.rotation,
            velocity.magnitude*Time.fixedDeltaTime+.03f,~0,QueryTriggerInteraction.Ignore);
        // The query includes our own hull/wheels. Discard those, but retain walls
        // and terrain as blockers. An overflow has no reliable nearest hit.
        if(count==sweepHits.Length)return;
        int nearest=-1;
        for(int i=0;i<count;i++) {
            var candidate=sweepHits[i];
            if(candidate.collider==null||candidate.collider.transform.IsChildOf(transform))continue;
            if(nearest<0||candidate.distance<sweepHits[nearest].distance)nearest=i;
        }
        if(nearest<0)return;
        var hit=sweepHits[nearest];
        // Overlap hits have an artificial normal/zero point: use actual contacts.
        if(hit.distance>0)TryImpact(hit.collider,hit.point,hit.normal,Vector3.zero,false);
    }
    void OnCollisionEnter(Collision collision){Contact(collision);}
    void OnCollisionStay(Collision collision){Contact(collision);}
    void Contact(Collision collision) {
        if(driver==null||failed)return;
        Vector3 impactVelocity=collision.relativeVelocity;impactVelocity.y=0;
        for(int i=0;i<collision.contactCount;i++) {
            var contact=collision.GetContact(i);
            TryImpact(collision.collider,contact.point,contact.normal,impactVelocity,true);
        }
    }
    void TryImpact(Collider collider,Vector3 point,Vector3 normal,Vector3 contactVelocity,bool physicalContact) {
        if(driver==null||failed||collider==null||collider.transform.IsChildOf(transform))return;
        try {
            if(collider.GetComponentInParent<Piece>()!=null)return;
            var character=collider.GetComponentInParent<Character>();
            var plant=collider.GetComponentInParent<Destructible>();
            bool vegetation=character==null&&plant!=null&&VehicleImpactRules.SmallVegetation(plant.name);
            if(character!=null) {
                if(character==driver||character.IsPlayer()||character.IsTamed()||character.IsDead()
                    ||!BaseAI.IsEnemy(character,driver))return;
            } else if(!vegetation)return;
            Vector3 impactVelocity=velocity;
            if(vegetation&&physicalContact) {
                // The solver may have stopped the car by the next fixed tick;
                // the actual collision still knows the incoming velocity.
                if(Vector3.Dot(contactVelocity,normal)<Vector3.Dot(impactVelocity,normal))impactVelocity=contactVelocity;
                if(Vector3.Dot(contactDrive,normal)<Vector3.Dot(impactVelocity,normal))impactVelocity=contactDrive;
            }
            float speed=Mathf.Max(0,-Vector3.Dot(impactVelocity,normal));
            float damage=VehicleImpactRules.Damage(speed,vegetation);
            if(damage<=0)return;
            int id=character!=null?character.GetInstanceID():plant.GetInstanceID();
            if(!cooldown.TryHit(id,Time.time))return;
            var hit=new HitData {
                m_point=point,m_dir=impactVelocity.normalized,m_hitCollider=collider,
                m_pushForce=vegetation?0:Mathf.Min(100,speed*5),m_toolTier=0,
                m_dodgeable=false,m_blockable=false,m_hitType=HitData.HitType.Impact
            };
            hit.SetAttacker(driver);
            if(vegetation){hit.m_damage.m_chop=damage;plant.Damage(hit);}
            else {hit.m_damage.m_blunt=damage;character.Damage(hit);}
        }catch(Exception e){failed=true;driver=null;log?.Invoke("350Z impact damage disabled: "+e);}
    }
}
}
