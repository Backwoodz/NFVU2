using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
namespace Rachel350Z {
// Owned encounter instances only. Restore original renderer blocks and movement on thaw.
public sealed class HordeElementStatus : MonoBehaviour {
    sealed class Tint {public Renderer Renderer;public int Slot;public MaterialPropertyBlock Before,Active;}
    readonly List<Tint> tints=new List<Tint>();
    readonly HordeFrostTimer frost=new HordeFrostTimer();
    Character target;float[] speeds;bool slowed;
    public bool Boss;
    public bool Frozen=>!Boss&&frost.Left>0;
    public void Freeze() {
        if(!frost.TryFreeze())return;
        target=GetComponent<Character>();if(target==null)return;
        speeds=new[]{target.m_speed,target.m_runSpeed,target.m_walkSpeed,target.m_swimSpeed,target.m_flySlowSpeed,target.m_flyFastSpeed};
        float factor=Boss?.65f:0;
        target.m_speed*=factor;target.m_runSpeed*=factor;target.m_walkSpeed*=factor;target.m_swimSpeed*=factor;target.m_flySlowSpeed*=factor;target.m_flyFastSpeed*=factor;slowed=true;
        if(!Boss){target.SetMoveDir(Vector3.zero);target.FreezeFrame(HordeFrostTimer.Duration);}
        TintBody(new Color(.65f,.88f,1),new Color(.15f,.3f,.45f));
    }
    public void Tick(float dt) {
        frost.Tick(dt);
        if(frost.Left<=0&&slowed)RestoreMovement();
        if(frost.Left<=0)RestoreTint();
    }
    void TintBody(Color color,Color emission) {
        if(tints.Count==0)foreach(var renderer in GetComponentsInChildren<Renderer>()) {
            if(!(renderer is SkinnedMeshRenderer)&&!(renderer is MeshRenderer))continue;
            var materials=renderer.sharedMaterials;
            for(int i=0;i<materials.Length;i++) {
                if(materials[i]==null||!materials[i].HasProperty("_Color"))continue;
                var before=new MaterialPropertyBlock();renderer.GetPropertyBlock(before,i);
                var active=new MaterialPropertyBlock();renderer.GetPropertyBlock(active,i);
                tints.Add(new Tint{Renderer=renderer,Slot=i,Before=before,Active=active});
            }
        }
        foreach(var tint in tints)if(tint.Renderer!=null){tint.Active.SetColor("_Color",color);tint.Active.SetColor("_EmissionColor",emission);tint.Renderer.SetPropertyBlock(tint.Active,tint.Slot);}
    }
    void RestoreMovement() {
        if(slowed&&target!=null){target.m_speed=speeds[0];target.m_runSpeed=speeds[1];target.m_walkSpeed=speeds[2];target.m_swimSpeed=speeds[3];target.m_flySlowSpeed=speeds[4];target.m_flyFastSpeed=speeds[5];}
        slowed=false;
    }
    void RestoreTint(){foreach(var tint in tints)if(tint.Renderer!=null)tint.Renderer.SetPropertyBlock(tint.Before,tint.Slot);tints.Clear();}
    void OnDestroy(){RestoreMovement();RestoreTint();}
}
[HarmonyPatch(typeof(MonsterAI),nameof(MonsterAI.UpdateAI))]
static class HordeFrozenAI {
    static bool Prefix(MonsterAI __instance,ref bool __result) {
        var effect=__instance.GetComponent<HordeElementStatus>();
        if(effect==null||!effect.Frozen)return true;
        __instance.StopMoving();__result=false;return false;
    }
}
}
