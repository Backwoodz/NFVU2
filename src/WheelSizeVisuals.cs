using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rachel350Z {
// Meshes are owned by Plugin.vehicleMeshes; retain immutable source coordinates
// so repeated slider changes and resets never accumulate geometry errors.
public sealed class WheelSizeVisuals {
    sealed class Entry {
        public Mesh Mesh;
        public Vector3[] Original,Normals,Vertices,UpdatedNormals;
    }
    readonly List<Entry> entries=new List<Entry>();
    readonly float bead,outside;
    readonly float nominal,min,max;
    float applied;
    public WheelSizeVisuals(ModelData model,float nominal=20,float min=16,float max=22) {
        this.nominal=nominal;this.min=min;this.max=max;applied=nominal;
        var tyre=model.parts.FirstOrDefault(p=>p.group=="wheel"&&model.materials[p.material].name.StartsWith("78743ca1_",StringComparison.Ordinal));
        if(tyre==null)throw new InvalidOperationException("Wheel sizing requires the original rubber surface.");
        var radii=tyre.indices.Select(i=>tyre.vertices[i]).Select(v=>Mathf.Sqrt(v.y*v.y+v.z*v.z)).ToArray();
        bead=radii.Min();outside=radii.Max();
        if(bead<=0||outside<=bead*1.1f)throw new InvalidOperationException("Unsupported wheel/tyre proportions.");
    }
    public void Track(Mesh mesh) {
        var vertices=mesh.vertices;
        entries.Add(new Entry{Mesh=mesh,Original=vertices,Normals=mesh.normals,
            Vertices=new Vector3[vertices.Length],UpdatedNormals=new Vector3[vertices.Length]});
    }
    public void Apply(float inches) {
        inches=WheelFitMath.Sanitize(inches,min,max,nominal);
        if(inches==applied)return;
        float scale=inches/nominal;
        foreach(var entry in entries) {
            for(int i=0;i<entry.Original.Length;i++) {
                var v=entry.Original[i];var n=entry.Normals[i];
                float radius=Mathf.Sqrt(v.y*v.y+v.z*v.z);
                if(radius<.000001f||inches==nominal){entry.Vertices[i]=v;entry.UpdatedNormals[i]=n;continue;}
                WheelFitMath.RadialShape(radius,bead,outside,scale,out var mapped,out var slope);
                float ratio=mapped/radius,uy=v.y/radius,uz=v.z/radius;
                entry.Vertices[i]=new Vector3(v.x,v.y*ratio,v.z*ratio);
                // Inverse-transpose of the radial deformation preserves the
                // original split normals instead of smoothing the rim edges.
                float radial=n.y*uy+n.z*uz;
                entry.UpdatedNormals[i]=new Vector3(n.x,
                    radial*uy/slope+(n.y-radial*uy)/ratio,
                    radial*uz/slope+(n.z-radial*uz)/ratio).normalized;
            }
            entry.Mesh.vertices=entry.Vertices;entry.Mesh.normals=entry.UpdatedNormals;
            entry.Mesh.RecalculateTangents();entry.Mesh.RecalculateBounds();
        }
        applied=inches;
    }
}
}
