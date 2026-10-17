using UnityEngine;
namespace Rachel350Z {
public sealed partial class TransformerMinigun {
    Transform laserHousing,laserEmitter;
    LineRenderer laserBeam,laserCore,laserDot;
    Material laserMaterial;
    Character laserDriver;
    bool laserReady;
    readonly Collider[] laserContacts=new Collider[16];
    void CreateLaser() {
        // Authored NFVU2 sight, deliberately separate from the original Rust
        // weapon. Native gun-local dimensions; root applies the weapon scale.
        laserHousing=Node("NFVU2 minigun laser sight",pitch,new Vector3(-.165f,.045f,.30f));
        Cylinder("NFVU2 laser housing",laserHousing,Vector3.zero,.025f,.12f,Quaternion.Euler(90,0,0));
        Cylinder("NFVU2 laser bracket",laserHousing,new Vector3(.028f,-.018f,-.025f),.018f,.06f,Quaternion.Euler(0,0,90));
        laserEmitter=Node("NFVU2 laser emitter",laserHousing,new Vector3(0,0,.061f));
        laserBeam=Line("MinigunLaserBeam",root,.018f);
        laserCore=Line("MinigunLaserCore",root,.006f);
        laserDot=Line("MinigunLaserDot",root,.055f);
        laserMaterial=new Material(glow){name="NFVU2 minigun laser glow"};
        laserBeam.sharedMaterial=laserCore.sharedMaterial=laserDot.sharedMaterial=laserMaterial;
        HideLaser();
    }
    void HideLaser() {
        laserReady=false;laserDriver=null;
        if(laserBeam!=null)laserBeam.enabled=false;
        if(laserCore!=null)laserCore.enabled=false;
        if(laserDot!=null)laserDot.enabled=false;
        if(laserHousing!=null)laserHousing.gameObject.SetActive(false);
    }
    // Called after Tick, so paused, parked and transforming guns cannot retain
    // a previous frame's beam. This is visual only; damage uses the bore trace.
    public void SetLaser(bool requested,float strength,Color color) {
        if(disposed||!laserReady||!enabled||laserDriver==null||laserDriver.IsDead()||laserEmitter==null||!requested) {
            HideLaser();return;
        }
        laserHousing.gameObject.SetActive(true);
        laserBeam.enabled=laserCore.enabled=laserDot.enabled=false;
        if(Obstructed||!Finite(AimPoint))return;
        Vector3 origin=laserEmitter.position,delta=AimPoint-origin;
        float distance=delta.magnitude;
        if(!Finite(origin)||!Finite(distance)||distance<.02f||Vector3.Dot(delta,pitch.forward)<=0)return;
        // Raycast alone misses enclosing colliders. Verify the physical sight's
        // short mounting/emitter segment as well, retaining the robot hull.
        int count=Physics.OverlapCapsuleNonAlloc(pitch.position,origin,.008f,laserContacts,
            Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        if(count==laserContacts.Length)return;
        for(int i=0;i<count;i++)if(laserContacts[i]!=null&&!laserContacts[i].transform.IsChildOf(laserDriver.transform))return;
        float range=Mathf.Min(distance+.02f,TransformerMinigunCombatRules.Range);
        bool hit=TurretTargeting.Ray(origin,delta/distance,range,vehicle,laserDriver,out var impact);
        Vector3 end=hit?impact.point:origin+delta/distance*Mathf.Min(distance,range);
        if(!Finite(end))return;
        strength=Finite(strength)?Mathf.Clamp(strength,.25f,4):2;
        if(!Finite(color.r)||!Finite(color.g)||!Finite(color.b))color=new Color(1,.015f,.01f,1);
        color.r=Mathf.Clamp01(color.r);color.g=Mathf.Clamp01(color.g);color.b=Mathf.Clamp01(color.b);
        float width=Mathf.Sqrt(strength);
        laserBeam.startWidth=laserBeam.endWidth=.018f*width;
        laserCore.startWidth=laserCore.endWidth=.006f*width;
        laserDot.startWidth=laserDot.endWidth=.055f*width;
        color.a=.85f;laserBeam.startColor=laserBeam.endColor=color;
        var core=Color.Lerp(color,Color.white*Mathf.Max(color.r,Mathf.Max(color.g,color.b)),.65f);
        core.a=1;laserCore.startColor=laserCore.endColor=core;
        color.a=1;laserDot.startColor=laserDot.endColor=color;
        var tint=new Color(strength,strength,strength,1);
        if(laserMaterial.HasColor("_TintColor"))laserMaterial.SetColor("_TintColor",tint);
        if(laserMaterial.HasColor("_Color"))laserMaterial.SetColor("_Color",tint);
        laserBeam.SetPosition(0,origin);laserBeam.SetPosition(1,end);
        laserCore.SetPosition(0,origin);laserCore.SetPosition(1,end);
        laserBeam.enabled=laserCore.enabled=true;
        if(hit) {
            // A short camera-facing line on the hit surface makes the spot
            // visible without moving the beam past the first physical hit.
            Vector3 normal=impact.normal;
            Vector3 tangent=Camera.main!=null?Vector3.ProjectOnPlane(Camera.main.transform.right,normal):Vector3.Cross(normal,Vector3.up);
            if(tangent.sqrMagnitude<.0001f)tangent=Vector3.Cross(normal,Vector3.right);
            tangent=tangent.normalized*.028f*width;
            Vector3 spot=end+normal*.003f;
            laserDot.SetPosition(0,spot-tangent);laserDot.SetPosition(1,spot+tangent);laserDot.enabled=true;
        }
    }
    void DisposeLaser() {
        HideLaser();
        if(laserMaterial!=null)UnityEngine.Object.Destroy(laserMaterial);
        laserMaterial=null;
    }
}
}
