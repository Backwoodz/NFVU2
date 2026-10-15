using System;
using BepInEx.Configuration;
using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    ConfigEntry<bool> transformerEnabled;
    ConfigEntry<KeyboardShortcut> transformerKey;
    bool transformerExitPending;
    bool Transforming=>current.transformer!=null&&current.transformer.Active;
    void InitializeTransformer() {
        transformerEnabled=Config.Bind("TransformerPrototype","Enabled",false,"Enable the local V18 robot test. Requires matching optional transformer assets. Single-player, flat terrain only.");
        transformerKey=Config.Bind("TransformerPrototype","ToggleKey",new KeyboardShortcut(KeyCode.F10),"Transform or return while stopped. Prototype keyboard toggle; movement supports keyboard/gamepad.");
    }
    void ToggleTransformer() {
        if(Transforming) {
            if(!current.transformer.Return())ShowVehicleWarning("Zum Zurueckverwandeln anhalten und Platz fuer das Auto schaffen.");
            BlockDrivingInput();return;
        }
        if(!transformerEnabled.Value){ShowVehicleWarning("Der lokale Transformer-Prototyp ist in der Konfiguration noch deaktiviert.");return;}
        if(!driving||!DriverInputReady||IsHummer||Flying||HordeVehicle||cabinDriver==null||Multiplayer()) {
            ShowVehicleWarning("Transformer: zuerst im 350Z Platz nehmen, Bodenmodus waehlen und den Einstieg abschliessen.");return;
        }
        if(showroom==null||Array.Exists(showroom.State.Progress,x=>x>.001f)){showroom?.State.CloseAll();ShowVehicleWarning("Zum Verwandeln zuerst Tueren, Haube und Kofferraum schliessen.");return;}
        if(current.transformerAsset==null){ShowVehicleWarning("Die passenden lokalen Transformer-Dateien fehlen.");return;}
        // Construct lazily, once per car. Asset parsing happened in spawn worker.
        if(current.transformer==null)current.transformer=new VehicleTransformer(current,current.transformerAsset);
        BlockDrivingInput();handling?.Step(false,0,0,1);vehicleImpact?.Step(null,false);ClearNitroVisuals();
        if(!current.transformer.Begin(player.transform,out string reason)){ShowVehicleWarning(reason);return;}
        cabinDriver.BeginTransformer();vehicleNeon?.Tick(false,0);
        transformerExitPending=false;
        status=current.transformer.WalkingReady?"Transformer: W/S gehen, A/D drehen, F10 zurueckverwandeln.":"Transformer-Standmodus: F10 zurueckverwandeln, F7 zurueck und aussteigen.";
    }
    void ReadTransformerControls() {
        if(Input.GetKeyDown(KeyCode.F7)||PadDown("back")) {
            transformerExitPending=true;
            if(!current.transformer.State.Returning&&!current.transformer.Return()) {
                transformerExitPending=false;
                ShowVehicleWarning("Zum Aussteigen erst Platz fuer die Rueckverwandlung schaffen.");
            }
        }
        if(transformerKey.Value.IsDown())ToggleTransformer();
        ReadVehicleControls();nitroHeld=burnoutHeld=turretHeld=honking=handbrakeHeld=false;current.flightVertical=0;
        if(padActive&&pad!=null)throttle=ControlRules.Axis(pad.rightTrigger.ReadValue(),.05f)-ControlRules.Axis(pad.leftTrigger.ReadValue(),.05f);
        if(!current.transformer.State.CanWalk||!current.transformer.WalkingReady||current.transformer.ReturnRequested||transformerExitPending){throttle=steer=0;brake=1;}
    }
    string[] TransformerHints()=>current.transformer!=null&&!current.transformer.WalkingReady?new[]{
        "Standmodus",
        padActive?"RS  Kamera":"Maus  Kamera",
        transformerKey.Value+"  Zurueckverwandeln",
        padActive?"B / Kreis  Zurueck & aussteigen":"F7  Zurueck & aussteigen",
        "F8  Prototyp entfernen"}:new[]{
        padActive?"RT / LT  Gehen vor / zurueck":"W / S  Gehen vor / zurueck",
        padActive?"LS  Drehen":"A / D  Drehen",
        padActive?"RS  Kamera":"Maus  Kamera",
        transformerKey.Value+"  Zurueckverwandeln (im Stand)",
        padActive?"B / Kreis  Zurueck & aussteigen":"F7  Zurueck & aussteigen",
        "F8  Prototyp entfernen"};
    void TickTransformer() {
        var t=current.transformer;
        bool blocked=audioBlocked||menuOpen||!Application.isFocused||Time.timeScale<=0;
        if(transformerExitPending&&t.State.CanWalk&&!blocked&&Mathf.Abs(t.Speed)<.05f)t.Return();
        t.Tick(Time.deltaTime,blocked);
        if(t.TakeBlockedReturn()) {
            transformerExitPending=false;
            ShowVehicleWarning("Hier fehlt Platz zum Zurueckverwandeln. Erst auf eine freie Flaeche gehen.");
        }
        orbitCamera.RobotBlend=t.State.Progress;
        if(t.Active) {
            t.Visual.SetNeon(NeonColors[Math.Max(0,Array.IndexOf(ColorNames,neonColor.Value))],neonEnabled.Value,neonPulsing.Value,
                NeonColors[Math.Max(0,Array.IndexOf(ColorNames,trunkNeonColor.Value))],trunkNeonEnabled.Value,trunkNeonPulsing.Value,Time.time);
            cabinDriver.TickTransformer(t.Visual,t.State.Progress);
            orbitCamera.LateFrame(car.transform.forward*t.Speed,dynamicCamera.Value,cameraFollow.Value,blocked);
        } else {
            BlockDrivingInput();
            cabinDriver.Tick(0,blocked,showroom.State);
            if(transformerExitPending){transformerExitPending=false;RequestExit();}
        }
    }
}
}
