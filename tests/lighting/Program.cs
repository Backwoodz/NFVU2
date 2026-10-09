using System;
using Rachel350Z;
static class Program {
    static int checks;
    static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;Console.WriteLine("PASS "+message);}
    static VehicleLightingState Park() {
        var state=new VehicleLightingState{ShowOffEnabled=true};
        state.Update(true,false);state.Update(false,false);return state;
    }
    static VehicleLightingState At(float time) {
        var state=Park();state.Update(false,false,deltaTime:time);return state;
    }
    static void Main() {
        var state=new VehicleLightingState();
        state.Update(false,false);
        Check(!state.Enabled&&!state.ShowOffEnabled&&!state.LeftHeadlightOn&&!state.RightHeadlightOn,"fresh car stays dark and show defaults off");
        state.ShowOffEnabled=true;state.Update(false,false,deltaTime:10);
        Check(!state.ShowOffActive,"show waits until the car has been entered and exited");
        state.Update(true,false,deltaTime:1);
        Check(state.Enabled&&state.LeftHeadlightOn&&state.RightHeadlightOn&&!state.ShowOffActive,"entry uses both normal headlights despite enabled show");
        state.Enabled=false;state.Update(true,false);
        Check(!state.LeftHeadlightOn&&!state.RightHeadlightOn,"manual light override still works while driving");
        state.Update(false,false,deltaTime:.3f);
        Check(state.ShowOffActive&&state.LeftHeadlightOn&&!state.RightHeadlightOn&&!state.Enabled,"exit starts first left flash even with main lights off");
        Check(state.HeadlightOn(-.6f)&&!state.HeadlightOn(.6f),"left uses vehicle-local negative X rather than reversed NFS names");
        Check(!state.BrakeLightsOn&&!state.RearLightsOn&&!state.ReverseLightsOn,"show does not flash rear brake or reverse lights");
        Check(!At(.22f).LeftHeadlightOn&&!At(.22f).RightHeadlightOn,"dark gap between left flashes");
        Check(At(.36f).LeftHeadlightOn&&!At(.36f).RightHeadlightOn,"second left flash");
        Check(!At(.56f).LeftHeadlightOn&&!At(.56f).RightHeadlightOn,"dark gap before right pair");
        Check(!At(.72f).LeftHeadlightOn&&At(.72f).RightHeadlightOn,"first right flash");
        Check(!At(.88f).LeftHeadlightOn&&!At(.88f).RightHeadlightOn,"dark gap between right flashes");
        Check(!At(1.02f).LeftHeadlightOn&&At(1.02f).RightHeadlightOn,"second right flash");
        Check(!At(1.7f).LeftHeadlightOn&&!At(1.7f).RightHeadlightOn,"long dark pause after both pairs");
        Check(At(2.45f).LeftHeadlightOn&&!At(2.45f).RightHeadlightOn,"sequence repeats with left after pause");
        Check(At(2400.05f).LeftHeadlightOn,"long frame skips completed cycles without losing timing");
        state=At(.72f);state.Update(false,false,deltaTime:5,paused:true);
        Check(state.RightHeadlightOn&&!state.LeftHeadlightOn,"pause or focus loss freezes the current phase");
        foreach(float dt in new[]{0,-1,float.NaN,float.PositiveInfinity})state.Update(false,false,deltaTime:dt);
        Check(state.RightHeadlightOn,"invalid or zero time cannot corrupt flash phase");
        state.Update(false,false,deltaTime:.16f);
        Check(!state.LeftHeadlightOn&&!state.RightHeadlightOn,"resume continues after the paused phase");
        state.ShowOffEnabled=false;state.Update(false,false);
        Check(!state.ShowOffActive&&!state.LeftHeadlightOn&&!state.RightHeadlightOn,"disabling show immediately restores parked lights");
        state.Enabled=true;state.Update(false,false);
        Check(state.LeftHeadlightOn&&state.RightHeadlightOn&&state.RearLightsOn,"parked steady lighting still works without show");
        state.ShowOffEnabled=true;state.Update(false,false);
        Check(state.ShowOffActive&&state.LeftHeadlightOn&&!state.RightHeadlightOn&&state.RearLightsOn,"enabling beside a previously driven car restarts show without affecting rear lights");
        state.Update(true,true,forwardSpeed:0,throttle:-1);
        Check(!state.ShowOffActive&&state.LeftHeadlightOn&&state.RightHeadlightOn&&state.BrakeLightsOn&&state.ReverseLightsOn,"reentry cancels show and restores normal brake and reverse behaviour");
        state.Update(false,false);
        Check(state.LeftHeadlightOn&&!state.RightHeadlightOn,"next exit restarts at the first left flash");
        var other=new VehicleLightingState();other.Update(true,false);other.Update(false,false);
        Check(state.ShowOffActive&&!other.ShowOffActive&&!other.LeftHeadlightOn,"each vehicle owns independent show state");
        Console.WriteLine(checks+" lighting checks passed; actual light projection and menu rendering require Unity.");
    }
}
