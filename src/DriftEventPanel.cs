using System;
using System.Globalization;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rachel350Z {
internal sealed class DriftEventPanel {
    RectTransform root;
    Canvas canvas;
    Hud owner;
    TextMeshProUGUI title,info,record,message;
    TextMeshProUGUI primaryText,secondaryText,restartText;
    Button primary,secondary,restart;
    Texture2D preview;
    readonly Action accept,cancel;
    readonly Action restartRace;
    bool paused;
    readonly DriftEventMenuNavigation navigation=new DriftEventMenuNavigation();
    readonly DriftLoadingScreen loading;
    readonly DriftStartCue startCue=new DriftStartCue();
    internal DriftEventPanel(Action accept,Action cancel,string directory=null,Action<string> log=null):this(accept,cancel,directory,log,null){}
    internal DriftEventPanel(Action accept,Action cancel,string directory,Action<string> log,Action restartRace){
        this.accept=accept;this.cancel=cancel;this.restartRace=restartRace;
        loading=new DriftLoadingScreen(directory??Path.GetDirectoryName(typeof(Plugin).Assembly.Location),cancel,log);
    }
    internal void Tick(Hud hud,DriftRaceRun run,int done,int planned,bool german) {
        if(hud==null){Clear();return;}
        if(owner!=hud){Clear();owner=hud;}
        if(root==null&&!Create(hud))return;
        paused=run.Phase==DriftRacePhase.Paused;
        restart.gameObject.SetActive(paused);
        Place(primary.GetComponent<RectTransform>(),28,477,paused?238:358,45);
        Place(secondary.GetComponent<RectTransform>(),paused?534:414,477,paused?238:358,45);
        Place(primaryText.rectTransform,12,5,paused?214:334,38);
        Place(secondaryText.rectTransform,12,5,paused?214:334,38);
        primaryText.fontSize=secondaryText.fontSize=paused?20:23;
        bool fullscreen=loading.Tick(hud,run.Phase==DriftRacePhase.Loading||run.Phase==DriftRacePhase.Returning,
            run.Phase==DriftRacePhase.Returning,done,planned,german,navigation.Selected==1);
        startCue.Tick(hud,run,german);
        root.gameObject.SetActive(run.BlocksDriving&&!fullscreen&&run.Phase!=DriftRacePhase.Countdown);
        if(!run.BlocksDriving)return;
        float scale=Mathf.Min((Screen.width-40)/800f,(Screen.height-60)/540f)/Mathf.Max(.01f,canvas.scaleFactor);
        root.localScale=Vector3.one*Mathf.Min(1.35f,scale);
        title.text="PARKADE DRIFT 1";
        info.text=(german?"3 RUNDEN":"3 LAPS")+"\n503 m / "+(german?"RUNDE":"LAP")+"\n\n"+(german?"NFSU2-KURS\nVALHEIM-NACHBAU":"NFSU2 COURSE\nVALHEIM ADAPTATION");
        record.text=(german?"REKORD":"HIGH SCORE")+"\n"+run.BestScore.ToString("N0",CultureInfo.GetCultureInfo(german?"de-DE":"en-US"));
        primary.interactable=true;secondary.interactable=true;
        primaryText.text=german?"RENNEN STARTEN":"START RACE";secondaryText.text=german?"ZURÜCK":"BACK";
        message.text=german?"Driftpunkte über drei vollständige Runden sammeln.":"Collect drift points over three complete laps.";
        switch(run.Phase) {
            case DriftRacePhase.Loading:
                message.text=(german?"STRECKE WIRD VORBEREITET":"PREPARING COURSE")+(planned>0?"  "+Mathf.Clamp(Mathf.RoundToInt(100f*done/planned),0,100)+" %":" …");
                primary.interactable=false;primaryText.text=german?"LADEN …":"LOADING …";secondaryText.text=german?"ABBRECHEN":"CANCEL";break;
            case DriftRacePhase.Returning:
                message.text=german?"ZURÜCK ZUM PARKPLATZ …":"RETURNING TO PARKING …";primary.interactable=secondary.interactable=false;break;
            case DriftRacePhase.Countdown:
                message.text=Mathf.CeilToInt(run.Countdown).ToString();primary.interactable=false;primaryText.text=german?"BEREIT MACHEN":"GET READY";secondaryText.text=german?"ABBRECHEN":"CANCEL";break;
            case DriftRacePhase.Paused:
                message.text=(german?"RENNEN PAUSIERT":"RACE PAUSED")+"  "+run.TotalScore.ToString("N0");primaryText.text=german?"WEITERFAHREN":"RESUME";secondaryText.text=german?"ZUM ÜBUNGSPLATZ":"TO PRACTICE";break;
            case DriftRacePhase.Finished:
                message.text=(run.NewRecord?(german?"NEUER REKORD!  ":"NEW RECORD!  "):(german?"ZIEL!  ":"FINISH!  "))+run.FinalScore.ToString("N0")+" "+(german?"PUNKTE":"POINTS");
                primaryText.text=german?"NOCH EIN RENNEN":"RACE AGAIN";secondaryText.text=german?"ZUM PARKPLATZ":"TO PARKING";break;
        }
        restartText.text=german?"NEU STARTEN":"RESTART RACE";restart.interactable=restartRace!=null;
        ApplyFocus();
    }
    internal void HandleInput(DriftRaceRun run,float x,float y,bool confirm,bool back,double now) {
        var action=navigation.Step(run.Phase,x,y,confirm,back,now);ApplyFocus();
        if(action==DriftEventMenuAction.Accept)accept();else if(action==DriftEventMenuAction.Cancel)cancel();else if(action==DriftEventMenuAction.Restart)restartRace?.Invoke();
    }
    internal void ResetNavigation()=>navigation.Reset();
    void ApplyFocus() {
        if(primary==null)return;
        primary.targetGraphic.color=navigation.Selected==0&&primary.interactable?new Color(.32f,.46f,.19f,1):new Color(.22f,.27f,.22f,1);
        secondary.targetGraphic.color=navigation.Selected==(paused?2:1)&&secondary.interactable?new Color(.32f,.46f,.19f,1):new Color(.22f,.27f,.22f,1);
        restart.targetGraphic.color=navigation.Selected==1&&paused?new Color(.32f,.46f,.19f,1):new Color(.22f,.27f,.22f,1);
    }
    bool Create(Hud hud) {
        var source=hud.m_buildSelection??hud.m_hoverName;canvas=hud.GetComponentInParent<Canvas>()?.rootCanvas;
        if(source==null||canvas==null)return false;
        var font=Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f=>f.name.IndexOf("Norse",StringComparison.OrdinalIgnoreCase)>=0)??source.font;
        if(font==null)return false;
        root=new GameObject("NFVU2 Drift Event",typeof(RectTransform)).GetComponent<RectTransform>();root.SetParent(canvas.transform,false);
        root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);root.sizeDelta=new Vector2(800,540);
        var background=root.gameObject.AddComponent<Image>();background.color=new Color(.035f,.045f,.04f,.98f);background.raycastTarget=true;
        title=Label("Title",font,34,new Color(1,.84f,.43f),28,18,744,46);
        var art=new GameObject("Measured course preview",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));art.transform.SetParent(root,false);
        var image=art.GetComponent<RawImage>();preview=ParkadeMapDrawing.Texture();image.texture=preview;image.raycastTarget=false;Place(image.rectTransform,62,80,328,328);
        info=Label("Event details",font,24,Color.white,456,86,320,180);
        record=Label("Record",font,28,new Color(.4f,.95f,.66f),456,275,320,106);
        message=Label("State",font,24,new Color(1,.84f,.43f),28,414,744,48);
        primary=ActionButton("Start",font,28,477,358,45,accept,out primaryText);
        secondary=ActionButton("Back",font,414,477,358,45,cancel,out secondaryText);
        restart=ActionButton("Restart",font,281,477,238,45,()=>restartRace?.Invoke(),out restartText);restartText.fontSize=20;
        return true;
    }
    TextMeshProUGUI Label(string name,TMP_FontAsset font,float size,Color color,float x,float y,float w,float h) {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(root,false);var text=go.AddComponent<TextMeshProUGUI>();
        text.font=font;text.fontSharedMaterial=font.material;text.fontSize=size;text.color=color;text.raycastTarget=false;text.richText=false;
        text.textWrappingMode=TextWrappingModes.Normal;text.overflowMode=TextOverflowModes.Truncate;Place(text.rectTransform,x,y,w,h);return text;
    }
    Button ActionButton(string name,TMP_FontAsset font,float x,float y,float w,float h,Action action,out TextMeshProUGUI label) {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button));go.transform.SetParent(root,false);
        var image=go.GetComponent<Image>();image.color=new Color(.22f,.27f,.22f,1);Place(image.rectTransform,x,y,w,h);
        var button=go.GetComponent<Button>();button.targetGraphic=image;button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick.AddListener(()=>action());
        label=Label(name+" text",font,23,Color.white,x+12,y+5,w-24,h-7);label.alignment=TextAlignmentOptions.Center;
        label.transform.SetParent(go.transform,false);Place(label.rectTransform,12,5,w-24,h-7);
        return button;
    }
    static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    internal void Clear(){loading.Clear();startCue.Clear();navigation.Reset();if(root!=null)UnityEngine.Object.Destroy(root.gameObject);if(preview!=null)UnityEngine.Object.Destroy(preview);root=null;preview=null;owner=null;canvas=null;}
}
}
