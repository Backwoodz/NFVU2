using System;
using HarmonyLib;
using UnityEngine;
namespace Rachel350Z {
// Native attacks/projectiles already look up IDestructible on the struck hull.
public sealed class VehicleHordeHull : MonoBehaviour,IDestructible {
    public Action<HitData> OnHit;
    public Action<float> OnCrash;
    float nextCrash;
    public DestructibleType GetDestructibleType()=>DestructibleType.Default;
    public void Damage(HitData hit)=>OnHit?.Invoke(hit);
    void OnCollisionEnter(Collision collision) {
        if(OnCrash==null||Time.time<nextCrash||collision.collider==null)return;
        if(collision.collider.GetComponentInParent<Character>()!=null)return;
        var plant=collision.collider.GetComponentInParent<Destructible>();
        if(plant!=null&&VehicleImpactRules.SmallVegetation(plant.name))return;
        float closing=0;
        for(int i=0;i<collision.contactCount;i++) {
            var contact=collision.GetContact(i);
            if(Mathf.Abs(contact.normal.y)>.6f)continue;
            closing=Mathf.Max(closing,Mathf.Abs(Vector3.Dot(collision.relativeVelocity,contact.normal)));
        }
        if(closing<=8)return;
        nextCrash=Time.time+.8f;OnCrash(closing);
    }
}
[HarmonyPatch(typeof(Character),nameof(Character.Damage))]
static class HordeDriverDamage {
    static bool Prefix(Character __instance,HitData hit)=>!(Plugin.ControlsOwner?.RouteHordeDriverDamage(__instance,hit)??false);
}
}
