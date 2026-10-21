using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rachel350Z {
// Copy only native mesh data into fresh objects, never instantiate gameplay
// prefabs. No tree health, falling trunks, drops, colliders or ZDOs can run.
internal sealed class DriftLandscape {
    readonly Action<string> log;
    readonly Dictionary<string,Model> models=new Dictionary<string,Model>();
    readonly Dictionary<int,GameObject> built=new Dictionary<int,GameObject>();
    readonly HashSet<string> unavailable=new HashSet<string>();
    readonly List<int> pending=new List<int>();
    GameObject root;
    object world;
    ZNetScene scene;
    float nextVisibility;
    float nextSearch;
    Vector3 searchPosition;
    int pendingIndex;
    internal DriftLandscape(Action<string> log){this.log=log;}
    internal void Tick(Vector3 viewer,object currentWorld) {
        if(currentWorld==null||ZNetScene.instance==null){Clear();return;}
        if(!ReferenceEquals(world,currentWorld)||scene!=ZNetScene.instance){Clear();world=currentWorld;scene=ZNetScene.instance;}
        if(float.IsNaN(viewer.x)||float.IsNaN(viewer.z)||float.IsInfinity(viewer.x)||float.IsInfinity(viewer.z))return;
        // Refresh a near-to-far queue after moving twenty metres. Dense forest
        // does not scan or sort thousands of pending trees on every frame.
        var clock=System.Diagnostics.Stopwatch.StartNew();
        if((viewer-searchPosition).sqrMagnitude>20*20||pendingIndex>=pending.Count&&Time.unscaledTime>=nextSearch)Search(viewer);
        for(int budget=0;budget<6&&pendingIndex<pending.Count;budget++) {
            int index=pending[pendingIndex];var p=DriftLandscapePlan.Spots[index];
            if(built.ContainsKey(index)||unavailable.Contains(p.Prefab)||Distance(viewer,p)>DriftLandscapePlan.BuildDistance*DriftLandscapePlan.BuildDistance){pendingIndex++;continue;}
            try{Build(index,p,viewer);}catch(Exception e){unavailable.Add(p.Prefab);log?.Invoke("Drift landscape "+p.Prefab+" unavailable: "+e.GetBaseException().Message);}
            if(built.ContainsKey(index)||unavailable.Contains(p.Prefab))pendingIndex++;
            // Always permit one operation, then yield after two milliseconds.
            if(clock.Elapsed.TotalMilliseconds>=2)break;
        }
        if(Time.unscaledTime<nextVisibility)return;
        nextVisibility=Time.unscaledTime+.25f;
        foreach(var item in built) {
            var p=DriftLandscapePlan.Spots[item.Key];float limit=p.Tree?DriftLandscapePlan.ViewDistance:DriftLandscapePlan.DetailDistance;
            bool visible=Distance(viewer,p)<limit*limit;
            if(item.Value.activeSelf!=visible)item.Value.SetActive(visible);
        }
    }
    void Search(Vector3 viewer) {
        pending.Clear();pendingIndex=0;searchPosition=viewer;nextSearch=Time.unscaledTime+1;
        for(int i=0;i<DriftLandscapePlan.Spots.Count;i++) {
            var p=DriftLandscapePlan.Spots[i];
            if(!built.ContainsKey(i)&&!unavailable.Contains(p.Prefab)&&Distance(viewer,p)<DriftLandscapePlan.BuildDistance*DriftLandscapePlan.BuildDistance)pending.Add(i);
        }
        pending.Sort((a,b)=>Distance(viewer,DriftLandscapePlan.Spots[a]).CompareTo(Distance(viewer,DriftLandscapePlan.Spots[b])));
    }
    static float Distance(Vector3 viewer,DriftLandscapeSpot p){float dx=viewer.x-p.X,dz=viewer.z-p.Z;return dx*dx+dz*dz;}
    void Build(int index,DriftLandscapeSpot p,Vector3 viewer) {
        if(root==null)root=new GameObject("NFVU2 Drift Landscape"){hideFlags=HideFlags.DontSave};
        if(!models.TryGetValue(p.Prefab,out var model)) {
            var prefab=scene.GetPrefab(p.Prefab);
            if(prefab==null)throw new InvalidOperationException("Native prefab missing");
            model=CopyModel(prefab,root.transform);models.Add(p.Prefab,model);
            return;
        }
        GameObject holder=null;
        try {
            holder=UnityEngine.Object.Instantiate(model.Template,root.transform,false);holder.name=(p.Parkade?"Parkade ":"Practice ")+p.Prefab+" "+index;
            // Fit both the height and conservative crown radius. Uniform scale
            // keeps native proportions and guarantees the planned clearances.
            float scale=Math.Min(p.Height/model.Height,p.Radius/model.Radius);
            holder.transform.localScale=Vector3.one*scale;
            float y=ParkadeLayout.TerrainHeightAt(p.X,p.Z)-model.Bottom*scale-.06f;
            holder.transform.SetPositionAndRotation(new Vector3(p.X,y,p.Z),Quaternion.Euler(0,p.Yaw,0));
            built.Add(index,holder);
            float limit=p.Tree?DriftLandscapePlan.ViewDistance:DriftLandscapePlan.DetailDistance;
            holder.SetActive(Distance(viewer,p)<limit*limit);
        }catch {if(holder!=null)UnityEngine.Object.Destroy(holder);throw;}
    }
    internal static Model CopyModel(GameObject source,Transform parent) {
        var template=new GameObject("Landscape template "+source.name);template.SetActive(false);template.transform.SetParent(parent,false);
        try {
            var transforms=new Dictionary<Transform,Transform>{{source.transform,template.transform}};
            var renderers=new Dictionary<Renderer,Renderer>();
            foreach(var r in source.GetComponentsInChildren<MeshRenderer>(true)) {
                var filter=r.GetComponent<MeshFilter>();
                if(filter==null||filter.sharedMesh==null||!ActivePath(r.transform,source.transform))continue;
                var target=CopyTransform(r.transform,transforms);
                target.gameObject.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;
                var renderer=target.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterials=r.sharedMaterials;
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
                renderers.Add(r,renderer);
            }
            if(renderers.Count==0)throw new InvalidOperationException("Native scenery has no visible static meshes");
            // Retain native LOD levels/materials; copying all levels as visible
            // meshes would multiply geometry and overlap the distant artwork.
            foreach(var group in source.GetComponentsInChildren<LODGroup>(true)) {
                if(!ActivePath(group.transform,source.transform))continue;
                var levels=group.GetLODs().Select(l=>new LOD(l.screenRelativeTransitionHeight,l.renderers.Where(r=>r!=null&&renderers.ContainsKey(r)).Select(r=>renderers[r]).ToArray())).ToArray();
                if(!levels.Any(l=>l.renderers.Length>0))continue;
                var copy=CopyTransform(group.transform,transforms).gameObject.AddComponent<LODGroup>();
                copy.SetLODs(levels);copy.localReferencePoint=group.localReferencePoint;copy.size=group.size;
                // Crossfade can require native controller data; hard native LOD
                // transitions retain the savings without copying any behaviours.
                copy.fadeMode=LODFadeMode.None;
            }
            float bottom=float.PositiveInfinity,top=float.NegativeInfinity,radius=0;
            foreach(var renderer in renderers.Values) {
                var filter=renderer.GetComponent<MeshFilter>();var b=filter.sharedMesh.bounds;
                foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1}) {
                    var v=template.transform.InverseTransformPoint(renderer.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));
                    bottom=Math.Min(bottom,v.y);top=Math.Max(top,v.y);radius=Math.Max(radius,new Vector2(v.x,v.z).magnitude);
                }
            }
            if(top-bottom<.01f||radius<.01f)throw new InvalidOperationException("Native scenery has empty bounds");
            return new Model(template,top-bottom,radius,bottom);
        }catch {UnityEngine.Object.Destroy(template);throw;}
    }
    static bool ActivePath(Transform node,Transform root) {
        for(var t=node;t!=root;t=t.parent)if(!t.gameObject.activeSelf)return false;
        return true;
    }
    static Transform CopyTransform(Transform node,Dictionary<Transform,Transform> map) {
        if(map.TryGetValue(node,out var result))return result;
        var parent=CopyTransform(node.parent,map);result=new GameObject(node.name).transform;
        result.SetParent(parent,false);result.localPosition=node.localPosition;result.localRotation=node.localRotation;result.localScale=node.localScale;
        map.Add(node,result);return result;
    }
    internal void Clear() {
        if(root!=null){root.SetActive(false);UnityEngine.Object.Destroy(root);}
        root=null;world=null;scene=null;built.Clear();models.Clear();unavailable.Clear();pending.Clear();pendingIndex=0;nextVisibility=nextSearch=0;searchPosition=Vector3.zero;
    }
    internal sealed class Model {
        internal readonly GameObject Template;
        internal readonly float Height,Radius,Bottom;
        internal Model(GameObject template,float height,float radius,float bottom){Template=template;Height=height;Radius=radius;Bottom=bottom;}
    }
}
}
