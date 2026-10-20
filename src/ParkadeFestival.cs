using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rachel350Z {
// Local render-only festival dressing. No saved objects, creature AI, physics,
// audio, particles or shadow lights. At most one rig/decor operation per frame.
internal sealed class ParkadeFestival {
    readonly string directory;
    readonly Action<string> log;
    readonly List<GameObject> scenery=new List<GameObject>(),crowd=new List<GameObject>();
    readonly List<Material> materials=new List<Material>();
    readonly List<Texture2D> textures=new List<Texture2D>();
    readonly List<Mesh> meshes=new List<Mesh>();
    readonly List<Light> lights=new List<Light>();
    readonly List<int> lightIndices=new List<int>();
    readonly List<Cheer> trolls=new List<Cheer>();
    readonly Dictionary<string,Material> surfaces=new Dictionary<string,Material>();
    readonly Dictionary<string,Material> logos=new Dictionary<string,Material>();
    readonly int[] terraceStage=new int[ParkadeFestivalPlan.Terraces.Count];
    readonly bool[] boardBuilt=new bool[ParkadeFestivalPlan.Boards.Count];
    readonly bool[] lampBuilt=new bool[ParkadeFestivalPlan.Lamps.Count];
    readonly bool[] trollBuilt=new bool[ParkadeFestivalPlan.Trolls.Count];
    static readonly string[] Brands={"hks","apexi","injen","falken","bbs","nfsu2"};
    static readonly Color[] Palette={new Color(.10f,.8f,1),new Color(1,.22f,.10f),new Color(.75f,.24f,1),new Color(.12f,1,.5f)};
    GameObject root;
    object world;
    ZNetScene scene;
    Material template;
    float nextVisibility;
    float floorTop=float.NaN;
    bool failed;
    internal ParkadeFestival(string directory,Action<string> log){this.directory=directory;this.log=log;}
    internal void Tick(Vector3 viewer,object currentWorld) {
        if(currentWorld==null||ZNetScene.instance==null){Clear();return;}
        if(!ReferenceEquals(world,currentWorld)||scene!=ZNetScene.instance){Clear();world=currentWorld;scene=ZNetScene.instance;}
        if(failed)return;
        try {
            var b=ParkadeLayout.Bounds;
            if(root==null) {
                if(viewer.x<b.MinX-190||viewer.x>b.MaxX+190||viewer.z<b.MinZ-190||viewer.z>b.MaxZ+190)return;
                root=new GameObject("NFVU2 Parkade Night Festival"){hideFlags=HideFlags.DontSave};
                var templates=VehicleMaterials.Create(null,m=>{});materials.AddRange(templates);template=templates[0];
            }
            BuildOne(viewer);
            foreach(var troll in trolls)if(troll.Holder.activeInHierarchy)troll.Pose(Time.time);
            if(Time.unscaledTime<nextVisibility)return;
            nextVisibility=Time.unscaledTime+.25f;
            Cull(scenery,viewer,ParkadeFestivalPlan.SceneryDistance);
            Cull(crowd,viewer,ParkadeFestivalPlan.ActorDistance);
            var nearest=ParkadeFestivalPlan.NearestLights(viewer.x,viewer.z);
            for(int i=0;i<lights.Count;i++)lights[i].enabled=Array.IndexOf(nearest,lightIndices[i])>=0&&lights[i].gameObject.activeInHierarchy;
        } catch(Exception error) {
            Clear();world=currentWorld;scene=ZNetScene.instance;failed=true;
            log?.Invoke("Parkade festival unavailable: "+error.GetBaseException().Message);
        }
    }
    static bool Near(Vector3 viewer,float x,float z,float distance) {
        float dx=viewer.x-x,dz=viewer.z-z;return dx*dx+dz*dz<distance*distance;
    }
    static void Cull(List<GameObject> list,Vector3 viewer,float distance) {
        foreach(var go in list){var p=go.transform.position;bool visible=Near(viewer,p.x,p.z,distance);if(go.activeSelf!=visible)go.SetActive(visible);}
    }
    void BuildOne(Vector3 viewer) {
        // Signs and light pools appear first, then the crowds build gradually.
        for(int i=0;i<boardBuilt.Length;i++) {
            var p=ParkadeFestivalPlan.Boards[i];
            if(boardBuilt[i]||!Near(viewer,p.X,p.Z,155))continue;
            CreateBoard(p,i);boardBuilt[i]=true;return;
        }
        for(int i=0;i<lampBuilt.Length;i++) {
            var p=ParkadeFestivalPlan.Lamps[i];
            if(lampBuilt[i]||!Near(viewer,p.X,p.Z,125))continue;
            CreateLamp(p,i);lampBuilt[i]=true;return;
        }
        for(int i=0;i<trollBuilt.Length;i++) {
            var p=ParkadeFestivalPlan.Trolls[i];
            if(trollBuilt[i]||!Near(viewer,p.X,p.Z,150))continue;
            var prefab=scene.GetPrefab("Troll");if(prefab==null)return;
            CreateTrollStage(p,i);
            var holder=Holder("Cheering Troll fan "+i,new Vector3(p.X,p.Y,p.Z),p.Yaw);
            var clone=Rig(prefab,holder.transform);var animator=clone.GetComponent<Animator>();
            var idle=animator.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name=="Idle_Breathing");
            if(idle==null)throw new InvalidOperationException("Native Troll idle clip unavailable");
            holder.SetActive(true);animator.enabled=false;idle.SampleAnimation(clone,0);
            var bones=clone.GetComponentsInChildren<Transform>(true);
            var face=bones.First(t=>t.name=="Nose").position-bones.First(t=>t.name=="Head").position;
            face.y=0;
            if(face.sqrMagnitude<.001f)throw new InvalidOperationException("Native Troll face direction unavailable");
            // Troll artwork faces backwards in its imported visual axes.
            // Calibrate the nose toward the course instead of guessing a yaw.
            clone.transform.rotation=Quaternion.AngleAxis(Vector3.SignedAngle(face,holder.transform.forward,Vector3.up),Vector3.up)*clone.transform.rotation;
            AlignFeet(clone,p.Y,"LeftFoot","RightFoot");
            var cheer=new Cheer(holder,clone,i);cheer.Pose(Time.time);trolls.Add(cheer);crowd.Add(holder);trollBuilt[i]=true;return;
        }
        for(int i=0;i<terraceStage.Length;i++) {
            var p=ParkadeFestivalPlan.Terraces[i];
            if(terraceStage[i]>=4||!Near(viewer,p.X,p.Z,110))continue;
            // Measure the real native floor, including its nested transform
            // scale. Keep feet and rails on its surface, not its origin.
            if(float.IsNaN(floorTop)) {
                var floor=scene.GetPrefab("stone_floor");if(floor==null)return;
                floorTop=float.NegativeInfinity;
                foreach(var box in floor.GetComponentsInChildren<BoxCollider>(true))
                    foreach(float x in new[]{-1f,1f})foreach(float z in new[]{-1f,1f}) {
                        var corner=box.center+Vector3.Scale(box.size*.5f,new Vector3(x,1,z));
                        floorTop=Math.Max(floorTop,floor.transform.InverseTransformPoint(box.transform.TransformPoint(corner)).y);
                    }
                // The rendered stone cap can extend above the collision hull.
                // Rails and spectators must clear that visible surface too.
                foreach(var filter in floor.GetComponentsInChildren<MeshFilter>(true)) {
                    if(filter.sharedMesh==null)continue;var bounds=filter.sharedMesh.bounds;
                    foreach(float x in new[]{-1f,1f})foreach(float cornerY in new[]{-1f,1f})foreach(float z in new[]{-1f,1f}) {
                        var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,cornerY,z));
                        floorTop=Math.Max(floorTop,floor.transform.InverseTransformPoint(filter.transform.TransformPoint(corner)).y);
                    }
                }
                if(float.IsInfinity(floorTop))throw new InvalidOperationException("Native viewing roof surface unavailable");
            }
            float y=ParkadeLayout.TerrainHeightAt(p.X,p.Z)+p.HeightOffset+floorTop;
            if(terraceStage[i]==0)CreateTerrace(p,y,i);
            else {
                var prefab=scene.GetPrefab("Greydwarf");if(prefab==null)return;
                var q=ParkadeLayout.Project(p.X,p.Z);float yaw=ParkadeFestivalPlan.LookAt(p.X,p.Z,q.X,q.Z);
                var rotation=Quaternion.Euler(0,yaw,0);
                int slot=terraceStage[i]-1;
                var offset=rotation*new Vector3((slot-1)*.93f,0,slot==1?-.35f:.25f);
                var holder=Holder("Terrace Greydwarf "+i+" / "+slot,new Vector3(p.X,y,p.Z)+offset,yaw);
                var clone=Rig(prefab,holder.transform);var animator=clone.GetComponent<Animator>();
                holder.SetActive(true);animator.Rebind();
                foreach(var parameter in animator.parameters) {
                    if(parameter.type==AnimatorControllerParameterType.Float)animator.SetFloat(parameter.nameHash,0);
                    if(parameter.type==AnimatorControllerParameterType.Bool)animator.SetBool(parameter.nameHash,false);
                }
                int idle=Animator.StringToHash("Base Layer.Movement");animator.Play(idle,0,0);animator.Update(0);
                AlignFeet(clone,y,"l_foot","r_foot","l_foot_end","r_foot_end");
                bool dance=(i+slot)%2==0;
                foreach(var parameter in animator.parameters)
                    if(parameter.type==AnimatorControllerParameterType.Bool&&parameter.name=="george_vibing")animator.SetBool(parameter.nameHash,dance);
                int state=Animator.StringToHash(dance?"Base Layer.George Vibing":"Base Layer.Movement");
                if(!animator.HasState(0,state))state=idle;
                animator.speed=.84f+((i+slot)%5)*.065f;animator.Play(state,0,((i*3+slot)*.381966f)%1);animator.Update(0);
                animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;crowd.Add(holder);
            }
            terraceStage[i]++;return;
        }
    }
    static void AlignFeet(GameObject clone,float y,params string[] names) {
        float feet=float.PositiveInfinity;
        foreach(var bone in clone.GetComponentsInChildren<Transform>(true))if(Array.IndexOf(names,bone.name)>=0)feet=Math.Min(feet,bone.position.y);
        if(float.IsInfinity(feet))throw new InvalidOperationException("Native spectator feet unavailable");
        clone.transform.position+=Vector3.up*(y-feet+.02f);
    }
    static GameObject Rig(GameObject prefab,Transform parent) {
        var native=prefab.GetComponentInChildren<Animator>(true);
        if(native==null||native.runtimeAnimatorController==null)throw new InvalidOperationException("Native spectator animator unavailable");
        var clone=UnityEngine.Object.Instantiate(native.gameObject,parent,false);
        clone.transform.localPosition=Vector3.zero;clone.transform.localRotation=native.transform.localRotation;
        foreach(var component in clone.GetComponentsInChildren<Component>(true)) {
            if(component==null||component is Transform||component is Renderer||component is MeshFilter||component is Animator||component is LODGroup)continue;
            if(component is Behaviour behaviour)behaviour.enabled=false;
            if(component is Collider collider)collider.enabled=false;
            if(component is ParticleSystem particles)particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            UnityEngine.Object.DestroyImmediate(component);
        }
        foreach(var renderer in clone.GetComponentsInChildren<ParticleSystemRenderer>(true))UnityEngine.Object.DestroyImmediate(renderer);
        foreach(var renderer in clone.GetComponentsInChildren<Renderer>(true)){renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;}
        var animator=clone.GetComponent<Animator>();animator.applyRootMotion=false;animator.fireEvents=false;animator.logWarnings=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        return clone;
    }
    GameObject Holder(string name,Vector3 position,float yaw) {
        var go=new GameObject(name){hideFlags=HideFlags.DontSave};go.SetActive(false);go.transform.SetParent(root.transform,false);
        go.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));return go;
    }
    Material Surface(string key,Color color,float emission=0) {
        if(surfaces.TryGetValue(key,out var existing))return existing;
        var material=new Material(template){name="Parkade festival "+key,hideFlags=HideFlags.DontSave};
        foreach(string p in new[]{"_Color","_BaseColor","_SkinColor"})if(material.HasColor(p))material.SetColor(p,color);
        foreach(string p in new[]{"_Glossiness","_Smoothness"})if(material.HasFloat(p))material.SetFloat(p,.12f);
        if(material.HasFloat("_Metallic"))material.SetFloat("_Metallic",0);
        VehicleMaterials.SetEmission(material,color*emission);materials.Add(material);surfaces.Add(key,material);return material;
    }
    void Box(Transform parent,string name,Vector3 pos,Vector3 scale,Material material) {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.hideFlags=HideFlags.DontSave;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=scale;
        var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
    }
    void CombineDecoration(GameObject holder) {
        // Collapse the rail/frame primitives into one draw per shared surface.
        // Original Unity primitive meshes are borrowed; only combined meshes
        // belong to this decoration and are destroyed on world exit.
        var groups=new Dictionary<Material,List<CombineInstance>>();
        var filters=holder.GetComponentsInChildren<MeshFilter>(true);
        foreach(var filter in filters) {
            var renderer=filter.GetComponent<MeshRenderer>();
            if(renderer==null||filter.sharedMesh==null)continue;
            var material=renderer.sharedMaterial;
            if(!groups.TryGetValue(material,out var group)){group=new List<CombineInstance>();groups.Add(material,group);}
            group.Add(new CombineInstance{mesh=filter.sharedMesh,transform=holder.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix});
        }
        foreach(var group in groups) {
            var mesh=new Mesh{name=holder.name+" / "+group.Key.name};mesh.CombineMeshes(group.Value.ToArray(),true,true);meshes.Add(mesh);
            var go=new GameObject("Combined festival detail");go.transform.SetParent(holder.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=group.Key;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        foreach(var filter in filters)UnityEngine.Object.DestroyImmediate(filter.gameObject);
    }
    void CreateTerrace(DriftTrackPiece p,float y,int index) {
        var go=Holder("Illuminated viewing terrace "+index,new Vector3(p.X,y,p.Z),p.Yaw);
        var iron=Surface("dark iron",new Color(.035f,.045f,.05f));
        var neon=Surface("terrace rim "+index%4,Palette[index%4],2.4f);
        foreach(float side in new[]{-1f,1f}) {
            Box(go.transform,"Terrace corner post",new Vector3(side*1.75f,.48f,-1.75f),new Vector3(.09f,1,.09f),iron);
            Box(go.transform,"Terrace corner post",new Vector3(side*1.75f,.48f,1.75f),new Vector3(.09f,1,.09f),iron);
            Box(go.transform,"Iron safety rail",new Vector3(0,.96f,side*1.75f),new Vector3(3.6f,.09f,.09f),iron);
            Box(go.transform,"Neon roof rim",new Vector3(0,.045f,side*1.88f),new Vector3(3.8f,.055f,.055f),neon);
            Box(go.transform,"Iron side rail",new Vector3(side*1.75f,.96f,0),new Vector3(.09f,.09f,3.6f),iron);
            Box(go.transform,"Neon roof rim",new Vector3(side*1.88f,.045f,0),new Vector3(.055f,.055f,3.8f),neon);
        }
        CombineDecoration(go);go.SetActive(true);scenery.Add(go);
    }
    void CreateTrollStage(ParkadeFestivalSpot p,int index) {
        // The road is raised above the flat surroundings. A timber fan stage
        // keeps the giants visible over the walls instead of down in a ditch.
        var go=Holder("Troll fan timber stage "+index,new Vector3(p.X,p.Y,p.Z),p.Yaw);
        var wood=Surface("billboard timber",new Color(.19f,.10f,.055f));
        var iron=Surface("dark iron",new Color(.035f,.045f,.05f));
        var rim=Surface("troll stage rim",Palette[2],2.2f);
        Box(go.transform,"Supported timber deck",new Vector3(0,-.22f,0),new Vector3(7,.4f,6),wood);
        foreach(float x in new[]{-2.8f,2.8f})foreach(float z in new[]{-2.3f,2.3f}) {
            var position=go.transform.TransformPoint(new Vector3(x,0,z));
            float height=Math.Max(.45f,p.Y-ParkadeLayout.TerrainHeightAt(position.x,position.z));
            Box(go.transform,"Timber support",new Vector3(x,-height*.5f-.1f,z),new Vector3(.38f,height,.38f),wood);
        }
        foreach(float side in new[]{-1f,1f}) {
            Box(go.transform,"Stage iron band",new Vector3(0,-.35f,side*3),new Vector3(7.1f,.12f,.07f),iron);
            Box(go.transform,"Purple stage edge",new Vector3(0,-.08f,side*3.01f),new Vector3(7,.06f,.05f),rim);
        }
        CombineDecoration(go);go.SetActive(true);scenery.Add(go);
    }
    void CreateLamp(ParkadeFestivalSpot p,int index) {
        var go=Holder("Parkade night light "+index,new Vector3(p.X,p.Y,p.Z),p.Yaw);
        float ground=ParkadeLayout.TerrainHeightAt(p.X,p.Z),height=p.Y-ground;
        var iron=Surface("dark iron",new Color(.035f,.045f,.05f));
        var amber=Surface("warm lantern",new Color(1,.52f,.16f),3);
        var color=index%3==0?Palette[0]:index%3==1?Palette[2]:new Color(1,.69f,.37f);
        Box(go.transform,"Iron lamp mast",new Vector3(0,-height*.5f,0),new Vector3(.15f,height,.15f),iron);
        Box(go.transform,"Lamp cap",new Vector3(0,.32f,0),new Vector3(.65f,.12f,.65f),iron);
        Box(go.transform,"Warm lantern glass",Vector3.zero,new Vector3(.27f,.55f,.27f),amber);
        Box(go.transform,"Coloured mast band",new Vector3(0,-.45f,0),new Vector3(.24f,.12f,.24f),Surface("mast band "+index%3,color,2));
        var light=go.AddComponent<Light>();light.type=LightType.Point;light.color=Color.Lerp(color,new Color(1,.75f,.5f),.55f);
        light.intensity=2.6f;light.range=23;light.shadows=LightShadows.None;light.enabled=false;lights.Add(light);lightIndices.Add(index);
        CombineDecoration(go);go.SetActive(true);scenery.Add(go);
    }
    void CreateBoard(ParkadeFestivalSpot p,int index) {
        var go=Holder("Parkade sponsor billboard "+Brands[index%Brands.Length],new Vector3(p.X,p.Y,p.Z),p.Yaw);
        var dark=Surface("billboard backing",new Color(.009f,.013f,.021f));
        var wood=Surface("billboard timber",new Color(.19f,.10f,.055f));
        var trim=Surface("billboard neon "+index%4,Palette[index%4],3);
        Box(go.transform,"Iron billboard",Vector3.zero,new Vector3(6,2.7f,.22f),dark);
        foreach(float side in new[]{-1f,1f}) {
            Box(go.transform,"Timber upright",new Vector3(side*2.7f,-1.45f,0),new Vector3(.20f,4.1f,.24f),wood);
            Box(go.transform,"Neon border",new Vector3(0,side*1.25f,-.14f),new Vector3(5.85f,.065f,.04f),trim);
            Box(go.transform,"Neon side",new Vector3(side*2.9f,0,-.14f),new Vector3(.065f,2.5f,.04f),trim);
        }
        // Boards face inward with their local -Z face, like the existing marquee.
        // Reverse the holder so that the texture front points toward the road.
        go.transform.rotation*=Quaternion.Euler(0,180,0);
        string brand=Brands[index%Brands.Length];
        var material=Logo(brand);
        if(material!=null) {
            var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Original local sponsor mark "+brand;
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());quad.transform.SetParent(go.transform,false);
            quad.transform.localPosition=new Vector3(0,0,-.14f);quad.transform.localScale=new Vector3(5.25f,2.1f,1);
            var renderer=quad.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        } else FallbackLabel(go.transform,brand.ToUpperInvariant());
        CombineDecoration(go);go.SetActive(true);scenery.Add(go);
    }
    Material Logo(string brand) {
        if(logos.TryGetValue(brand,out var cached))return cached;
        string path=Path.Combine(directory,"venue",brand+".png");Material material=null;
        if(File.Exists(path)&&new FileInfo(path).Length<=1024*1024) {
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false){name="Local venue "+brand,wrapMode=TextureWrapMode.Clamp};textures.Add(texture);
            if(!ImageConversion.LoadImage(texture,File.ReadAllBytes(path),true)||texture.width>1024||texture.height>512)throw new InvalidDataException("Invalid venue sponsor image");
            material=VehicleMaterials.CreateGlowMaterial(texture);material.name="Illuminated original "+brand;materials.Add(material);
            foreach(string p in new[]{"_Color","_TintColor","_BaseColor"})if(material.HasColor(p))material.SetColor(p,new Color(1,1,1,.7f));
        }
        logos.Add(brand,material);return material;
    }
    void FallbackLabel(Transform parent,string value) {
        var font=Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f=>f!=null&&f.name.IndexOf("Norse",StringComparison.OrdinalIgnoreCase)>=0);
        if(font==null)return;
        // Initialize TMP under the live root; the target board is deliberately
        // inactive while being assembled. Remove TMP before the next frame.
        var source=new GameObject("Temporary billboard glyphs");source.transform.SetParent(root.transform,false);
        try {
            var label=source.AddComponent<TextMeshPro>();label.font=font;label.fontSharedMaterial=font.material;label.text=value;label.fontSize=20;
            label.alignment=TextAlignmentOptions.Center;label.textWrappingMode=TextWrappingModes.NoWrap;label.ForceMeshUpdate(true,true);
            var mesh=UnityEngine.Object.Instantiate(label.mesh);meshes.Add(mesh);var bounds=mesh.bounds;
            var go=new GameObject("Native sponsor lettering");go.transform.SetParent(parent,false);go.transform.localPosition=new Vector3(0,0,-.15f);
            float scale=Math.Min(4.8f/Math.Max(.01f,bounds.size.x),1.6f/Math.Max(.01f,bounds.size.y));
            go.transform.localPosition-=bounds.center*scale;
            go.transform.localScale=Vector3.one*scale;go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=font.material;renderer.shadowCastingMode=ShadowCastingMode.Off;
        }finally{UnityEngine.Object.DestroyImmediate(source);}
    }
    sealed class Cheer {
        internal readonly GameObject Holder;
        readonly Transform[] bones;
        readonly Vector3[] positions;
        readonly Quaternion[] rotations;
        readonly Transform left,right,leftElbow,rightElbow,leftHand,rightHand,head,spine;
        readonly float phase,leftSide,rightSide;
        internal Cheer(GameObject holder,GameObject rig,int index) {
            Holder=holder;phase=index*2.7f;bones=rig.GetComponentsInChildren<Transform>(true);
            positions=bones.Select(t=>t.localPosition).ToArray();rotations=bones.Select(t=>t.localRotation).ToArray();
            left=Find("LeftArm");right=Find("RightArm");leftElbow=Find("LeftForeArm");rightElbow=Find("RightForeArm");
            leftHand=Find("LeftHand");rightHand=Find("RightHand");head=Find("Head");spine=Find("Spine1");
            // Imported bone names need not agree with the holder's X axis.
            // Keep each hand on its shoulder's actual side of the chest.
            leftSide=Vector3.Dot(left.position-head.position,holder.transform.right)<0?-1:1;
            rightSide=Vector3.Dot(right.position-head.position,holder.transform.right)<0?-1:1;
        }
        Transform Find(string name)=>bones.First(t=>t.name==name);
        internal void Pose(float time) {
            for(int i=0;i<bones.Length;i++){bones[i].localPosition=positions[i];bones[i].localRotation=rotations[i];}
            float wave=(float)Math.Sin(time*2.4f+phase);
            spine.rotation=Quaternion.AngleAxis(wave*3,Holder.transform.forward)*spine.rotation;
            Arm(left,leftElbow,leftHand,leftSide,wave);Arm(right,rightElbow,rightHand,rightSide,-wave);
        }
        void Arm(Transform upper,Transform lower,Transform hand,float side,float wave) {
            var tr=Holder.transform;
            float a=Vector3.Distance(upper.position,lower.position),b=Vector3.Distance(lower.position,hand.position);
            float reach=a+b;
            var target=upper.position+tr.up*(reach*.84f+wave*.18f)+tr.right*(side*reach*.33f)+tr.forward*(reach*.08f);
            var delta=target-upper.position;var direction=delta.normalized;
            float distance=Math.Max(Math.Abs(a-b)+.01f,Math.Min(a+b-.01f,delta.magnitude));
            var bend=(tr.forward*.7f+tr.right*side*.75f);bend=(bend-direction*Vector3.Dot(direction,bend)).normalized;
            float along=(a*a-b*b+distance*distance)/(2*distance);
            var elbow=upper.position+direction*along+bend*(float)Math.Sqrt(Math.Max(0,a*a-along*along));
            var end=upper.position+direction*distance;
            upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,elbow-upper.position)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,end-lower.position)*lower.rotation;
        }
    }
    internal void Clear() {
        if(root!=null){root.SetActive(false);UnityEngine.Object.Destroy(root);}
        foreach(var mesh in meshes)if(mesh!=null)UnityEngine.Object.Destroy(mesh);
        foreach(var material in materials)if(material!=null)UnityEngine.Object.Destroy(material);
        foreach(var texture in textures)if(texture!=null)UnityEngine.Object.Destroy(texture);
        scenery.Clear();crowd.Clear();materials.Clear();textures.Clear();meshes.Clear();lights.Clear();lightIndices.Clear();trolls.Clear();surfaces.Clear();logos.Clear();
        Array.Clear(terraceStage,0,terraceStage.Length);Array.Clear(boardBuilt,0,boardBuilt.Length);Array.Clear(lampBuilt,0,lampBuilt.Length);Array.Clear(trollBuilt,0,trollBuilt.Length);
        root=null;world=null;scene=null;template=null;failed=false;nextVisibility=0;floorTop=float.NaN;
    }
}
}
