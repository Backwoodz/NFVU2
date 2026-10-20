using System;
using Rachel350Z;
static class Program {
    static int checks;
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);checks++;}
    static DriftArrivalCandidate Car(float distance,bool driven=false,bool hummer=false)=>new DriftArrivalCandidate(hummer,driven,distance);
    static void Main(string[] args) {
        var state=new DriftArrivalState();var first=new object();var second=new object();
        state.Begin(first,false,true);Check(!state.Pending,"death/respawn cannot initiate a parking arrival");
        state.Begin(first,true,false);Check(!state.Pending,"ordinary or network worlds never initiate arrival");
        state.Begin(first,true,true);Check(state.Pending&&!state.Prepared&&!state.LoadRequested,"first world entry initiates one arrival");
        state.Prepared=true;state.LoadRequested=true;
        state.Observe(first);Check(state.Pending&&state.Prepared&&state.LoadRequested,"streaming/player-null frames retain arrival and loading state");
        state.Begin(first,true,true);Check(state.Pending&&state.LoadRequested,"repeated native readiness calls do not reset staged loading");
        state.Complete();Check(!state.Pending&&state.Prepared,"successful seat entry completes arrival");
        for(int i=0;i<10;i++) {
            state.Observe(first);state.Begin(first,false,true);
            Check(!state.Pending,"death, dismount or removal cannot respawn the parking car "+i);
        }
        state.Begin(first,true,true);Check(!state.Pending,"even another initial-spawn poll cannot repeat an already prepared arrival");
        state.Begin(second,true,true);Check(state.Pending&&!state.Prepared&&!state.LoadRequested,"a different loaded world gets its own arrival");
        state.Prepared=true;state.LoadRequested=true;state.Complete();
        state.Observe(null);Check(!state.Pending&&!state.Prepared&&!state.LoadRequested,"logout clears state without following player instances");
        state.Begin(first,true,true);Check(state.Pending&&!state.Prepared,"reloading original world starts a fresh parking arrival");
        Check(DriftParkingPlan.X==148&&DriftParkingPlan.Z==-8&&DriftParkingPlan.Yaw==90,"parking arrival belongs to existing apron and faces the exit");
        Check(DriftParkingPlan.Choose(Array.Empty<DriftArrivalCandidate>())==-1,"empty world requests a new 350Z");
        Check(DriftParkingPlan.Choose(new[]{Car(0,hummer:true),Car(10)})==1,"nearby H2 never substitutes for a drift 350Z");
        Check(DriftParkingPlan.Choose(new[]{Car(10),Car(20),Car(5)})==2,"closest unused 350Z is reused");
        Check(DriftParkingPlan.Choose(new[]{Car(1),Car(500,true),Car(10)})==1,"previously driven 350Z is preferred over unused cars");
        Check(DriftParkingPlan.Choose(new[]{Car(100,true),Car(50,true),Car(0)})==1,"nearest previously driven 350Z returns to parking");
        Check(DriftParkingPlan.Choose(new[]{Car(20,true),Car(20,true)})==0,"tied cars use stable first record selection");
        Check(DriftParkingPlan.Choose(new[]{Car(float.NaN),Car(float.PositiveInfinity),Car(-1),Car(9)})==3,"corrupt positions cannot win car selection");
        Check(DriftParkingPlan.Choose(new[]{Car(0,hummer:true),Car(1,true,true)})==-1,"H2-only garage cannot be silently converted or deleted");
        state.LoadRequested=true;state.Complete();state.Begin(first,false,true);
        Check(!state.Pending,"failed or cancelled load stays cancelled for that world entry");
        if(args.Length>=2)ArrivalNativeContracts.Run(args[0],args[1],Check);
        Console.WriteLine(checks+" parking-arrival policy/API/wiring checks passed; not native world/loading/seat acceptance.");
    }
}
