using System;
using BepInEx.Configuration;
using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    ConfigEntry<bool> dynamicCamera,driftCamera,hudEnabled,hudMph;
    ConfigEntry<int> hudDesign;
    ConfigEntry<float> cameraFollow,hudScale;
    ConfigEntry<string> hudColor;
    static readonly string[] GaugeNames={"NFSU2 01","NFSU2 02","NFSU2 03","NFSU2 04","NFSU2 05","NFSU2 06","NFSU2 07","NFSU2 08","NFSU2 09","NFSU2 10","NFSU2 11"};
    void InitializeDrivingMenu() {
        driftMode=BindVehicle("Driving","DriftMode",false,"Optional drift setup: rear drive, lower rear lateral grip and rear handbrake on Space.");
        dynamicCamera=Config.Bind("Camera","DynamicFollow",true,"Let camera heading trail corners and partly follow travel direction; mouse look holds before recentering while moving.");
        driftCamera=Config.Bind("Camera","DriftFollow",true,"Use a softer velocity-following view in 350Z drift mode, showing the side of the car. Independent of DynamicFollow; mouse/right-stick look remains available.");
        cameraFollow=Config.Bind("Camera","FollowSpeed",2.5f,new ConfigDescription("Heading follow response; lower values create more corner lag.",new AcceptableValueRange<float>(1,6)));
        hudEnabled=Config.Bind("HUD","Enabled",true,"Show the vehicle gauges while driving.");
        hudDesign=Config.Bind("HUD","Design",0,new ConfigDescription("Original NFSU2 custom gauge index.",new AcceptableValueRange<int>(0,10)));
        hudScale=Config.Bind("HUD","Scale",1f,new ConfigDescription("Gauge size.",new AcceptableValueRange<float>(.65f,1.4f)));
        hudMph=Config.Bind("HUD","MilesPerHour",false,"Show MPH instead of KM/H.");
        hudColor=Config.Bind("HUD","Color","Cyan",new ConfigDescription("Gauge accent colour.",new AcceptableValueList<string>(ColorNames)));
        if(driftMode.Value)terrainMode.Value=false;
    }
    void ApplyDriveMode(){if(Flying)return;ReleaseDriftPhysics();if(EffectiveDrift&&!IsHummer)handling?.SetDriftMode();else handling?.SetTerrainMode(EffectiveTerrain);if(IsHummer&&body!=null)HummerTuning.Configure(body,wheels);}
    string DriveModeName=>Flying?(current.flight.State.Landing?"Flugmodus / Landung":current.flight.State.Phase==FlightPhase.Folding?"Flugmodus / Start":"Flugmodus / Schweben"):DriftPhysicsActive?"Drift / Heckantrieb":EffectiveDrift&&!IsHummer?"Drift / Heckantrieb":EffectiveTerrain?"Gelände / Allradhilfe":"Straße / Heckantrieb";
    void DrawDrivingDisplayMenu() {
        hudEnabled.Value=UiToggle(new Rect(25,142,250,28),hudEnabled.Value," Tacho anzeigen");
        hudMph.Value=UiToggle(new Rect(310,142,210,28),hudMph.Value," MPH statt KM/H");
        hudDesign.Value=UiGrid(new Rect(25,177,510,96),hudDesign.Value,GaugeNames,4);
        UiLabel(new Rect(25,280,510,24),"Größe  "+Mathf.RoundToInt(hudScale.Value*100)+" %",labelStyle);
        hudScale.Value=UiSlider(new Rect(25,311,510,18),hudScale.Value,.65f,1.4f);
        int selected=Math.Max(0,Array.IndexOf(ColorNames,hudColor.Value));
        hudColor.Value=ColorNames[UiGrid(new Rect(25,343,510,52),selected,ColorNames,4)];
        dynamicCamera.Value=UiToggle(new Rect(25,406,300,28),dynamicCamera.Value," Dynamische Verfolgerkamera");
        driftCamera.Value=UiToggle(new Rect(340,406,195,28),driftCamera.Value," Driftkamera");
        UiLabel(new Rect(25,440,510,25),"Nachziehen: weich                         direkt",mutedStyle);
        GUI.enabled=dynamicCamera.Value||driftCamera.Value&&EffectiveDrift&&!IsHummer;
        cameraFollow.Value=UiSlider(new Rect(25,470,510,18),cameraFollow.Value,1,6);GUI.enabled=true;
        UiLabel(new Rect(25,495,510,24),dashboard==null?"Originalgrafiken fehlen – einfache Anzeige aktiv.":padActive?"RS: Kamera · R3: Hupe":"Mittlere Maustaste: Kamera zentrieren",mutedStyle);
    }
    void DrawDashboard() {
        if(!driving||body==null||!hudEnabled.Value||audioBlocked||Hud.IsUserHidden()||Transforming)return;
        float speed=body.linearVelocity.magnitude,rpm=DriftPhysicsActive?current.driftPhysics.Rpm:vehicleAudio?.Rpm??950;
        int gear=DriftPhysicsActive?current.driftPhysics.Gear:vehicleAudio?.Gear??(throttle<0?-1:1);
        float charge=ActiveNitro?.Charge??0;
        bool nos=ActiveNitro!=null&&nitroEnabled.Value;
        if(dashboard==null){UiBox(new Rect(Screen.width-280,Screen.height-VehicleHintBottomInset-75,265,65),Mathf.RoundToInt(HudMath.Speed(speed,hudMph.Value))+(hudMph.Value?" MPH":" KM/H")+" | "+(gear<0?"R":"G "+gear)+"\nNOS "+(nos?Mathf.RoundToInt(charge*100)+" %":"AUS"));return;}
        dashboard.Draw(hudDesign.Value,hudScale.Value,NeonColors[Math.Max(0,Array.IndexOf(ColorNames,hudColor.Value))],hudMph.Value,speed,rpm,gear,charge,nos,ActiveNitro!=null&&ActiveNitro.Boosting,VehicleHintBottomInset,vehicleAudio?.TurboPressure??0,!IsHummer);
    }
}
}
