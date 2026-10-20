using System;
using UnityEngine;
using UnityEngine.UI;

namespace Rachel350Z {
// A local course drawing, shared by both native map widgets. No saved pins,
// biome textures or exploration data are changed. The planned route is visible
// through unexplored areas, just as a supplied circuit plan would be.
internal sealed class DriftCircuitMap {
    Minimap map;
    object world;
    Texture2D texture;
    DriftCircuitMapView small,large;
    bool failed;
    readonly Action<string> log;
    readonly Func<Color32[]> draw;
    readonly int size;
    readonly float minX,minZ,span;
    internal DriftCircuitMap(Action<string> log,Func<Color32[]> draw=null,int size=512,float minX=DriftCircuitMapDrawing.MinX,float minZ=DriftCircuitMapDrawing.MinZ,float span=DriftCircuitMapDrawing.WorldSize){this.log=log;this.draw=draw??DriftCircuitMapDrawing.Pixels;this.size=size;this.minX=minX;this.minZ=minZ;this.span=span;}
    internal void Tick(Minimap current,object currentWorld,bool enabled) {
        if(!enabled||current==null||currentWorld==null){Clear();return;}
        if(map!=current||world!=currentWorld){Clear();map=current;world=currentWorld;}
        if(failed)return;
        try {
            if(current.m_mapImageSmall==null||current.m_mapImageLarge==null)return;
            if(current.m_textureSize<=0||current.m_pixelSize<=0)return;
            if(texture==null) {
                texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
                texture.name="NFVU2 drift course map";
                texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;
                texture.SetPixels32(draw());texture.Apply(false,true);
            }
            if(small==null)small=Create(current.m_mapImageSmall);
            if(large==null)large=Create(current.m_mapImageLarge);
        } catch(Exception error) {
            ReleaseViews();failed=true;
            log?.Invoke("Drift course map unavailable: "+error.GetBaseException().Message);
        }
    }
    DriftCircuitMapView Create(RawImage target) {
        var root=new GameObject("NFVU2 drift map",typeof(RectTransform),typeof(RectMask2D));
        root.transform.SetParent(target.transform,false);root.transform.SetAsFirstSibling();
        var rect=(RectTransform)root.transform;
        rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        var imageObject=new GameObject("Course",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));
        imageObject.transform.SetParent(root.transform,false);
        var image=imageObject.GetComponent<RawImage>();image.texture=texture;image.raycastTarget=false;
        image.rectTransform.anchorMin=image.rectTransform.anchorMax=Vector2.zero;image.rectTransform.pivot=Vector2.zero;
        var view=root.AddComponent<DriftCircuitMapView>();view.Map=map;view.Target=target;view.Image=image;
        view.MinX=minX;view.MinZ=minZ;view.Span=span;
        view.Refresh();return view;
    }
    void ReleaseViews() {
        if(small!=null)UnityEngine.Object.Destroy(small.gameObject);
        if(large!=null)UnityEngine.Object.Destroy(large.gameObject);
        if(texture!=null)UnityEngine.Object.Destroy(texture);
        small=large=null;texture=null;
    }
    internal void Clear(){ReleaseViews();map=null;world=null;failed=false;}
}

internal sealed class DriftCircuitMapView : MonoBehaviour {
    internal Minimap Map;
    internal RawImage Target,Image;
    internal float MinX=DriftCircuitMapDrawing.MinX,MinZ=DriftCircuitMapDrawing.MinZ,Span=DriftCircuitMapDrawing.WorldSize;
    // Native Minimap.Update has already centered/zoomed its RawImage UVs.
    void LateUpdate(){Refresh();}
    internal void Refresh() {
        if(Map==null||Target==null||Image==null)return;
        var uv=Target.uvRect;var rect=Target.rectTransform.rect;
        var placement=DriftCircuitMapDrawing.Project(Map.m_textureSize,Map.m_pixelSize,uv.x,uv.y,uv.width,uv.height,rect.width,rect.height,MinX,MinZ,Span);
        Image.enabled=placement.Visible;
        if(!placement.Visible)return;
        Image.rectTransform.anchoredPosition=new Vector2(placement.X,placement.Y);
        Image.rectTransform.sizeDelta=new Vector2(placement.Width,placement.Height);
    }
}
}
