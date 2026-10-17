using System;
using System.Collections.Generic;
using UnityEngine;
namespace Rachel350Z {
internal sealed partial class TransformerVisual {
    struct BadgeTriangle {public Vector3 A,B,C;}
    // Remove only the former dorsal tube, its carrier, and the six rear vent
    // lights being replaced. Other coolant lines and underbody neon remain.
    bool ReplacedRearNeonTriangle(TransformerBatch batch,int triangle,Vector3[] vertices) {
        string material=asset.Manifest.materials[batch.Material].name;
        bool allVent=material=="V9 - V13 coolant glow";
        for(int c=0;c<3;c++) {
            int i=batch.Indices[triangle+c];string bone=asset.Manifest.nodes[batch.Bones[batch.Weights[i]]].name;
            if(bone=="neon.underbody rear"||bone.EndsWith(" - neon.underbody rear",StringComparison.Ordinal))return true;
            var p=vertices[i];
            allVent&=bone=="pelvis"&&Mathf.Abs(p.x)<.45f&&p.y>2.60f&&p.y<2.90f&&p.z< -1.20f;
        }
        return allVent;
    }
    void CreateRearBadge(Material[] materials) {
        if(!ids.TryGetValue("panel.hatch",out int hatch))return;
        var surface=new List<BadgeTriangle>();
        foreach(var batch in asset.Batches) {
            if(batch.Concealed)continue;
            for(int i=0;i<batch.Indices.Length;i+=3) {
                int a=batch.Indices[i],b=batch.Indices[i+1],c=batch.Indices[i+2];
                if(batch.Bones[batch.Weights[a]]!=hatch||batch.Bones[batch.Weights[b]]!=hatch||batch.Bones[batch.Weights[c]]!=hatch)continue;
                surface.Add(new BadgeTriangle{A=BadgeVertex(batch,a),B=BadgeVertex(batch,b),C=BadgeVertex(batch,c)});
            }
        }
        int glow=Array.FindIndex(asset.Manifest.materials,m=>m.neonGroup==2);
        int metal=Array.FindIndex(asset.Manifest.materials,m=>m.name=="V9 - V13 reactor gunmetal");
        if(surface.Count==0||glow<0||metal<0)return;
        // Dedicated steady green emission, independent of trunk colour,
        // enable/pulse settings and whether a driver is sitting on the robot.
        var neon=new Material(materials[glow]){name="SLY hatch neon"};owned.Add(neon);
        var green=new Color(.2f,1,.08f);
        foreach(var key in new[]{"_Color","_BaseColor"})if(neon.HasColor(key))neon.SetColor(key,green);
        VehicleMaterials.SetEmission(neon,green*2);
        BadgeMesh("SLY mounted channels",surface,hatch,materials[metal],TransformerBadgeLayout.ChannelWidth,.002f,.016f);
        BadgeMesh("SLY neon tubes",surface,hatch,neon,TransformerBadgeLayout.TubeWidth,.016f,.032f);
    }
    static Vector3 BadgeVertex(TransformerBatch batch,int vertex) {
        int k=vertex*8;return new Vector3(batch.Vertices[k],batch.Vertices[k+1],batch.Vertices[k+2]);
    }
    static Vector3 BadgePoint(List<BadgeTriangle> surface,float x,float y,float offset) {
        float rear=float.PositiveInfinity;
        foreach(var t in surface) {
            float bx=t.B.x-t.A.x,by=t.B.y-t.A.y,cx=t.C.x-t.A.x,cy=t.C.y-t.A.y;
            float determinant=bx*cy-by*cx;if(Mathf.Abs(determinant)<.0000001f)continue;
            float px=x-t.A.x,py=y-t.A.y;
            float u=(px*cy-py*cx)/determinant,v=(bx*py-by*px)/determinant;
            if(u<-.0001f||v<-.0001f||u+v>1.0001f)continue;
            rear=Mathf.Min(rear,t.A.z+u*(t.B.z-t.A.z)+v*(t.C.z-t.A.z));
        }
        if(float.IsInfinity(rear))throw new InvalidOperationException("SLY badge has no hatch surface at its mounting point");
        return new Vector3(x,y,rear-offset);
    }
    void BadgeMesh(string name,List<BadgeTriangle> surface,int hatch,Material material,float width,float inner,float outer) {
        var vertices=new List<Vector3>();var indices=new List<int>();
        var inverse=neutral[hatch].inverse;
        foreach(var stroke in TransformerBadgeLayout.Strokes) {
            float scale=TransformerBadgeLayout.Scale;
            var a=new Vector2(stroke.Ax*scale,TransformerBadgeLayout.CenterY+stroke.Ay*scale);
            var b=new Vector2(stroke.Bx*scale,TransformerBadgeLayout.CenterY+stroke.By*scale);
            var sideways=new Vector2(-(b-a).y,(b-a).x).normalized*(width*scale*.5f);
            int steps=Mathf.Max(1,Mathf.CeilToInt((b-a).magnitude/.035f));
            for(int part=0;part<steps;part++) {
                var from=Vector2.Lerp(a,b,part/(float)steps);var to=Vector2.Lerp(a,b,(part+1)/(float)steps);
                var corners=new[]{from-sideways,to-sideways,to+sideways,from+sideways};int start=vertices.Count;
                for(int layer=0;layer<2;layer++)foreach(var corner in corners)
                    vertices.Add(inverse.MultiplyPoint3x4(BadgePoint(surface,corner.x,corner.y,layer==0?inner:outer)));
                int[] faces={0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,1,2,6,1,6,5,2,3,7,2,7,6,3,0,4,3,4,7};
                for(int i=0;i<faces.Length;i+=3){indices.Add(start+faces[i]);indices.Add(start+faces[i+2]);indices.Add(start+faces[i+1]);}
            }
        }
        var mesh=new Mesh{name=name};meshes.Add(mesh);mesh.SetVertices(vertices);mesh.triangles=indices.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
        var obj=new GameObject(name);obj.transform.SetParent(nodes[hatch],false);
        obj.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
        // Same reveal as the authored mechanism. The geometry stays rigidly
        // attached to the hatch for both transformation directions and walking.
        renderers.Add(Tuple.Create<Renderer,bool>(renderer,true));
    }
}
}
