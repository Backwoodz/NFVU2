using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rachel350Z {
// Imported vehicle surfaces are thin shells. Give each side its own normals
// instead of relying on shader-specific culling switches or smoothing the seam.
internal static class VehicleMesh {
    public static void AddBackfaces(Mesh mesh) {
        var vertices=mesh.vertices;var normals=mesh.normals;
        var uv=mesh.uv;var colors=mesh.colors;var front=mesh.triangles;
        int count=vertices.Length;
        if(mesh.subMeshCount!=1||normals.Length!=count||uv.Length!=count||colors.Length!=count)
            throw new ArgumentException("Backfaces require one complete visual mesh");
        var bothNormals=Duplicate(normals);
        for(int i=0;i<count;i++)bothNormals[count+i]=-normals[i];
        var indices=new int[front.Length*2];Array.Copy(front,indices,front.Length);
        for(int i=0;i<front.Length;i+=3) {
            indices[front.Length+i]=front[i]+count;
            indices[front.Length+i+1]=front[i+2]+count;
            indices[front.Length+i+2]=front[i+1]+count;
        }
        // Changing the index format clears the index buffer; assign triangles last.
        if(count*2>65535)mesh.indexFormat=IndexFormat.UInt32;
        mesh.vertices=Duplicate(vertices);mesh.normals=bothNormals;
        mesh.uv=Duplicate(uv);mesh.colors=Duplicate(colors);mesh.triangles=indices;
        // Caller recalculates tangents/bounds after completing the mesh.
    }
    static T[] Duplicate<T>(T[] source) {
        var result=new T[source.Length*2];
        Array.Copy(source,result,source.Length);
        Array.Copy(source,0,result,source.Length,source.Length);
        return result;
    }
}
}
