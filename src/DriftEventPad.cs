using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rachel350Z {
internal sealed class DriftEventPad {
    internal const float X=178,Z=-10,Radius=4;
    GameObject root;
    Material material;
    TextMeshPro label;
    readonly List<Mesh> meshes=new List<Mesh>();
    internal void Tick(bool visible) {
        if(root==null)Create();if(label==null)CreateLabel();root.SetActive(visible);
    }
    void Create() {
        root=new GameObject("NFVU2 Parkade event marker");root.hideFlags=HideFlags.DontSave;
        root.transform.position=new Vector3(X,DriftWorldRules.GroundHeight+.07f,Z);
        material=VehicleMaterials.CreateGlowMaterial(Texture2D.whiteTexture);material.name="NFVU2 green event light";
        foreach(string property in new[]{"_TintColor","_Color","_BaseColor"})if(material.HasColor(property))material.SetColor(property,new Color(.12f,.95f,.35f,.22f));
        if(material.HasFloat("_Cull"))material.SetFloat("_Cull",0);
        Build();
    }
    void CreateLabel() {
        var font=Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f=>f.name.IndexOf("Norse",StringComparison.OrdinalIgnoreCase)>=0);
        if(font==null)return;
        var go=new GameObject("Parkade ground name");go.transform.SetParent(root.transform,false);
        go.transform.localPosition=new Vector3(0,.025f,0);go.transform.localRotation=Quaternion.Euler(90,90,0);
        label=go.AddComponent<TextMeshPro>();label.font=font;label.fontSharedMaterial=font.material;
        label.text="PARKADE\nDRIFT 1";label.color=new Color(.45f,1,.58f);label.alignment=TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta=new Vector2(6,3);label.fontSize=9;label.enableAutoSizing=true;label.fontSizeMin=5;label.fontSizeMax=9;
        label.textWrappingMode=TextWrappingModes.NoWrap;label.raycastTarget=false;
        label.renderer.shadowCastingMode=ShadowCastingMode.Off;label.renderer.receiveShadows=false;
    }
    void Build() {
        const int steps=48;var vertices=new Vector3[(steps+1)*2];var colors=new Color[vertices.Length];var indices=new int[steps*6];
        for(int i=0;i<=steps;i++) {
            double a=2*Math.PI*i/steps;float x=(float)Math.Cos(a),z=(float)Math.Sin(a);
            vertices[2*i]=new Vector3(x*Radius,0,z*Radius);
            vertices[2*i+1]=new Vector3(x*(Radius-.25f),0,z*(Radius-.25f));
            colors[2*i]=colors[2*i+1]=new Color(.12f,1,.3f,.9f);
            if(i==steps)continue;int p=i*6,n=i*2;
            indices[p]=n;indices[p+1]=n+2;indices[p+2]=n+1;indices[p+3]=n+1;indices[p+4]=n+2;indices[p+5]=n+3;
        }
        var mesh=new Mesh{name="Event ring"};mesh.vertices=vertices;mesh.colors=colors;mesh.triangles=indices;
        mesh.uv=new Vector2[vertices.Length];mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
        var go=new GameObject("Event ring");go.transform.SetParent(root.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
    }
    internal void Clear() {
        if(root!=null)UnityEngine.Object.Destroy(root);if(material!=null)UnityEngine.Object.Destroy(material);
        foreach(var mesh in meshes)UnityEngine.Object.Destroy(mesh);meshes.Clear();root=null;material=null;label=null;
    }
}
}
