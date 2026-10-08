using System;
using UnityEngine;

namespace Rachel350Z {
// Each slot owns its animated copy; Plugin's cached source meshes remain untouched.
public sealed class SubwooferVisual : IDisposable {
    Mesh mesh;
    readonly Vector3[] rest,deformed;
    readonly float[] weights;
    float previous;
    public SubwooferVisual(Transform node,ModelPart part,float radius) {
        var filter=node.GetComponent<MeshFilter>();
        mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);mesh.name=part.name+"_Membrane";mesh.MarkDynamic();filter.sharedMesh=mesh;
        rest=mesh.vertices;deformed=(Vector3[])rest.Clone();weights=new float[rest.Length];
        for(int i=0;i<rest.Length;i++)weights[i]=BassMotion.ConeWeight(rest[i].x,rest[i].y,part.normals[i].z,radius);
        var bounds=mesh.bounds;bounds.Expand(.04f);mesh.bounds=bounds;
    }
    public void Tick(float offset) {
        if(mesh==null||Mathf.Abs(previous-offset)<.00001f)return;previous=offset;
        for(int i=0;i<rest.Length;i++){deformed[i]=rest[i];deformed[i].z+=weights[i]*offset;}
        mesh.vertices=deformed;mesh.RecalculateNormals();
    }
    public void Dispose(){if(mesh!=null)UnityEngine.Object.Destroy(mesh);mesh=null;}
}
}
