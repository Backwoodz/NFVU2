using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
namespace Rachel350Z {
public sealed partial class TransformerMinigun {
    readonly TransformerMinigunHeatModel heatState=new TransformerMinigunHeatModel();
    readonly List<MeshRenderer> heatRenderers=new List<MeshRenderer>();
    Material heatMaterial;
    Texture2D heatMask;
    readonly Color[] heatPixels={Color.clear,Color.white};
    bool heatTextureTint;
    float appliedHeat=-1;
    struct HeatVertex {
        public Vector3 point,normal;
        public HeatVertex(Vector3 p,Vector3 n){point=p;normal=n;}
    }
    static List<HeatVertex> ClipHeatFront(List<HeatVertex> triangle) {
        var result=new List<HeatVertex>(4);var previous=triangle[triangle.Count-1];
        foreach(var current in triangle) {
            bool before=previous.point.z>=.30f,after=current.point.z>=.30f;
            if(before!=after) {
                float t=(.30f-previous.point.z)/(current.point.z-previous.point.z);
                result.Add(new HeatVertex(Vector3.Lerp(previous.point,current.point,t),Vector3.Lerp(previous.normal,current.normal,t).normalized));
            }
            if(after)result.Add(current);
            previous=current;
        }
        return result;
    }
    void CreateHeat(ModelData model) {
        // A Z-derived per-vertex mask is carried in overlay UVs as well as
        // vertex colors. The texture keeps the mask valid for glow-shader
        // fallbacks which do not consume vertex colors. Original UVs/materials
        // remain on the untouched underlying Rust mesh.
        heatMask=new Texture2D(2,1,TextureFormat.RGBA32,false){name="NFVU2 barrel heat gradient",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
        heatMask.SetPixels(heatPixels);heatMask.Apply(false,false);
        heatMaterial=VehicleMaterials.CreateGlowMaterial(heatMask);heatMaterial.name="NFVU2 original barrel metal heat";
        // Built-in Unlit/Transparent has no color property and can ignore
        // vertex colors. Keep only that fallback's tiny mask CPU-readable so
        // its texels themselves carry both the warm tint and cooling alpha.
        heatTextureTint=!heatMaterial.HasColor("_TintColor")&&!heatMaterial.HasColor("_Color");
        if(!heatTextureTint)heatMask.Apply(false,true);
        if(heatMaterial.HasFloat("_ZWrite"))heatMaterial.SetFloat("_ZWrite",0);
        if(heatMaterial.HasFloat("_Cull"))heatMaterial.SetFloat("_Cull",2);
        foreach(var part in model.parts.Where(p=>p.name.StartsWith("barrels|"))) {
            float maximum=part.vertices.Max(v=>v.z);
            var triangles=new List<int>();var vertices=new List<Vector3>();var normals=new List<Vector3>();var colors=new List<Color>();var uv=new List<Vector2>();
            for(int i=0;i<part.indices.Length;i+=3) {
                int a=part.indices[i],b=part.indices[i+1],c=part.indices[i+2];
                if(part.vertices[a].z<=.30f&&part.vertices[b].z<=.30f&&part.vertices[c].z<=.30f)continue;
                var polygon=ClipHeatFront(new List<HeatVertex>{
                    new HeatVertex(V(part.vertices[a]),V(part.normals[a])),new HeatVertex(V(part.vertices[b]),V(part.normals[b])),new HeatVertex(V(part.vertices[c]),V(part.normals[c]))});
                if(polygon.Count<3)continue;
                int start=vertices.Count;
                foreach(var vertex in polygon) {
                    float mask=TransformerMinigunHeatModel.FrontMask(vertex.point.z,maximum);
                    vertices.Add(vertex.point-V(rig.barrelPivot)+vertex.normal*.00035f);normals.Add(vertex.normal);
                    colors.Add(new Color(1,1,1,mask));uv.Add(new Vector2(.25f+mask*.5f,.5f));
                }
                for(int v=1;v<polygon.Count-1;v++){triangles.Add(start);triangles.Add(start+v);triangles.Add(start+v+1);}
            }
            if(triangles.Count==0)continue;
            var mesh=new Mesh{name="NFVU2 heat on "+part.name};meshes.Add(mesh);
            mesh.vertices=vertices.ToArray();mesh.normals=normals.ToArray();mesh.triangles=triangles.ToArray();mesh.colors=colors.ToArray();mesh.uv=uv.ToArray();mesh.RecalculateBounds();
            var node=Node(mesh.name,barrels,Vector3.zero);node.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=node.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=heatMaterial;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.enabled=false;heatRenderers.Add(renderer);
        }
    }
    void TickHeat(float dt,int shots) {
        heatState.Step(dt,shots);
        float heat=heatState.Heat;
        foreach(var renderer in heatRenderers)if(renderer!=null)renderer.enabled=heat>.001f;
        if(heatMaterial==null||heat<=.001f||heat==appliedHeat)return;
        appliedHeat=heat;
        var color=Color.Lerp(new Color(.8f,.012f,.001f,1),new Color(2.2f,.42f,.025f,1),heat);
        color.r*=heat;color.g*=heat;color.b*=heat;color.a=heat;
        if(heatTextureTint) {
            heatPixels[1]=color;heatMask.SetPixels(heatPixels);heatMask.Apply(false,false);
        } else {
            if(heatMaterial.HasColor("_TintColor"))heatMaterial.SetColor("_TintColor",color);
            if(heatMaterial.HasColor("_Color"))heatMaterial.SetColor("_Color",color);
        }
    }
    void DisposeHeat() {
        heatState.Reset();heatRenderers.Clear();
        if(heatMaterial!=null)UnityEngine.Object.Destroy(heatMaterial);heatMaterial=null;
        if(heatMask!=null)UnityEngine.Object.Destroy(heatMask);heatMask=null;
        // Overlay meshes belong to the main bounded per-gun mesh list.
    }
}
}
