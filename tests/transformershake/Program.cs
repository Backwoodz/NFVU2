using System;
using Rachel350Z;
class Program {
    static int checks;
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;Console.WriteLine("PASS "+label);}
    static bool Quiet(TransformerStepShake s)=>s.Pitch==0&&s.Roll==0;
    static float Pulse(float run) {
        var s=new TransformerStepShake();s.Step(.01f,true,0,0,run);
        float maximum=0;
        for(int i=0;i<100;i++){s.Step(.01f,true,1,0,run);maximum=Math.Max(maximum,Math.Abs(s.Pitch));}
        if(!Quiet(s))throw new Exception("impulse never settles");
        return maximum;
    }
    static void Rehearse(bool sprint) {
        var g=new TransformerGait();var s=new TransformerStepShake();
        const float dt=1f/120;float speed=0,maxPitch=0;uint landings=0;
        for(int i=0;i<16*120;i++) {
            bool requested=i>=120&&i<10*120;
            speed=TransformerGait.AdvanceSpeed(speed,requested?1:0,dt,g.Ready,sprint);
            bool left=g.FootGrounded(false),right=g.FootGrounded(true);
            uint before=g.LeftFootfalls+g.RightFootfalls;
            g.Step(dt,speed,0,requested);
            uint expected=(uint)((!left&&g.FootGrounded(false)?1:0)+(!right&&g.FootGrounded(true)?1:0));
            if(g.LeftFootfalls+g.RightFootfalls-before!=expected)throw new Exception("footfall diverged from contact transition");
            landings+=expected;
            s.Step(dt,true,g.LeftFootfalls,g.RightFootfalls,g.RunBlend);
            maxPitch=Math.Max(maxPitch,Math.Abs(s.Pitch));
            if(i<120&&(!Quiet(s)||before!=0))throw new Exception("idle generated footsteps");
        }
        Check(landings>8&&g.Settled&&Quiet(s),sprint?"actual sprint touchdowns trigger shake and settle after stopping":"actual walking touchdowns trigger shake without idle events");
        Check(maxPitch>0&&maxPitch<.3f,"camera pulse remains subtle with the actual "+(sprint?"sprint":"walk")+" controller");
        g.Reset();Check(g.LeftFootfalls==0&&g.RightFootfalls==0,"gait reset clears landing counters");
    }
    static void Main() {
        float walk=Pulse(0),run=Pulse(1);
        Check(walk>0&&walk<.2f&&Math.Abs(run/walk-1.8f)<.001f,"sprint landing is 80 percent stronger than the small walking impulse");
        var s=new TransformerStepShake();s.Step(.01f,true,10,20,0);
        Check(Quiet(s),"binding an existing gait never replays past footsteps");
        s.Step(.04f,true,11,20,0);float left=s.Roll;
        s.Reset();s.Step(.01f,true,0,0,0);s.Step(.04f,true,0,1,0);
        Check(left>0&&s.Roll<0,"left and right footsteps tilt in opposite directions");
        s.Step(.02f,false,0,2,1);Check(Quiet(s),"menu, focus loss and transformation gating clear the pulse immediately");
        s.Step(.02f,true,0,2,1);Check(Quiet(s),"resuming cannot replay a blocked footstep");
        s.Step(.02f,true,1,2,1);Check(!Quiet(s),"fresh landing after resume still shakes");
        s.Stop();Check(Quiet(s),"camera blocking removes residual rotation");
        foreach(float dt in new[]{0,-1,float.NaN,float.PositiveInfinity})s.Step(dt,true,2,3,1);
        Check(Quiet(s),"paused and invalid time cannot produce a pulse");
        s.Step(.02f,true,3,4,float.NaN);Check(Quiet(s),"invalid sprint blend cannot poison the camera");
        s.Step(.02f,true,0,0,0);Check(Quiet(s),"a reset controller does not produce a false landing");
        s.Step(.02f,true,50,50,1);Check(Math.Abs(s.Pitch)<.3f,"multiple contacts between frames do not stack unbounded camera shake");
        s.Reset();Check(Quiet(s),"exit or camera rebinding clears all offsets");
        Rehearse(false);Rehearse(true);
        Console.WriteLine(checks+" Transformer footstep shake checks passed; camera feel requires the game test.");
    }
}
