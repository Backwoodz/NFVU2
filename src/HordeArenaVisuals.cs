using System;
using System.Collections.Generic;
using UnityEngine;
namespace Rachel350Z {
public sealed class HordeArenaVisuals : IDisposable {
    public sealed class Pickup {
        public HordePowerup Kind;public Vector3 Point;public GameObject Root;
        public Transform Gem;public float Age;
    }
    public readonly List<Pickup> Pickups=new List<Pickup>();
    readonly Material glow;
    readonly HordeShieldVisual shield;
    float animation;
    public HordeArenaVisuals(Transform car) {
        glow=VehicleMaterials.CreateGlowMaterial(Texture2D.whiteTexture);
        shield=new HordeShieldVisual(car,glow);
    }
    public static Color ColorFor(HordePowerup kind)=>kind==HordePowerup.Explosive?new Color(1,.35f,.04f):kind==HordePowerup.Automatic?new Color(.8f,.18f,1):kind==HordePowerup.Shield?new Color(.05f,.65f,1):new Color(.15f,1,.25f);
    LineRenderer Line(Transform parent,string name,Color color,float width) {
        var go=new GameObject(name);go.transform.SetParent(parent,false);
        var line=go.AddComponent<LineRenderer>();line.sharedMaterial=glow;line.useWorldSpace=false;
        line.startWidth=line.endWidth=width;line.startColor=line.endColor=color;
        line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
        return line;
    }
    public void Add(Vector3 point,HordePowerup kind) {
        var pickup=new Pickup{Point=point,Kind=kind,Root=new GameObject("NFVU2_Powerup_"+kind)};
        pickup.Root.transform.position=point;Pickups.Add(pickup);
        var color=ColorFor(kind);
        var beam=Line(pickup.Root.transform,"Beacon",color,.9f);beam.endWidth=3;
        beam.positionCount=2;beam.SetPosition(0,Vector3.up*.2f);beam.SetPosition(1,Vector3.up*16);
        var bottom=color;bottom.a=.3f;var top=color;top.a=.02f;beam.startColor=bottom;beam.endColor=top;
        var ring=Line(pickup.Root.transform,"PickupRing",color,.09f);ring.positionCount=33;
        for(int i=0;i<33;i++){float a=i*Mathf.PI/16;ring.SetPosition(i,new Vector3(Mathf.Sin(a)*1.5f,.18f,Mathf.Cos(a)*1.5f));}
        var gem=Line(pickup.Root.transform,"FloatingRune",color,.11f);pickup.Gem=gem.transform;
        var up=Vector3.up*.9f;var down=Vector3.down*.9f;
        var f=Vector3.forward*.6f;var b=Vector3.back*.6f;var l=Vector3.left*.6f;var r=Vector3.right*.6f;
        var path=new[]{up,f,down,r,up,b,down,l,up,f,r,b,l,f};
        gem.positionCount=path.Length;gem.SetPositions(path);
    }
    public void Remove(Pickup pickup){if(pickup.Root!=null)UnityEngine.Object.Destroy(pickup.Root);Pickups.Remove(pickup);}
    public void Tick(float dt,float shieldCharge,float hitFlash) {
        animation+=dt;shield.Tick(animation,shieldCharge,hitFlash);
        foreach(var pickup in Pickups) {
            pickup.Age+=dt;pickup.Gem.localPosition=Vector3.up*(1.6f+Mathf.Sin(animation*2)*.22f);
            pickup.Gem.localRotation=Quaternion.Euler(0,animation*65,0);
        }
    }
    public void Dispose() {
        foreach(var pickup in Pickups)if(pickup.Root!=null)UnityEngine.Object.Destroy(pickup.Root);
        Pickups.Clear();shield?.Dispose();if(glow!=null)UnityEngine.Object.Destroy(glow);
    }
}
}
