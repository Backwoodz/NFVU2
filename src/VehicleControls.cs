using System;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Rachel350Z {
public partial class Plugin {
    internal static Plugin ControlsOwner;
    Harmony controlsHarmony;
    ConfigEntry<string> controllerStyle,uiLanguage;
    ConfigEntry<float> stickDeadzone,controllerSensitivity,hintScale;
    ConfigEntry<bool> controllerInvertY;
    Gamepad pad;
    Gamepad previousPad;Vector2 previousLeft,previousRight,previousDpad;float previousGas,previousReverse;
    bool padActive,handbrakeHeld,turretHeld,exitAfterLanding;
    readonly ReleaseGate controlsGate=new ReleaseGate();
    string songToast;float songToastUntil;VehicleRadio toastRadio;int toastTrack=-1;
    void InitializeControls() {
        uiLanguage=Config.Bind("Interface","Language","DE",new ConfigDescription("Interface language.",new AcceptableValueList<string>("DE","EN")));
        controllerStyle=Config.Bind("Controls","ControllerStyle","Auto",new ConfigDescription("Button labels; override for controllers exposed as Xbox by input wrappers.",new AcceptableValueList<string>("Auto","Xbox","PlayStation")));
        stickDeadzone=Config.Bind("Controls","StickDeadzone",.16f,new ConfigDescription("Controller stick deadzone.",new AcceptableValueRange<float>(.05f,.4f)));
        controllerSensitivity=Config.Bind("Camera","ControllerSensitivity",120f,new ConfigDescription("Controller orbit degrees per second.",new AcceptableValueRange<float>(30,300)));
        controllerInvertY=Config.Bind("Camera","ControllerInvertY",false,"Invert controller vertical camera movement.");
        hintScale=Config.Bind("Interface","HintScale",1f,new ConfigDescription("Control hint text scale.",new AcceptableValueRange<float>(.8f,1.5f)));
        ControlsOwner=this;controlsHarmony=new Harmony("local.rachel350z.controls");controlsHarmony.PatchAll(typeof(NativeControlCapture));
        controlsHarmony.PatchAll(typeof(NativeHotbarNavigation));
        controlsHarmony.PatchAll(typeof(NativeHotbarClick));
        controlsHarmony.PatchAll(typeof(NativeHotbarUse));
        controlsHarmony.PatchAll(typeof(NativeMenuLookCapture));
        controlsHarmony.PatchAll(typeof(NativeVehicleCameraShake));
        InitializeHordeGame();
    }
    internal bool CaptureCameraShake(GameCamera camera)=>camera!=null&&!camera.enabled&&(camera==gameCamera||camera==menuCamera);
    void ReleaseCameraRumble(MonoBehaviour camera) {
        if(camera is GameCamera native&&CaptureCameraShake(native)) {
            native.GetAttenuatedShakeIntensity(); // Also clear the stored shake before restoring native control.
            Gamepad.current?.ResetHaptics();
        }
    }
    bool Playstation=>controllerStyle.Value=="PlayStation"||controllerStyle.Value=="Auto"&&pad!=null&&(pad.layout.IndexOf("Dual",StringComparison.OrdinalIgnoreCase)>=0||(pad.description.manufacturer??"").IndexOf("Sony",StringComparison.OrdinalIgnoreCase)>=0);
    string ButtonName(string xbox,string ps)=>Playstation?ps:xbox;
    bool PadDown(string name) {
        if(pad==null)return false;
        switch(name){case "menu":return pad.leftStickButton.wasPressedThisFrame;case "back":return pad.buttonEast.wasPressedThisFrame;case "confirm":return pad.buttonSouth.wasPressedThisFrame;case "prev":return pad.dpad.left.wasPressedThisFrame;case "next":return pad.dpad.right.wasPressedThisFrame;case "up":return pad.dpad.up.wasPressedThisFrame;case "down":return pad.dpad.down.wasPressedThisFrame;case "leftCategory":return pad.leftShoulder.wasPressedThisFrame;case "rightCategory":return pad.rightShoulder.wasPressedThisFrame;default:return false;}
    }
    void SampleControls() {
        pad=Gamepad.current;
        if(pad!=null&&!pad.added)pad=null;
        if(pad!=previousPad){controlsGate.Block();padActive=false;previousLeft=previousRight=previousDpad=Vector2.zero;previousGas=previousReverse=0;previousPad=pad;}
        if(pad==null){if(padActive)controlsGate.Block();padActive=false;return;}
        var left=pad.leftStick.ReadUnprocessedValue();var right=pad.rightStick.ReadUnprocessedValue();var dpad=pad.dpad.ReadValue();float gas=pad.rightTrigger.ReadValue(),reverse=pad.leftTrigger.ReadValue();
        bool meaningful=pad.buttonSouth.wasPressedThisFrame||pad.buttonEast.wasPressedThisFrame||pad.buttonWest.wasPressedThisFrame||pad.buttonNorth.wasPressedThisFrame||pad.leftShoulder.wasPressedThisFrame||pad.rightShoulder.wasPressedThisFrame||pad.leftStickButton.wasPressedThisFrame||pad.rightStickButton.wasPressedThisFrame||(dpad-previousDpad).sqrMagnitude>.1f||left.magnitude>stickDeadzone.Value&&(left-previousLeft).magnitude>.025f||right.magnitude>stickDeadzone.Value&&(right-previousRight).magnitude>.025f||gas>.08f&&Mathf.Abs(gas-previousGas)>.025f||reverse>.08f&&Mathf.Abs(reverse-previousReverse)>.025f;
        previousLeft=left;previousRight=right;previousDpad=dpad;previousGas=gas;previousReverse=reverse;
        if(meaningful)padActive=true;
        if((Keyboard.current?.anyKey.wasPressedThisFrame??false)||Input.mouseScrollDelta.y!=0||Input.GetAxisRaw("Mouse X")!=0||Input.GetAxisRaw("Mouse Y")!=0)padActive=false;
    }
    bool ControlsHeld()=>pad!=null&&(pad.rightTrigger.ReadValue()>.06f||pad.leftTrigger.ReadValue()>.06f||pad.buttonWest.isPressed||pad.buttonSouth.isPressed||pad.leftShoulder.isPressed||pad.rightShoulder.isPressed)||Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.Space)||Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.LeftControl)||Input.GetMouseButton(0)||Input.GetMouseButton(1);
    void SelectControlMode(int mode) {
        if(HordeVehicle){if(mode!=0)ShowVehicleWarning("Hordenmodus: nur Gelände verfügbar.");return;}
        if(driving&&!DriverInputReady||IsHummer&&mode==2)return;
        int previousMode=CurrentControlMode;
        exitAfterLanding=false;
        if(mode==3)SelectFlightMode();
        else {
            driftMode.Value=mode==2;terrainMode.Value=mode==0;
            if(Flying)current.flight.Land();else ApplyDriveMode();
            status=DriveModeName;
        }
        // Announce only accepted changes, across keyboard, controller and menu.
        // Use the requested ground mode during landing, not the flight phase label.
        int selectedMode=CurrentControlMode;
        if(selectedMode!=previousMode&&MessageHud.instance!=null) {
            string name=selectedMode==3?"Fliegen":selectedMode==2?"Drift":selectedMode==0?"Gelände":"Straße";
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center,Tr("Fahrmodus")+": '"+Tr(name)+"'",log:false);
        }
    }
    int CurrentControlMode=>HordeVehicle?0:Flying&&!current.flight.State.Landing?3:!IsHummer&&driftMode.Value?2:terrainMode.Value?0:1;
    void TickControlActions(MonoBehaviour local) {
        if(Input.GetKeyDown(KeyCode.F7)||PadDown("back")) {
            if(driving){if(Flying){exitAfterLanding=true;current.flight.Land();status="Landung zum Aussteigen …";}else RequestExit();}
            else if(CanAccessCurrent(local))Enter(local);
            controlsGate.Block();
        }
        if(!driving||!DriverInputReady)return;
        for(int i=0;i<4;i++)if(Input.GetKeyDown(KeyCode.Alpha1+i))SelectControlMode(i);
        if(PadDown("up"))SelectControlMode(ControlRules.CycleMode(CurrentControlMode,1,IsHummer));
        if(PadDown("down"))SelectControlMode(ControlRules.CycleMode(CurrentControlMode,-1,IsHummer));
        if(vehicleRadio!=null&&radioEnabled.Value) {
            if(PadDown("prev")||Input.GetKeyDown(KeyCode.LeftArrow))vehicleRadio.Previous();
            if(PadDown("next")||Input.GetKeyDown(KeyCode.RightArrow))vehicleRadio.Next();
        }
        if(exitAfterLanding&&!Flying&&body.linearVelocity.magnitude<=.5f){exitAfterLanding=false;RequestExit();}
    }
    void ReadVehicleControls() {
        handbrakeHeld=turretHeld=false;
        if(!controlsGate.Allow(ControlsHeld())){BlockDrivingInput();return;}
        if(padActive&&pad!=null) {
            float gas=ControlRules.Axis(pad.rightTrigger.ReadValue(),.05f),reverse=ControlRules.Axis(pad.leftTrigger.ReadValue(),.05f);
            if(Flying){throttle=gas-reverse;brake=0;current.flightVertical=(pad.rightShoulder.isPressed?1:0)-(pad.leftShoulder.isPressed?1:0);}
            else {ControlRules.Pedals(gas,reverse,Vector3.Dot(body.linearVelocity,body.transform.forward),out float pedalThrottle,out float pedalBrake);throttle=pedalThrottle;brake=pedalBrake;current.flightVertical=0;}
            steer=ControlRules.Axis(pad.leftStick.ReadUnprocessedValue().x,stickDeadzone.Value);
            handbrakeHeld=!Flying&&pad.buttonSouth.isPressed;
            if(handbrakeHeld&&!EffectiveDrift)brake=1;
            burnoutHeld=!Flying&&gas>.8f&&reverse>.8f;
            nitroHeld=nitroEnabled.Value&&pad.buttonWest.isPressed;
            turretHeld=!Flying&&pad.leftShoulder.isPressed;honking=pad.rightStickButton.isPressed;
            var look=pad.rightStick.ReadUnprocessedValue();look=new Vector2(ControlRules.Axis(look.x,stickDeadzone.Value),ControlRules.Axis(look.y,stickDeadzone.Value));
            orbitCamera.ControllerFrame(look,controllerSensitivity.Value,controllerInvertY.Value);
        }else {
            burnoutHeld=!Flying&&Input.GetKey(KeyCode.W)&&Input.GetKey(KeyCode.S);
            throttle=(Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0);steer=(Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0);
            brake=!Flying&&Input.GetKey(KeyCode.Space)?1:0;handbrakeHeld=!Flying&&Input.GetKey(KeyCode.Space);
            honking=Input.GetKey(KeyCode.H);turretHeld=!Flying&&Input.GetMouseButton(1);
            nitroHeld=ReadBoostInput();orbitCamera.InputFrame(true,mouseSensitivity.Value,invertMouseY.Value);
            current.flightVertical=FlightState.Axis(Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift),Input.GetKey(KeyCode.Space),Flying);
        }
    }
    internal bool CaptureHotbar(Player player)=>ready&&(driving||menuOpen)&&player!=null&&player==LocalPlayer()&&!Multiplayer();
    internal bool CaptureNative(PlayerController controller) {
        if(!ready||controller.GetComponent<Player>()!=LocalPlayer()||Multiplayer())return false;
        if(driving||menuOpen)return true;
        var p=Gamepad.current;
        return !UIVisible()&&Application.isFocused&&p!=null&&(p.leftStickButton.isPressed||p.buttonEast.isPressed&&CanAccessCurrent(LocalPlayer()));
    }
    void TickSongToast() {
        if(!driving||vehicleRadio==null){toastRadio=null;toastTrack=-1;return;}
        if(vehicleRadio.Paused||!radioEnabled.Value||!vehicleRadio.CabinAudible)return;
        if(toastRadio!=vehicleRadio||toastTrack!=vehicleRadio.CurrentIndex){toastRadio=vehicleRadio;toastTrack=vehicleRadio.CurrentIndex;songToast=vehicleRadio.CurrentTitle;songToastUntil=Time.unscaledTime+5;}
    }
}
[HarmonyPatch(typeof(PlayerController),"FixedUpdate")]
static class NativeControlCapture {
    static bool Prefix(PlayerController __instance) {
        if(Plugin.ControlsOwner==null||!Plugin.ControlsOwner.CaptureNative(__instance))return true;
        __instance.GetComponent<Player>().SetControls(Vector3.zero,false,false,false,false,false,false,false,false,false,false);
        return false;
    }
}
}
