using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Rachel350Z {
// Optional local asset: the original car and its handling remain independently loadable.
internal sealed class VehicleInterior:IDisposable {
    readonly List<Mesh> meshes=new List<Mesh>();
    readonly List<GameObject> objects=new List<GameObject>();
    readonly List<Renderer> hidden=new List<Renderer>();
    readonly List<Tuple<Renderer,Material>> glassOriginals=new List<Tuple<Renderer,Material>>();
    readonly List<Tuple<Renderer,bool>> driverWindows=new List<Tuple<Renderer,bool>>();
    Material[] materials;Texture2D[] textures;Material glass;
    Transform steering;
    public static readonly Vector3 Seat=new Vector3(-.354f,.35f,-.25f);
    public static readonly Vector3 WheelPivot=new Vector3(-.353755f,.733926f,.33613f);
    static readonly Vector3 WheelAxis=new Vector3(0,.355071f,-.934839f);
    // Targets describe the wrist behind the rim, allowing the fingers to reach onto it.
    public Vector3 RightGrip=>steering.TransformPoint(new Vector3(-.215f,.78f,.26f)+WheelAxis*.06f-WheelPivot);
    public Vector3 RightHandForward=>steering.TransformDirection(-WheelAxis);
    public bool SupportsTint=>glass!=null;
    bool driverWindowOpen;
    float lastTint=-1;
    public static VehicleInterior Create(GameObject car,MonoBehaviour player,string path,Action<string> log) {
        if(!File.Exists(path))return null;
        var result=new VehicleInterior();
        try {result.Load(car,player,ModelFile.Load(path),log);return result;}
        catch {result.Dispose();throw;}
    }
    void Load(GameObject car,MonoBehaviour player,ModelData model,Action<string> log) {
        materials=VehicleMaterials.CreateForModel(player,model,log,out textures);
        foreach(var t in textures)t.wrapMode=TextureWrapMode.Repeat;
        var wheel=new GameObject("350Z_CabinSteering");objects.Add(wheel);steering=wheel.transform;
        steering.SetParent(car.transform,false);steering.localPosition=WheelPivot;
        foreach(var p in model.parts) {
            var obj=new GameObject(p.name);objects.Add(obj);
            bool isWheel=p.name.StartsWith("Cabin_Nis350Z_steer_");
            Transform parent=isWheel?steering:car.transform;
            if(!string.IsNullOrEmpty(p.movingPart)) {
                parent=car.transform.Find("350Z_Motion_"+p.movingPart);
                if(parent==null)throw new InvalidDataException("Cabin door requires showroom pivot");
            }
            obj.transform.SetParent(parent,false);
            var mesh=new Mesh{name=p.name};meshes.Add(mesh);
            mesh.vertices=p.vertices.Select(v=>new Vector3(v.x,v.y,v.z)-(isWheel?WheelPivot:Vector3.zero)).ToArray();
            mesh.normals=p.normals.Select(v=>new Vector3(v.x,v.y,v.z)).ToArray();
            mesh.uv=p.uv.Select(v=>new Vector2(v.u,v.v)).ToArray();mesh.triangles=p.indices;
            mesh.colors=Enumerable.Repeat(Color.white,p.vertices.Length).ToArray();mesh.RecalculateBounds();mesh.RecalculateTangents();
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=materials[p.material];
        }
        foreach(var r in car.GetComponentsInChildren<MeshRenderer>()) {
            string n=r.name;
            if(n.Contains("DOOR_PANEL_")||n=="350Z_BASE_A_slice3_3"||n=="350Z_BASE_A_slice5_5"||n=="350Z_BASE_A_slice6_6") {
                if(r.enabled){hidden.Add(r);r.enabled=false;}
            }
        }
        // NFS RIGHT is the physical left door after our coordinate conversion.
        foreach(var r in car.GetComponentsInChildren<MeshRenderer>())
            if(r.name.Contains("DOOR_RIGHT_A")&&r.sharedMaterial!=null&&r.sharedMaterial.name.StartsWith("350Z_471a1dca_"))
                driverWindows.Add(Tuple.Create<Renderer,bool>(r,r.enabled));
        // Keep the original window geometry, now with a transparent material.
        var template=Resources.FindObjectsOfTypeAll<Material>().FirstOrDefault(m=>m!=null&&m.shader!=null&&m.shader.isSupported&&
            m.name.IndexOf("glass",StringComparison.OrdinalIgnoreCase)>=0&&m.renderQueue>=2500&&m.HasColor("_Color")&&
            !m.name.StartsWith("350Z_"));
        var shader=Shader.Find("Standard")??Shader.Find("Legacy Shaders/Transparent/Diffuse")??Shader.Find("Unlit/Transparent");
        if(template!=null||shader!=null&&shader.isSupported) {
            // Loaded glass can be lavalanternglass_mat with red _EMISSION enabled.
            // Reuse its shader reference only, never its saved colours/maps/keywords.
            glass=new Material(template!=null?template.shader:shader);glass.name="350Z_CabinGlass";glass.renderQueue=3000;
            if(glass.HasTexture("_MainTex"))glass.SetTexture("_MainTex",Texture2D.whiteTexture);
            glass.DisableKeyword("_EMISSION");
            foreach(var name in new[]{"_EmissionColor","_EmissiveColor"})if(glass.HasColor(name))glass.SetColor(name,Color.black);
            if(glass.HasTexture("_EmissionMap"))glass.SetTexture("_EmissionMap",Texture2D.blackTexture);
            glass.SetColor("_Color",new Color(.16f,.22f,.25f,.16f));
            glass.SetOverrideTag("RenderType","Transparent");
            if(glass.HasFloat("_Mode"))glass.SetFloat("_Mode",2);
            if(glass.HasFloat("_SrcBlend"))glass.SetFloat("_SrcBlend",5);
            if(glass.HasFloat("_DstBlend"))glass.SetFloat("_DstBlend",10);
            if(glass.HasFloat("_ZWrite"))glass.SetFloat("_ZWrite",0);
            if(glass.HasFloat("_Glossiness"))glass.SetFloat("_Glossiness",.8f);
            glass.DisableKeyword("_ALPHATEST_ON");glass.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            glass.EnableKeyword("_ALPHABLEND_ON");
            log("Cabin glass shader: "+glass.shader.name+"; reference: "+(template!=null?template.name:"Shader.Find")+"; fresh material, emission disabled");
            foreach(var r in car.GetComponentsInChildren<MeshRenderer>())
                if(r.sharedMaterial!=null&&r.sharedMaterial.name.StartsWith("350Z_471a1dca_")) {
                    glassOriginals.Add(Tuple.Create<Renderer,Material>(r,r.sharedMaterial));r.sharedMaterial=glass;
                }
        } else log("Cabin: transparent window shader unavailable; original windows retained.");
        log("Cabin loaded: "+model.parts.Length+" meshes, separate steering and door trims.");
    }
    public void Tick(float angle,float tint,bool occupied) {
        if(steering!=null)steering.localRotation=Quaternion.AngleAxis(CabinAppearance.SteeringDegrees(angle),WheelAxis);
        SetDriverWindowOpen(occupied);
        tint=CabinAppearance.Tint(tint);
        if(glass==null||Mathf.Approximately(lastTint,tint))return;
        lastTint=tint;float shade=Mathf.Lerp(.2f,.015f,tint);
        var color=new Color(shade,shade,shade,CabinAppearance.WindowAlpha(tint));
        foreach(var name in new[]{"_Color","_BaseColor","_TintColor"})if(glass.HasColor(name))glass.SetColor(name,color);
    }
    public void SetDriverWindowOpen(bool open) {
        if(driverWindowOpen==open)return;driverWindowOpen=open;
        foreach(var item in driverWindows)if(item.Item1!=null)item.Item1.enabled=!open&&item.Item2;
    }
    public void Dispose() {
        SetDriverWindowOpen(false);driverWindows.Clear();
        foreach(var r in hidden)if(r!=null)r.enabled=true;hidden.Clear();
        foreach(var entry in glassOriginals)if(entry.Item1!=null)entry.Item1.sharedMaterial=entry.Item2;glassOriginals.Clear();
        foreach(var obj in objects)if(obj!=null)UnityEngine.Object.Destroy(obj);objects.Clear();
        foreach(var mesh in meshes)if(mesh!=null)UnityEngine.Object.Destroy(mesh);meshes.Clear();
        if(materials!=null)foreach(var m in materials)if(m!=null)UnityEngine.Object.Destroy(m);
        if(textures!=null)foreach(var t in textures)if(t!=null)UnityEngine.Object.Destroy(t);
        if(glass!=null)UnityEngine.Object.Destroy(glass);
    }
}
}
