using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rachel350Z {
// Locally authored temporary overlays, never terrain deformation or paint.
// The world root deliberately remains independent of the moving vehicle.
internal sealed class TransformerFootstepEffects:IDisposable {
    const int Columns=5,Rows=9,ParticleLimit=96;
    readonly TransformerFootstepEffectModel model=new TransformerFootstepEffectModel();
    readonly RaycastHit[] hits=new RaycastHit[48];
    readonly Mesh[] meshes=new Mesh[TransformerFootstepEffectModel.Capacity];
    readonly MeshRenderer[] stamps=new MeshRenderer[TransformerFootstepEffectModel.Capacity];
    readonly float[] shownAlpha=new float[TransformerFootstepEffectModel.Capacity];
    readonly Vector3[] stampCenters=new Vector3[TransformerFootstepEffectModel.Capacity],stampDirections=new Vector3[TransformerFootstepEffectModel.Capacity];
    readonly Vector2[] stampSizes=new Vector2[TransformerFootstepEffectModel.Capacity];
    readonly Vector3[] vertices=new Vector3[Columns*Rows];
    readonly System.Random random=new System.Random(35029);
    readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
    Transform vehicle;
    GameObject root;
    Texture2D footprintTexture,dustTexture;
    Material footprintMaterial,dustMaterial;
    ParticleSystem dust;
    bool paused,disposed;
    public bool Owns(Transform candidate)=>root!=null&&candidate!=null&&candidate.IsChildOf(root.transform);
    public static TransformerFootstepEffects Create(GameObject vehicle,Action<string> log) {
        var effect=new TransformerFootstepEffects();
        try{effect.Initialize(vehicle);return effect;}
        catch(Exception error){effect.Dispose();log?.Invoke("Transformer ground effects unavailable: "+error.Message);return null;}
    }
    void Initialize(GameObject car) {
        vehicle=car.transform;
        root=new GameObject("NFVU2_TransformerGroundEffects");
        footprintTexture=Mask(true);dustTexture=Mask(false);
        footprintMaterial=AlphaMaterial(footprintTexture,"NFVU2 temporary footprint",2990);
        dustMaterial=AlphaMaterial(dustTexture,"NFVU2 foot contact dust",3000);
        var uv=new Vector2[Columns*Rows];var normals=new Vector3[Columns*Rows];
        for(int z=0;z<Rows;z++)for(int x=0;x<Columns;x++) {
            int i=z*Columns+x;uv[i]=new Vector2((float)x/(Columns-1),(float)z/(Rows-1));normals[i]=Vector3.up;
        }
        var triangles=new int[(Columns-1)*(Rows-1)*6];int at=0;
        for(int z=0;z<Rows-1;z++)for(int x=0;x<Columns-1;x++) {
            int i=z*Columns+x;
            triangles[at++]=i;triangles[at++]=i+Columns;triangles[at++]=i+1;
            triangles[at++]=i+1;triangles[at++]=i+Columns;triangles[at++]=i+Columns+1;
        }
        for(int i=0;i<stamps.Length;i++) {
            var obj=new GameObject("TemporarySolePrint_"+i);obj.transform.SetParent(root.transform,false);
            var mesh=new Mesh{name="NFVU2 terrain-conforming sole"};meshes[i]=mesh;
            mesh.MarkDynamic();mesh.vertices=vertices;mesh.uv=uv;mesh.normals=normals;mesh.triangles=triangles;
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=obj.AddComponent<MeshRenderer>();stamps[i]=renderer;renderer.sharedMaterial=footprintMaterial;Configure(renderer);
            obj.SetActive(false);
        }
        var cloud=new GameObject("ContactDust");cloud.transform.SetParent(root.transform,false);
        dust=cloud.AddComponent<ParticleSystem>();dust.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=dust.main;main.playOnAwake=false;main.loop=true;main.maxParticles=ParticleLimit;
        main.simulationSpace=ParticleSystemSimulationSpace.World;main.scalingMode=ParticleSystemScalingMode.Local;
        main.startLifetime=.65f;main.startSpeed=0;main.startSize=.25f;main.startColor=Color.white;main.gravityModifier=-.015f;
        var emission=dust.emission;emission.enabled=false;
        var shape=dust.shape;shape.enabled=false;
        var size=dust.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.5f,1,2.4f));
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
            new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.08f),new GradientAlphaKey(.5f,.4f),new GradientAlphaKey(0,1)});
        var color=dust.colorOverLifetime;color.enabled=true;color.color=gradient;
        var particleRenderer=dust.GetComponent<ParticleSystemRenderer>();particleRenderer.sharedMaterial=dustMaterial;
        particleRenderer.renderMode=ParticleSystemRenderMode.Billboard;Configure(particleRenderer);
        dust.Play(false);
    }
    static void Configure(Renderer renderer) {
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
    }
    static Material AlphaMaterial(Texture2D texture,string name,int queue) {
        // A luminous additive fallback would turn soil into glowing footprints.
        Shader shader=null;
        foreach(string candidate in new[]{"Legacy Shaders/Particles/Alpha Blended","Sprites/Default"}) {
            var found=Shader.Find(candidate);if(found!=null&&found.isSupported){shader=found;break;}
        }
        if(shader==null)throw new InvalidOperationException("No supported non-additive alpha shader");
        var material=new Material(shader){name=name,renderQueue=queue};
        material.SetTexture("_MainTex",texture);
        foreach(string key in new[]{"_Color","_TintColor","_BaseColor"})if(material.HasColor(key))material.SetColor(key,Color.white);
        if(material.HasFloat("_ZWrite"))material.SetFloat("_ZWrite",0);
        if(material.HasFloat("_Cull"))material.SetFloat("_Cull",0);
        return material;
    }
    static Texture2D Mask(bool sole) {
        const int side=64;
        var texture=new Texture2D(side,side,TextureFormat.RGBA32,false){name=sole?"NFVU2 mechanical sole mask":"NFVU2 contact dust mask",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
        var pixels=new Color[side*side];
        for(int y=0;y<side;y++)for(int x=0;x<side;x++) {
            float u=(x+.5f)/side,v=(y+.5f)/side,alpha;
            if(sole) {
                // Rectangular toe/heel plates with a central hinge gap and
                // short transverse tread bars. Soft edges avoid a painted tile.
                float width=v<.32f?.39f:.47f;
                float edge=Mathf.Clamp01((width-Mathf.Abs(u-.5f))*18)*Mathf.Clamp01(Mathf.Min(v,1-v)*22);
                float hinge=Mathf.Abs(v-.36f)<.025f?.12f:1;
                float tread=Mathf.Sin(v*58)>.45f?.48f:1;
                alpha=edge*hinge*tread*.45f;
                pixels[y*side+x]=new Color(.24f,.18f,.115f,alpha);
            } else {
                float dx=(u-.5f)*2,dy=(v-.5f)*2,edge=Mathf.Clamp01(1-dx*dx-dy*dy);
                alpha=edge*edge*(.78f+.22f*Mathf.Sin(x*.71f+Mathf.Sin(y*.63f)));
                pixels[y*side+x]=new Color(.48f,.37f,.24f,alpha);
            }
        }
        texture.SetPixels(pixels);texture.Apply(false,true);return texture;
    }
    public void Observe(bool walking,bool blocked,uint left,uint right,Vector3 leftSole,Vector3 rightSole,
        Vector3 leftForward,Vector3 rightForward,Vector2 leftSize,Vector2 rightSize,float runBlend) {
        if(disposed||root==null)return;
        int contacts=model.Observe(walking,blocked,left,right);
        if((contacts&1)!=0)Contact(leftSole,leftForward,leftSize,runBlend);
        if((contacts&2)!=0)Contact(rightSole,rightForward,rightSize,runBlend);
    }
    bool Ground(Vector3 sole,out RaycastHit ground,out Heightmap map) {
        ground=new RaycastHit();map=null;
        if(!Finite(sole))return false;
        // Probe only around the actual sole. A missing platform must not find
        // unrelated terrain several metres below a suspended robot.
        int count=Physics.RaycastNonAlloc(sole+Vector3.up*.35f,Vector3.down,hits,.95f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        if(count==0||count==hits.Length)return false;
        float nearest=float.PositiveInfinity;
        for(int i=0;i<count;i++) {
            var candidate=hits[i];if(candidate.collider==null||candidate.collider.transform.IsChildOf(vehicle))continue;
            if(candidate.distance<nearest){ground=candidate;nearest=candidate.distance;}
        }
        if(float.IsPositiveInfinity(nearest)||!Finite(ground.point)||!Finite(ground.normal)||ground.normal.y<.6f)return false;
        map=ground.collider.GetComponent<Heightmap>();
        if(map==null)return false; // Never project through a rock, roof or floor.
        if(ZoneSystem.instance!=null&&ground.point.y<=ZoneSystem.instance.m_waterLevel+.05f)return false;
        if(Floating.GetLiquidLevel(ground.point,0)>=ground.point.y-.01f)return false;
        var surface=map.GetGroundMaterial(ground.normal,ground.point);
        return surface==FootStep.GroundMaterial.Grass||surface==FootStep.GroundMaterial.Mud||surface==FootStep.GroundMaterial.GenericGround;
    }
    void Contact(Vector3 sole,Vector3 direction,Vector2 size,float runBlend) {
        if(!Finite(direction)||!Finite(size.x)||!Finite(size.y)||size.x<.1f||size.y<.1f||size.x>3||size.y>4)return;
        if(!Ground(sole,out var ground,out _))return;
        var forward=Vector3.ProjectOnPlane(direction,Vector3.up).normalized;
        if(forward.sqrMagnitude<.1f)return;
        var right=Vector3.Cross(Vector3.up,forward);
        // A small grid conforms the transparent overlay to the terrain without
        // altering a heightmap, terrain material mask or world save. Grass is
        // temporarily hidden only by the separate renderer override helper.
        for(int z=0;z<Rows;z++)for(int x=0;x<Columns;x++) {
            var point=ground.point+right*(((float)x/(Columns-1)-.5f)*size.x)+forward*(((float)z/(Rows-1)-.5f)*size.y);
            // GetWorldHeight rounds to a terrain grid vertex. Raycast the
            // rendered terrain triangles instead, so the overlay cannot float
            // on a grid stair or cross a nearby non-terrain obstacle.
            if(!Ground(point,out var surface,out _)||Mathf.Abs(surface.point.y-ground.point.y)>.7f)return;
            point.y=surface.point.y+.014f;vertices[z*Columns+x]=point;
        }
        int slot=model.Allocate();var stamp=stamps[slot];meshes[slot].vertices=vertices;meshes[slot].RecalculateBounds();
        TransformerFootstepGrass.Remove(this,slot);
        stampCenters[slot]=ground.point;stampDirections[slot]=forward;stampSizes[slot]=size;
        stamp.gameObject.SetActive(true);ApplyAlpha(slot,1);
        int particles=Finite(runBlend)&&runBlend>.5f?10:7;
        for(int i=0;i<particles;i++) {
            float angle=(float)random.NextDouble()*Mathf.PI*2;
            var outward=right*Mathf.Cos(angle)+forward*Mathf.Sin(angle);
            var emit=new ParticleSystem.EmitParams {
                position=ground.point+right*((float)(random.NextDouble()-.5)*size.x*.8f)+forward*((float)(random.NextDouble()-.5)*size.y*.8f)+ground.normal*.035f,
                velocity=outward*(.35f+(float)random.NextDouble()*.65f)+ground.normal*(.15f+(float)random.NextDouble()*.35f),
                startLifetime=.45f+(float)random.NextDouble()*.35f,startSize=.18f+(float)random.NextDouble()*.16f,
                startColor=new Color(1,1,1,.28f),rotation=(float)random.NextDouble()*360
            };
            dust.Emit(emit,1);
        }
    }
    void ApplyAlpha(int slot,float alpha) {
        shownAlpha[slot]=alpha;properties.Clear();var color=new Color(1,1,1,alpha*.65f);
        properties.SetColor("_Color",color);properties.SetColor("_TintColor",color);properties.SetColor("_BaseColor",color);
        stamps[slot].SetPropertyBlock(properties);
        TransformerFootstepGrass.Set(this,slot,stampCenters[slot],stampDirections[slot],stampSizes[slot],alpha);
    }
    public void Tick(float dt,bool blocked) {
        if(disposed||root==null)return;
        bool frozen=blocked||!Finite(dt)||dt<=0;
        model.Tick(dt,frozen);
        if(frozen&&!paused){dust.Pause(false);paused=true;}
        else if(!frozen&&paused){dust.Play(false);paused=false;}
        for(int i=0;i<stamps.Length;i++) {
            float alpha=model.Alpha(i);
            if(alpha<=0){if(stamps[i].gameObject.activeSelf){stamps[i].gameObject.SetActive(false);TransformerFootstepGrass.Remove(this,i);}shownAlpha[i]=0;}
            else if(Mathf.Abs(alpha-shownAlpha[i])>.002f)ApplyAlpha(i,alpha);
        }
    }
    public void Dispose() {
        if(disposed)return;disposed=true;
        TransformerFootstepGrass.RemoveOwner(this);
        if(root!=null){root.SetActive(false);UnityEngine.Object.Destroy(root);}root=null;dust=null;
        foreach(var mesh in meshes)if(mesh!=null)UnityEngine.Object.Destroy(mesh);
        if(footprintMaterial!=null)UnityEngine.Object.Destroy(footprintMaterial);
        if(dustMaterial!=null)UnityEngine.Object.Destroy(dustMaterial);
        if(footprintTexture!=null)UnityEngine.Object.Destroy(footprintTexture);
        if(dustTexture!=null)UnityEngine.Object.Destroy(dustTexture);
        footprintMaterial=dustMaterial=null;footprintTexture=dustTexture=null;
    }
    static bool Finite(Vector3 value)=>Finite(value.x)&&Finite(value.y)&&Finite(value.z);
    static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
}
}
