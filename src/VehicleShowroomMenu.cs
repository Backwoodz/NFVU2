using System;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace Rachel350Z {
public partial class Plugin {
    ConfigEntry<int> trunkLayout,engineStyle,doorStyle;
    readonly ConfigEntry<int>[] audioSlots=new ConfigEntry<int>[6];
    readonly int[] audioOptions=new int[6];
    ConfigEntry<bool> trunkNeonEnabled,trunkNeonPulsing;
    ConfigEntry<string> trunkNeonColor;
    int selectedAudioSlot;
    void InitializeShowroomMenu() {
        trunkLayout=Config.Bind("Showroom","TrunkLayout",2,new ConfigDescription("Original 350Z trunk enclosure: 0 stock, 1 two slots, 2 four slots, 3 six slots.",new AcceptableValueRange<int>(0,3)));
        engineStyle=Config.Bind("Showroom","EngineStyle",0,new ConfigDescription("Visual engine model: 0 stock, 1 style 1, 2 style 2. Does not change performance.",new AcceptableValueRange<int>(0,2)));
        doorStyle=Config.Bind("Showroom","DoorStyle",0,new ConfigDescription("Door animation: 0 regular, 1 suicide, 2 scissor. Changes apply after closing.",new AcceptableValueRange<int>(0,2)));
        for(int i=0;i<6;i++)audioSlots[i]=Config.Bind("Showroom","AudioSlot"+(i+1),1,new ConfigDescription("Per-slot part: 0 cover, 1-4 subwoofer, 5-8 amplifier, 9-12 LCD. Size follows the enclosure.",new AcceptableValueRange<int>(0,12)));
        trunkNeonEnabled=Config.Bind("Showroom","NeonEnabled",true,"Illuminate the trunk system, independent of underbody neon.");
        trunkNeonPulsing=Config.Bind("Showroom","NeonPulsing",false,"Pulse the trunk neon; this is a visual pulse, not beat detection.");
        trunkNeonColor=Config.Bind("Showroom","NeonColor","Grün",new ConfigDescription("Trunk neon colour.",new AcceptableValueList<string>(ColorNames)));
    }
    void ApplyShowroomSettings() {
        if(showroom==null)return;
        for(int i=0;i<6;i++)audioOptions[i]=audioSlots[i].Value;
        showroom.Configure(trunkLayout.Value,engineStyle.Value,audioOptions);
    }
    bool ShowroomReady() {
        if(showroom!=null)return true;
        GUI.Label(new Rect(25,150,510,80),"Die Modelldaten enthalten noch keinen Kofferraumausbau und keine Animationen.",labelStyle);return false;
    }
    void MotionButton(Rect rect,int part,string label) {
        bool stationary=body!=null&&body.linearVelocity.magnitude<=.5f;
        GUI.enabled=stationary||showroom.State.Open[part];
        if(GUI.Button(rect,label+(showroom.State.Open[part]?" schließen":" öffnen")))showroom.State.Toggle(part,stationary);
        GUI.enabled=true;
    }
    void DrawBodyMenu() {
        if(!ShowroomReady())return;
        GUI.Label(new Rect(25,140,510,26),"Türen und Hauben",labelStyle);
        // The existing car conversion maps the NFS RIGHT mesh to Unity -X.
        MotionButton(new Rect(25,179,245,36),1,"Tür links");
        MotionButton(new Rect(285,179,245,36),0,"Tür rechts");
        MotionButton(new Rect(25,225,245,36),2,"Motorhaube");
        MotionButton(new Rect(285,225,245,36),3,"Kofferraum");
        GUI.Label(new Rect(25,280,510,26),"Türmechanik",labelStyle);
        doorStyle.Value=GUI.Toolbar(new Rect(25,317,505,36),doorStyle.Value,new[]{"Normal","Suicide","Scherentüren"});
        GUI.Label(new Rect(25,365,510,26),"Motoroptik",labelStyle);
        engineStyle.Value=GUI.Toolbar(new Rect(25,402,505,36),engineStyle.Value,new[]{"Serie","Style 1","Style 2"});
        GUI.Label(new Rect(25,455,510,62),"Öffnen im Stand. Beim Losfahren schließt alles automatisch. Eine neue Türmechanik wird nach dem Schließen aktiv.",mutedStyle);
    }
    void DrawTrunkMenu() {
        if(!ShowroomReady())return;
        trunkLayout.Value=GUI.Toolbar(new Rect(25,143,505,32),trunkLayout.Value,showroom.Layouts.Select(l=>l.title).ToArray());
        int slots=showroom.Layouts[trunkLayout.Value].sockets.Length;
        if(slots>0) {
            selectedAudioSlot=Mathf.Clamp(selectedAudioSlot,0,slots-1);
            selectedAudioSlot=GUI.Toolbar(new Rect(25,188,505,30),selectedAudioSlot,Enumerable.Range(1,slots).Select(i=>"Platz "+i).ToArray());
            var choices=showroom.Choices(trunkLayout.Value,selectedAudioSlot);
            audioSlots[selectedAudioSlot].Value=GUI.SelectionGrid(new Rect(25,225,505,128),audioSlots[selectedAudioSlot].Value,choices.Select(c=>c.title).ToArray(),4,songButtonStyle);
        } else GUI.Label(new Rect(25,205,505,80),"Serien-Kofferraum. Wähle einen Ausbau, um Subwoofer, Verstärker und Bildschirme einzusetzen.",labelStyle);
        trunkNeonEnabled.Value=GUI.Toggle(new Rect(25,363,250,28),trunkNeonEnabled.Value," Kofferraum-Neon");
        trunkNeonPulsing.Value=GUI.Toggle(new Rect(285,363,245,28),trunkNeonPulsing.Value," Pulsierend");
        int color=Math.Max(0,Array.IndexOf(ColorNames,trunkNeonColor.Value));
        int next=GUI.SelectionGrid(new Rect(25,402,505,60),color,ColorNames,4);
        if(next!=color)trunkNeonColor.Value=ColorNames[next];
        MotionButton(new Rect(25,478,245,34),3,"Kofferraum");
        if(GUI.Button(new Rect(285,478,245,34),"Standard-System")) {
            trunkLayout.Value=2;foreach(var slot in audioSlots)slot.Value=1;
            trunkNeonEnabled.Value=true;trunkNeonPulsing.Value=false;trunkNeonColor.Value="Grün";
        }
    }
}
}
