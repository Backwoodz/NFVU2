using System;
using System.Linq;
using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    int uiFocus,uiCount,uiLastCount,uiHorizontal;bool uiActivate;float uiRepeat;int uiLastDirection;
    bool uiBuilding;
    void ChangeMenuTab(int tab){menuTab=tab;uiFocus=0;uiActivate=false;uiHorizontal=0;}
    void TickControllerMenu() {
        if(PadDown("back")){if(laserSettingsOpen){laserSettingsOpen=false;uiFocus=0;}else CloseVehicleMenu();return;}
        if(PadDown("leftCategory")||PadDown("rightCategory")) {
            int categoryDirection=PadDown("rightCategory")?1:-1;
            int tab=(menuTab+categoryDirection+12)%12;
            if(!CanAccessCurrent(LocalPlayer()))tab=menuTab==10?11:10;
            ChangeMenuTab(tab);MenuSound(0);return;
        }
        if(!padActive||pad==null)return;
        var stick=pad.leftStick.ReadValue();var dpad=pad.dpad.ReadValue();
        float x=Mathf.Abs(dpad.x)>.5f?dpad.x:stick.x,y=Mathf.Abs(dpad.y)>.5f?dpad.y:stick.y;
        int direction=Mathf.Abs(y)>.5f?(y>0?1:2):Mathf.Abs(x)>.5f?(x>0?3:4):0;
        if(direction==0){uiLastDirection=0;uiRepeat=0;}
        else if(direction!=uiLastDirection||Time.unscaledTime>=uiRepeat) {
            bool first=direction!=uiLastDirection;uiLastDirection=direction;uiRepeat=Time.unscaledTime+(first?.35f:.12f);
            if(direction<=2){uiFocus=ControlRules.MenuMove(uiFocus,direction==1?-1:1,uiLastCount);MenuSound(0);}
            else uiHorizontal=direction==3?1:-1;
        }
        if(PadDown("confirm"))uiActivate=true;
    }
    bool UiFocused(Rect rect) {
        if(!uiBuilding||!GUI.enabled)return false;
        int index=uiCount++;bool focus=padActive&&index==uiFocus;
        if(focus&&Event.current.type==EventType.Repaint) {
            var old=GUI.color;GUI.color=MenuAccent;
            GUI.DrawTexture(new Rect(rect.x-3,rect.y-3,rect.width+6,2),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x-3,rect.yMax+1,rect.width+6,2),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x-3,rect.y-3,2,rect.height+6),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax+1,rect.y-3,2,rect.height+6),Texture2D.whiteTexture);
            GUI.color=old;
        }
        return focus&&Event.current.type==EventType.Repaint;
    }
    bool UiPress(bool focus){if(!focus||!uiActivate)return false;uiActivate=false;GUI.changed=true;return true;}
    int UiDirection(bool focus){if(!focus)return 0;int d=uiHorizontal;uiHorizontal=0;if(d!=0){GUI.changed=true;MenuSound(0);}return d;}
    bool UiButton(Rect rect,string text,GUIStyle style=null,bool translate=true) {
        bool focus=UiFocused(rect);string label=translate?Tr(text):text;return (style==null?GUI.Button(rect,label):GUI.Button(rect,label,style))||UiPress(focus);
    }
    bool UiToggle(Rect rect,bool value,string text) {
        bool focus=UiFocused(rect);bool result=GUI.Toggle(rect,value,Tr(text));return UiPress(focus)?!value:result;
    }
    float UiSlider(Rect rect,float value,float min,float max,float step=0) {
        bool focus=UiFocused(rect);float result=GUI.HorizontalSlider(rect,value,min,max);
        return Mathf.Clamp(result+UiDirection(focus)*(step>0?step:(max-min)/40),min,max);
    }
    int UiToolbar(Rect rect,int selected,string[] names) {
        if(names.Length==0)return selected;
        bool focus=UiFocused(rect);int result=GUI.Toolbar(rect,selected,names.Select(Tr).ToArray());
        int d=UiDirection(focus);if(UiPress(focus))d=1;return d==0?result:(Math.Max(0,selected)+d+names.Length)%names.Length;
    }
    int UiGrid(Rect rect,int selected,string[] names,int columns,GUIStyle style=null) {
        if(names.Length==0)return selected;
        bool focus=UiFocused(rect);var translated=names.Select(Tr).ToArray();
        int result=style==null?GUI.SelectionGrid(rect,selected,translated,columns):GUI.SelectionGrid(rect,selected,translated,columns,style);
        int d=UiDirection(focus);if(UiPress(focus))d=1;return d==0?result:(Math.Max(0,selected)+d+names.Length)%names.Length;
    }
    void UiLabel(Rect rect,string text,GUIStyle style=null){if(style==null)GUI.Label(rect,Tr(text));else GUI.Label(rect,Tr(text),style);}
    void UiBox(Rect rect,string text,GUIStyle style=null){if(style==null)GUI.Box(rect,Tr(text));else GUI.Box(rect,Tr(text),style);}
    void UiBox(Rect rect,GUIContent text,GUIStyle style){GUI.Box(rect,text,style);}
    void DrawInterfaceSettings() {
        UiLabel(new Rect(25,142,510,25),"Sprache / Language",labelStyle);
        int language=UiToolbar(new Rect(25,178,510,32),uiLanguage.Value=="EN"?1:0,new[]{"Deutsch","English"});uiLanguage.Value=language==1?"EN":"DE";
        UiLabel(new Rect(25,230,510,25),"Controller-Tastenanzeige",labelStyle);
        string[] styles={"Auto","Xbox","PlayStation"};controllerStyle.Value=styles[UiToolbar(new Rect(25,263,510,32),Math.Max(0,Array.IndexOf(styles,controllerStyle.Value)),styles)];
        UiLabel(new Rect(25,310,510,25),"Stick-Totzone",labelStyle);stickDeadzone.Value=UiSlider(new Rect(25,343,510,20),stickDeadzone.Value,.05f,.4f);
        UiLabel(new Rect(25,370,510,25),"Controller-Kameraempfindlichkeit",labelStyle);controllerSensitivity.Value=UiSlider(new Rect(25,403,510,20),controllerSensitivity.Value,30,300);
        controllerInvertY.Value=UiToggle(new Rect(25,432,510,28),controllerInvertY.Value,"Controller-Kamera Y invertieren");
        UiLabel(new Rect(25,469,510,25),"Größe der Steuerungshinweise",labelStyle);hintScale.Value=UiSlider(new Rect(25,501,510,16),hintScale.Value,.8f,1.5f);
    }
}
}
