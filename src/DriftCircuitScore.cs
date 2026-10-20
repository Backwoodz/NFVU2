using UnityEngine;
namespace Rachel350Z {
public partial class Plugin {
    readonly DriftLapScore driftLapScore=new DriftLapScore();
    ZDOMan driftScoreWorld;
    Game driftScoreGame;
    long driftScoreWorldId;
    VehicleRuntime driftScoreVehicle;
    int driftScoreSavedRevision;
    float driftScoreRetry;
    bool DriftCircuitScoreVisible=>DriftWorldRules.Current&&Player.m_localPlayer!=null&&driving&&car!=null&&!IsHummer&&!Flying&&!Transforming;

    // Call once for the selected vehicle, outside ForEachVehicle. Rendering and
    // frame menus may stop elsewhere; this adapter still suspends their scoring.
    void TickDriftCircuitScore() {
        var local=Player.m_localPlayer;
        if(!DriftWorldRules.Current||local==null||ZNet.instance==null||ZDOMan.instance==null
            ||ZoneSystem.instance==null||Game.instance==null||Game.instance.IsShuttingDown()||Multiplayer()) {
            ClearDriftCircuitScore();return;
        }
        long worldId=ZNet.instance.GetWorldUID();
        if(driftScoreWorld!=ZDOMan.instance||driftScoreGame!=Game.instance||driftScoreWorldId!=worldId) {
            ClearDriftCircuitScore();driftScoreWorld=ZDOMan.instance;driftScoreGame=Game.instance;driftScoreWorldId=worldId;
            ZoneSystem.instance.GetGlobalKey(DriftLapRecord.Key,out var stored);
            DriftLapRecord.Decode(stored,out var best,out var fastest);driftLapScore.LoadRecord(best,fastest);
        }
        // A completed lap belongs to this world even after the driver exits or
        // opens a menu. Retry before any driving/UI early return; never before
        // the world identity above has been checked.
        SavePendingDriftCircuitRecord();
        if(DriftRaceOwnsVehicle){driftLapScore.Interrupt();driftScoreVehicle=null;return;}
        if(!DriftCircuitScoreVisible||body==null||!EffectiveDrift||local.IsDead()||local.IsTeleporting()) {
            driftLapScore.Interrupt();driftScoreVehicle=null;return;
        }
        if(driftScoreVehicle!=current){driftLapScore.Interrupt();driftScoreVehicle=current;}
        if(!DriverInputReady||audioBlocked||UIVisible()||menuOpen||!Application.isFocused||Time.timeScale<=0) {
            driftLapScore.Pause();return;
        }
        var position=body.position;var velocity=body.linearVelocity;var forward=body.rotation*Vector3.forward;
        int contacts=0;
        if(DriftPhysicsActive)for(int i=0;i<4;i++){if(current.driftPhysics.Grounded(i))contacts++;}
        else if(wheels!=null)foreach(var wheel in wheels)if(wheel!=null&&wheel.isGrounded)contacts++;
        // Flat-world ground height rejects flight, launch and vertical teleports
        // even if an out-of-date tyre contact is reported for one update.
        bool grounded=contacts>=2&&Mathf.Abs(position.y-DriftWorldRules.GroundHeight)<3;
        driftLapScore.Step(new DriftLapSample(position.x,position.z,velocity.x,velocity.z,forward.x,forward.z,Time.deltaTime,grounded));
        SavePendingDriftCircuitRecord();
    }
    void SavePendingDriftCircuitRecord() {
        if(driftLapScore.RecordRevision==driftScoreSavedRevision||Time.unscaledTime<driftScoreRetry||ZRoutedRpc.instance==null)return;
        try {
            ZoneSystem.instance.SetGlobalKey(DriftLapRecord.Key+" "+DriftLapRecord.Encode(driftLapScore.BestScore,driftLapScore.BestLapTime));
            driftScoreSavedRevision=driftLapScore.RecordRevision;
        } catch(System.Exception error) {
            driftScoreRetry=Time.unscaledTime+5;
            Logger.LogWarning("Drift lap record could not be saved yet: "+error.Message);
        }
    }
    void ClearDriftCircuitScore() {
        driftLapScore.Clear();driftScoreWorld=null;driftScoreGame=null;driftScoreWorldId=0;
        driftScoreVehicle=null;driftScoreSavedRevision=0;driftScoreRetry=0;
    }
}
}
