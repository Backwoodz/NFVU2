using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    int uiFocus,uiCount,uiLastCount,uiHorizontal,uiVertical,uiRegion;bool uiActivate,uiWaitNeutral;float uiRepeat;int uiLastDirection;
    readonly MenuFocusState uiNavigation=new MenuFocusState();
    readonly List<MenuControlBounds> uiBounds=new List<MenuControlBounds>();
    bool uiBuilding,uiPointerInRegion=true;
    void ChangeMenuTab(int tab){if(tab!=menuTab)laserSettingsOpen=false;menuTab=tab;uiFocus=0;uiActivate=false;uiHorizontal=uiVertical=0;uiNavigation.SelectPane(uiNavigation.Sidebar);uiBounds.Clear();uiLastCount=0;}
    void FocusMenuPane(bool sidebar){uiNavigation.SelectPane(sidebar);uiActivate=false;uiHorizontal=uiVertical=0;uiWaitNeutral=true;MenuSound(0);}
    void MoveMenuFocus(int direction) {
        if(uiNavigation.Sidebar) {
            if(direction<=2){ChangeMenuTab(ControlRules.MenuNextTab(menuTab,direction==1?-1:1,MenuNames.Length,CanAccessCurrent(LocalPlayer())));MenuSound(0);}
            else if(direction==3)FocusMenuPane(false);
        }else if(uiNavigation.Editing) {
            if(direction<=2)uiVertical=direction==1?-1:1;else uiHorizontal=direction==3?1:-1;
        }else if(direction<=2){uiFocus=ControlRules.MenuMove(uiFocus,direction==1?-1:1,uiLastCount);MenuSound(0);}
        else {
            int next=ControlRules.MenuHorizontalNeighbor(uiFocus,direction==3?1:-1,uiBounds.ToArray());
            if(next>=0){uiFocus=next;MenuSound(0);}else if(direction==4)FocusMenuPane(true);
        }
    }
    void TickControllerMenu() {
        if(PadDown("back")){
            if(uiNavigation.Editing)uiNavigation.Back();
            else if(menuTab==9&&laserSettingsOpen&&!uiNavigation.Sidebar){laserSettingsOpen=false;uiFocus=0;}
            else if(uiNavigation.Back())CloseVehicleMenu();
            uiActivate=false;uiHorizontal=uiVertical=0;uiWaitNeutral=true;return;
        }
        if(PadDown("leftCategory")||PadDown("rightCategory")) {
            FocusMenuPane(PadDown("leftCategory"));return;
        }
        if(!padActive||pad==null)return;
        var stick=pad.leftStick.ReadValue();var dpad=pad.dpad.ReadValue();
        float x=Mathf.Abs(dpad.x)>.5f?dpad.x:stick.x,y=Mathf.Abs(dpad.y)>.5f?dpad.y:stick.y;
        int direction=Mathf.Abs(y)>.5f?(y>0?1:2):Mathf.Abs(x)>.5f?(x>0?3:4):0;
        if(direction==0){uiLastDirection=0;uiRepeat=0;uiWaitNeutral=false;}
        else if(!uiWaitNeutral&&(direction!=uiLastDirection||Time.unscaledTime>=uiRepeat)) {
            bool first=direction!=uiLastDirection;uiLastDirection=direction;uiRepeat=Time.unscaledTime+(first?.35f:.12f);
            MoveMenuFocus(direction);
        }
        if(PadDown("confirm")){if(uiNavigation.Sidebar){uiNavigation.Confirm(false);uiWaitNeutral=true;}else uiActivate=true;MenuSound(0);}
    }
    void DrawMenuFocus(Rect rect,bool editing=false) {
        var old=GUI.color;GUI.color=editing?Color.white:MenuAccent;
        GUI.DrawTexture(new Rect(rect.x-3,rect.y-3,rect.width+6,2),Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x-3,rect.yMax+1,rect.width+6,2),Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x-3,rect.y-3,2,rect.height+6),Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax+1,rect.y-3,2,rect.height+6),Texture2D.whiteTexture);
        GUI.color=old;
    }
    bool UiFocused(Rect rect,bool adjustable=false) {
        if(!uiBuilding||!GUI.enabled)return false;
        int index=uiCount++;
        if(Event.current.type==EventType.MouseDown&&Event.current.button==0&&uiPointerInRegion&&rect.Contains(Event.current.mousePosition)) {
            uiFocus=index;uiNavigation.SelectPane(false);uiActivate=false;uiHorizontal=uiVertical=0;uiWaitNeutral=true;
        }
        if(Event.current.type==EventType.Repaint){var point=GUIUtility.GUIToScreenPoint(rect.center);float height=Mathf.Abs(GUIUtility.GUIToScreenPoint(new Vector2(rect.x,rect.yMax)).y-GUIUtility.GUIToScreenPoint(rect.position).y);uiBounds.Add(new MenuControlBounds(point.x,point.y,height,uiRegion));}
        bool focus=padActive&&!uiNavigation.Sidebar&&index==uiFocus;
        if(focus&&Event.current.type==EventType.Repaint) {
            if(uiActivate&&adjustable){uiNavigation.Confirm(true);uiActivate=false;uiWaitNeutral=true;}
            DrawMenuFocus(rect,uiNavigation.Editing);
        }
        return focus&&Event.current.type==EventType.Repaint;
    }
    bool UiPress(bool focus){if(!focus||!uiActivate)return false;uiActivate=false;GUI.changed=true;return true;}
    int UiDirection(bool focus,int columns=1){if(!focus||!uiNavigation.Editing)return 0;int d=uiHorizontal!=0?uiHorizontal:uiVertical*columns;uiHorizontal=uiVertical=0;if(d!=0){GUI.changed=true;MenuSound(0);}return d;}
    bool UiButton(Rect rect,string text,GUIStyle style=null,bool translate=true) {
        bool focus=UiFocused(rect);string label=translate?Tr(text):text;return (style==null?GUI.Button(rect,label):GUI.Button(rect,label,style))||UiPress(focus);
    }
    bool UiToggle(Rect rect,bool value,string text) {
        bool focus=UiFocused(rect);bool result=GUI.Toggle(rect,value,Tr(text));return UiPress(focus)?!value:result;
    }
    float UiSlider(Rect rect,float value,float min,float max,float step=0) {
        bool focus=UiFocused(rect,true);float result=GUI.HorizontalSlider(rect,value,min,max);
        return Mathf.Clamp(result+UiDirection(focus,-1)*(step>0?step:(max-min)/40),min,max);
    }
    int UiToolbar(Rect rect,int selected,string[] names) {
        if(names.Length==0)return selected;
        bool focus=UiFocused(rect,true);int result=GUI.Toolbar(rect,selected,names.Select(Tr).ToArray());
        int d=UiDirection(focus);return d==0?result:ControlRules.MenuMove(Math.Max(0,selected),d,names.Length);
    }
    int UiGrid(Rect rect,int selected,string[] names,int columns,GUIStyle style=null) {
        if(names.Length==0)return selected;
        bool focus=UiFocused(rect,true);var translated=names.Select(Tr).ToArray();
        int result=style==null?GUI.SelectionGrid(rect,selected,translated,columns):GUI.SelectionGrid(rect,selected,translated,columns,style);
        int d=UiDirection(focus,columns);return d==0?result:ControlRules.MenuMove(Math.Max(0,selected),d,names.Length);
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
