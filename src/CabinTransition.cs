using System;
namespace Rachel350Z {
// Deterministic door/seat timing, independent of Unity and frame rate.
public enum CabinPhase { Approach, OpenEnter, Enter, CloseEnter, Seated, OpenExit, Exit, Complete }
public sealed class CabinTransition {
    public CabinPhase Phase {get;private set;}=CabinPhase.Approach;
    public float TimeInPhase {get;private set;}
    public bool CanDrive=>Phase==CabinPhase.Seated;
    public bool DoorOpen=>Phase==CabinPhase.OpenEnter||Phase==CabinPhase.Enter||Phase==CabinPhase.OpenExit||Phase==CabinPhase.Exit;
    // Once seated, the menu owns the door again. Only the entry/exit sequence forces its target.
    public void ApplyDoorRequest(ShowroomState doors){if(!CanDrive)doors.Open[1]=DoorOpen;}
    public float SeatBlend=>Phase==CabinPhase.Enter?Smooth(TimeInPhase/1.4f):Phase==CabinPhase.Exit?1-Smooth(TimeInPhase/1.4f):
        Phase==CabinPhase.CloseEnter||Phase==CabinPhase.Seated||Phase==CabinPhase.OpenExit?1:0;
    public void Arrive(){if(Phase==CabinPhase.Approach){Phase=CabinPhase.OpenEnter;TimeInPhase=0;}}
    public bool BeginExit(){if(!CanDrive)return false;Phase=CabinPhase.OpenExit;TimeInPhase=0;return true;}
    public void CancelExit(){if(Phase==CabinPhase.OpenExit||Phase==CabinPhase.Exit||Phase==CabinPhase.Complete){Phase=CabinPhase.Seated;TimeInPhase=0;}}
    public void Step(float dt,bool blocked,float doorProgress) {
        if(blocked||float.IsNaN(dt)||float.IsInfinity(dt)||dt<=0)return;
        if(Phase==CabinPhase.Approach||Phase==CabinPhase.Seated||Phase==CabinPhase.Complete)return;
        TimeInPhase+=Math.Min(dt,.1f);
        bool next=Phase==CabinPhase.OpenEnter||Phase==CabinPhase.OpenExit?doorProgress>=.97f:
            Phase==CabinPhase.CloseEnter?doorProgress<=.03f:TimeInPhase>=1.4f;
        if(!next)return;
        Phase=Phase==CabinPhase.OpenEnter?CabinPhase.Enter:Phase==CabinPhase.Enter?CabinPhase.CloseEnter:
            Phase==CabinPhase.CloseEnter?CabinPhase.Seated:Phase==CabinPhase.OpenExit?CabinPhase.Exit:CabinPhase.Complete;
        TimeInPhase=0;
    }
    public static float Smooth(float value){value=Math.Max(0,Math.Min(1,value));return value*value*(3-2*value);}
}
}
