using System;
using Rachel350Z;
class ShowroomTests {
    static int checks;
    static void Check(bool value,string text){if(!value)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
    static void Main() {
        var state=new ShowroomState();
        Check(!state.Toggle(0,false)&&state.Progress[0]==0,"opening requires a stationary car");
        Check(state.Toggle(0,true),"left door opens independently");
        for(int i=0;i<5;i++)state.Step(.1f,false,0);
        Check(Math.Abs(state.Progress[0]-.5f)<.001f&&state.Progress[1]==0,"motion advances smoothly while other door stays closed");
        Check(state.Toggle(0,false),"closing remains possible when moving");
        state.Step(.1f,false,0);
        Check(Math.Abs(state.Progress[0]-.4f)<.001f,"direction reversal starts at current pose");
        state.Toggle(0,true);state.Toggle(1,true);
        for(int i=0;i<20;i++)state.Step(.1f,false,0);
        Check(state.Progress[0]==1&&state.Progress[1]==1,"both doors stop exactly at final sample");
        state.Step(.1f,false,1);
        Check(state.DoorStyle==0&&state.Progress[0]>.8f&&!state.Open[0],"changing hinge style closes using the previous animation");
        for(int i=0;i<20;i++)state.Step(.1f,false,1);
        Check(state.DoorStyle==1&&state.Progress[0]==0&&state.Progress[1]==0,"new hinge activates only with both doors shut");
        for(int i=0;i<4;i++)state.Toggle(i,true);
        for(int i=0;i<20;i++)state.Step(.1f,false,1);
        state.Step(.1f,true,1);
        Check(Array.TrueForAll(state.Open,v=>!v)&&Array.TrueForAll(state.Progress,v=>v<1),"driving closes doors hood and hatch");
        state.CloseAll();for(int i=0;i<20;i++)state.Step(.1f,false,1);
        Check(Array.TrueForAll(state.Progress,v=>v==0),"closed poses never drift below zero");
        state.Toggle(3,true);state.Step(float.NaN,false,1);state.Step(float.PositiveInfinity,false,1);
        Check(state.Progress[3]==0,"invalid delta time cannot poison animations");
        state.Step(4,false,1);
        Check(state.Progress[3]<=.1f,"long frame is clamped without snapping open");
        Check(!state.Toggle(-1,true)&&!state.Toggle(4,true),"invalid part selection ignored");
        Console.WriteLine(checks+" showroom state checks passed; Unity rendering still needs a game test.");
    }
}
