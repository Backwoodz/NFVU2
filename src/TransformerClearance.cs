using System;
using System.Collections.Generic;
using UnityEngine;
namespace Rachel350Z {
// Broad phase only finds neighbours. Actual blocking is tested against each
// rigid part's bone-local bounds, rather than a six-metre empty cube.
internal sealed class TransformerClearance:IDisposable {
    struct Part {public int Bone;public Vector3 Center,Half;public string Name;public Vector3[] Points;public bool Concealed,LowerPart;}
    struct Shape {public Vector3 Center,Half;public Quaternion Rotation;public string Name;public Vector3[] Points;public Matrix4x4 Source;public bool LowerPart;}
    struct Neighbour {public Collider Collider;public Bounds Bounds;public Heightmap Terrain;public Vector3 Position;public Quaternion Rotation;}
    readonly List<Part> parts=new List<Part>();
    readonly TransformerAsset asset;
    readonly int riderBone;
    readonly BoxCollider probe;
    readonly GameObject probeObject;
    readonly Collider[] neighbours=new Collider[128];
    readonly Neighbour[] candidates=new Neighbour[128];
    readonly List<Shape> shapes=new List<Shape>();
    readonly Matrix4x4[] previousWalkingPose;
    readonly float[] lastWalkingTime;
    float terrainFloor;
    public string BlockedPart {get;private set;}
    public TransformerClearance(Transform car,TransformerAsset asset) {
        this.asset=asset;
        riderBone=Array.FindIndex(asset.Manifest.nodes,n=>n.name=="rider.anchor");
        int nodeCount=asset.Manifest.nodes.Length;
        var minimum=new Vector3[nodeCount*2];var maximum=new Vector3[minimum.Length];var used=new bool[minimum.Length];
        var inverse=new Matrix4x4[nodeCount];
        var points=new HashSet<Vector3>[minimum.Length];previousWalkingPose=new Matrix4x4[nodeCount];lastWalkingTime=new float[nodeCount];
        for(int i=0;i<inverse.Length;i++)inverse[i]=Matrix(asset.Manifest.nodes[i].neutral).inverse;
        foreach(var batch in asset.Batches)for(int v=0;v<batch.Weights.Length;v++) {
            int bone=batch.Bones[batch.Weights[v]],key=bone+(batch.Concealed?nodeCount:0),k=v*8;
            var p=inverse[bone].MultiplyPoint3x4(new Vector3(batch.Vertices[k],batch.Vertices[k+1],batch.Vertices[k+2]));
            if(!used[key]){minimum[key]=maximum[key]=p;used[key]=true;points[key]=new HashSet<Vector3>();}
            else {minimum[key]=Vector3.Min(minimum[key],p);maximum[key]=Vector3.Max(maximum[key],p);}
            points[key].Add(p);
        }
        for(int i=0;i<used.Length;i++)if(used[i]){int bone=i%nodeCount;var vertices=new Vector3[points[i].Count];points[i].CopyTo(vertices);parts.Add(new Part{Bone=bone,Center=(minimum[i]+maximum[i])*.5f,Half=(maximum[i]-minimum[i])*.5f,Name=asset.Manifest.nodes[bone].name,Points=vertices,Concealed=i>=nodeCount,LowerPart=WalkingLowerPart(bone)});}
        // Keep the enabled query shape away from gameplay; ComputePenetration
        // poses it without moving it or participating in the car's physics.
        probeObject=new GameObject("350Z_TransformerClearanceProbe"){hideFlags=HideFlags.HideAndDontSave};probeObject.transform.position=Vector3.down*10000;
        probe=probeObject.AddComponent<BoxCollider>();probe.isTrigger=true;probe.size=Vector3.one;
    }
    bool WalkingLowerPart(int bone) {
        for(int node=bone;node>=0;node=asset.Manifest.nodes[node].parent) {
            string name=asset.Manifest.nodes[node].name;
            if(name.StartsWith("panel.hipguard_")||name.StartsWith("panel.sill1_"))return false;
            if(name.StartsWith("panel.floor0_"))return name.Contains("_fold1_")||name.Contains("_fold2_");
            if(name.StartsWith("thigh."))return false;
            if(name.StartsWith("shin.")||name.StartsWith("foot."))return true;
        }
        return false;
    }
    static Matrix4x4 Matrix(float[] a){var m=new Matrix4x4();for(int r=0;r<4;r++)for(int c=0;c<4;c++)m[r,c]=a[r*4+c];return m;}
    static Matrix4x4 Frame(float[] a,int i){int k=i*10;return Matrix4x4.TRS(new Vector3(a[k],a[k+1],a[k+2]),new Quaternion(a[k+3],a[k+4],a[k+5],a[k+6]),new Vector3(a[k+7],a[k+8],a[k+9]));}
    static Vector3 Absolute(Vector3 v)=>new Vector3(Mathf.Abs(v.x),Mathf.Abs(v.y),Mathf.Abs(v.z));
    static Vector3 AxisBounds(Shape s)=>Absolute(s.Rotation*Vector3.right)*s.Half.x+Absolute(s.Rotation*Vector3.up)*s.Half.y+Absolute(s.Rotation*Vector3.forward)*s.Half.z;
    static Shape Place(Part part,Matrix4x4 pose,Vector3 origin,Quaternion rotation,float margin) {
        var scale=Absolute(pose.lossyScale);
        return new Shape{Center=origin+rotation*pose.MultiplyPoint3x4(part.Center),Half=Vector3.Scale(part.Half,scale)+Vector3.one*margin,Rotation=rotation*pose.rotation,Name=part.Name,Points=part.Points,Source=Matrix4x4.TRS(origin,rotation,Vector3.one)*pose,LowerPart=part.LowerPart};
    }
    static float TerrainMaximum(Heightmap map,Bounds bounds,Vector3 origin) {
        int x0=Mathf.Clamp(Mathf.FloorToInt((bounds.min.x-origin.x)/map.m_scale+map.m_width*.5f),0,map.m_width);
        int x1=Mathf.Clamp(Mathf.CeilToInt((bounds.max.x-origin.x)/map.m_scale+map.m_width*.5f),0,map.m_width);
        int z0=Mathf.Clamp(Mathf.FloorToInt((bounds.min.z-origin.z)/map.m_scale+map.m_width*.5f),0,map.m_width);
        int z1=Mathf.Clamp(Mathf.CeilToInt((bounds.max.z-origin.z)/map.m_scale+map.m_width*.5f),0,map.m_width);
        float height=float.NegativeInfinity;
        // Linear triangle heights cannot exceed their grid vertices. This
        // exact upper bound cheaply excludes high armor before per-vertex work.
        for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)height=Mathf.Max(height,map.GetHeight(x,z)+origin.y);
        return height;
    }
    static bool TerrainHeight(Heightmap map,Vector3 origin,Vector3 world,out float height) {
        float x=(world.x-origin.x)/map.m_scale+map.m_width*.5f,z=(world.z-origin.z)/map.m_scale+map.m_width*.5f;
        height=0;if(x<0||z<0||x>map.m_width||z>map.m_width)return false;
        int ix=Mathf.Min(map.m_width-1,Mathf.FloorToInt(x)),iz=Mathf.Min(map.m_width-1,Mathf.FloorToInt(z));
        height=origin.y+TransformerPlacement.TriangleHeight(x-ix,z-iz,map.GetHeight(ix,iz),map.GetHeight(ix+1,iz),map.GetHeight(ix,iz+1),map.GetHeight(ix+1,iz+1));
        return true;
    }
    bool AboveConvexFloor(Shape shape,Collider other,Bounds otherBounds,Bounds bounds) {
        if(shape.Points==null||!(other is BoxCollider)&&!(other is MeshCollider mesh&&mesh.convex))return false;
        var center=bounds.center;
        for(int i=0;i<6;i++) {
            float x=i==0?center.x:(i==1||i==3?bounds.min.x:bounds.max.x);
            float z=i==0?center.z:(i<=2?bounds.min.z:bounds.max.z);
            if(i==5) {
                // At a finite platform edge, rectangle corners may all miss
                // the supporting face. Find a point just inside its footprint.
                var closest=other.ClosestPoint(new Vector3(center.x,otherBounds.max.y+1,center.z));
                var inside=Vector3.Lerp(closest,otherBounds.center,.001f);x=inside.x;z=inside.z;
            }
            var ray=new Ray(new Vector3(x,bounds.max.y+.25f,z),Vector3.down);
            if(!other.Raycast(ray,out var hit,bounds.size.y+.5f)||hit.normal.y<.965f||hit.point.y>terrainFloor+.6f)continue;
            // Every outward face of a convex collider defines a supporting
            // half-space containing that collider. If every actual vertex of
            // this part is outside it, no triangle can intersect the floor.
            // The rule remains valid when part of the boot leaves the platform.
            bool above=true;
            foreach(var point in shape.Points) {
                var world=shape.Source.MultiplyPoint3x4(point);
                if(Vector3.Dot(hit.normal,world-hit.point)<-.003f){above=false;break;}
            }
            if(above)return true;
        }
        return false;
    }
    bool Query(Func<Collider,bool> own,bool walking=false) {
        BlockedPart=null;if(shapes.Count==0)return true;
        var min=shapes[0].Center-AxisBounds(shapes[0]);var max=shapes[0].Center+AxisBounds(shapes[0]);
        foreach(var s in shapes){var extent=AxisBounds(s);min=Vector3.Min(min,s.Center-extent);max=Vector3.Max(max,s.Center+extent);}
        int count=Physics.OverlapBoxNonAlloc((min+max)*.5f,(max-min)*.5f,neighbours,Quaternion.identity,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        // A full buffer cannot establish that all relevant obstacles were seen.
        if(count==neighbours.Length){BlockedPart="crowded";return false;}
        // One transformation contains tens of thousands of part-frame boxes.
        // Physics cannot advance inside this synchronous query: capture each
        // neighbour once, rather than repeating native lookups per frame box.
        int candidateCount=0;
        for(int i=0;i<count;i++) {
            var other=neighbours[i];if(own(other))continue;
            var transform=other.transform;
            candidates[candidateCount++]=new Neighbour{Collider=other,Bounds=other.bounds,Terrain=other.GetComponent<Heightmap>(),Position=transform.position,Rotation=transform.rotation};
        }
        foreach(var shape in shapes) {
            var bounds=new Bounds(shape.Center,AxisBounds(shape)*2);
            for(int i=0;i<candidateCount;i++) {
                var candidate=candidates[i];var other=candidate.Collider;
                if(!bounds.Intersects(candidate.Bounds))continue;
                bool terrain=candidate.Terrain!=null;
                if(terrain&&shape.Points!=null) {
                    // Testing actual vertices avoids rejecting empty corners
                    // of a diagonal armor OBB against an otherwise valid slope.
                    var map=candidate.Terrain;var origin=candidate.Position;
                    float allowance=walking&&shape.LowerPart?TransformerPlacement.WalkingTerrainSink:.045f;
                    if(TerrainMaximum(map,bounds,origin)<bounds.min.y-allowance)continue;
                    foreach(var point in shape.Points) {
                        var world=shape.Source.MultiplyPoint3x4(point);
                        // Cosmetic boot/neon penetration is preferable to a
                        // flat-only walking gate. This exception is limited to
                        // terrain heightmaps and low walking-leg points; the
                        // original solid rock/wall penetration query stays strict.
                        float tolerance=TransformerPlacement.TerrainTolerance(walking,shape.LowerPart,world.y-terrainFloor);
                        if(TerrainHeight(map,origin,world,out float floor)&&world.y<floor-tolerance){BlockedPart=shape.Name;return false;}
                    }
                    continue;
                }
                probe.size=shape.Half*2;
                if(Physics.ComputePenetration(probe,shape.Center,shape.Rotation,other,candidate.Position,candidate.Rotation,out _,out float penetration)&&penetration>(terrain?.05f:.003f)) {
                    if(AboveConvexFloor(shape,other,candidate.Bounds,bounds))continue;
                    BlockedPart=shape.Name;return false;
                }
            }
        }
        return true;
    }
    static Shape Sweep(Part part,Shape previous,Shape endpoint) {
        var shape=endpoint;
        var halfTravel=Quaternion.Inverse(shape.Rotation)*(previous.Center-shape.Center)*.5f;
        float radius=part.Center.magnitude*endpoint.Source.lossyScale.magnitude+Mathf.Max(previous.Half.magnitude,shape.Half.magnitude);
        float arc=2*Mathf.Sin(Quaternion.Angle(previous.Rotation,shape.Rotation)*Mathf.Deg2Rad*.5f)*radius;
        shape.Center=(shape.Center+previous.Center)*.5f;
        shape.Half=Vector3.Max(shape.Half,previous.Half)+Absolute(halfTravel)+Vector3.one*arc;
        return shape;
    }
    Shape Rider(Vector3 origin,Quaternion rotation,Matrix4x4[] matrices) {
        var anchor=riderBone>=0?matrices[riderBone].MultiplyPoint3x4(Vector3.zero)+Vector3.up*.45f:new Vector3(0,5.2f,-.15f);
        return new Shape{Center=origin+rotation*anchor,Half=new Vector3(.42f,.85f,.48f),Rotation=rotation,Name="rider"};
    }
    public bool Transformation(Vector3 origin,Quaternion yaw,Quaternion carTilt,Quaternion supportTilt,float clearance,Func<Collider,bool> own,Func<float,float,Matrix4x4[]> preparedPose=null,float standingOffset=0) {
        // Test every exported frame. Enclose translation/rotation between each
        // adjacent pair, including the root's late upright alignment.
        shapes.Clear();terrainFloor=origin.y;
        foreach(var part in parts) {
            Shape previous=new Shape();bool havePrevious=false;
            for(int f=0;f<asset.Frames.Length;f++) {
                float p=f/(float)(asset.Frames.Length-1),smooth=Mathf.SmoothStep(0,1,p);
                if(part.Concealed&&p<11.5f/120)continue;
                var rotation=yaw*Quaternion.Slerp(carTilt,supportTilt,TransformerPlacement.Align(p));
                var shape=Place(part,Frame(asset.Frames[f],part.Bone),origin+Vector3.up*(clearance*(1-smooth)),rotation,.035f);
                // Vanishing packed mechanism has no useful collision volume.
                if(shape.Half.sqrMagnitude<.004f)continue;
                var endpoint=shape;
                if(havePrevious)shape=Sweep(part,previous,endpoint);
                shapes.Add(shape);
                // Keep the actual frame, not the already swept interval, as
                // the next interval's starting endpoint.
                previous=endpoint;havePrevious=true;
            }
        }
        // On a slope the closed source robot first opens its existing armor
        // carriers while parallel to the support. Only the prepared body is
        // then brought upright. Sample the same pure pose builder and actual
        // hypothetical foot probes used by that controller, including the
        // root-height path taken when returning from a walked position.
        if(preparedPose!=null) {
            var previous=new Shape[parts.Count];bool havePrevious=false;
            for(int f=0;f<=128;f++) {
                float release=Mathf.Min(1,f/64f),align=Mathf.Max(0,(f-64)/64f);
                var matrices=preparedPose(release,align);
                if(matrices==null||matrices.Length!=asset.Manifest.nodes.Length){BlockedPart="preparation";return false;}
                var rotation=yaw*Quaternion.Slerp(supportTilt,Quaternion.identity,align);
                var position=origin+Vector3.up*(standingOffset*align);
                for(int i=0;i<parts.Count;i++) {
                    var endpoint=Place(parts[i],matrices[parts[i].Bone],position,rotation,.035f);
                    shapes.Add(havePrevious?Sweep(parts[i],previous[i],endpoint):endpoint);previous[i]=endpoint;
                }
                shapes.Add(Rider(position,rotation,matrices));havePrevious=true;
            }
        }
        // The native rider is not exported as a generic mannequin mesh.
        shapes.Add(new Shape{Center=origin+yaw*new Vector3(0,5.2f,-.15f),Half=new Vector3(.42f,.85f,.48f),Rotation=yaw,Name="rider"});
        return Query(own);
    }
    public bool Movement(Vector3 from,Quaternion previousRotation,Vector3 to,Quaternion nextRotation,Func<int,Matrix4x4> pose,Func<Collider,bool> own) {
        shapes.Clear();terrainFloor=Mathf.Min(from.y,to.y);var rotation=Quaternion.Slerp(previousRotation,nextRotation,.5f);var mid=(from+to)*.5f;
        foreach(var part in parts) {
            var current=pose(part.Bone);var shape=Place(part,current,mid,rotation,.045f);
            var travel=Quaternion.Inverse(shape.Rotation)*(to-from)*.5f;
            float arc=2*Mathf.Sin(Quaternion.Angle(previousRotation,nextRotation)*Mathf.Deg2Rad*.5f)*((shape.Center-mid).magnitude+shape.Half.magnitude);
            // Include the observed limb motion, not only translation of the
            // robot root. Two ticks of look-ahead reserve its next swing space.
            float elapsed=Time.fixedTime-lastWalkingTime[part.Bone];
            if(lastWalkingTime[part.Bone]>0&&elapsed>0&&elapsed<=.1f) {
                var previous=previousWalkingPose[part.Bone];
                var localMotion=Vector3.ClampMagnitude((current.MultiplyPoint3x4(part.Center)-previous.MultiplyPoint3x4(part.Center))*(Time.fixedDeltaTime/elapsed)*2,.55f);
                var projected=Quaternion.Inverse(shape.Rotation)*(rotation*localMotion)*.5f;
                shape.Center+=rotation*localMotion*.5f;shape.Half+=Absolute(projected);
                arc+=2*Mathf.Sin(Quaternion.Angle(previous.rotation,current.rotation)*Mathf.Deg2Rad*.5f)*(shape.Half.magnitude+part.Center.magnitude)*Mathf.Min(2,Time.fixedDeltaTime/elapsed*2);
            }
            shape.Half+=Absolute(travel)+Vector3.one*arc;shapes.Add(shape);
        }
        foreach(var part in parts){previousWalkingPose[part.Bone]=pose(part.Bone);lastWalkingTime[part.Bone]=Time.fixedTime;}
        var riderAnchor=riderBone>=0?pose(riderBone).MultiplyPoint3x4(Vector3.zero)+Vector3.up*.45f:new Vector3(0,5.2f,-.15f);
        shapes.Add(new Shape{Center=mid+rotation*riderAnchor,Half=new Vector3(.42f,.85f,.48f)+Absolute(to-from)*.5f,Rotation=rotation,Name="rider"});
        return Query(own,true);
    }
    public void Dispose(){if(probeObject!=null)UnityEngine.Object.Destroy(probeObject);}
}
}
