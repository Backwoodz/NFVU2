using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    readonly List<Texture2D> menuTextures=new List<Texture2D>();
    GUISkin menuSkin;
    GUIStyle navStyle,navSelectedStyle;
    VehicleMenuAssets menuAssets;
    ConfigEntry<float> menuVolume;
    int hoveredTab=-1;
    static readonly string[] MenuNames={"Fahrzeug","Radio","Licht","Neon","Nitro","Räder","Türen","Ausbau","Anzeige","Turret","Garage"};
    static readonly Color MenuAccent=new Color(.83f,.96f,.22f);
    Texture2D MenuTexture(Color color) {
        var texture=new Texture2D(1,1);texture.SetPixel(0,0,color);texture.Apply();menuTextures.Add(texture);return texture;
    }
    void InitializeMenuTheme() {
        menuVolume=Config.Bind("Menu","SoundVolume",.5f,new ConfigDescription("Original NFS frontend navigation sound volume. Zero mutes menu sounds.",new AcceptableValueRange<float>(0,1)));
    }
    void EnsureMenuAssets() {
        if(menuAssets==null)menuAssets=VehicleMenuAssets.Load(Path.Combine(Path.GetDirectoryName(Info.Location),"menu"),message=>Logger.LogInfo(message));
    }
    void MenuSound(int sound){if(!audioBlocked)menuAssets?.Play(sound,menuVolume.Value);}
    bool MenuButton(Rect rect,string text,GUIStyle style=null) {
        bool pressed=style==null?GUI.Button(rect,text):GUI.Button(rect,text,style);
        if(pressed)MenuSound(1);return pressed;
    }
    void BuildMenuSkin() {
        menuSkin=Instantiate(GUI.skin);
        var normal=MenuTexture(new Color(.11f,.15f,.19f));
        var hover=MenuTexture(new Color(.19f,.25f,.29f));
        var selected=MenuTexture(MenuAccent);
        var button=menuSkin.button;button.fontSize=14;button.fontStyle=FontStyle.Bold;
        button.border=new RectOffset();button.margin=new RectOffset(2,2,2,2);
        button.normal.background=normal;button.normal.textColor=Color.white;
        button.hover.background=hover;button.hover.textColor=MenuAccent;
        button.active.background=selected;button.active.textColor=Color.black;
        button.onNormal.background=button.onHover.background=button.onActive.background=selected;
        button.onNormal.textColor=button.onHover.textColor=button.onActive.textColor=Color.black;
        button.focused.background=hover;button.focused.textColor=MenuAccent;
        button.onFocused.background=selected;button.onFocused.textColor=Color.black;
        menuSkin.toggle.fontSize=15;menuSkin.toggle.normal.textColor=menuSkin.toggle.onNormal.textColor=Color.white;
        menuSkin.horizontalSlider.normal.background=normal;menuSkin.horizontalSlider.fixedHeight=6;
        menuSkin.horizontalSlider.border=new RectOffset();
        foreach(var state in new[]{menuSkin.horizontalSliderThumb.normal,menuSkin.horizontalSliderThumb.hover,menuSkin.horizontalSliderThumb.active})state.background=selected;
        menuSkin.horizontalSliderThumb.fixedWidth=14;menuSkin.horizontalSliderThumb.fixedHeight=16;
        menuSkin.horizontalSliderThumb.border=new RectOffset();
        navStyle=new GUIStyle(button){alignment=TextAnchor.MiddleLeft,fontSize=14,padding=new RectOffset(47,4,0,0)};
        navSelectedStyle=new GUIStyle(navStyle);navSelectedStyle.normal.background=selected;navSelectedStyle.normal.textColor=Color.black;
        navSelectedStyle.hover.background=selected;navSelectedStyle.hover.textColor=Color.black;
        songButtonStyle=new GUIStyle(button){fontSize=13,wordWrap=true,alignment=TextAnchor.MiddleLeft};
    }
    void MenuNavigation(bool accessible) {
        var e=Event.current;
        if(e.type==EventType.KeyDown&&(e.keyCode==KeyCode.UpArrow||e.keyCode==KeyCode.DownArrow)) {
            menuTab=accessible?(menuTab+(e.keyCode==KeyCode.DownArrow?1:10))%11:10;MenuSound(0);e.Use();
        }
        int hover=-1;
        for(int i=0;i<MenuNames.Length;i++) {
            var rect=new Rect(12,104+i*35,166,31);GUI.enabled=accessible||i==10;
            if(GUI.enabled&&rect.Contains(e.mousePosition))hover=i;
            if(GUI.Button(rect,MenuNames[i],i==menuTab?navSelectedStyle:navStyle)){menuTab=i;MenuSound(1);}
            var icon=menuAssets?.Icons[i];
            if(icon!=null)GUI.DrawTexture(new Rect(18,105+i*35,36,29),icon,ScaleMode.ScaleToFit,true);
            else GUI.Label(new Rect(24,108+i*35,30,23),i==9?"◎":"›",mutedStyle);
        }
        GUI.enabled=true;
        if(e.type==EventType.Repaint&&hover!=hoveredTab){if(hover>=0)MenuSound(0);hoveredTab=hover;}
    }
    void DisposeMenuTheme() {
        menuAssets?.Dispose();menuAssets=null;
        if(menuSkin!=null)Destroy(menuSkin);menuSkin=null;
        foreach(var texture in menuTextures)Destroy(texture);menuTextures.Clear();
    }
}
}
