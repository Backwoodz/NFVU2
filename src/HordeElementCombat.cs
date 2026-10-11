using System;
using System.Collections.Generic;
using UnityEngine;
namespace Rachel350Z {
// Bounded visual pool and a single chain per cooldown; no recursive hit callbacks.
public sealed class HordeElementCombat : IDisposable {
    readonly LineRenderer[] bolts=new LineRenderer[16];
    readonly float[] life=new float[16];
    readonly Material material;
    int next;float chainLeft;
    public HordeElementCombat(){material=VehicleMaterials.CreateGlowMaterial(Texture2D.whiteTexture);}
    public void Tick(float dt) {
        chainLeft=Mathf.Max(0,chainLeft-dt);
        for(int i=0;i<bolts.Length;i++)if(bolts[i]!=null){life[i]=Mathf.Max(0,life[i]-dt);bolts[i].enabled=life[i]>0;}
    }
    static bool Eligible(Character enemy,Character driver)=>enemy!=null&&!enemy.IsDead()&&!enemy.IsPlayer()&&!enemy.IsTamed()&&enemy.GetComponent<HordeEnemyMarker>()!=null&&BaseAI.IsEnemy(enemy,driver);
    public void Hit(RaycastHit hit,Character driver,HordePact pact) {
        if(driver==null||hit.collider==null)return;
        var target=hit.collider.GetComponentInParent<Character>();
        if(!Eligible(target,driver))return;
        if(pact==HordePact.Frost){target.GetComponent<HordeElementStatus>()?.Freeze();return;}
        if(pact!=HordePact.Storm||chainLeft>0)return;
        chainLeft=HordeLightningRules.Interval;
        var visited=new HashSet<Character>();var current=target;Vector3 from=hit.point;
        for(int jump=0;jump<=HordeLightningRules.MaxJumps&&current!=null;jump++) {
            visited.Add(current);Vector3 point=current.GetCenterPoint();
            var damage=new HitData{m_point=point,m_dir=(point-from).normalized};damage.SetAttacker(driver);
            damage.m_damage.m_lightning=HordeLightningRules.Damage(jump);current.Damage(damage);
            Bolt(from,point);from=point;
            Character nearest=null;float best=HordeLightningRules.Range;
            foreach(var other in Character.GetAllCharacters()) {
                if(visited.Contains(other)||!Eligible(other,driver))continue;
                float distance=Vector3.Distance(from,other.GetCenterPoint());if(distance>=best)continue;
                bool blocked=false;
                foreach(var cover in Physics.RaycastAll(from,(other.GetCenterPoint()-from).normalized,distance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)) {
                    var blocker=cover.collider.GetComponentInParent<Character>();
                    if(blocker!=other&&blocker!=current){blocked=true;break;}
                }
                if(blocked)continue;
                nearest=other;best=distance;
            }
            current=nearest;
        }
    }
    void Bolt(Vector3 from,Vector3 to) {
        int index=next++%bolts.Length;var line=bolts[index];
        if(line==null) {
            line=new GameObject("NFVU2_ChainLightning").AddComponent<LineRenderer>();bolts[index]=line;line.sharedMaterial=material;
            line.useWorldSpace=true;line.positionCount=7;line.startWidth=.09f;line.endWidth=.035f;
            line.startColor=new Color(.65f,.8f,1);line.endColor=new Color(.35f,.45f,1);
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
        }
        for(int i=0;i<7;i++)line.SetPosition(i,Vector3.Lerp(from,to,i/6f)+(i==0||i==6?Vector3.zero:UnityEngine.Random.insideUnitSphere*.25f));
        life[index]=.16f;line.enabled=true;
    }
    public void Dispose(){foreach(var line in bolts)if(line!=null)UnityEngine.Object.Destroy(line.gameObject);UnityEngine.Object.Destroy(material);}
}
}
