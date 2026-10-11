using System;
using System.Collections.Generic;
using UnityEngine;
namespace Rachel350Z {
// One lightweight mesh and dedicated additive material; no collider/light or per-frame allocations.
public sealed class HordeShieldVisual : IDisposable {
    readonly GameObject root;
    readonly Mesh mesh;
    readonly MeshRenderer renderer;
    readonly Material shieldMaterial;
    readonly Color[] colors;
    readonly float[] weights,phases;
    public HordeShieldVisual(Transform car,Material material) {
        root=new GameObject("NFVU2_HordeShield");root.transform.SetParent(car,false);
        root.transform.localPosition=new Vector3(0,-.18f,0);
        var vertices=new List<Vector3>();var indices=new List<int>();var strength=new List<float>();var phase=new List<float>();
        // Map a regular hexagonal disk onto a hemisphere. Shared cell corners
        // follow the same projection, so the dome has a continuous honeycomb.
        Vector3 Shape(Vector3 v) {
            float r=v.x*v.x+v.z*v.z,denominator=1+r;
            return new Vector3(1.85f*2*v.x/denominator,2.55f*(1-r)/denominator,3.2f*2*v.z/denominator);
        }
        void Vertex(Vector3 v,float weight,float p){vertices.Add(Shape(v));strength.Add(weight);phase.Add(p);}
        const float radius=.1f;
        for(int row=-8;row<=8;row++) {
            for(int column=-8;column<=8;column++) {
                var center=new Vector3(Mathf.Sqrt(3)*radius*(column+row*.5f),0,1.5f*radius*row);
                if(center.sqrMagnitude>1)continue;
                float p=center.z*3+center.x*2;
                for(int side=0;side<6;side++) {
                    float a=(side+.5f)*Mathf.PI/3,b=(side+1.5f)*Mathf.PI/3;
                    var va=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius;
                    var vb=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b))*radius;
                    int start=vertices.Count;
                    Vertex(center+va*.99f,1,p);Vertex(center+vb*.99f,1,p);
                    Vertex(center+vb*.94f,1,p);Vertex(center+va*.94f,1,p);
                    indices.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
                    start=vertices.Count;
                    Vertex(center,.08f,p);Vertex(center+va*.94f,.18f,p);Vertex(center+vb*.94f,.18f,p);
                    indices.AddRange(new[]{start,start+1,start+2});
                }
            }
        }
        weights=strength.ToArray();phases=phase.ToArray();colors=new Color[vertices.Count];
        mesh=new Mesh{name="NFVU2_ShieldHexShell"};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);
        var uv=new Vector2[vertices.Count];for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(.5f,.5f);mesh.uv=uv;
        mesh.colors=colors;mesh.RecalculateBounds();
        // Cabin glass uses queue 3000. A separate, later transparent pass keeps
        // glass from painting over the near side of the dome as the camera orbits.
        // Retain depth testing against the world and do not write shield depth.
        shieldMaterial=new Material(material){name="NFVU2_ShieldDome",renderQueue=3100};
        if(shieldMaterial.HasFloat("_ZWrite"))shieldMaterial.SetFloat("_ZWrite",0);
        if(shieldMaterial.HasFloat("_ZTest"))shieldMaterial.SetFloat("_ZTest",(float)UnityEngine.Rendering.CompareFunction.LessEqual);
        root.AddComponent<MeshFilter>().sharedMesh=mesh;renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=shieldMaterial;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
        Tick(0,1,0);
    }
    public void Tick(float time,float charge,float flash) {
        renderer.enabled=charge>0||flash>0;if(!renderer.enabled)return;
        float hit=Mathf.Clamp01(flash/.35f);
        root.transform.localScale=Vector3.one*(1+.025f*hit);
        var tint=Color.Lerp(new Color(.06f,.6f,1),new Color(.65f,.95f,1),hit);
        for(int i=0;i<colors.Length;i++) {
            float sweep=Mathf.Pow(.5f+.5f*Mathf.Sin(phases[i]-time*1.6f),8);
            float idle=charge>0?(.11f+.065f*sweep)*(.7f+.3f*Mathf.Clamp01(charge)):0;
            tint.a=weights[i]*(idle+.65f*hit*hit);colors[i]=tint;
        }
        mesh.colors=colors;
    }
    public void Dispose(){if(root!=null)UnityEngine.Object.Destroy(root);if(mesh!=null)UnityEngine.Object.Destroy(mesh);if(shieldMaterial!=null)UnityEngine.Object.Destroy(shieldMaterial);}
}
}
