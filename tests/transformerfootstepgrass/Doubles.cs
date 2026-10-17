using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
namespace UnityEngine {
public class Object {public string name;}
public class Mesh:Object {}
public class Material:Object {}
public class GameObject:Object {
    public InstanceRenderer Renderer;
    public T GetComponent<T>() where T:class=>Renderer as T;
}
public class Transform {
    public Vector3 position;public Transform parent;
    public bool IsChildOf(Transform other){for(var p=this;p!=null;p=p.parent)if(ReferenceEquals(p,other))return true;return false;}
}
public struct Vector3 {
    public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
    public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);
    public static Vector3 operator/(Vector3 v,float d)=>new Vector3(v.x/d,v.y/d,v.z/d);
}
public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
public struct Matrix4x4 {
    public float m00,m01,m02,m03,m10,m11,m12,m13,m20,m21,m22,m23,m30,m31,m32,m33;
    public static Matrix4x4 At(float x,float y,float z)=>new Matrix4x4{m00=1,m11=1,m22=1,m33=1,m03=x,m13=y,m23=z};
}
public static class Mathf {
    public static float Abs(float v)=>Math.Abs(v);
    public static float Max(float a,float b)=>Math.Max(a,b);
    public static float Clamp01(float v)=>Math.Max(0,Math.Min(1,v));
}
public static class Time {public static float unscaledTime;}
}
public class ClutterSystem {
    public class Clutter {
        public bool m_enabled=true,m_instanced=true;
        public string m_name;
        public UnityEngine.GameObject m_prefab;
    }
    public static ClutterSystem instance;
    public readonly UnityEngine.Transform transform=new UnityEngine.Transform();
    public readonly List<Clutter> m_clutter=new List<Clutter>();
    public float m_grassPatchSize=8;
}
public class InstanceRenderer {
    public UnityEngine.Mesh m_mesh;
    public UnityEngine.Material m_material;
    public UnityEngine.Transform transform=new UnityEngine.Transform();
    private UnityEngine.Matrix4x4[] m_instances=new UnityEngine.Matrix4x4[1024];
    private int m_instanceCount;
    private bool m_dirtyBounds=true;
    public bool Throw,SeenMasked;public int BoundsBuilds;public float BuiltScale;
    public Action DuringDraw;
    public UnityEngine.Matrix4x4 Observed;
    public void Configure(UnityEngine.Matrix4x4 matrix){m_instances[0]=matrix;m_instanceCount=1;m_dirtyBounds=true;}
    public UnityEngine.Matrix4x4 Current=>m_instances[0];
    public bool Dirty=>m_dirtyBounds;
    private void UpdateBounds(){BoundsBuilds++;BuiltScale=m_instances[0].m11;m_dirtyBounds=false;}
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void CustomUpdate(float dt,float time) {
        Rachel350Z.TransformerFootstepGrass.DrawState state=null;
        bool patched=HarmonyLib.Harmony.Installed;
        if(patched)NativeDrawHarness.Prefix(this,out state);
        try {
            if(m_instanceCount<1)return;
            if(m_dirtyBounds)UpdateBounds();
            Observed=m_instances[0];SeenMasked=Observed.m11<.999f;
            DuringDraw?.Invoke();
            if(Throw)throw new InvalidOperationException("native draw failure");
        }catch(Exception error){if(patched)NativeDrawHarness.Finalizer(error,state);throw;}
        if(patched){NativeDrawHarness.Postfix(state);NativeDrawHarness.Finalizer(null,state);}
    }
}
