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
        var bass=new BassSignal();var lowTone=new float[4096];var highTone=new float[4096];var stereo=new float[8192];
        for(int i=0;i<lowTone.Length;i++) {
            lowTone[i]=.25f*(float)Math.Sin(i*2*Math.PI*90/48000);
            highTone[i]=.25f*(float)Math.Sin(i*2*Math.PI*3000/48000);
            stereo[2*i]=lowTone[i];stereo[2*i+1]=-lowTone[i];
        }
        var original=(float[])lowTone.Clone();float low=bass.Read(lowTone,1,48000);bass.Reset();float high=bass.Read(highTone,1,48000);
        Check(low>high*6,"music analysis responds to bass rather than treble");
        bool untouched=true;for(int i=0;i<lowTone.Length;i++)untouched&=lowTone[i]==original[i];
        Check(untouched,"bass analysis leaves playback samples untouched");
        bass.Reset();float antiPhase=bass.Read(stereo,2,48000);
        Check(Math.Abs(low-antiPhase)<.0001f,"opposite-phase stereo does not cancel woofer motion");
        bass.Reset();Check(bass.Read(new float[2048],2,48000)==0,"silent music produces no bass energy");
        Check(bass.Read(lowTone,0,48000)==0&&bass.Read(lowTone,2,0)==0,"invalid stream layout cannot divide by zero");
        var motion=new BassMotion();float minimum=1,maximum=-1;
        for(int i=0;i<60;i++){float value=motion.Step(1f/60,low,true);minimum=Math.Min(minimum,value);maximum=Math.Max(maximum,value);}
        Check(minimum<-.002f&&maximum>.002f&&minimum>=-.0141f&&maximum<=.0141f,"real bass drives bounded inward and outward membrane excursion");
        for(int i=0;i<120;i++)motion.Step(1f/60,low,false);
        Check(motion.Offset==0,"pause mute or stopped playback returns membrane to rest");
        motion.Step(.02f,low,true);motion.Reset();Check(motion.Offset==0,"track changes clear old bass motion");
        Check(BassMotion.ConeWeight(0,0,1,.2f)==1,"membrane centre follows full excursion");
        Check(BassMotion.ConeWeight(.2f,0,1,.2f)==0&&BassMotion.ConeWeight(0,0,-1,.2f)==0,"speaker rim and rear housing remain fixed");
        float surround=BassMotion.ConeWeight(.15f,0,1,.2f);Check(surround>0&&surround<1,"surround blends membrane movement into fixed rim");
        Console.WriteLine(checks+" showroom and bass checks passed; Unity rendering and audio callbacks still need a game test.");
    }
}
