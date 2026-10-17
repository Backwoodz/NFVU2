using System;
using BepInEx.Configuration;
using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    ConfigEntry<bool> transformerEnabled;
    ConfigEntry<KeyboardShortcut> transformerKey;
    bool transformerExitPending;
    bool transformerEntryPending;
    bool transformerSprintHeld;
    bool Transforming=>current.transformer!=null&&current.transformer.Active;
    void InitializeTransformer() {
        TransformerFootstepGrass.Initialize(controlsHarmony,message=>Logger.LogInfo(message));
        transformerEnabled=Config.Bind("TransformerPrototype","Enabled",false,"Enable the local robot playtest. F7 dismounts by backflip; near the parked robot F7 boards from any side. F10 returns to the car. Single-player.");
        transformerKey=Config.Bind("TransformerPrototype","ToggleKey",new KeyboardShortcut(KeyCode.F10),"Transform or return while stopped. Prototype keyboard toggle; movement supports keyboard/gamepad.");
    }
    void ToggleTransformer() {
        if(transformerEntryPending||transformerExitPending||cabinDriver!=null&&cabinDriver.RobotTransitionActive)return;
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
        transformerExitPending=transformerSprintHeld=false;
        status=current.transformer.WalkingReady?"Transformer: W/S gehen, Shift + W rennen, A/D drehen, F10 zurueckverwandeln.":"Transformer-Standmodus: F10 zurueckverwandeln, F7 zurueck und aussteigen.";
    }
    void ReadTransformerControls() {
        if(Input.GetKeyDown(KeyCode.F7)||PadDown("back")) {
            RequestRobotExit();return;
        }
        if(transformerKey.Value.IsDown())ToggleTransformer();
        ReadVehicleControls();nitroHeld=burnoutHeld=turretHeld=honking=handbrakeHeld=false;current.flightVertical=0;
        bool allowed=controlsGate.Allow(ControlsHeld());
        if(allowed&&padActive&&pad!=null)throttle=ControlRules.Axis(pad.rightTrigger.ReadValue(),.05f)-ControlRules.Axis(pad.leftTrigger.ReadValue(),.05f);
        transformerSprintHeld=allowed&&throttle>0&&(padActive&&pad!=null?pad.buttonWest.isPressed:Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift));
        if(!allowed||!DriverInputReady||!current.transformer.State.CanWalk||!current.transformer.WalkingReady||current.transformer.ReturnRequested||transformerExitPending){throttle=steer=0;brake=1;transformerSprintHeld=false;}
    }
    string[] TransformerHints()=>current.transformer!=null&&!current.transformer.WalkingReady?new[]{
        "Standmodus",
        padActive?"RS  Kamera":"Maus  Kamera",
        transformerKey.Value+"  Zurueckverwandeln",
        padActive?"B / Kreis  Backflip absteigen":"F7  Backflip absteigen",
        "Einzelspieler-Prototyp"}:new[]{
        padActive?"RT / LT  Gehen vor / zurueck":"W / S  Gehen vor / zurueck",
        padActive?ButtonName("X","Quadrat")+" halten  Rennen":"Shift + W  Rennen",
        padActive?"LS  Drehen":"A / D  Drehen",
        padActive?"RS  Kamera":"Maus  Kamera",
        transformerKey.Value+"  Zurueckverwandeln (im Stand)",
        padActive?"B / Kreis  Backflip absteigen":"F7  Backflip absteigen",
        "Einzelspieler-Prototyp"};
    void TickTransformerReturnTail() {
        if(current.transformerReturnAudio==null)return;
        try {
            bool blocked=audioBlocked||menuOpen||!Application.isFocused||Time.timeScale<=0
                ||current.worldStreaming!=null&&current.worldStreaming.Waiting
                ||current.parkedStreaming!=null&&current.parkedStreaming.Waiting;
            // Runs for driven and parked cars. Only CompleteReturn can arm a
            // ring, and all ordinary inactive ticks remain silent afterwards.
            current.transformerReturnAudio.Tick(Time.deltaTime,false,false,0,blocked,soundVolume.Value,car.transform.position);
        }catch(Exception e) {
            current.transformerReturnAudio.Dispose();current.transformerReturnAudio=null;
            Logger.LogWarning("Transformer return tail disabled after audio error: "+e);
        }
    }
    bool FootstepEffectsBlocked=>audioBlocked||menuOpen||!Application.isFocused||Time.timeScale<=0
        ||Time.deltaTime<=0||float.IsNaN(Time.deltaTime)||float.IsInfinity(Time.deltaTime)
        ||current.worldStreaming!=null&&current.worldStreaming.Waiting
        ||current.parkedStreaming!=null&&current.parkedStreaming.Waiting;
    void TickTransformerFootstepEffects() {
        var effect=current.transformerFootstepEffects;
        if(effect==null)return;
        try {
            effect.Tick(Time.deltaTime,FootstepEffectsBlocked);
            if(!Transforming)effect.Observe(false,true,0,0,Vector3.zero,Vector3.zero,
                Vector3.forward,Vector3.forward,Vector2.one,Vector2.one,0);
        } catch(Exception e) {
            effect.Dispose();current.transformerFootstepEffects=null;
            current.transformerFootstepEffectsFailed=true;
            Logger.LogWarning("Transformer footprint effects disabled: "+e);
        }
    }
    void ObserveTransformerFootsteps(VehicleTransformer t) {
        if(current.transformerFootstepEffectsFailed)return;
        if(!t.State.CanWalk&&current.transformerFootstepEffects==null)return;
        if(current.transformerFootstepEffects==null)
            current.transformerFootstepEffects=TransformerFootstepEffects.Create(car,message=>Logger.LogInfo(message));
        var effect=current.transformerFootstepEffects;
        if(effect==null){current.transformerFootstepEffectsFailed=true;return;}
        try {
            t.Visual.FootContact(false,out var left,out var leftForward,out var leftSize);
            t.Visual.FootContact(true,out var right,out var rightForward,out var rightSize);
            effect.Observe(t.State.CanWalk,FootstepEffectsBlocked,t.Visual.LeftFootfalls,t.Visual.RightFootfalls,
                left,right,leftForward,rightForward,leftSize,rightSize,t.Visual.RunBlend);
        } catch(Exception e) {
            effect.Dispose();current.transformerFootstepEffects=null;
            current.transformerFootstepEffectsFailed=true;
            Logger.LogWarning("Transformer footprint effects disabled: "+e);
        }
    }
    void TickTransformer() {
        var t=current.transformer;
        bool blocked=audioBlocked||menuOpen||!Application.isFocused||Time.timeScale<=0||current.worldStreaming!=null&&current.worldStreaming.Waiting;
        bool rider=driving&&cabinDriver!=null;
        t.Mounting=rider&&!t.ReturnRequested&&(transformerEntryPending||transformerExitPending||cabinDriver.RobotTransitionActive);
        if(rider&&cabinDriver.RobotTransitionActive)t.Visual.SetMountPose(cabinDriver.RobotTransitionProgress,cabinDriver.RobotTransitionEntering);
        else t.Visual.ClearMountPose();
        t.Tick(Time.deltaTime,blocked);
        ObserveTransformerFootsteps(t);
        if(current.transformerAudio!=null)try {
            // F7 marks mounting before the last swing lands. Keep those genuine
            // contacts audible; the settled mount pose creates no new footfalls.
            current.transformerAudio.Tick(Time.deltaTime,t.State.CanWalk,blocked,
                t.Visual.LeftFootfalls,t.Visual.RightFootfalls,t.Visual.RunBlend,soundVolume.Value,
                t.Visual.Node("foot.L").position,t.Visual.Node("foot.R").position);
        }catch(Exception e) {
            current.transformerAudio.Dispose();current.transformerAudio=null;
            Logger.LogWarning("Transformer footsteps disabled after audio error: "+e);
        }
        if(current.transformerTurnAudio!=null)try {
            bool turnBlocked=blocked||current.parkedStreaming!=null&&current.parkedStreaming.Waiting;
            current.transformerTurnAudio.Tick(Time.deltaTime,t.State.CanWalk,turnBlocked,
                t.ActualTurnRate,soundVolume.Value,t.Visual.Node("thorax").position);
        }catch(Exception e) {
            current.transformerTurnAudio.Dispose();current.transformerTurnAudio=null;
            Logger.LogWarning("Transformer turn audio disabled after audio error: "+e);
        }
        if(current.transformerDeployAudio!=null)try {
            bool deployBlocked=blocked||current.parkedStreaming!=null&&current.parkedStreaming.Waiting;
            current.transformerDeployAudio.Tick(Time.deltaTime,t.Active,t.State.Returning,t.State.Progress,
                deployBlocked,soundVolume.Value,t.Visual.Node("thorax").position);
        }catch(Exception e) {
            current.transformerDeployAudio.Dispose();current.transformerDeployAudio=null;
            Logger.LogWarning("Transformer deployment audio disabled after audio error: "+e);
        }
        if(current.transformerReturnAudio!=null)try {
            bool returnBlocked=blocked||current.parkedStreaming!=null&&current.parkedStreaming.Waiting;
            current.transformerReturnAudio.Tick(Time.deltaTime,t.Active,t.State.Returning,t.State.Progress,
                returnBlocked,soundVolume.Value,t.Active?t.Visual.Node("thorax").position:car.transform.position);
        }catch(Exception e) {
            current.transformerReturnAudio.Dispose();current.transformerReturnAudio=null;
            Logger.LogWarning("Transformer return audio disabled after audio error: "+e);
        }
        if(current.transformerReactorAudio!=null)try {
            bool reactorBlocked=blocked||current.parkedStreaming!=null&&current.parkedStreaming.Waiting;
            current.transformerReactorAudio.Tick(Time.deltaTime,t.Active,t.State.Returning,t.State.Progress,
                reactorBlocked,soundVolume.Value,t.Visual.ReactorWorldPosition);
        }catch(Exception e) {
            current.transformerReactorAudio.Dispose();current.transformerReactorAudio=null;
            Logger.LogWarning("Transformer reactor audio disabled after audio error: "+e);
        }
        if(t.Active&&vehicleAudio!=null)try {
            vehicleAudio.TickRobotIdle(rider||ParkedRunning,blocked,soundVolume.Value,t.State.Progress,rider);
        }catch(Exception e) {
            vehicleAudio.Dispose();vehicleAudio=null;
            Logger.LogWarning("Transformer idle disabled after audio error: "+e);
        }
        if(t.TakeBlockedReturn()) {
            transformerExitPending=false;
            ShowVehicleWarning("Hier fehlt Platz zum Zurueckverwandeln. Erst auf eine freie Flaeche gehen.");
        }
        if(rider) {
            orbitCamera.RobotBlend=t.State.Progress;
            orbitCamera.RobotFootsteps(Time.deltaTime,t.State.CanWalk&&!blocked&&!t.Mounting,
                t.Visual.LeftFootfalls,t.Visual.RightFootfalls,t.Visual.RunBlend);
        }
        if(t.Active) {
            t.Visual.SetNeon(NeonColors[Math.Max(0,Array.IndexOf(ColorNames,neonColor.Value))],neonEnabled.Value,neonPulsing.Value,
                NeonColors[Math.Max(0,Array.IndexOf(ColorNames,trunkNeonColor.Value))],trunkNeonEnabled.Value,trunkNeonPulsing.Value,Time.time);
            if(rider) {
                if(TickRobotMount(t,blocked))return;
                orbitCamera.LateFrame(car.transform.forward*t.Speed,dynamicCamera.Value,cameraFollow.Value,blocked);
            }
        } else {
            if(rider) {
                if(transformerEntryPending) {
                    // Boarding requested during an already running inverse
                    // transformation continues through the ordinary car door.
                    transformerEntryPending=transformerExitPending=false;
                    cabinDriver.Dispose();
                    var outside=lastExitPosition;
                    if(FindCabinExit(player,out var door))outside=door;
                    cabinDriver=new VehicleCabinDriver(player,car.transform,current.interior,outside);
                }
                BlockDrivingInput();cabinDriver.Tick(0,blocked,showroom.State);
            }
        }
    }
}
}
