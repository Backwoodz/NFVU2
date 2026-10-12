// Array storage only; these checks do not execute Unity rendering.
using System;
namespace UnityEngine.Rendering { public enum IndexFormat { UInt16,UInt32 } }
namespace UnityEngine {
public struct Vector3 {
    public float x,y,z;
    public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
    public static Vector3 operator -(Vector3 v)=>new Vector3(-v.x,-v.y,-v.z);
}
public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} }
public struct Color { public float r,g,b,a; public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;} }
public class Mesh {
    public Vector3[] vertices,normals;
    public Vector2[] uv;
    public Color[] colors;
    public int subMeshCount=1;
    int[] indices;
    Rendering.IndexFormat format;
    public Rendering.IndexFormat indexFormat {
        get=>format;
        set {format=value;indices=Array.Empty<int>();subMeshCount=1;}
    }
    public int[] triangles {
        get=>indices;
        set {
            foreach(int i in value)if(i<0||i>=vertices.Length||format==Rendering.IndexFormat.UInt16&&i>=65535)
                throw new ArgumentException("Triangle exceeds vertex/index buffer");
            indices=value;
        }
    }
}
}
