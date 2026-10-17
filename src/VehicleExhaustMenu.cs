using System;
using BepInEx.Configuration;
using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    ConfigEntry<int> z_exhaustSound;
    ConfigEntry<int> z_exhaustStyle;
    ConfigEntry<bool> z_idleExhaustSmoke;
    ConfigEntry<bool> idleExhaustSmoke=>VehicleConfig(z_idleExhaustSmoke);
    ConfigEntry<string> z_exhaustFlameColor;
    ConfigEntry<int> exhaustSound=>VehicleConfig(z_exhaustSound);
    ConfigEntry<int> exhaustStyle=>VehicleConfig(z_exhaustStyle);
    ConfigEntry<string> exhaustFlameColor=>VehicleConfig(z_exhaustFlameColor);
    static readonly string[] ExhaustNames={"Original","Tief","Sport"};
    void InitializeExhaustMenu() {
        z_idleExhaustSmoke=BindVehicle("Exhaust","IdleSmoke",true,"Subtle NFVU2-authored exhaust vapour while idling or slowly coasting, including the robot's moving exhaust tips.");
        z_exhaustStyle=BindVehicle("Exhaust","Style",8,new ConfigDescription("Original NFS exhaust tip STYLE01..10. Requires the updated local model catalog; STYLE08 is the existing default.",new AcceptableValueRange<int>(1,10)));
        z_exhaustSound=BindVehicle("Exhaust","SoundProfile",0,new ConfigDescription("NFVU2 treatments of the installed original engine recordings: 0 original, 1 deeper, 2 sport. Not recovered NFS tuning banks.",new AcceptableValueRange<int>(0,2)));
        z_exhaustFlameColor=BindVehicle("Exhaust","FlameColor","Original",new ConfigDescription("Exhaust nitro and wheel-thruster colour. Original preserves green exhaust and blue wheel thrusters.",new AcceptableValueList<string>(ExhaustColors)));
    }
    static readonly string[] ExhaustColors={"Original","Green","Cyan","Blue","Purple","Red","Orange","White"};
    static readonly string[] ExhaustColorLabels={"Original","Grün","Cyan","Blau","Violett","Rot","Orange","Weiß"};
    static readonly Color[] ExhaustFlameColors={Color.white,new Color(.12f,1,.025f),new Color(0,1,1),new Color(.12f,.35f,1),new Color(.65f,.12f,1),new Color(1,.04f,.025f),new Color(1,.4f,.02f),Color.white};
    void TickExhaustCustomization() {
        current.exhaustVisuals?.Apply(exhaustStyle.Value);
        nitroEffects?.SetExhaustStyle(exhaustStyle.Value);
        vehicleAudio?.SetExhaustProfile(exhaustSound.Value);
        int color=Math.Max(0,Array.IndexOf(ExhaustColors,exhaustFlameColor.Value));
        nitroEffects?.SetFlameColor(color==0?new Color(.12f,1,.025f):ExhaustFlameColors[color]);
        current.flightEffects?.SetFlameColor(color==0?new Color(.12f,.65f,1):ExhaustFlameColors[color]);
    }
    void DrawExhaustMenu() {
        UiLabel(new Rect(25,140,510,25),"Motorsound / Auspuffklang",labelStyle);
        exhaustSound.Value=UiToolbar(new Rect(25,173,510,32),ExhaustSoundProfile.Clamp(exhaustSound.Value),ExhaustNames);
        UiLabel(new Rect(25,212,510,46),"Originalaufnahmen mit NFVU2-Klangvarianten.\nBeim laufenden Motor direkt hörbar.",mutedStyle);
        UiLabel(new Rect(25,270,510,25),"Nitro-Flammenfarbe",labelStyle);
        int selected=Math.Max(0,Array.IndexOf(ExhaustColors,exhaustFlameColor.Value));
        exhaustFlameColor.Value=ExhaustColors[UiGrid(new Rect(25,303,510,58),selected,ExhaustColorLabels,4)];
        UiLabel(new Rect(25,370,510,26),IsHummer?"Die Farbe gilt für die Flugdüsen des H2.":"Die Farbe gilt für Auspuffflammen und Flugdüsen.",mutedStyle);
        UiLabel(new Rect(25,410,510,25),"Originale Auspuffblenden",labelStyle);
        if(current.exhaustVisuals?.Available==true) {
            exhaustStyle.Value=1+UiGrid(new Rect(25,447,510,60),exhaustStyle.Value-1,new[]{"01","02","03","04","05","06","07","08","09","10"},5);
        }else UiLabel(new Rect(25,447,510,60),IsHummer?"H2-Endrohre: aktuelle Originaloptik.":"Auspuffblenden fehlen im lokalen Fahrzeugmodell.",mutedStyle);
    }
}
}
