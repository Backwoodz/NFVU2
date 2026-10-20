using HarmonyLib;
using UnityEngine;

namespace Rachel350Z {
public partial class Plugin {
    bool DriftRaceMenuShortcutDown()=>Input.GetKeyDown(KeyCode.Escape)||(pad!=null&&pad.startButton.wasPressedThisFrame);
    bool DriftPauseArea=>driftRace.UsesRaceTrack||driftRace.Phase==DriftRacePhase.PracticePaused
        ||body!=null&&ParkadeLayout.InVenueArea(body.position.x,body.position.z);
    void TickDriftEventInput() {
        Vector2 direction=Vector2.zero;
        if(padActive&&pad!=null) {
            var dpad=pad.dpad.ReadValue();var stick=pad.leftStick.ReadValue();
            direction=dpad.sqrMagnitude>.25f?dpad:stick;
        }
        if(Input.GetKey(KeyCode.LeftArrow)||Input.GetKey(KeyCode.UpArrow))direction=new Vector2(-1,0);
        else if(Input.GetKey(KeyCode.RightArrow)||Input.GetKey(KeyCode.DownArrow))direction=new Vector2(1,0);
        driftEventPanel.HandleInput(driftRace,direction.x,direction.y,
            Input.GetKey(KeyCode.Return)||(pad!=null&&pad.buttonSouth.isPressed),
            Input.GetKey(KeyCode.Escape)||(pad!=null&&pad.buttonEast.isPressed),Time.unscaledTime);
    }
    // Native Menu.Update can run before this plugin and consumes Escape. Queue
    // our pause at Parkade, including free driving there. The practice course
    // and parking retain native pause; garage remains on F9/L3.
    internal bool CaptureDriftEventMenu() {
        var local=Player.m_localPlayer;
        bool valid=DriftSessionRules.Active&&!Multiplayer()
            &&Game.instance!=null&&!Game.instance.IsShuttingDown()
            &&driftEventGame==Game.instance&&ZDOMan.instance!=null&&driftEventWorld==ZDOMan.instance
            &&local!=null&&!local.IsDead()&&!local.IsTeleporting()
            &&driving&&car!=null&&body!=null&&current!=null&&!IsHummer&&!Flying&&!Transforming
            &&(driftRace.Phase==DriftRacePhase.Practice||current==driftEventVehicle);
        if(!valid)return false;
        if(driftRaceUi)return true;
        if(!DriftPauseArea||driftRace.Phase!=DriftRacePhase.Racing&&driftRace.Phase!=DriftRacePhase.Practice)return false;
        driftPauseRequested=true;return true;
    }
}
[HarmonyPatch(typeof(Menu),"Show")]
internal static class DriftEventNativeMenu {
    static bool Prefix()=>Plugin.ControlsOwner?.CaptureDriftEventMenu()!=true;
}
}
