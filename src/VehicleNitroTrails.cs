using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rachel350Z {
// Short world-space ribbons with depth testing; positions come from rear lamps.
public sealed class VehicleNitroTrails : IDisposable {
    readonly List<TrailRenderer> trails=new List<TrailRenderer>();
    Material material;
    Texture2D texture;
    bool emitting;
    Vector3 lastPosition;
    readonly Transform car;
    public VehicleNitroTrails(Transform vehicle,Vector3[] positions) {
        car=vehicle;lastPosition=car.position;
        try {
            texture=new Texture2D(32,32,TextureFormat.RGBA32,false,true){name="NFVU2_SoftLightRibbon",wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[32*32];
            for(int y=0;y<32;y++)for(int x=0;x<32;x++) {
                float d=Mathf.Abs((y+.5f)/16-1),a=Mathf.Pow(Mathf.Max(0,1-d*d),3);
                pixels[y*32+x]=new Color(a,a,a,a);
            }
            texture.SetPixels(pixels);texture.Apply(false,true);
            material=VehicleMaterials.CreateGlowMaterial(texture);material.name="NFVU2_TailRibbon";
            // Keep depth testing so trails cannot shine through terrain/buildings.
            if(material.HasFloat("_ZTest"))material.SetFloat("_ZTest",(float)CompareFunction.LessEqual);
            foreach(string name in new[]{"_Color","_TintColor","_BaseColor"})
                if(material.HasColor(name))material.SetColor(name,new Color(1,.035f,.008f,.7f));
            foreach(var position in positions) {
                var node=new GameObject("NFVU2_NitroTailTrail");node.transform.SetParent(car,false);
                node.transform.localPosition=position;
                var trail=node.AddComponent<TrailRenderer>();trails.Add(trail);
                trail.sharedMaterial=material;trail.time=.13f;trail.minVertexDistance=.06f;
                trail.autodestruct=false;trail.emitting=false;trail.alignment=LineAlignment.View;
                trail.textureMode=LineTextureMode.Stretch;trail.numCornerVertices=2;trail.numCapVertices=2;
                trail.shadowCastingMode=ShadowCastingMode.Off;trail.receiveShadows=false;
                trail.lightProbeUsage=LightProbeUsage.Off;trail.reflectionProbeUsage=ReflectionProbeUsage.Off;
                trail.widthCurve=AnimationCurve.Linear(0,1,1,0);
                trail.startColor=Color.white;trail.endColor=new Color(1,1,1,0);
            }
        }catch{Dispose();throw;}
    }
    public void Tick(bool boost,bool allowed,float strength,float speed) {
        bool discontinuity=car==null||(car.position-lastPosition).sqrMagnitude>100;
        if(car!=null)lastPosition=car.position;
        bool active=boost&&allowed&&strength>0&&speed>1&&!discontinuity;
        foreach(var trail in trails) {
            if(!allowed||strength<=0||discontinuity)trail.Clear();
            if(active&&!emitting)trail.Clear();
            trail.widthMultiplier=.19f*Mathf.Clamp01(strength);
            // Cap the ribbon length at roughly 3 m even at aerial boost speeds.
            trail.time=Mathf.Min(.16f,3/Mathf.Max(speed,1));
            trail.emitting=active;
        }
        emitting=active;
    }
    public void Dispose() {
        foreach(var trail in trails)if(trail!=null){trail.emitting=false;trail.Clear();UnityEngine.Object.Destroy(trail.gameObject);}
        trails.Clear();
        if(material!=null)UnityEngine.Object.Destroy(material);
        if(texture!=null)UnityEngine.Object.Destroy(texture);
    }
}
}
