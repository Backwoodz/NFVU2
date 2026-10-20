using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Rachel350Z {
// Keep the actual starter visible: only a small number at the top of the HUD,
// no modal preview panel between the driver and the start gesture.
internal sealed class DriftStartCue {
    RectTransform root;
    TextMeshProUGUI number,hint;
    Hud owner;
    internal void Tick(Hud hud,DriftRaceRun run,bool german) {
        if(owner!=hud){Clear();owner=hud;}
        bool counting=run.Phase==DriftRacePhase.Countdown;
        bool go=run.Phase==DriftRacePhase.Racing&&run.LaunchElapsed<.7f;
        if(hud==null||!counting&&!go){if(root!=null)root.gameObject.SetActive(false);return;}
        if(root==null&&!Create(hud))return;
        root.gameObject.SetActive(true);
        number.text=counting?Mathf.Max(1,Mathf.CeilToInt(run.Countdown)).ToString():german?"LOS!":"GO!";
        number.color=counting?new Color(1,.86f,.45f):new Color(.64f,1,.33f);
        hint.text=counting?(german?"B / Esc: Abbrechen":"B / Esc: Cancel"):"";
    }
    bool Create(Hud hud) {
        var canvas=hud.GetComponentInParent<Canvas>()?.rootCanvas;
        var source=hud.m_buildSelection??hud.m_hoverName;
        if(canvas==null||source==null)return false;
        var font=Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f=>f.name.IndexOf("Norse",StringComparison.OrdinalIgnoreCase)>=0)??source.font;
        if(font==null)return false;
        root=new GameObject("NFVU2 Starter Countdown",typeof(RectTransform),typeof(CanvasGroup)).GetComponent<RectTransform>();
        root.SetParent(canvas.transform,false);root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,1);
        root.anchoredPosition=new Vector2(0,-30);root.sizeDelta=new Vector2(360,130);
        var group=root.GetComponent<CanvasGroup>();group.interactable=false;group.blocksRaycasts=false;
        number=Label("Start number",font,64,0,82);
        hint=Label("Start cancel hint",font,19,83,32);
        return true;
    }
    TextMeshProUGUI Label(string name,TMP_FontAsset font,float size,float y,float height) {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(root,false);
        var text=go.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSharedMaterial=font.material;text.fontSize=size;
        text.color=new Color(.9f,.93f,.83f);text.raycastTarget=false;text.richText=false;text.alignment=TextAlignmentOptions.Center;
        text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Truncate;
        var rect=text.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=new Vector2(0,-y);rect.sizeDelta=new Vector2(360,height);return text;
    }
    internal void Clear(){if(root!=null)UnityEngine.Object.Destroy(root.gameObject);root=null;number=hint=null;owner=null;}
}
}
