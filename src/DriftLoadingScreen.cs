using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rachel350Z {
// One static approved image, three lightweight UI quads. Animation uses the
// real-time clock while course construction and the held car keep their limits.
internal sealed class DriftLoadingScreen {
    readonly string directory;
    readonly Action<string> log;
    RectTransform root,design;
    Canvas canvas;
    Hud owner;
    Texture2D artwork;
    TextMeshProUGUI progress;
    readonly DriftLoadingSegment[] segments=new DriftLoadingSegment[3];
    bool attempted;
    internal DriftLoadingScreen(string directory,Action<string> log){this.directory=directory;this.log=log;}
    internal bool Tick(Hud hud,bool visible,bool returning,int done,int planned,bool german) {
        if(owner!=hud){Clear();owner=hud;}
        if(!visible||hud==null){if(root!=null)root.gameObject.SetActive(false);return false;}
        if(root==null&&!Create(hud))return false;
        root.gameObject.SetActive(true);
        var frame=(RectTransform)canvas.transform;
        design.localScale=Vector3.one*Mathf.Min(frame.rect.width/1920f,frame.rect.height/1080f);
        int percent=planned>0?Mathf.Clamp(Mathf.FloorToInt(100f*done/planned),0,100):0;
        progress.text=returning?(german?"ZURÜCK ZUM PARKPLATZ …":"RETURNING TO PARKING …"):
            (german?"STRECKE WIRD VORBEREITET":"PREPARING COURSE")+(planned>0?"  "+percent+" %":" …");
        for(int i=0;i<segments.Length;i++)segments[i].color=new Color(.72f,.94f,.32f,DriftLoadingAnimation.Brightness(Time.unscaledTime,i));
        return true;
    }
    bool Create(Hud hud) {
        if(attempted)return false;
        var source=hud.m_buildSelection??hud.m_hoverName;
        canvas=hud.GetComponentInParent<Canvas>()?.rootCanvas;
        if(source==null||canvas==null)return false;
        var font=Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f=>f.name.IndexOf("Norse",StringComparison.OrdinalIgnoreCase)>=0)??source.font;
        if(font==null)return false;
        attempted=true;
        try {
            var path=Path.Combine(directory,"loading","background.png");
            if(!File.Exists(path))return false;
            artwork=new Texture2D(2,2,TextureFormat.RGBA32,false){name="NFVU2 approved loading artwork",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            if(!ImageConversion.LoadImage(artwork,File.ReadAllBytes(path),true))throw new InvalidDataException("Loading image could not be decoded");
            root=new GameObject("NFVU2 Course Loading",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster)).GetComponent<RectTransform>();
            root.SetParent(canvas.transform,false);root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
            var overlay=root.GetComponent<Canvas>();overlay.overrideSorting=true;overlay.sortingOrder=30000;
            root.gameObject.AddComponent<Image>().color=Color.black;
            design=new GameObject("Widescreen artwork",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage)).GetComponent<RectTransform>();
            design.SetParent(root,false);design.anchorMin=design.anchorMax=design.pivot=new Vector2(.5f,.5f);design.sizeDelta=new Vector2(1920,1080);
            var image=design.GetComponent<RawImage>();image.texture=artwork;image.raycastTarget=false;
            var strip=Box("Loading strip",new Color(.015f,.02f,.018f,.88f),350,455,410,62);
            var word=Label("LOADING",font,30,new Color(.76f,.9f,.48f),378,462,220,48);word.fontStyle=FontStyles.Italic;word.text="LOADING";
            for(int i=0;i<3;i++) {
                var go=new GameObject("Loading segment "+(i+1),typeof(RectTransform),typeof(CanvasRenderer),typeof(DriftLoadingSegment));go.transform.SetParent(design,false);
                segments[i]=go.GetComponent<DriftLoadingSegment>();segments[i].raycastTarget=false;Place(segments[i].rectTransform,630+i*31,472,27,29);
            }
            Box("Bottom shading",new Color(.005f,.01f,.008f,.85f),0,986,1920,94);
            progress=Label("Course progress",font,25,new Color(.9f,.93f,.83f),52,1010,1260,48);
            return true;
        } catch(Exception error) {
            if(root!=null)UnityEngine.Object.Destroy(root.gameObject);root=null;
            if(artwork!=null)UnityEngine.Object.Destroy(artwork);artwork=null;
            log?.Invoke("Optional loading screen unavailable: "+error.GetBaseException().Message);return false;
        }
    }
    Image Box(string name,Color color,float x,float y,float width,float height) {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(design,false);
        var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;Place(image.rectTransform,x,y,width,height);return image;
    }
    TextMeshProUGUI Label(string name,TMP_FontAsset font,float size,Color color,float x,float y,float width,float height) {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(design,false);
        var text=go.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSharedMaterial=font.material;text.fontSize=size;text.color=color;text.raycastTarget=false;text.richText=false;
        text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Truncate;
        Place(text.rectTransform,x,y,width,height);return text;
    }
    static void Place(RectTransform r,float x,float y,float width,float height){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);}
    internal void Clear(){if(root!=null)UnityEngine.Object.Destroy(root.gameObject);if(artwork!=null)UnityEngine.Object.Destroy(artwork);root=design=null;artwork=null;owner=null;canvas=null;attempted=false;}
}

internal sealed class DriftLoadingSegment:MaskableGraphic {
    protected override void OnPopulateMesh(VertexHelper mesh) {
        mesh.Clear();var r=GetPixelAdjustedRect();float skew=r.width*.38f;
        mesh.AddVert(new Vector3(r.xMin,r.yMin),color,Vector2.zero);
        mesh.AddVert(new Vector3(r.xMin+skew,r.yMax),color,Vector2.up);
        mesh.AddVert(new Vector3(r.xMax,r.yMax),color,Vector2.one);
        mesh.AddVert(new Vector3(r.xMax-skew,r.yMin),color,Vector2.right);
        mesh.AddTriangle(0,1,2);mesh.AddTriangle(2,3,0);
    }
}
}
