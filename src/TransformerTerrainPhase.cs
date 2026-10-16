using System;
namespace Rachel350Z {
public enum TransformerTerrainStage {Flat,Open,Align,Move,AlignBack,Close}
// Keep the closed vehicle panels terrain-parallel. Only an open, stationary
// robot may align upright or lower back onto its original transformation plane.
public sealed class TransformerTerrainPhase {
    public const float AlignDuration=.4f;
    float elapsed;
    public TransformerTerrainStage Stage {get;private set;}
    public float Alignment {get;private set;}=1;
    public bool ReturnStarted {get;private set;}
    public bool HoldReady=>Stage==TransformerTerrainStage.Open||Stage==TransformerTerrainStage.Align||Stage==TransformerTerrainStage.Move||Stage==TransformerTerrainStage.AlignBack;
    public bool CanMove=>!ReturnStarted&&(Stage==TransformerTerrainStage.Flat||Stage==TransformerTerrainStage.Move);
    public bool CanRetract {get;private set;}
    public void Reset(bool required) {
        Stage=required?TransformerTerrainStage.Open:TransformerTerrainStage.Flat;
        Alignment=required?0:1;elapsed=0;ReturnStarted=false;CanRetract=false;
    }
    // A walking robot can meet a slope after a flat-ground deployment. It is
    // already upright and open: do not replay preparation or tilt it mid-step.
    public void ActivateUpright() {
        if(Stage!=TransformerTerrainStage.Flat||ReturnStarted)return;
        Stage=TransformerTerrainStage.Move;Alignment=1;elapsed=0;CanRetract=false;
    }
    public void Step(float dt,bool ready,bool feetSettled,bool returning,bool blocked=false,bool closed=false) {
        if(blocked||float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0)return;
        dt=Math.Min(dt,.1f);CanRetract=false;
        if(Stage==TransformerTerrainStage.Flat) {
            CanRetract=feetSettled&&closed;
            ReturnStarted=returning&&feetSettled;
            return;
        }
        if(returning&&feetSettled&&!ReturnStarted&&(ready||Alignment==0)) {
            // Reverse an unfinished align continuously if return was requested
            // immediately after deployment. Closing still waits for flat IK.
            ReturnStarted=true;Stage=TransformerTerrainStage.AlignBack;
            elapsed=(1-Alignment)*AlignDuration;
        }
        if(Stage==TransformerTerrainStage.Open) {
            if(ready){Stage=TransformerTerrainStage.Align;elapsed=0;}
        } else if(Stage==TransformerTerrainStage.Align||Stage==TransformerTerrainStage.AlignBack) {
            elapsed=Math.Min(AlignDuration,elapsed+dt);
            float t=elapsed/AlignDuration;
            Alignment=Stage==TransformerTerrainStage.Align?t:1-t;
            if(elapsed>=AlignDuration) {
                bool back=Stage==TransformerTerrainStage.AlignBack;
                Stage=back?TransformerTerrainStage.Close:TransformerTerrainStage.Move;
                Alignment=back?0:1;
            }
        }
        if(Stage==TransformerTerrainStage.Close)CanRetract=feetSettled&&closed;
    }
}
}
