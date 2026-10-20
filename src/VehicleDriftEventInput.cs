using HarmonyLib;
using UnityEngine;

namespace Rachel350Z {
public partial class Plugin {
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
    // Native Menu.Update runs before this plugin and consumes Escape. Reserve
    // Show only while our own event panel owns this live seated driver's UI;
    // normal pause, garage, other worlds and teardown retain native behavior.
    internal bool CaptureDriftEventMenu() {
        var local=Player.m_localPlayer;
        return driftRaceUi&&DriftSessionRules.Active&&!Multiplayer()
            &&Game.instance!=null&&!Game.instance.IsShuttingDown()
            &&driftEventGame==Game.instance&&ZDOMan.instance!=null&&driftEventWorld==ZDOMan.instance
            &&local!=null&&!local.IsDead()&&!local.IsTeleporting()
            &&driving&&car!=null&&body!=null&&current==driftEventVehicle;
    }
}
[HarmonyPatch(typeof(Menu),"Show")]
internal static class DriftEventNativeMenu {
    static bool Prefix()=>Plugin.ControlsOwner?.CaptureDriftEventMenu()!=true;
}
}
