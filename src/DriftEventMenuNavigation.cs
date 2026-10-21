using System;

namespace Rachel350Z {
internal enum DriftEventMenuAction {None,Accept,Cancel,Restart}

// The event's native buttons use our existing gamepad input, rather than
// depending on Valheim's currently selected EventSystem object.
internal sealed class DriftEventMenuNavigation {
    internal int Selected {get;private set;}
    bool initialized,confirmHeld,backHeld,waitNeutral;
    int lastDirection;
    double repeatAt;
    DriftRacePhase phase;
    internal static bool IsPause(DriftRacePhase phase)=>phase==DriftRacePhase.Paused||phase==DriftRacePhase.PracticePaused;
    internal static bool PrimaryEnabled(DriftRacePhase phase)=>phase==DriftRacePhase.Preview||IsPause(phase)||phase==DriftRacePhase.Finished;
    internal static bool SecondaryEnabled(DriftRacePhase phase)=>PrimaryEnabled(phase)||phase==DriftRacePhase.Countdown;
    internal void Reset(){initialized=false;Selected=0;}
    internal DriftEventMenuAction Step(DriftRacePhase current,float x,float y,bool confirm,bool back,double now) {
        bool primary=PrimaryEnabled(current),secondary=SecondaryEnabled(current);
        if(!initialized||phase!=current) {
            initialized=true;phase=current;Selected=primary?0:secondary?1:-1;
            confirmHeld=confirm;backHeld=back;waitNeutral=true;lastDirection=0;
            return DriftEventMenuAction.None;
        }
        bool submit=confirm&&!confirmHeld,cancel=back&&!backHeld;
        confirmHeld=confirm;backHeld=back;
        if(!primary&&!secondary)return DriftEventMenuAction.None;
        // The countdown has no visible menu selection. A is also the driving
        // handbrake, so only the explicit Back/Escape action may cancel here.
        if(current==DriftRacePhase.Countdown)return cancel?DriftEventMenuAction.Cancel:DriftEventMenuAction.None;
        int direction=Math.Abs(y)>.55f?(y>0?-1:1):Math.Abs(x)>.55f?(x>0?1:-1):0;
        if(direction==0){lastDirection=0;waitNeutral=false;}
        else if(!waitNeutral&&(direction!=lastDirection||now>=repeatAt)) {
            bool first=direction!=lastDirection;lastDirection=direction;repeatAt=now+(first?.35:.14);
            int count=IsPause(current)?3:2;
            Selected=primary&&secondary?(Selected+direction+count)%count:primary?0:1;
        }
        if(cancel&&secondary)return IsPause(current)?DriftEventMenuAction.Accept:DriftEventMenuAction.Cancel;
        if(submit) {
            if(IsPause(current))return Selected==0?DriftEventMenuAction.Accept:Selected==1?DriftEventMenuAction.Restart:DriftEventMenuAction.Cancel;
            return Selected==0&&primary?DriftEventMenuAction.Accept:Selected==1&&secondary?DriftEventMenuAction.Cancel:DriftEventMenuAction.None;
        }
        return DriftEventMenuAction.None;
    }
}

internal static class DriftLoadingAnimation {
    internal static float Brightness(double unscaledTime,int segment) {
        if(double.IsNaN(unscaledTime)||double.IsInfinity(unscaledTime))unscaledTime=0;
        int step=(int)(Math.Floor(Math.Max(0,unscaledTime)/.14)%3);
        int age=(step-segment+3)%3;
        return age==0?1:age==1?.65f:.25f;
    }
}
}
