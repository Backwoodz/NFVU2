using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rachel350Z {
// Two local, collision-free meshes; original game assets and saved native
// objects are unnecessary for the start/finish paint. Owner clears on world exit.
internal sealed class ParkadeStartLine {
    readonly Action<string> log;
    readonly List<Mesh> meshes=new List<Mesh>();
    Material[] materials;
    GameObject root;
    bool failed;
    internal ParkadeStartLine(Action<string> log=null){this.log=log;}
    internal void Tick() {
        if(root!=null||failed)return;
        try {
            // Uses an actual supported native shader and neutral textures, just
            // like the already verified practice marking. No assumed Standard.
            materials=VehicleMaterials.Create(null,message=>{});
            Configure(materials[0],"Parkade start chalk",new Color(.9f,.86f,.74f),.55f);
            Configure(materials[1],"Parkade start charcoal",new Color(.04f,.045f,.04f),.08f);
            root=new GameObject("NFVU2 Parkade Start Finish"){hideFlags=HideFlags.DontSave};
            var start=ParkadeLayout.AtDistance(0);
            for(int parity=0;parity<2;parity++) {
                var vertices=new List<Vector3>();var indices=new List<int>();
                for(int row=0;row<2;row++)for(int column=0;column<18;column++) {
                    if((row+column)%2!=parity)continue;
                    float a=-.8f+row*.8f,b=a+.8f,left=-9+column,right=left+1;
                    int first=vertices.Count;
                    vertices.Add(Point(start,a,left));vertices.Add(Point(start,a,right));
                    vertices.Add(Point(start,b,right));vertices.Add(Point(start,b,left));
                    indices.AddRange(new[]{first,first+1,first+2,first,first+2,first+3});
                }
                var mesh=new Mesh{name="Parkade ground checker "+parity,hideFlags=HideFlags.DontSave};
                mesh.vertices=vertices.ToArray();mesh.triangles=indices.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
                var go=new GameObject("Parkade start ground paint "+parity){hideFlags=HideFlags.DontSave};go.transform.SetParent(root.transform,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=materials[parity];
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            }
        } catch(Exception error) {
            Clear();failed=true;log?.Invoke("Parkade ground marking unavailable: "+error.GetBaseException().Message);
        }
    }
    static Vector3 Point(DriftTrackSample p,float forward,float across) {
        float x=p.X+p.TangentX*forward-p.TangentZ*across,z=p.Z+p.TangentZ*forward+p.TangentX*across;
        return new Vector3(x,ParkadeLayout.TerrainHeightAt(x,z)+.06f,z);
    }
    static void Configure(Material material,string name,Color color,float emission) {
        material.name=name;material.hideFlags=HideFlags.DontSave;
        foreach(string property in new[]{"_Color","_BaseColor","_SkinColor"})if(material.HasColor(property))material.SetColor(property,color);
        foreach(string property in new[]{"_Glossiness","_Smoothness"})if(material.HasFloat(property))material.SetFloat(property,.06f);
        if(material.HasFloat("_Metallic"))material.SetFloat("_Metallic",0);
        VehicleMaterials.SetEmission(material,color*emission);
    }
    internal void Clear() {
        if(root!=null){root.SetActive(false);UnityEngine.Object.Destroy(root);root=null;}
        foreach(var mesh in meshes)UnityEngine.Object.Destroy(mesh);meshes.Clear();
        if(materials!=null)foreach(var material in materials)if(material!=null)UnityEngine.Object.Destroy(material);
        materials=null;failed=false;
    }
}
}
