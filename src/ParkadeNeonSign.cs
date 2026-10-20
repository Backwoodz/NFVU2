using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rachel350Z {
// Local decoration only: one bounded timber-framed marquee, no gameplay
// components, colliders, lights or ZDOs. Glyphs are generated once from Valheim's
// own font, then clipped by moving their cached vertices/UVs inside the board.
internal sealed class ParkadeNeonSign {
    internal const string Message="RIP McRib";
    internal const float Along=12,Across=18,Width=13,CullDistance=185;
    const float TextHalfWidth=5.95f,BorderHalfHeight=1.7f;
    readonly Action<string> log;
    readonly List<Material> materials=new List<Material>();
    readonly List<Mesh> meshes=new List<Mesh>();
    readonly List<Transform> runners=new List<Transform>();
    readonly List<Marquee> text=new List<Marquee>();
    GameObject root;
    object world;
    ZNetScene scene;
    Vector3 position;
    float nextFontAttempt;
    bool failed;
    sealed class Marquee {
        internal Mesh Mesh;
        internal Vector3[] Source,Vertices;
        internal Vector4[] SourceUV;
        internal List<Vector4> UV;
        internal float Period;
        internal int Copy;
    }
    internal ParkadeNeonSign(Action<string> log){this.log=log;}
    internal void Tick(Vector3 viewer,object currentWorld) {
        if(currentWorld==null||ZNetScene.instance==null){Clear();return;}
        if(!ReferenceEquals(world,currentWorld)||scene!=ZNetScene.instance) {
            Clear();world=currentWorld;scene=ZNetScene.instance;
        }
        if(failed)return;
        var forward=new Vector3(ParkadeLayout.StartTangentX,0,ParkadeLayout.StartTangentZ);
        var across=new Vector3(-forward.z,0,forward.x);
        position=new Vector3(ParkadeLayout.StartX,ParkadeLayout.BaseHeight+4.8f,ParkadeLayout.StartZ)+forward*Along+across*Across;
        float dx=viewer.x-position.x,dz=viewer.z-position.z;
        bool visible=dx*dx+dz*dz<CullDistance*CullDistance;
        if(root!=null)root.SetActive(visible);
        if(!visible)return;
        try {
            if(root==null)Create(forward);
            if(text.Count==0&&Time.unscaledTime>=nextFontAttempt) {
                nextFontAttempt=Time.unscaledTime+2;
                CreateText();
            }
            Animate(Time.unscaledTime);
        } catch(Exception error) {
            Clear();world=currentWorld;scene=ZNetScene.instance;failed=true;
            log?.Invoke("Parkade marquee unavailable: "+error.GetBaseException().Message);
        }
    }
    void Create(Vector3 forward) {
        root=new GameObject("NFVU2 Parkade RIP McRib marquee"){hideFlags=HideFlags.DontSave};
        // Local +Z faces along the road; the sign's front is local -Z, towards
        // the car waiting behind the start line. All scenery stays outside it.
        root.transform.SetPositionAndRotation(position,Quaternion.LookRotation(forward,Vector3.up));
        var templates=VehicleMaterials.Create(null,message=>{});materials.AddRange(templates);
        var wood=templates[0];var dark=templates[1];
        Surface(wood,"Parkade carved dark timber",new Color(.23f,.125f,.055f),0);
        Surface(dark,"Parkade dark iron sign",new Color(.012f,.025f,.019f),.05f);
        var amber=new Material(wood){name="Parkade amber sign trim",hideFlags=HideFlags.DontSave};materials.Add(amber);
        Surface(amber,amber.name,new Color(.85f,.47f,.09f),1.5f);
        var green=VehicleMaterials.CreateGlowMaterial(Texture2D.whiteTexture);green.name="Parkade green running neon";
        materials.Add(green);
        foreach(string p in new[]{"_Color","_TintColor","_BaseColor"})
            if(green.HasColor(p))green.SetColor(p,new Color(.12f,1,.36f,.85f));
        Box("Dark iron display",Vector3.zero,new Vector3(Width,3.8f,.3f),dark);
        foreach(float side in new[]{-1f,1f}) {
            Box("Carved timber top bottom",new Vector3(0,side*2.02f,0),new Vector3(13.65f,.3f,.5f),wood);
            Box("Carved timber end",new Vector3(side*6.68f,0,0),new Vector3(.3f,4.35f,.5f),wood);
            Box("Amber edge",new Vector3(0,side*BorderHalfHeight,-.19f),new Vector3(12.5f,.065f,.04f),amber);
            float x=side*5.8f;
            var ground=root.transform.TransformPoint(new Vector3(x,0,0));
            float bottom=ParkadeLayout.TerrainHeightAt(ground.x,ground.z)-position.y;
            float top=-1.9f;
            Box("Timber billboard support",new Vector3(x,(bottom+top)*.5f,.18f),new Vector3(.5f,top-bottom,.5f),wood);
        }
        for(int i=0;i<20;i++) {
            var runner=Box("Moving green rim light "+i,Vector3.zero,new Vector3(.34f,.10f,.025f),green);
            runners.Add(runner.transform);
        }
    }
    GameObject Box(string name,Vector3 localPosition,Vector3 scale,Material material) {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.hideFlags=HideFlags.DontSave;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(root.transform,false);go.transform.localPosition=localPosition;go.transform.localScale=scale;
        var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        return go;
    }
    static void Surface(Material material,string name,Color color,float emission) {
        material.name=name;material.hideFlags=HideFlags.DontSave;
        foreach(string p in new[]{"_Color","_BaseColor","_SkinColor"})if(material.HasColor(p))material.SetColor(p,color);
        foreach(string p in new[]{"_Glossiness","_Smoothness"})if(material.HasFloat(p))material.SetFloat(p,.12f);
        if(material.HasFloat("_Metallic"))material.SetFloat("_Metallic",0);
        VehicleMaterials.SetEmission(material,color*emission);
    }
    void CreateText() {
        var font=Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
            .FirstOrDefault(f=>f!=null&&f.name.IndexOf("Norse",StringComparison.OrdinalIgnoreCase)>=0);
        if(font==null)return;
        var source=new GameObject("Temporary native marquee glyphs"){hideFlags=HideFlags.DontSave};
        try {
            source.SetActive(false);source.transform.SetParent(root.transform,false);
            var label=source.AddComponent<TextMeshPro>();label.font=font;label.fontSharedMaterial=font.material;
            label.text=Message;label.fontSize=20;label.alignment=TextAlignmentOptions.Center;
            label.textWrappingMode=TextWrappingModes.NoWrap;label.overflowMode=TextOverflowModes.Overflow;
            label.rectTransform.sizeDelta=new Vector2(100,10);label.color=new Color(.36f,1,.50f);
            // Let TMP initialize its native renderer before sampling. The
            // temporary source is destroyed synchronously before any frame.
            source.SetActive(true);
            label.ForceMeshUpdate(true,true);
            if(label.textInfo.materialCount!=1)throw new InvalidOperationException("Marquee requires a single native Norse font atlas");
            var bounds=label.mesh.bounds;
            if(bounds.size.y<.001f||bounds.size.x<.001f)throw new InvalidOperationException("Native marquee glyphs have empty geometry");
            var material=new Material(font.material){name="Parkade native Norse neon letters",hideFlags=HideFlags.DontSave};
            materials.Add(material);
            // Optional native SDF glow; bitmap fonts still render the bright
            // face colour without switching to an assumed shader.
            if(material.HasFloat("_GlowPower")) {
                material.EnableKeyword("GLOW_ON");material.SetFloat("_GlowPower",.45f);
                if(material.HasFloat("_GlowOuter"))material.SetFloat("_GlowOuter",.20f);
                if(material.HasColor("_GlowColor"))material.SetColor("_GlowColor",new Color(.08f,1,.25f,.7f));
            }
            float scale=1.55f/bounds.size.y;
            float period=Math.Max(bounds.size.x*scale+2.5f,TextHalfWidth+.5f);
            for(int copy=0;copy<3;copy++) {
                var mesh=UnityEngine.Object.Instantiate(label.mesh);meshes.Add(mesh);
                mesh.name="Cached clipped RIP McRib glyphs "+copy;mesh.hideFlags=HideFlags.DontSave;mesh.MarkDynamic();
                var vertices=mesh.vertices;var uv=new List<Vector4>(vertices.Length);mesh.GetUVs(0,uv);
                for(int i=0;i<vertices.Length;i++)vertices[i]=new Vector3((vertices[i].x-bounds.min.x)*scale,(vertices[i].y-bounds.center.y)*scale,0);
                var item=new Marquee{Mesh=mesh,Source=vertices,Vertices=(Vector3[])vertices.Clone(),SourceUV=uv.ToArray(),UV=uv,Period=period,Copy=copy};
                text.Add(item);
                var go=new GameObject("Scrolling RIP McRib native text "+copy){hideFlags=HideFlags.DontSave};
                go.transform.SetParent(root.transform,false);go.transform.localPosition=new Vector3(0,0,-.23f);
                // TMP glyphs face local -Z like an ordinary native world sign.
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                mesh.bounds=new Bounds(Vector3.zero,new Vector3(Width,3,.1f));
            }
            log?.Invoke("Parkade RIP McRib marquee ready: native Norse font, three cached glyph meshes, no colliders or real-time lights");
        } finally {UnityEngine.Object.DestroyImmediate(source);}
    }
    void Animate(float time) {
        const float half=6.16f;
        float perimeter=4*(half+BorderHalfHeight);
        for(int i=0;i<runners.Count;i++) {
            float s=(time*3+i*perimeter/runners.Count)%perimeter;
            Vector3 p;float angle;
            if(s<2*half){p=new Vector3(-half+s,BorderHalfHeight,-.245f);angle=0;}
            else if((s-=2*half)<2*BorderHalfHeight){p=new Vector3(half,BorderHalfHeight-s,-.245f);angle=90;}
            else if((s-=2*BorderHalfHeight)<2*half){p=new Vector3(half-s,-BorderHalfHeight,-.245f);angle=0;}
            else {s-=2*half;p=new Vector3(-half,-BorderHalfHeight+s,-.245f);angle=90;}
            runners[i].localPosition=p;runners[i].localRotation=Quaternion.Euler(0,0,angle);
        }
        foreach(var item in text) {
            float offset=-TextHalfWidth+item.Copy*item.Period-(time*1.8f)%item.Period;
            for(int q=0;q<item.Source.Length;q+=4) {
                float left=item.Source[q].x+offset,right=item.Source[q+2].x+offset;
                float width=right-left;
                for(int n=0;n<4;n++) {
                    int i=q+n;var v=item.Source[i];float x=Mathf.Clamp(v.x+offset,-TextHalfWidth,TextHalfWidth);
                    float t=width>.00001f?Mathf.Clamp01((x-left)/width):0;
                    v.x=x;item.Vertices[i]=v;
                    // TMP's BL/TL/TR/BR UV layout; interpolate the two horizontal
                    // edges together so partial characters clip without smear.
                    bool top=n==1||n==2;
                    item.UV[i]=Vector4.Lerp(item.SourceUV[q+(top?1:0)],item.SourceUV[q+(top?2:3)],t);
                }
            }
            // Keep all UV0 components: newer TMP atlases also encode SDF data
            // in Z/W, which the legacy Mesh.uv setter would discard.
            item.Mesh.vertices=item.Vertices;item.Mesh.SetUVs(0,item.UV);
        }
    }
    internal void Clear() {
        if(root!=null){root.SetActive(false);UnityEngine.Object.Destroy(root);}
        foreach(var mesh in meshes)if(mesh!=null)UnityEngine.Object.Destroy(mesh);
        foreach(var material in materials)if(material!=null)UnityEngine.Object.Destroy(material);
        meshes.Clear();materials.Clear();runners.Clear();text.Clear();root=null;world=null;scene=null;nextFontAttempt=0;failed=false;
    }
}
}
