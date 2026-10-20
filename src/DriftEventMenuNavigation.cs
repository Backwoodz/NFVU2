using System;

namespace Rachel350Z {
internal enum DriftEventMenuAction {None,Accept,Cancel}

// The event's native buttons use our existing gamepad input, rather than
// depending on Valheim's currently selected EventSystem object.
internal sealed class DriftEventMenuNavigation {
    internal int Selected {get;private set;}
    bool initialized,confirmHeld,backHeld,waitNeutral;
    int lastDirection;
    double repeatAt;
    DriftRacePhase phase;
    internal static bool PrimaryEnabled(DriftRacePhase phase)=>phase==DriftRacePhase.Preview||phase==DriftRacePhase.Paused||phase==DriftRacePhase.Finished;
    internal static bool SecondaryEnabled(DriftRacePhase phase)=>PrimaryEnabled(phase)||phase==DriftRacePhase.Loading||phase==DriftRacePhase.Countdown;
    internal void Reset(){initialized=false;Selected=0;}
    internal DriftEventMenuAction Step(DriftRacePhase current,float x,float y,bool confirm,bool back,double now) {
        bool primary=PrimaryEnabled(current),secondary=SecondaryEnabled(current);
        if(!initialized||phase!=current) {
            initialized=true;phase=current;Selected=primary?0:1;
            confirmHeld=confirm;backHeld=back;waitNeutral=true;lastDirection=0;
            return DriftEventMenuAction.None;
        }
        bool submit=confirm&&!confirmHeld,cancel=back&&!backHeld;
        confirmHeld=confirm;backHeld=back;
        if(!primary&&!secondary)return DriftEventMenuAction.None;
        int direction=Math.Abs(y)>.55f?(y>0?-1:1):Math.Abs(x)>.55f?(x>0?1:-1):0;
        if(direction==0){lastDirection=0;waitNeutral=false;}
        else if(!waitNeutral&&(direction!=lastDirection||now>=repeatAt)) {
            bool first=direction!=lastDirection;lastDirection=direction;repeatAt=now+(first?.35:.14);
            Selected=primary&&secondary?1-Selected:primary?0:1;
        }
        if(cancel&&secondary)return DriftEventMenuAction.Cancel;
        if(submit)return Selected==0&&primary?DriftEventMenuAction.Accept:Selected==1&&secondary?DriftEventMenuAction.Cancel:DriftEventMenuAction.None;
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
