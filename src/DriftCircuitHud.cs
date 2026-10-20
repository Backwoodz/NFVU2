using System;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rachel350Z {
// Native Valheim typography with the retail drift-score backing. The score and
// route logic belong to NFVU2; no original executable or UI scripts are loaded.
internal sealed class DriftCircuitHud {
    RectTransform root,progress;
    Canvas canvas;
    Hud owner;
    TextMeshProUGUI title,lap,points,multiplier,detail,best,clock;
    Texture2D backing;
    bool artTried;
    float nextRefresh;
    readonly string directory;
    readonly Action<string> log;
    static readonly Color Gold=new Color(1,.84f,.43f),White=new Color(.96f,.95f,.89f),Teal=new Color(.38f,.91f,.85f);
    internal DriftCircuitHud(string directory,Action<string> log){this.directory=directory;this.log=log;}
    internal void Tick(Hud hud,DriftLapScore score,bool visible,bool german,string eventTitle=null,int completedPoints=0,int eventBest=-1,int lapLimit=0) {
        if(hud==null||owner!=hud){Clear();owner=hud;}
        if(!visible||hud==null){if(root!=null)root.gameObject.SetActive(false);return;}
        if(root==null&&!Create(hud))return;
        root.gameObject.SetActive(true);
        float scale=Mathf.Min(Mathf.Clamp(Screen.height/1080f,.72f,1.4f),(Screen.width-40)/540f);
        float units=scale/Mathf.Max(.01f,canvas.scaleFactor);
        root.localScale=Vector3.one*units;
        root.anchoredPosition=new Vector2(0,-28*scale/Mathf.Max(.01f,canvas.scaleFactor));
        progress.sizeDelta=new Vector2(532*Mathf.Clamp01(score.Progress),3);
        if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.08f;
        var culture=CultureInfo.GetCultureInfo(german?"de-DE":"en-US");
        title.text=eventTitle??"DRIFT";lap.text=(german?"RUNDE ":"LAP ")+(lapLimit>0?Math.Min(lapLimit,score.LapCount+1):score.LapCount+1)+(lapLimit>0?" / "+lapLimit:"");
        points.text=Math.Min(1000000000L,(long)score.Score+completedPoints).ToString("N0",culture);multiplier.text="x"+score.Multiplier;
        multiplier.color=score.Multiplier>1?Gold:White;
        string status;
        switch(score.Status) {
            case DriftLapStatus.WrongWay:status=german?"FALSCHE RICHTUNG":"WRONG WAY";break;
            case DriftLapStatus.OffCourse:status=german?"ZURÜCK AUF DIE STRECKE":"RETURN TO THE TRACK";break;
            case DriftLapStatus.Interrupted:status=german?"NEUSTART AN START/ZIEL":"RESTART AT START/FINISH";break;
            case DriftLapStatus.Running:status=score.Combo>0?"DRIFT  +"+score.Combo.ToString("N0",culture):(german?"RUNDE LÄUFT":"LAP IN PROGRESS");break;
            default:status=german?"STARTLINIE ÜBERQUEREN":"CROSS THE START LINE";break;
        }
        detail.text=status;detail.color=score.Status==DriftLapStatus.WrongWay?new Color(1,.43f,.3f):Teal;
        best.text=(eventBest>=0?(german?"REKORD  ":"HIGH SCORE  "):(german?"BESTE RUNDE  ":"BEST LAP  "))+(eventBest>=0?eventBest:score.BestScore).ToString("N0",culture);
        clock.text=TimeText(score.LapTime)+"  /  "+(score.BestLapTime>0?TimeText(score.BestLapTime):"–:––.–");
    }
    static string TimeText(float time) {
        if(float.IsNaN(time)||float.IsInfinity(time)||time<0)time=0;
        return ((int)time/60)+":"+(time%60).ToString("00.0",CultureInfo.InvariantCulture);
    }
    bool Create(Hud hud) {
        var source=hud.m_buildSelection??hud.m_hoverName;
        canvas=hud.GetComponentInParent<Canvas>()?.rootCanvas;
        if(source==null||canvas==null)return false;
        var font=Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f=>f.name.IndexOf("Norse",StringComparison.OrdinalIgnoreCase)>=0)??source.font;
        if(font==null)return false;
        root=new GameObject("NFVU2 Drift Score",typeof(RectTransform)).GetComponent<RectTransform>();root.SetParent(canvas.transform,false);
        root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,1);root.sizeDelta=new Vector2(540,183);
        var group=root.gameObject.AddComponent<CanvasGroup>();group.interactable=false;group.blocksRaycasts=false;
        if(!artTried) {
            artTried=true;
            try{backing=DriftScoreArt.Load(directory);}catch(Exception error){log?.Invoke("Optional drift score artwork unavailable: "+error.GetBaseException().Message);}
        }
        var art=new GameObject("Score backing",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));art.transform.SetParent(root,false);
        var image=art.GetComponent<RawImage>();image.texture=backing!=null?backing:Texture2D.whiteTexture;
        image.color=backing!=null?new Color(1,1,1,.85f):new Color(.025f,.035f,.04f,.72f);image.raycastTarget=false;
        Place(image.rectTransform,-16,28,568,72);
        title=Text("Title",source,font,24,Gold,0,0,330,30);
        lap=Text("Lap",source,font,20,White,344,0,188,30,TextAlignmentOptions.TopRight);
        points=Text("Score",source,font,56,White,0,28,352,66);
        multiplier=Text("Multiplier",source,font,42,Gold,356,36,176,56,TextAlignmentOptions.TopRight);
        detail=Text("Drift chain",source,font,23,Teal,0,98,532,32);
        var track=Fill("Lap progress",new Color(.12f,.16f,.17f,.85f),0,134,532,3);
        progress=Fill("Completed",Gold,0,134,0,3);
        best=Text("Best lap",source,font,20,Gold,0,147,310,30);
        clock=Text("Lap time",source,font,18,White,310,147,222,30,TextAlignmentOptions.TopRight);
        return true;
    }
    TextMeshProUGUI Text(string name,TMP_Text source,TMP_FontAsset font,float size,Color color,float x,float y,float width,float height,TextAlignmentOptions alignment=TextAlignmentOptions.TopLeft) {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(root,false);
        var text=go.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSharedMaterial=font==source.font?source.fontSharedMaterial:font.material;
        text.fontSize=size;text.color=color;text.richText=false;text.raycastTarget=false;
        text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Truncate;text.alignment=alignment;
        Place(text.rectTransform,x,y,width,height);return text;
    }
    RectTransform Fill(string name,Color color,float x,float y,float width,float height) {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(root,false);
        var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;Place(image.rectTransform,x,y,width,height);return image.rectTransform;
    }
    static void Place(RectTransform rect,float x,float y,float width,float height) {
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);
    }
    internal void Clear() {
        if(root!=null)UnityEngine.Object.Destroy(root.gameObject);
        if(backing!=null)UnityEngine.Object.Destroy(backing);
        root=progress=null;canvas=null;owner=null;backing=null;artTried=false;nextRefresh=0;
    }
}
}
