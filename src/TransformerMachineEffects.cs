using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Rachel350Z {
internal sealed partial class TransformerVisual {
    static readonly Vector3 ReactorCenter=new Vector3(0,2.98f,.56f);
    static readonly string[] MachineWheels={"panel.front_wheel_L","panel.front_wheel_R","panel.rear_wheel_L","panel.rear_wheel_R"};
    readonly List<Tuple<Transform,bool>> reactorParts=new List<Tuple<Transform,bool>>();
    readonly Dictionary<Transform,float> rollingWheels=new Dictionary<Transform,float>();
    readonly Dictionary<int,float> wheelAngles=new Dictionary<int,float>();
    ParticleSystem reactorFlame;
    Texture2D reactorTexture;
    float reactorAngle;
    bool flameRunning;
    // Only authored front containment hardware, never car panels, glass or hoses
    // outside the circular assembly. All points remain bound to the same thorax.
    bool ReactorTriangle(TransformerBatch b,int triangle,Vector3[] vertices) {
        if(asset.Manifest.materials[b.Material].source>=0)return false;
        for(int c=0;c<3;c++) {
            int v=b.Indices[triangle+c];var p=vertices[v]-ReactorCenter;
            if(asset.Manifest.nodes[b.Bones[b.Weights[v]]].name!="thorax"||p.x*p.x+p.y*p.y>.36f*.36f||p.z<.015f||p.z>.18f)return false;
        }
        return true;
    }
    void ReactorMesh(Vector3[] vertices,Vector3[] normals,Vector2[] uv,int[] triangles,Material material,bool rotating) {
        var map=new Dictionary<int,int>();var v=new List<Vector3>();var n=new List<Vector3>();var tex=new List<Vector2>();
        var indices=new int[triangles.Length];
        for(int i=0;i<triangles.Length;i++) {
            int original=triangles[i];
            if(!map.TryGetValue(original,out int index)) {
                index=v.Count;map[original]=index;v.Add(vertices[original]-ReactorCenter);
                if(normals!=null)n.Add(normals[original]);tex.Add(uv!=null?uv[original]:Vector2.zero);
            }
            indices[i]=index;
        }
        var mesh=new Mesh{name=rotating?"Reactor containment rotor":"Recessed reactor bowl"};meshes.Add(mesh);
        mesh.SetVertices(v);mesh.SetUVs(0,tex);mesh.triangles=indices;
        if(normals!=null)mesh.SetNormals(n);else mesh.RecalculateNormals();mesh.RecalculateBounds();
        var obj=new GameObject(mesh.name);obj.transform.SetParent(Root.transform,false);
        obj.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderers.Add(Tuple.Create<Renderer,bool>(renderer,true));reactorParts.Add(Tuple.Create(obj.transform,rotating));
    }
    void CreateReactor(Material[] materials) {
        int plasma=Array.FindIndex(asset.Manifest.materials,m=>m.name=="V9 - V13 reactor plasma");
        int metal=Array.FindIndex(asset.Manifest.materials,m=>m.name=="V9 - V13 reactor gunmetal");
        if(plasma<0||metal<0)return;
        // The casing ends at z=.585; keep the cavity floor in front of it.
        // Rim z=.710, floor z=.600: genuine inward depth, no outward sphere.
        const int segments=48,rows=7;var vertices=new List<Vector3>();var triangles=new List<int>();
        for(int r=0;r<=rows;r++)for(int j=0;j<=segments;j++) {
            float radius=.142f*r/rows,a=2*Mathf.PI*j/segments;
            vertices.Add(new Vector3(radius*Mathf.Cos(a),2.98f+radius*Mathf.Sin(a),.600f+.110f*(r*r)/(rows*rows)));
        }
        for(int r=0;r<rows;r++)for(int j=0;j<segments;j++) {
            int a=r*(segments+1)+j,b=a+segments+1;
            triangles.Add(a);triangles.Add(b);triangles.Add(b+1);
            triangles.Add(a);triangles.Add(b+1);triangles.Add(a+1);
        }
        // Dark metallic cavity makes its depth readable; a small dim central
        // glow and a restrained flame replace the previous full glowing dome.
        ReactorMesh(vertices.ToArray(),null,null,triangles.ToArray(),materials[metal],false);
        var coreVertices=new List<Vector3>{new Vector3(0,2.98f,.603f)};var coreTriangles=new List<int>();
        for(int j=0;j<=segments;j++) {
            float a=2*Mathf.PI*j/segments;coreVertices.Add(new Vector3(.052f*Mathf.Cos(a),2.98f+.052f*Mathf.Sin(a),.617f));
            if(j>0){coreTriangles.Add(0);coreTriangles.Add(j);coreTriangles.Add(j+1);}
        }
        ReactorMesh(coreVertices.ToArray(),null,null,coreTriangles.ToArray(),materials[plasma],false);
        // Reuse the car's original FIREB flame atlas and supported glow shader.
        // Looking straight into a forward-only jet looked like a tiny green dot.
        var exhaust=car.GetComponentsInChildren<ParticleSystemRenderer>(true)
            .FirstOrDefault(r=>r.name=="GreenExhaustFlame"&&r.sharedMaterial!=null);
        Material mat;
        if(exhaust!=null)mat=new Material(exhaust.sharedMaterial);
        else {
            reactorTexture=new Texture2D(32,32,TextureFormat.RGBA32,false){name="Reactor flame falloff",wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[32*32];
            for(int y=0;y<32;y++)for(int x=0;x<32;x++) {
                float d=new Vector2((x-15.5f)/15.5f,(y-15.5f)/15.5f).magnitude;
                float alpha=Mathf.Pow(Mathf.Clamp01(1-d),2);pixels[y*32+x]=new Color(alpha,alpha,alpha,alpha);
            }
            reactorTexture.SetPixels(pixels);reactorTexture.Apply();
            mat=VehicleMaterials.CreateGlowMaterial(reactorTexture);
        }
        mat.name="Reactor central plasma flame";owned.Add(mat);
        foreach(var key in new[]{"_TintColor","_Color","_BaseColor"})if(mat.HasColor(key))mat.SetColor(key,new Color(.12f,.8f,.24f,.7f));
        var obj=new GameObject("Reactor flame");obj.transform.SetParent(Root.transform,false);
        reactorParts.Add(Tuple.Create(obj.transform,false));
        reactorFlame=obj.AddComponent<ParticleSystem>();reactorFlame.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=reactorFlame.main;main.playOnAwake=false;main.loop=true;main.maxParticles=64;
        main.simulationSpace=ParticleSystemSimulationSpace.Local;main.scalingMode=ParticleSystemScalingMode.Local;
        main.startLifetime=new ParticleSystem.MinMaxCurve(.30f,.48f);
        main.startSize=new ParticleSystem.MinMaxCurve(.11f,.20f);main.startSpeed=new ParticleSystem.MinMaxCurve(.65f,1f);
        main.startColor=Color.white;main.gravityModifier=0;
        var emission=reactorFlame.emission;emission.enabled=true;emission.rateOverTime=90;
        var shape=reactorFlame.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Cone;
        // Emit just in front of the .710 m containment lip, rather than behind
        // it. Camera-facing flame sprites remain legible from the front too.
        shape.angle=9;shape.radius=.032f;shape.position=new Vector3(0,0,.17f);shape.rotation=new Vector3(-60,0,0);
        var size=reactorFlame.sizeOverLifetime;size.enabled=true;
        size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.45f),new Keyframe(.2f,1),new Keyframe(1,.03f)));
        var gradient=new Gradient();
        gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
            new[]{new GradientAlphaKey(.75f,0),new GradientAlphaKey(.5f,.6f),new GradientAlphaKey(0,1)});
        var fade=reactorFlame.colorOverLifetime;fade.enabled=true;fade.color=gradient;
        if(exhaust!=null) {
            var sheet=reactorFlame.textureSheetAnimation;sheet.enabled=true;sheet.numTilesX=2;sheet.numTilesY=2;
            sheet.animation=ParticleSystemAnimationType.WholeSheet;
            sheet.frameOverTime=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,0,1,1));
            sheet.startFrame=new ParticleSystem.MinMaxCurve(0,1);sheet.cycleCount=1;
        }
        var renderer=obj.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=mat;
        renderer.renderMode=ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
    }
    void UpdateMachineEffects(float progress,float dt,float turn) {
        bool robot=progress>=1;
        foreach(string name in MachineWheels) {
            if(!ids.TryGetValue(name,out int i))continue;
            if(!wheelAngles.ContainsKey(i))wheelAngles[i]=0;
            if(robot&&dt>0)wheelAngles[i]=Mathf.Repeat(wheelAngles[i]-turn*TransformerGait.TurnDegrees*dt*Position(neutral[i]).x/.35f,360);
            // Visual roll fades away with the closing panels before car mode.
            else if(progress<=0)wheelAngles[i]=0;
        }
        if(robot&&dt>0)reactorAngle=Mathf.Repeat(reactorAngle+dt*120,360);
        int thorax=ids["thorax"];
        foreach(var part in reactorParts) {
            var rotation=part.Item2?Quaternion.AngleAxis(-reactorAngle,Vector3.forward):Quaternion.identity;
            Apply(part.Item1,pose[thorax]*neutral[thorax].inverse*Matrix4x4.TRS(ReactorCenter,rotation,Vector3.one));
        }
        if(reactorFlame==null)return;
        if(robot) {
            if(!flameRunning){reactorFlame.Play();flameRunning=true;}
            if(dt<=0)reactorFlame.Pause();else if(reactorFlame.isPaused)reactorFlame.Play();
        } else if(flameRunning){reactorFlame.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);flameRunning=false;}
    }
}
}
