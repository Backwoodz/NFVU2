using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Rachel350Z {
// Owns only the optional robot display. Original car meshes/materials are borrowed
// read-only, including the four tuned wheels and currently selected exhaust.
internal sealed class TransformerVisual:IDisposable {
    public readonly GameObject Root;
    readonly TransformerAsset asset;
    readonly Transform car;
    readonly Transform[] nodes;
    readonly Matrix4x4[] neutral,pose,delta,carInverse;
    readonly Dictionary<string,int> ids;
    readonly Dictionary<int,Vector3> releases;
    readonly int[] gaitParent;
    readonly bool[] gaitTranslate;
    readonly TransformerGait gait=new TransformerGait();
    readonly List<Mesh> meshes=new List<Mesh>();
    readonly List<Material> owned=new List<Material>();
    readonly List<Tuple<Material,int>> neonMaterials=new List<Tuple<Material,int>>();
    readonly List<Tuple<Renderer,bool>> renderers=new List<Tuple<Renderer,bool>>();
    readonly List<Tuple<Transform,int,Matrix4x4,bool>> borrowed=new List<Tuple<Transform,int,Matrix4x4,bool>>();
    public bool ReadyToWalk=>gait.Ready;
    public bool Settled=>gait.Settled;
    public Transform Node(string name)=>nodes[ids[name]];
    static Matrix4x4 Matrix(float[] values){var m=new Matrix4x4();for(int r=0;r<4;r++)for(int c=0;c<4;c++)m[r,c]=values[r*4+c];return m;}
    static Vector3 Position(Matrix4x4 m)=>m.GetColumn(3);
    static Matrix4x4 Frame(float[] v,int k)=>Matrix4x4.TRS(new Vector3(v[k],v[k+1],v[k+2]),new Quaternion(v[k+3],v[k+4],v[k+5],v[k+6]),new Vector3(v[k+7],v[k+8],v[k+9]));
    static void Apply(Transform t,Matrix4x4 m){t.localPosition=Position(m);t.localRotation=m.rotation;t.localScale=m.lossyScale;}
    public TransformerVisual(Transform car,TransformerAsset asset,Material[] original) {
        this.car=car;this.asset=asset;
        Root=new GameObject("350Z_TransformerPrototype");Root.transform.SetParent(car,false);Root.SetActive(false);
        nodes=new Transform[asset.Manifest.nodes.Length];neutral=new Matrix4x4[nodes.Length];pose=new Matrix4x4[nodes.Length];delta=new Matrix4x4[nodes.Length];carInverse=new Matrix4x4[nodes.Length];
        ids=asset.Manifest.nodes.Select((n,i)=>new {n.name,i}).ToDictionary(x=>x.name,x=>x.i);
        releases=asset.Manifest.gaitReleases.ToDictionary(r=>r.node,r=>new Vector3(r.offset[0],r.offset[1],r.offset[2]));
        gaitParent=new int[nodes.Length];gaitTranslate=new bool[nodes.Length];
        for(int i=0;i<nodes.Length;i++) {
            var n=asset.Manifest.nodes[i];gaitParent[i]=n.parent;
            if(n.name.StartsWith("panel.hipguard_")||n.name.StartsWith("panel.sill1_"))gaitParent[i]=ids["thigh."+n.name.Substring(n.name.Length-1)];
            else if(n.name.StartsWith("panel.")||n.name.StartsWith("neon.")) {
                int ancestor=n.parent;
                while(ancestor>=0) {
                    string name=asset.Manifest.nodes[ancestor].name;
                    if(name.StartsWith("thigh."))break;
                    if(name.StartsWith("shin.")||name.StartsWith("foot.")) {
                        gaitParent[i]=ids["foot."+name.Substring(name.Length-1)];gaitTranslate[i]=true;break;
                    }
                    ancestor=asset.Manifest.nodes[ancestor].parent;
                }
            }
        }
        try {
            for(int i=0;i<nodes.Length;i++) {
                nodes[i]=new GameObject(asset.Manifest.nodes[i].name).transform;nodes[i].SetParent(Root.transform,false);
                neutral[i]=Matrix(asset.Manifest.nodes[i].neutral);carInverse[i]=Frame(asset.Frames[0],i*10).inverse;
            }
            var mats=new Material[asset.Manifest.materials.Length];
            for(int i=0;i<mats.Length;i++) {
                var m=asset.Manifest.materials[i];
                if(m.source>=0) {
                    if(m.source>=original.Length)throw new InvalidOperationException("Transformer material table mismatch");
                    mats[i]=original[m.source];
                    if(!string.IsNullOrEmpty(m.sourcePart)) {
                        var live=car.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r=>r.name==m.sourcePart);
                        if(live!=null)mats[i]=live.sharedMaterial;
                    }
                    // The cabin replaces source glass with the current tint material.
                    if(m.name.StartsWith("471a1dca_")) {
                        var glass=car.GetComponentsInChildren<Renderer>(true).Select(r=>r.sharedMaterial).FirstOrDefault(x=>x!=null&&x.name=="350Z_CabinGlass");
                        if(glass!=null)mats[i]=glass;
                    }
                } else {
                    var shader=Shader.Find("Standard");if(shader==null||!shader.isSupported)shader=original[0].shader;
                    var mat=new Material(shader){name="Transformer_"+m.name};owned.Add(mat);mats[i]=mat;
                    if(m.neonGroup>0)neonMaterials.Add(Tuple.Create(mat,m.neonGroup));
                    foreach(var key in new[]{"_Color","_BaseColor"})if(mat.HasColor(key))mat.SetColor(key,new Color(m.color[0],m.color[1],m.color[2],m.color[3]));
                    foreach(var key in new[]{"_MainTex","_BaseMap"})if(mat.HasTexture(key))mat.SetTexture(key,Texture2D.whiteTexture);
                    if(mat.HasFloat("_Metallic"))mat.SetFloat("_Metallic",m.metallic);
                    if(mat.HasFloat("_Glossiness"))mat.SetFloat("_Glossiness",m.smoothness);
                    if(m.emission.Take(3).Any(x=>x>0)) {
                        // The Blender plasma value blooms into a white disc in Valheim.
                        // Keep its green hue, with enough headroom to read the containment rings.
                        float gain=m.name=="V9 - V13 reactor plasma"?.15f:1f;
                        VehicleMaterials.SetEmission(mat,new Color(m.emission[0],m.emission[1],m.emission[2])*gain);
                    }
                }
            }
            foreach(var b in asset.Batches) {
                var mesh=new Mesh{name="Transformer batch"};meshes.Add(mesh);
                int count=b.Weights.Length;var v=new Vector3[count];var n=new Vector3[count];var uv=new Vector2[count];var weights=new BoneWeight[count];
                for(int j=0;j<count;j++){int k=j*8;var a=b.Vertices;v[j]=new Vector3(a[k],a[k+1],a[k+2]);n[j]=new Vector3(a[k+3],a[k+4],a[k+5]);uv[j]=new Vector2(a[k+6],a[k+7]);weights[j]=new BoneWeight{boneIndex0=b.Weights[j],weight0=1};}
                mesh.vertices=v;mesh.normals=n;mesh.uv=uv;mesh.colors=Enumerable.Repeat(Color.white,count).ToArray();mesh.triangles=b.Indices;
                mesh.boneWeights=weights;mesh.bindposes=b.Bones.Select(i=>neutral[i].inverse).ToArray();mesh.RecalculateTangents();mesh.RecalculateBounds();
                var obj=new GameObject("Transformer batch");obj.transform.SetParent(Root.transform,false);
                var renderer=obj.AddComponent<SkinnedMeshRenderer>();renderer.sharedMesh=mesh;renderer.sharedMaterial=mats[b.Material];
                renderer.bones=b.Bones.Select(i=>nodes[i]).ToArray();renderer.rootBone=Root.transform;
                renderer.localBounds=new Bounds(new Vector3(0,2.5f,0),new Vector3(9,8,9));renderer.updateWhenOffscreen=true;renderer.quality=SkinQuality.Bone1;
                renderers.Add(Tuple.Create<Renderer,bool>(renderer,b.Concealed));
            }
            Evaluate(0,0,0,0);
        }catch {Dispose();throw;}
    }
    public void CaptureCurrentCar() {
        foreach(var entry in borrowed)if(entry.Item1!=null)UnityEngine.Object.Destroy(entry.Item1.gameObject);borrowed.Clear();
        var represented=new HashSet<string>(asset.Manifest.representedParts);
        foreach(var renderer in car.GetComponentsInChildren<MeshRenderer>(true)) {
            if(renderer.transform.IsChildOf(Root.transform)||!renderer.enabled||!renderer.gameObject.activeInHierarchy||represented.Contains(renderer.name)||renderer.name=="350Z_TrunkNeonStrip")continue;
            var filter=renderer.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;
            string bone="thorax";bool compact=true;
            for(var t=renderer.transform;t!=null&&t!=car;t=t.parent) {
                if(t.name.StartsWith("OriginalWheel")||t.name.StartsWith("OriginalBrake")) {
                    int wheel=int.Parse(t.name.Substring(t.name.Length-1));bone="panel."+(wheel<2?"front":"rear")+"_wheel_"+(wheel%2==0?"R":"L");compact=false;break;
                }
            }
            if(renderer.name.Contains("EXHAUST")) {bone="panel.exhaust_"+(car.InverseTransformPoint(renderer.bounds.center).x>=0?"L":"R");compact=false;}
            // Preserve imported cabin/trunk/engine equipment while closed; its
            // extra inner volume retracts as the shell opens (authorised trick).
            if(renderer.name.StartsWith("350Z_Neon")||renderer.name.Contains("Turret"))continue;
            var obj=new GameObject("Retained_"+renderer.name);obj.transform.SetParent(Root.transform,false);
            obj.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;obj.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
            var local=car.worldToLocalMatrix*renderer.transform.localToWorldMatrix;
            int index=ids[bone];var start=Frame(asset.Frames[0],index*10);
            var inverse=compact?Matrix4x4.TRS(Position(start),start.rotation,Vector3.one).inverse:carInverse[index];
            borrowed.Add(Tuple.Create(obj.transform,index,inverse*local,compact));
        }
    }
    public void SetNeon(Color underbody,bool underbodyOn,bool underbodyPulse,Color trunk,bool trunkOn,bool trunkPulse,float time) {
        foreach(var item in neonMaterials) {
            bool boot=item.Item2==2,on=boot?trunkOn:underbodyOn,pulse=boot?trunkPulse:underbodyPulse;
            var color=boot?trunk:underbody;
            float gain=on?(pulse?.3f+.7f*(.5f+.5f*Mathf.Sin(time*2.5f)):1):0;
            foreach(var key in new[]{"_Color","_BaseColor"})if(item.Item1.HasColor(key))item.Item1.SetColor(key,on?color:color*.12f);
            VehicleMaterials.SetEmission(item.Item1,color*gain*2);
        }
    }
    public void Evaluate(float progress,float dt,float speed,float turn,bool motionRequested=false) {
        float sample=Mathf.Clamp01(progress)*240;int first=Mathf.FloorToInt(sample),second=Math.Min(240,first+1);float u=sample-first;
        var a=asset.Frames[first];var b=asset.Frames[second];
        for(int i=0;i<nodes.Length;i++) {
            int k=i*10;
            pose[i]=Matrix4x4.TRS(Vector3.Lerp(new Vector3(a[k],a[k+1],a[k+2]),new Vector3(b[k],b[k+1],b[k+2]),u),
                Quaternion.Slerp(new Quaternion(a[k+3],a[k+4],a[k+5],a[k+6]),new Quaternion(b[k+3],b[k+4],b[k+5],b[k+6]),u),
                Vector3.Lerp(new Vector3(a[k+7],a[k+8],a[k+9]),new Vector3(b[k+7],b[k+8],b[k+9]),u));
        }
        if(progress>=1&&asset.Manifest.walkingReady)Walk(dt,speed,turn,motionRequested);else gait.Reset();
        for(int i=0;i<nodes.Length;i++)Apply(nodes[i],pose[i]);
        foreach(var r in renderers)r.Item1.enabled=!r.Item2||progress>=11.5f/120;
        foreach(var entry in borrowed) {
            var anchor=pose[entry.Item2];
            if(entry.Item4)anchor=Matrix4x4.TRS(Position(anchor),anchor.rotation,Vector3.one);
            var m=anchor*entry.Item3;
            float compact=entry.Item4?1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.06f,.28f,progress)):1;
            entry.Item1.gameObject.SetActive(compact>.002f);
            if(compact>.002f)Apply(entry.Item1,m*Matrix4x4.Scale(Vector3.one*compact));
        }
    }
    void Walk(float dt,float speed,float turn,bool requested) {
        gait.Step(dt,speed,turn,requested);
        var overrides=new Dictionary<int,Matrix4x4>();
        foreach(string side in new[]{"L","R"}) {
            int thigh=ids["thigh."+side],shin=ids["shin."+side],foot=ids["foot."+side];
            gait.Foot(side=="R",out float forward,out float lift,out float swing);
            var hip=Position(neutral[thigh]);var knee=Position(neutral[shin]);var ankle=Position(neutral[foot]);
            var targetAnkle=ankle+new Vector3(0,lift,forward);
            float l1=Vector3.Distance(hip,knee),l2=Vector3.Distance(knee,ankle);
            var d=targetAnkle-hip;float distance=Mathf.Clamp(d.magnitude,Mathf.Abs(l1-l2)+.001f,l1+l2-.001f);var direction=d.normalized;
            float along=(l1*l1-l2*l2+distance*distance)/(2*distance);
            var bend=Vector3.ProjectOnPlane(knee-hip,direction).normalized;
            var targetKnee=hip+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));
            overrides[thigh]=Matrix4x4.TRS(hip,Quaternion.FromToRotation(knee-hip,targetKnee-hip)*neutral[thigh].rotation,Vector3.one);
            overrides[shin]=Matrix4x4.TRS(targetKnee,Quaternion.FromToRotation(ankle-knee,targetAnkle-targetKnee)*neutral[shin].rotation,Vector3.one);
            overrides[foot]=Matrix4x4.TRS(targetAnkle,neutral[foot].rotation,Vector3.one);
            int arm=ids["upperarm."+side];var origin=Position(neutral[arm]);
            overrides[arm]=Matrix4x4.TRS(origin,Quaternion.AngleAxis(swing,Vector3.right)*neutral[arm].rotation,Vector3.one);
            int forearm=ids["forearm."+side];var armDelta=overrides[arm]*neutral[arm].inverse;
            var elbow=armDelta*neutral[forearm];
            overrides[forearm]=Matrix4x4.TRS(Position(elbow),armDelta.rotation*Quaternion.AngleAxis(-gait.Elbow(side=="R"),Vector3.right)*neutral[forearm].rotation,Vector3.one);
        }
        for(int i=0;i<nodes.Length;i++) {
            int parent=gaitParent[i];
            pose[i]=overrides.TryGetValue(i,out var m)?m:(parent<0?neutral[i]:
                gaitTranslate[i]?Matrix4x4.Translate(Position(pose[parent])-Position(neutral[parent]))*neutral[i]:delta[parent]*neutral[i]);
            if(releases.TryGetValue(i,out var release)) {
                float deployment=Mathf.SmoothStep(0,1,gait.Release);
                var p=Position(pose[i])+release*deployment;
                if(asset.Manifest.nodes[i].name.StartsWith("panel.sill1_"))p.z+=.04f*Mathf.Max(0,Mathf.Min(1,deployment/.1f,(.8f-deployment)/.1f));
                pose[i].SetColumn(3,new Vector4(p.x,p.y,p.z,1));
            }
            delta[i]=pose[i]*neutral[i].inverse;
        }
        // Two telescoping sleeves remain attached while limbs move. Their
        // endpoints follow the anatomical parent and the moving panel.
        foreach(var node in ids.Where(e=>e.Key.StartsWith("object:V18 connected sleeve"))) {
            string name=node.Key;int separator=name.IndexOf(" - ",StringComparison.Ordinal);string role=name.Substring(separator+3);
            string targetBone=role.StartsWith("neon.")?role:"panel."+role;
            if(!ids.TryGetValue(targetBone,out int end))continue;
            int parent=asset.Manifest.nodes[end].parent;
            // Exported neutral sleeve start is an exact mount point.
            string firstName="object:V18 connected sleeve 0 - "+role;
            if(!ids.TryGetValue(firstName,out int start))continue;
            var p=delta[parent].MultiplyPoint3x4(Position(neutral[start]));var q=Position(pose[end]);var direction=q-p;
            bool outer=name.Contains("sleeve 0 - ");
            var rotation=Quaternion.FromToRotation(Vector3.up,direction.normalized);
            // Blender cylinder Z becomes Unity Y. Local geometry was exported
            // with its neutral basis; retain that basis when rebuilding TRS.
            pose[node.Value]=Matrix4x4.TRS(p+direction*(outer?0:.4f),rotation,new Vector3(1,direction.magnitude*.6f,1));
        }
    }
    public void Dispose(){if(Root!=null){Root.SetActive(false);UnityEngine.Object.Destroy(Root);}foreach(var m in meshes)UnityEngine.Object.Destroy(m);foreach(var m in owned)UnityEngine.Object.Destroy(m);}
}
}
