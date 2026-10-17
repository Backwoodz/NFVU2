using System;
using BepInEx.Configuration;
using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    ConfigEntry<bool> transformerEnabled;
    ConfigEntry<bool> transformerCinematic;
    ConfigEntry<string> transformerMinigunAmmo;
    ConfigEntry<KeyboardShortcut> transformerKey;
    bool transformerExitPending;
    bool transformerEntryPending;
    bool transformerSprintHeld;
    bool Transforming=>current.transformer!=null&&current.transformer.Active;
    string TransformerToggleHint=>padActive?ButtonName("RB","R1"):transformerKey.Value.ToString();
    bool TransformerToggleDown()=>transformerKey.Value.IsDown()||ControlRules.ControllerTransform(PadDown("transform"),driving,Flying);
    void InitializeTransformer() {
        TransformerFootstepGrass.Initialize(controlsHarmony,message=>Logger.LogInfo(message));
        transformerEnabled=Config.Bind("TransformerPrototype","Enabled",false,"Enable the local robot playtest. E or B/Circle boards and dismounts by backflip. F10 or RB/R1 transforms while stopped and boarded. Single-player.");
        transformerKey=Config.Bind("TransformerPrototype","ToggleKey",new KeyboardShortcut(KeyCode.F10),"Transform or return while stopped. Keyboard toggle; RB/R1 also toggles when boarded and on the ground.");
        transformerCinematic=Config.Bind("TransformerPrototype","CinematicCamera",true,"Cinematic camera during car-to-robot deployment. Mouse look/right stick takes control; return transformation uses the normal camera.");
        transformerMinigunAmmo=Config.Bind("TransformerPrototype","MinigunAmmo","Explosiv",new ConfigDescription("Robot minigun ammunition. Explosiv adds the existing impact blast and small terrain craters.",new AcceptableValueList<string>("Normal","Explosiv")));
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
        if(transformerCinematic.Value)orbitCamera.BeginTransformationShot();
        cabinDriver.BeginTransformer();vehicleNeon?.Tick(false,0);
        transformerExitPending=transformerSprintHeld=false;
        status=current.transformer.WalkingReady?"Transformer: "+TransformerToggleHint+" zurueckverwandeln (im Stand).":
            "Transformer-Standmodus: "+TransformerToggleHint+" zurueckverwandeln, "+(padActive?ButtonName("B","Kreis"):"E")+" aussteigen.";
    }
    void ReadTransformerControls() {
        if(VehicleAccessDown(LocalPlayer())) {
            RequestRobotExit();return;
        }
        if(TransformerToggleDown()){ToggleTransformer();return;}
        if(Input.GetMouseButtonDown(0))padActive=false;
        ReadVehicleControls();nitroHeld=burnoutHeld=honking=handbrakeHeld=false;current.flightVertical=0;
        bool allowed=controlsGate.Allow(ControlsHeld());
        // The car retains RMB; the robot uses LMB with the same release gate.
        if(!padActive||pad==null)turretHeld=allowed&&Input.GetMouseButton(0);
        if(allowed&&padActive&&pad!=null)throttle=ControlRules.Axis(pad.rightTrigger.ReadValue(),.05f)-ControlRules.Axis(pad.leftTrigger.ReadValue(),.05f);
        transformerSprintHeld=allowed&&throttle>0&&(padActive&&pad!=null?pad.buttonWest.isPressed:Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift));
        if(!allowed||!DriverInputReady||!current.transformer.State.CanWalk||!current.transformer.WalkingReady||current.transformer.ReturnRequested||transformerExitPending){throttle=steer=0;brake=1;transformerSprintHeld=turretHeld=false;}
    }
    string[] TransformerHints() {
        string[] hints=current.transformer!=null&&!current.transformer.WalkingReady?new[]{
        "Standmodus",
        padActive?"RS  Kamera":"Maus  Kamera",
        TransformerToggleHint+"  Zurueckverwandeln",
        padActive?ButtonName("B","Kreis")+"  Backflip absteigen":"E  Backflip absteigen",
        "Einzelspieler-Prototyp"}:new[]{
        padActive?"RT / LT  Gehen vor / zurueck":"W / S  Gehen vor / zurueck",
        padActive?ButtonName("X","Quadrat")+" halten  Rennen":"Shift + W  Rennen",
        padActive?"LS  Drehen":"A / D  Drehen",
        padActive?"RS  Kamera":"Maus  Kamera",
        TransformerToggleHint+"  Zurueckverwandeln (im Stand)",
        padActive?ButtonName("B","Kreis")+"  Backflip absteigen":"E  Backflip absteigen",
        "Einzelspieler-Prototyp"};
        if(current.transformerMinigun!=null) {
            Array.Resize(ref hints,hints.Length+1);
            hints[hints.Length-1]=padActive?ButtonName("LB","L1")+"  Minigun":"LMB  Minigun | R  Nachladen";
        }
        return hints;
    }
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
            // Dismount marks mounting before the last swing lands. Keep those genuine
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
            orbitCamera.TransformationFrame(Time.deltaTime,t.Active,t.State.Returning||t.ReturnRequested||!transformerCinematic.Value,t.State.Progress,blocked);
            orbitCamera.RobotFootsteps(Time.deltaTime,t.State.CanWalk&&!blocked&&!t.Mounting,
                t.Visual.LeftFootfalls,t.Visual.RightFootfalls,t.Visual.RunBlend);
        }
        if(t.Active) {
            t.Visual.SetNeon(NeonColors[Math.Max(0,Array.IndexOf(ColorNames,neonColor.Value))],neonEnabled.Value,neonPulsing.Value,
                NeonColors[Math.Max(0,Array.IndexOf(ColorNames,trunkNeonColor.Value))],trunkNeonEnabled.Value,trunkNeonPulsing.Value,Time.time);
            if(rider) {
                if(TickRobotMount(t,blocked))return;
                orbitCamera.LateFrame(car.transform.forward*t.Speed,dynamicCamera.Value,cameraFollow.Value,blocked,
                    current.transformerMinigun!=null&&t.State.CanWalk);
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
