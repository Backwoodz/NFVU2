using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rachel350Z {
// Small local, mesh-only maintenance areas behind existing outer buildings.
// Native art is shared; containers and workstations never run gameplay scripts.
internal sealed class ParkadeServiceYards {
    readonly Action<string> log;
    readonly Dictionary<string,DriftLandscape.Model> models=new Dictionary<string,DriftLandscape.Model>();
    readonly Dictionary<int,GameObject> built=new Dictionary<int,GameObject>();
    readonly HashSet<string> unavailable=new HashSet<string>();
    GameObject root;
    ZNetScene scene;
    object world;
    float nextVisibility;
    internal ParkadeServiceYards(Action<string> log){this.log=log;}
    internal void Tick(Vector3 viewer,object currentWorld) {
        if(currentWorld==null||ZNetScene.instance==null){Clear();return;}
        if(!ReferenceEquals(world,currentWorld)||scene!=ZNetScene.instance){Clear();world=currentWorld;scene=ZNetScene.instance;}
        for(int i=0;i<ParkadeServiceYardPlan.Pieces.Count;i++) {
            var p=ParkadeServiceYardPlan.Pieces[i];
            if(built.ContainsKey(i)||unavailable.Contains(p.Prefab)||Distance(viewer,p)>170*170)continue;
            try {
                if(root==null)root=new GameObject("NFVU2 Parkade Maintenance Yards"){hideFlags=HideFlags.DontSave};
                if(!models.TryGetValue(p.Prefab,out var model)) {
                    var prefab=scene.GetPrefab(p.Prefab);if(prefab==null)throw new InvalidOperationException("Native prefab missing");
                    models.Add(p.Prefab,DriftLandscape.CopyModel(prefab,root.transform));break;
                }
                var go=UnityEngine.Object.Instantiate(model.Template,root.transform,false);go.name="Maintenance "+p.Prefab+" "+i;
                float y=ParkadeLayout.TerrainHeightAt(p.X,p.Z)+p.Y-(p.Grounded?model.Bottom:0);
                go.transform.SetPositionAndRotation(new Vector3(p.X,y,p.Z),Quaternion.Euler(0,p.Yaw,0));
                built.Add(i,go);go.SetActive(Distance(viewer,p)<ParkadeServiceYardPlan.ViewDistance*ParkadeServiceYardPlan.ViewDistance);
            }catch(Exception e){unavailable.Add(p.Prefab);log?.Invoke("Parkade maintenance "+p.Prefab+" unavailable: "+e.GetBaseException().Message);}
            break;
        }
        if(Time.unscaledTime<nextVisibility)return;nextVisibility=Time.unscaledTime+.3f;
        foreach(var item in built) {
            bool visible=Distance(viewer,ParkadeServiceYardPlan.Pieces[item.Key])<ParkadeServiceYardPlan.ViewDistance*ParkadeServiceYardPlan.ViewDistance;
            if(item.Value.activeSelf!=visible)item.Value.SetActive(visible);
        }
    }
    static float Distance(Vector3 v,ParkadeYardPiece p){float dx=v.x-p.X,dz=v.z-p.Z;return dx*dx+dz*dz;}
    internal void Clear() {
        if(root!=null){root.SetActive(false);UnityEngine.Object.Destroy(root);}
        root=null;scene=null;world=null;nextVisibility=0;models.Clear();built.Clear();unavailable.Clear();
    }
}
}
