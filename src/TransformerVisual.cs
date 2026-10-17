using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Rachel350Z {
// Owns only the optional robot display. Original car meshes/materials are borrowed
// read-only, including the four tuned wheels and currently selected exhaust.
internal sealed partial class TransformerVisual:IDisposable {
    public readonly GameObject Root;
    readonly TransformerAsset asset;
    readonly Transform car;
    readonly Transform[] nodes;
    readonly Matrix4x4[] neutral,pose,delta,carInverse;
    readonly Dictionary<string,int> ids;
    readonly Dictionary<int,Vector3> releases;
    readonly int[] gaitParent;
    readonly bool[] gaitTranslate;
    readonly Dictionary<int,Vector3> terrainHinges=new Dictionary<int,Vector3>();
    readonly Vector3[][] solePoints=new Vector3[2][];
    readonly float[] soleHeights=new float[2];
    TransformerGait gait=new TransformerGait();
    readonly List<Mesh> meshes=new List<Mesh>();
    readonly List<Material> owned=new List<Material>();
    readonly List<Tuple<Material,int>> neonMaterials=new List<Tuple<Material,int>>();
    readonly List<Tuple<Renderer,bool>> renderers=new List<Tuple<Renderer,bool>>();
    readonly List<Tuple<Transform,int,Matrix4x4,bool>> borrowed=new List<Tuple<Transform,int,Matrix4x4,bool>>();
    public bool ReadyToWalk=>gait.Ready;
    public bool Settled=>gait.Settled;
    public bool HoldReady {get;set;}
    public bool FeetSettled=>gait.FeetSettled;
    public uint LeftFootfalls=>gait.LeftFootfalls;
    public uint RightFootfalls=>gait.RightFootfalls;
    public float RunBlend=>gait.RunBlend;
    public Transform Node(string name)=>nodes[ids[name]];
    public Matrix4x4 Pose(int node)=>pose[node];
    public void ExhaustOutlet(Vector3 sourcePoint,Vector3 sourceDirection,out Vector3 point,out Vector3 direction) {
        int bone=ids["panel.exhaust_"+(sourcePoint.x>=0?"L":"R")];
        var transform=Root.transform.localToWorldMatrix*pose[bone]*carInverse[bone];
        point=transform.MultiplyPoint3x4(sourcePoint);direction=transform.MultiplyVector(sourceDirection).normalized;
    }
    public Func<Vector3,Vector4> GroundPlane;
    public Matrix4x4[] CapturePreparationPose(float release,Func<Vector3,Vector4> ground) {
        var originalGait=gait;var originalGround=GroundPlane;bool originalHold=HoldReady;
        var originalPose=(Matrix4x4[])pose.Clone();var originalDelta=(Matrix4x4[])delta.Clone();
        try {
            gait=TransformerGait.Preparation(release);GroundPlane=ground;HoldReady=false;
            Array.Copy(neutral,pose,pose.Length);Walk(0,0,0,false);
            return (Matrix4x4[])pose.Clone();
        } finally {
            gait=originalGait;GroundPlane=originalGround;HoldReady=originalHold;
            Array.Copy(originalPose,pose,pose.Length);Array.Copy(originalDelta,delta,delta.Length);
        }
    }
    static Matrix4x4 Matrix(float[] values){var m=new Matrix4x4();for(int r=0;r<4;r++)for(int c=0;c<4;c++)m[r,c]=values[r*4+c];return m;}
    static Vector3 Position(Matrix4x4 m)=>m.GetColumn(3);
    static Matrix4x4 Frame(float[] v,int k)=>Matrix4x4.TRS(new Vector3(v[k],v[k+1],v[k+2]),new Quaternion(v[k+3],v[k+4],v[k+5],v[k+6]),new Vector3(v[k+7],v[k+8],v[k+9]));
    static void Apply(Transform t,Matrix4x4 m){t.localPosition=Position(m);t.localRotation=m.rotation;t.localScale=m.lossyScale;}
    readonly Material[] sourceMaterials;
    bool initialized;
    public TransformerVisual(Transform car,TransformerAsset asset,Material[] original,bool staged=false) {
        this.car=car;this.asset=asset;sourceMaterials=original;
        Root=new GameObject("350Z_TransformerPrototype");Root.transform.SetParent(car,false);Root.SetActive(false);
        nodes=new Transform[asset.Manifest.nodes.Length];neutral=new Matrix4x4[nodes.Length];pose=new Matrix4x4[nodes.Length];delta=new Matrix4x4[nodes.Length];carInverse=new Matrix4x4[nodes.Length];
        ids=asset.Manifest.nodes.Select((n,i)=>new {n.name,i}).ToDictionary(x=>x.name,x=>x.i);
        releases=asset.Manifest.gaitReleases.ToDictionary(r=>r.node,r=>new Vector3(r.offset[0],r.offset[1],r.offset[2]));
        gaitParent=new int[nodes.Length];gaitTranslate=new bool[nodes.Length];
        for(int i=0;i<nodes.Length;i++) {
            var n=asset.Manifest.nodes[i];gaitParent[i]=n.parent;
            if(n.name.StartsWith("panel.door_rear_scale"))gaitParent[i]=ids["clavicle."+n.name.Substring(n.name.Length-1)];
            else if(n.name=="panel.pelvis_rear"||n.name=="neon.trunk rear"||n.name.StartsWith("panel.exhaust_"))gaitParent[i]=ids["thorax"];
            else if(n.name.StartsWith("panel.belt_")&&n.name.Contains("hipfold1"))gaitParent[i]=ids["thorax"];
            else if(n.name.StartsWith("panel.floor0_")&&(n.name.Contains("_fold1_")||n.name.Contains("_fold2_"))){gaitParent[i]=ids["foot."+(n.name.Contains("_L_")?"L":"R")];}
            else if(n.name.StartsWith("panel.hipguard_")||n.name.StartsWith("panel.sill1_"))gaitParent[i]=ids["thigh."+n.name.Substring(n.name.Length-1)];
            else if((n.name.StartsWith("panel.")||n.name.StartsWith("neon."))&&!n.name.StartsWith("panel.toe_")) {
                int ancestor=n.parent;
                while(ancestor>=0) {
                    string name=asset.Manifest.nodes[ancestor].name;
                    if(name.StartsWith("thigh."))break;
                    if(name.StartsWith("shin.")||name.StartsWith("foot.")) {
                        gaitParent[i]=ids["foot."+name.Substring(name.Length-1)];gaitTranslate[i]=n.name.StartsWith("neon.underbody side");break;
                    }
                    ancestor=asset.Manifest.nodes[ancestor].parent;
                }
            }
        }
        if(!staged)try {foreach(var step in LoadSteps()){} }catch {Dispose();throw;}
    }
    public IEnumerable<int> LoadSteps() {
        if(initialized)yield break;
        var original=sourceMaterials;
            for(int i=0;i<nodes.Length;i++) {
                nodes[i]=new GameObject(asset.Manifest.nodes[i].name).transform;nodes[i].SetParent(Root.transform,false);
                neutral[i]=Matrix(asset.Manifest.nodes[i].neutral);carInverse[i]=Frame(asset.Frames[0],i*10).inverse;
                if((i&31)==31)yield return StagedWork.NextFrame;
            }
            CacheSoles();yield return StagedWork.NextFrame;
            CacheTerrainHinges();yield return StagedWork.NextFrame;
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
                var indices=new List<int>(b.Indices.Length);
                var rotating=new List<int>();
                for(int j=0;j<b.Indices.Length;j+=3) {
                    bool hidden=ReplacedRearNeonTriangle(b,j,v);
                    for(int corner=0;corner<3;corner++) {
                        string name=asset.Manifest.nodes[b.Bones[b.Weights[b.Indices[j+corner]]]].name;
                        if(name=="neon.trunk rear"||name.EndsWith(" - neon.trunk rear",StringComparison.Ordinal)){hidden=true;break;}
                    }
                    // Replace the outward plasma sphere with a recessed bowl.
                    if(asset.Manifest.materials[b.Material].name=="V9 - V13 reactor plasma")hidden=true;
                    if(!hidden&&ReactorTriangle(b,j,v)) {
                        rotating.Add(b.Indices[j]);rotating.Add(b.Indices[j+1]);rotating.Add(b.Indices[j+2]);hidden=true;
                    }
                    if(!hidden){indices.Add(b.Indices[j]);indices.Add(b.Indices[j+1]);indices.Add(b.Indices[j+2]);}
                }
                if(rotating.Count>0)ReactorMesh(v,n,uv,rotating.ToArray(),mats[b.Material],true);
                mesh.vertices=v;mesh.normals=n;mesh.uv=uv;mesh.colors=Enumerable.Repeat(Color.white,count).ToArray();mesh.triangles=indices.ToArray();
                mesh.boneWeights=weights;mesh.bindposes=b.Bones.Select(i=>neutral[i].inverse).ToArray();mesh.RecalculateTangents();mesh.RecalculateBounds();
                var obj=new GameObject("Transformer batch");obj.transform.SetParent(Root.transform,false);
                var renderer=obj.AddComponent<SkinnedMeshRenderer>();renderer.sharedMesh=mesh;renderer.sharedMaterial=mats[b.Material];
                renderer.bones=b.Bones.Select(i=>nodes[i]).ToArray();renderer.rootBone=Root.transform;
                renderer.localBounds=new Bounds(new Vector3(0,2.5f,0),new Vector3(9,8,9));renderer.updateWhenOffscreen=true;renderer.quality=SkinQuality.Bone1;
                renderers.Add(Tuple.Create<Renderer,bool>(renderer,b.Concealed));
                yield return StagedWork.NextFrame;
            }
            CreateReactor(mats);CreateRearBadge(mats);yield return StagedWork.NextFrame;
            Evaluate(0,0,0,0);initialized=true;
    }
    public void CaptureCurrentCar() {
        foreach(var entry in borrowed)if(entry.Item1!=null)UnityEngine.Object.Destroy(entry.Item1.gameObject);borrowed.Clear();rollingWheels.Clear();
        var represented=new HashSet<string>(asset.Manifest.representedParts);
        foreach(var renderer in car.GetComponentsInChildren<MeshRenderer>(true)) {
            if(renderer.transform.IsChildOf(Root.transform)||!renderer.enabled||!renderer.gameObject.activeInHierarchy||represented.Contains(renderer.name)||renderer.name=="350Z_TrunkNeonStrip")continue;
            var filter=renderer.GetComponent<MeshFilter>();if(filter==null||filter.sharedMesh==null)continue;
            string bone="thorax";bool compact=true,roll=false;
            for(var t=renderer.transform;t!=null&&t!=car;t=t.parent) {
                if(t.name.StartsWith("OriginalWheel")||t.name.StartsWith("OriginalBrake")) {
                    int wheel=int.Parse(t.name.Substring(t.name.Length-1));bone="panel."+(wheel<2?"front":"rear")+"_wheel_"+(wheel%2==0?"R":"L");compact=false;roll=t.name.StartsWith("OriginalWheel");break;
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
            if(roll)rollingWheels[obj.transform]=Vector3.Dot(local.MultiplyVector(Vector3.right),Vector3.right)<0?-1:1;
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
        ApplyMountPose(progress);
        UpdateMachineEffects(progress,dt,turn);
        for(int i=0;i<nodes.Length;i++)Apply(nodes[i],pose[i]);
        foreach(var r in renderers)r.Item1.enabled=!r.Item2||progress>=11.5f/120;
        foreach(var entry in borrowed) {
            var anchor=pose[entry.Item2];
            if(entry.Item4)anchor=Matrix4x4.TRS(Position(anchor),anchor.rotation,Vector3.one);
            var m=anchor*entry.Item3;
            if(rollingWheels.TryGetValue(entry.Item1,out float direction)) {
                // Source wheels are centered on their local X axle. The
                // authored panel holder has a different orientation: spinning
                // that holder made the wheel tumble instead of the tyre roll.
                float roll=Mathf.DeltaAngle(0,wheelAngles[entry.Item2])*direction*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1,progress));
                m=m*Matrix4x4.Rotate(Quaternion.AngleAxis(roll,Vector3.right));
            }
            float compact=entry.Item4?1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.06f,.28f,progress)):1;
            entry.Item1.gameObject.SetActive(compact>.002f);
            if(compact>.002f)Apply(entry.Item1,m*Matrix4x4.Scale(Vector3.one*compact));
        }
    }
    // The foot and its lower armor form one rigid boot on the ankle carrier.
    // Use their complete actual geometry to keep heel/toe roll above ground.
    void CacheSoles() {
        for(int side=0;side<2;side++) {
            int foot=ids["foot."+(side==0?"L":"R")];var points=new HashSet<Vector3>();
            foreach(var batch in asset.Batches)for(int j=0;j<batch.Weights.Length;j++) {
                int bone=batch.Bones[batch.Weights[j]],ancestor=bone;
                while(ancestor>=0&&ancestor!=foot&&!gaitTranslate[ancestor])ancestor=gaitParent[ancestor];
                if(ancestor!=foot)continue;
                int k=j*8;points.Add(neutral[foot].inverse.MultiplyPoint3x4(new Vector3(batch.Vertices[k],batch.Vertices[k+1],batch.Vertices[k+2])));
            }
            solePoints[side]=points.ToArray();
            if(solePoints[side].Length==0)throw new InvalidOperationException("Transformer foot has no rigid sole geometry");
            soleHeights[side]=solePoints[side].Min(p=>neutral[foot].MultiplyPoint3x4(p).y);
        }
    }
    static bool TerrainHinge(string name)=>(name.StartsWith("panel.floor1_")&&name.Contains("_fold0_"))||(name.StartsWith("panel.floor0_")&&name.Contains("_fold1_"));
    void CacheTerrainHinges() {
        var rows=new Dictionary<string,List<int>>();var minimum=new Dictionary<string,float>();
        for(int i=0;i<nodes.Length;i++)if(TerrainHinge(asset.Manifest.nodes[i].name)) {
            string row=asset.Manifest.nodes[i].name.Substring(0,asset.Manifest.nodes[i].name.Length-2);
            if(!rows.TryGetValue(row,out var members)){members=new List<int>();rows[row]=members;minimum[row]=float.PositiveInfinity;}
            members.Add(i);
        }
        foreach(var batch in asset.Batches)for(int v=0;v<batch.Weights.Length;v++) {
            int bone=batch.Bones[batch.Weights[v]];string name=asset.Manifest.nodes[bone].name;
            if(!TerrainHinge(name))continue;string row=name.Substring(0,name.Length-2);
            minimum[row]=Mathf.Min(minimum[row],batch.Vertices[v*8+1]);
        }
        foreach(var row in rows)foreach(int bone in row.Value)
            terrainHinges[bone]=new Vector3(0,minimum[row.Key],Position(neutral[row.Value[0]]).z);
    }
    Vector4 GroundAt(Vector3 point) {
        var plane=GroundPlane!=null?GroundPlane(point):new Vector4(0,1,0,0);
        var normal=new Vector3(plane.x,plane.y,plane.z);
        if(float.IsNaN(plane.w)||float.IsInfinity(plane.w))plane.w=0;
        if(float.IsNaN(normal.sqrMagnitude)||float.IsInfinity(normal.sqrMagnitude)||normal.sqrMagnitude<.001f||normal.y<=.5f)normal=Vector3.up;
        normal.Normalize();return new Vector4(normal.x,normal.y,normal.z,plane.w);
    }
    static Matrix4x4 Around(Vector3 pivot,Quaternion rotation)=>Matrix4x4.Translate(pivot)*Matrix4x4.Rotate(rotation)*Matrix4x4.Translate(-pivot);
    void Walk(float dt,float speed,float turn,bool requested) {
        gait.Step(dt,speed,turn,requested||HoldReady);
        var overrides=new Dictionary<int,Matrix4x4>();
        var targets=new Vector3[2];var footRotations=new Quaternion[2];var groundAlign=new Quaternion[2];var armFlex=new float[2];
        var supportHeights=new float[2];float terrainExposure=0;
        int pelvis=ids["pelvis"],thorax=ids["thorax"];
        var pelvisYaw=Around(Position(neutral[pelvis]),Quaternion.AngleAxis(gait.PelvisYaw,Vector3.up));
        float pelvisHeight=gait.PelvisHeight;
        for(int side=0;side<2;side++) {
            bool right=side==1;int thigh=ids["thigh."+(right?"R":"L")],shin=ids["shin."+(right?"R":"L")],foot=ids["foot."+(right?"R":"L")];
            gait.Foot(right,out float forward,out float lift,out _);
            var ankle=Position(neutral[foot])+new Vector3(gait.FootSide(right),0,forward);
            var ground=GroundAt(new Vector3(ankle.x,soleHeights[side],ankle.z));
            var normal=new Vector3(ground.x,ground.y,ground.z);var planePoint=new Vector3(ankle.x,ground.w,ankle.z);
            groundAlign[side]=Quaternion.FromToRotation(Vector3.up,normal);
            var rest=Position(neutral[foot]);
            supportHeights[side]=ground.w-(normal.x*(rest.x-ankle.x)+normal.z*(rest.z-ankle.z))/normal.y;
            terrainExposure=Mathf.Max(terrainExposure,Quaternion.Angle(Quaternion.identity,groundAlign[side])/15);
            var flatRotation=groundAlign[side]*Quaternion.AngleAxis(gait.FootYaw(right),Vector3.up)*neutral[foot].rotation;
            footRotations[side]=groundAlign[side]*Quaternion.AngleAxis(gait.FootYaw(right),Vector3.up)*Quaternion.AngleAxis(-gait.FootPitch(right),Vector3.right)*neutral[foot].rotation;
            var rotated=Matrix4x4.TRS(ankle,footRotations[side],Vector3.one);
            var pivot=solePoints[side][0];float lowest=Vector3.Dot(normal,rotated.MultiplyPoint3x4(pivot)-planePoint);
            foreach(var point in solePoints[side]) {float distance=Vector3.Dot(normal,rotated.MultiplyPoint3x4(point)-planePoint);if(distance<lowest){lowest=distance;pivot=point;}}
            var contactOffset=flatRotation*pivot-footRotations[side]*pivot;
            var tangentShift=new Vector3(contactOffset.x,0,contactOffset.z);ankle+=tangentShift;
            // Preserve the material heel/toe contact and original sole gap on
            // the actual ground plane; gait lift remains a vertical distance.
            ankle.y+=(soleHeights[side]+lift*normal.y-lowest-Vector3.Dot(normal,tangentShift))/normal.y;
            targets[side]=ankle;
            var hip=pelvisYaw.MultiplyPoint3x4(Position(neutral[thigh]))+new Vector3(gait.PelvisSide,0,0);
            float reach=Vector3.Distance(Position(neutral[thigh]),Position(neutral[shin]))+Vector3.Distance(Position(neutral[shin]),Position(neutral[foot]))-.001f;
            float horizontal=(ankle.x-hip.x)*(ankle.x-hip.x)+(ankle.z-hip.z)*(ankle.z-hip.z);
            // The complete fixed-length mechanism follows a long step. Lower
            // its pelvis before solving either leg; never stretch a shin.
            pelvisHeight=Mathf.Min(pelvisHeight,ankle.y+Mathf.Sqrt(Mathf.Max(.001f,reach*reach-horizontal))-hip.y);
        }
        terrainExposure=Mathf.Clamp01(Mathf.Max(terrainExposure,Mathf.Abs(supportHeights[0]-supportHeights[1])/.2f));
        var pelvisDelta=Matrix4x4.Translate(new Vector3(gait.PelvisSide,pelvisHeight,0))*pelvisYaw;
        overrides[pelvis]=pelvisDelta*neutral[pelvis];
        var torsoRotation=Quaternion.AngleAxis(gait.TorsoYaw,Vector3.up)*Quaternion.AngleAxis(gait.TorsoLean,Vector3.right)*Quaternion.AngleAxis(-gait.TorsoRoll,Vector3.forward);
        var torsoDelta=pelvisDelta*Around(Position(neutral[thorax]),torsoRotation);
        overrides[thorax]=torsoDelta*neutral[thorax];
        for(int side=0;side<2;side++) {
            bool right=side==1;string suffix=right?"R":"L";
            int thigh=ids["thigh."+suffix],shin=ids["shin."+suffix],foot=ids["foot."+suffix];
            gait.Foot(right,out _,out _,out float swing);
            var restHip=Position(neutral[thigh]);var knee=Position(neutral[shin]);var ankle=Position(neutral[foot]);
            var hip=pelvisDelta.MultiplyPoint3x4(restHip);var targetAnkle=targets[side];
            float l1=Vector3.Distance(restHip,knee),l2=Vector3.Distance(knee,ankle);
            var d=targetAnkle-hip;float distance=Mathf.Clamp(d.magnitude,Mathf.Abs(l1-l2)+.001f,l1+l2-.001f);var direction=d.normalized;
            float along=(l1*l1-l2*l2+distance*distance)/(2*distance);
            // A forward anatomical pole prevents the authored splayed neutral
            // knees from driving the gait sideways like a waddling toddler.
            var restPole=Vector3.ProjectOnPlane(knee-restHip,(ankle-restHip).normalized);
            var authoredPole=Vector3.ProjectOnPlane(restPole,direction).normalized;
            var forwardPole=Vector3.ProjectOnPlane(Vector3.forward,direction).normalized;
            var bend=Vector3.Slerp(authoredPole,forwardPole,Mathf.SmoothStep(0,1,gait.Amount)).normalized;
            var targetKnee=hip+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));
            overrides[thigh]=Matrix4x4.TRS(hip,Quaternion.FromToRotation(knee-restHip,targetKnee-hip)*neutral[thigh].rotation,Vector3.one);
            overrides[shin]=Matrix4x4.TRS(targetKnee,Quaternion.FromToRotation(ankle-knee,targetAnkle-targetKnee)*neutral[shin].rotation,Vector3.one);
            overrides[foot]=Matrix4x4.TRS(targetAnkle,footRotations[side],Vector3.one);
            // Long rigid light tubes use an ankle yaw gimbal: they turn with
            // the boot while remaining upright during heel/toe articulation.
            int sideNeon=ids["neon.underbody side "+suffix];
            var yaw=Quaternion.AngleAxis(gait.FootYaw(right),Vector3.up);
            var lightPose=Matrix4x4.TRS(targetAnkle,yaw*neutral[foot].rotation,Vector3.one)*neutral[foot].inverse*neutral[sideNeon];
            var lightPosition=Position(lightPose)+yaw*new Vector3(0,-.45f,-.1f)*Mathf.SmoothStep(0,1,gait.Release);
            lightPose.SetColumn(3,new Vector4(lightPosition.x,lightPosition.y,lightPosition.z,1));overrides[sideNeon]=lightPose;

            int arm=ids["upperarm."+suffix],forearm=ids["forearm."+suffix],hand=ids["hand."+suffix];
            float clear=Mathf.Clamp01(Mathf.SmoothStep(0,1,gait.Release)/.2f),run=gait.RunBlend,sign=right?-1:1;
            var origin=torsoDelta.MultiplyPoint3x4(Position(neutral[arm]));
            var axis=(Position(neutral[forearm])-Position(neutral[arm])).normalized;
            // The tuned sill armor is wider than a human hip. Open the humerus
            // before that armor deploys and externally rotate the elbow plane;
            // the full opposite-arm swing remains intact outside the leg shell.
            var abduction=Quaternion.AngleAxis(sign*(9+3*run)*clear,Vector3.forward);
            var external=Quaternion.AngleAxis(-sign*(12+8*run)*clear,axis);
            float backswingBias=gait.Amount*(8+10*run);
            overrides[arm]=Matrix4x4.TRS(origin,torsoDelta.rotation*abduction*Quaternion.AngleAxis(swing+backswingBias,Vector3.right)*external*neutral[arm].rotation,Vector3.one);
            var armDelta=overrides[arm]*neutral[arm].inverse;var elbow=armDelta*neutral[forearm];
            float flex=gait.Elbow(right)+16*clear*(1-run);armFlex[side]=flex;
            overrides[forearm]=Matrix4x4.TRS(Position(elbow),armDelta.rotation*Quaternion.AngleAxis(-flex,Vector3.right)*neutral[forearm].rotation,Vector3.one);
            var forearmDelta=overrides[forearm]*neutral[forearm].inverse;var wrist=forearmDelta*neutral[hand];
            // Keep the authored palm facing inward instead of imposing a
            // fixed sprint pronation that turns both palms towards the front.
            var inward=right?Vector3.right:Vector3.left;
            var restWrist=Position(neutral[hand]);
            var palm=Vector3.Cross(Position(neutral[ids["digit00."+suffix]])-restWrist,Position(neutral[ids["digit30."+suffix]])-restWrist).normalized;
            if(Vector3.Dot(palm,inward)<0)palm=-palm;
            var wristAxis=(Position(wrist)-Position(elbow)).normalized;
            var from=Vector3.ProjectOnPlane(forearmDelta.MultiplyVector(palm),wristAxis);
            var to=Vector3.ProjectOnPlane(torsoDelta.MultiplyVector(inward),wristAxis);
            float pronation=from.sqrMagnitude>.0001f&&to.sqrMagnitude>.0001f?Vector3.SignedAngle(from,to,wristAxis):0;
            var roll=Quaternion.AngleAxis(pronation*Mathf.SmoothStep(0,1,Mathf.Clamp01(run/.6f))*clear,wristAxis);
            overrides[forearm]=Matrix4x4.TRS(Position(elbow),roll*overrides[forearm].rotation,Vector3.one);
            forearmDelta=overrides[forearm]*neutral[forearm].inverse;wrist=forearmDelta*neutral[hand];
            overrides[hand]=Matrix4x4.TRS(Position(wrist),forearmDelta.rotation*neutral[hand].rotation,Vector3.one);
        }
        for(int i=0;i<nodes.Length;i++) {
            int parent=gaitParent[i];
            pose[i]=overrides.TryGetValue(i,out var m)?m:(parent<0?neutral[i]:
                gaitTranslate[i]?Matrix4x4.Translate(Position(pose[parent])-Position(neutral[parent]))*neutral[i]:delta[parent]*neutral[i]);
            if(releases.TryGetValue(i,out var release)&&!(asset.Manifest.nodes[i].name.StartsWith("panel.floor0_")&&asset.Manifest.nodes[i].name.Contains("_fold2_"))) {
                float deployment=Mathf.SmoothStep(0,1,gait.Release);
                if(asset.Manifest.nodes[i].name.StartsWith("panel.sill1_"))deployment=Mathf.Clamp01(deployment/.25f);
                var p=Position(pose[i])+delta[parent].MultiplyVector(release*deployment);
                if(asset.Manifest.nodes[i].name.StartsWith("panel.sill1_")) {
                    p.z+=.04f*Mathf.Max(0,Mathf.Min(1,deployment/.1f,(.8f-deployment)/.1f));
                    int side=asset.Manifest.nodes[i].name.EndsWith("R")?1:0;
                    float slope=Mathf.Clamp01(Mathf.Abs(groundAlign[side].z)/.1305262f);
                    p+=delta[parent].MultiplyVector(Vector3.right*(side==0?.08f:-.08f)*slope*deployment);

                }
                pose[i].SetColumn(3,new Vector4(p.x,p.y,p.z,1));
            }
            if(asset.Manifest.nodes[i].name.StartsWith("panel.belt_")&&asset.Manifest.nodes[i].name.Contains("hipfold1")) {
                float fold=Mathf.Clamp01(Mathf.SmoothStep(0,1,gait.Release)/.7f);
                float angle=asset.Manifest.nodes[i].name.Contains("_L_")?90:-90;
                pose[i]=delta[parent]*Around(Position(neutral[i]),Quaternion.AngleAxis(angle*fold,Vector3.forward))*neutral[i];
            }
            // Stow the knee plates on their existing telescoping carriers.
            // Offsets are anatomical-parent vectors, so the complete mounted
            // assembly keeps its clearances as the thigh pitches and yaws.
            string name=asset.Manifest.nodes[i].name;
            float stow=Mathf.SmoothStep(0,1,gait.Release);var offset=Vector3.zero;
            if(name.StartsWith("panel.floor2_")&&(name.Contains("_fold1_")||name.Contains("_fold2_"))) {
                float slide=Mathf.Clamp01((stow-.2f)/.5f);
                offset+=new Vector3(0,-.06f*Mathf.Max(0,Mathf.Min(1,stow/.2f,(1-stow)/.3f)),.32f*slide);
            }
            if(name.StartsWith("panel.floor0_")&&name.Contains("_fold0_")) {
                // Prepare once while the feet are stationary; the cassette stays
                // on the thorax through sprint braking and reverse transitions.
                float run=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,.95f,stow));
                float withdraw=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.06f,run));
                float park=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.06f,.3f,run))*stow;
                float down=.12f*withdraw*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,.55f,run)))*stow;
                var from=delta[parent];var to=delta[thorax]*Matrix4x4.Translate(new Vector3(0,0,-.95f));
                var pivot=Position(neutral[parent]);var p=Vector3.Lerp(from.MultiplyPoint3x4(pivot),to.MultiplyPoint3x4(pivot),park)+delta[thorax].MultiplyVector(Vector3.down*down);
                var carrier=Matrix4x4.TRS(p,Quaternion.Slerp(from.rotation,to.rotation,park),Vector3.one)*Matrix4x4.Translate(-pivot);
                pose[i]=carrier*neutral[i];
            }
            if(name.StartsWith("panel.floor0_")&&(name.Contains("_fold1_")||name.Contains("_fold2_")))
                offset+=new Vector3(0,-.6f*Mathf.Clamp01((stow-.5f)/.5f),-.45f*Mathf.Clamp01(stow/.5f));
            if(name.StartsWith("panel.hipguard_")||name.StartsWith("panel.rear_corner_")||name.StartsWith("panel.floor2_")||name.StartsWith("panel.sill1_")) {
                float move=Mathf.Clamp01((stow-.25f)/.75f);
                offset+=new Vector3(0,-.15f*Mathf.Clamp01(move/.4f),.25f*Mathf.Clamp01((move-.4f)/.6f));
            }
            if(terrainHinges.TryGetValue(i,out var hinge))
                pose[i]=delta[parent]*Around(hinge,Quaternion.AngleAxis(25*terrainExposure*stow,Vector3.right))*neutral[i];
            if(name.StartsWith("panel.door_front_scale0")) {
                string side=name.Substring(name.Length-1);int forearm=ids["forearm."+side],hand=ids["hand."+side];
                var axis=(Position(neutral[hand])-Position(neutral[forearm])).normalized;
                float unlock=Mathf.Clamp01(stow/.2f),fold=Mathf.Clamp01((stow-.2f)/.8f);
                pose[i]=delta[forearm]*Matrix4x4.Translate(-axis*.18f*unlock)*Around(Position(neutral[forearm]),Quaternion.AngleAxis((side=="R"?1:-1)*90*fold,axis))*neutral[i];
            }
            if(name=="neon.trunk rear")offset+=new Vector3(0,.32f*Mathf.Clamp01((stow-.4f)/.6f),.45f*Mathf.Clamp01(stow/.4f));
            if(name.StartsWith("panel.belt_")&&name.Contains("hipfold1"))offset.y+=.1f*Mathf.Clamp01(stow/.7f);
            if(offset!=Vector3.zero) {
                var p=Position(pose[i])+delta[parent].MultiplyVector(offset);
                pose[i].SetColumn(3,new Vector4(p.x,p.y,p.z,1));
            }
            delta[i]=pose[i]*neutral[i].inverse;
        }
        // Exported sleeve objects are flattened under a thorax fallback,
        // but their authored roots belong to the target's anatomical parent.
        foreach(var node in ids.Where(e=>e.Key.StartsWith("object:V18 connected sleeve 0 - "))) {
            string role=node.Key.Substring(node.Key.IndexOf(" - ",StringComparison.Ordinal)+3);
            if(!ids.TryGetValue(role.StartsWith("neon.")?role:"panel."+role,out int end))continue;
            int parent=asset.Manifest.nodes[end].parent;
            var p=delta[parent].MultiplyPoint3x4(Position(neutral[node.Value]));pose[node.Value].SetColumn(3,new Vector4(p.x,p.y,p.z,1));
        }
        ReconnectSleeves();
    }
    void ReconnectSleeves() {
        // Two telescoping sleeves remain attached while limbs move. Their
        // endpoints follow the anatomical parent and the moving panel.
        foreach(var node in ids.Where(e=>e.Key.StartsWith("object:V18 connected sleeve"))) {
            string name=node.Key;int separator=name.IndexOf(" - ",StringComparison.Ordinal);string role=name.Substring(separator+3);
            string targetBone=role.StartsWith("neon.")?role:"panel."+role;
            if(!ids.TryGetValue(targetBone,out int end))continue;
            // Exported neutral sleeve start is an exact mount point.
            string firstName="object:V18 connected sleeve 0 - "+role;
            if(!ids.TryGetValue(firstName,out int start))continue;
            var p=Position(pose[start]);var q=Position(pose[end]);var direction=q-p;
            bool outer=name.Contains("sleeve 0 - ");
            var rotation=Quaternion.FromToRotation(Vector3.up,direction.normalized);
            // Blender cylinder Z becomes Unity Y. Local geometry was exported
            // with its neutral basis; retain that basis when rebuilding TRS.
            pose[node.Value]=Matrix4x4.TRS(p+direction*(outer?0:.4f),rotation,new Vector3(1,direction.magnitude*.6f,1));
        }
    }
    public void Dispose(){if(Root!=null){Root.SetActive(false);UnityEngine.Object.Destroy(Root);}foreach(var m in meshes)UnityEngine.Object.Destroy(m);foreach(var m in owned)UnityEngine.Object.Destroy(m);if(reactorTexture!=null)UnityEngine.Object.Destroy(reactorTexture);}
}
}
