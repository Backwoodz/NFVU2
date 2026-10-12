using System;
using System.Linq;
using Rachel350Z;
using UnityEngine;
using UnityEngine.Rendering;
class Program {
    static int checks;
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
    static Mesh Surface(int count=4) {
        var mesh=new Mesh{vertices=new Vector3[count],normals=new Vector3[count],uv=new Vector2[count],colors=new Color[count]};
        mesh.vertices[0]=new Vector3(0,0,0);mesh.vertices[1]=new Vector3(1,0,0);
        mesh.vertices[2]=new Vector3(0,1,0);mesh.vertices[count-1]=new Vector3(1,1,0);
        for(int i=0;i<count;i++) {
            mesh.normals[i]=new Vector3(0,0,1);
            mesh.uv[i]=new Vector2(i*.1f,i*.2f);mesh.colors[i]=new Color(.2f,.3f,.4f,1);
        }
        mesh.triangles=new[]{0,1,2,1,count-1,2};return mesh;
    }
    // Sign of projected triangle area: opposite viewpoints must see opposite sides.
    static float Facing(Mesh m,int triangle,float viewZ) {
        Vector3 a=m.vertices[m.triangles[triangle]],b=m.vertices[m.triangles[triangle+1]],c=m.vertices[m.triangles[triangle+2]];
        return ((b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x))*viewZ;
    }
    static void Main() {
        var mesh=Surface();var vertices=mesh.vertices;var normals=mesh.normals;var uv=mesh.uv;var colors=mesh.colors;var indices=mesh.triangles;
        VehicleMesh.AddBackfaces(mesh);
        Check(mesh.vertices.Length==8&&mesh.triangles.Length==12,"both sides share one mesh and one draw surface");
        Check(mesh.vertices.Take(4).SequenceEqual(vertices)&&mesh.triangles.Take(6).SequenceEqual(indices),"exterior positions and triangle order remain intact");
        Check(mesh.vertices.Skip(4).SequenceEqual(vertices),"backfaces retain the exact shell shape without an offset or silhouette change");
        Check(mesh.uv.Take(4).SequenceEqual(uv)&&mesh.uv.Skip(4).SequenceEqual(uv)&&mesh.colors.Skip(4).SequenceEqual(colors),"both sides retain UV seams and vertex colors");
        Check(mesh.normals.Take(4).SequenceEqual(normals)&&mesh.normals.Skip(4).All(n=>n.z==-1),"separate inward normals avoid exterior shading cancellation");
        Check(Facing(mesh,0,1)>0&&Facing(mesh,6,1)<0&&Facing(mesh,0,-1)<0&&Facing(mesh,6,-1)>0,"inside and outside views each have exactly one front-facing surface");
        Check(indices.SequenceEqual(new[]{0,1,2,1,3,2})&&normals.All(n=>n.z==1)&&vertices.Length==4,"source arrays stay unchanged for cached models and respawns");
        Check(mesh.indexFormat==IndexFormat.UInt16,"small surfaces retain 16-bit indices");
        foreach(int count in new[]{32767,32768,65535}) {
            mesh=Surface(count);VehicleMesh.AddBackfaces(mesh);
            Check(mesh.indexFormat==(count*2>65535?IndexFormat.UInt32:IndexFormat.UInt16)&&mesh.triangles.Max()==count*2-1,
                "index buffer survives vertex duplication at "+count+" source vertices");
        }
        mesh=Surface();mesh.subMeshCount=2;
        bool rejected=false;try{VehicleMesh.AddBackfaces(mesh);}catch(ArgumentException){rejected=true;}
        Check(rejected&&mesh.vertices.Length==4,"unexpected multi-material meshes fail before modification");
        Console.WriteLine(checks+" geometry checks passed; no Unity rendering executed.");
    }
}
