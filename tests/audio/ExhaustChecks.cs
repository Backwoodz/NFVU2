using System;
using Rachel350Z;
static class ExhaustChecks {
    static ExhaustOverrunModel Loaded(int fps=60) {
        var model=new ExhaustOverrunModel();
        for(int i=0;i<fps;i++)model.Step(1f/fps,true,5900,1,12,false);
        return model;
    }
    public static void Run(Action<bool,string> check) {
        foreach(int fps in new[]{30,60,144}) {
            var model=Loaded(fps);int pops=0;float last=1,firstTime=-1,lastTime=0;
            for(int i=0;i<fps*2;i++) {
                model.Step(1f/fps,true,5900,0,12,false);
                if(!model.Pop)continue;
                pops++;if(firstTime<0)firstTime=i/(float)fps;lastTime=i/(float)fps;
                check(model.Gain>0&&model.Gain<last&&model.Pitch>.7f&&model.Pitch<1.1f,"burble intensity decays at "+fps+" FPS");last=model.Gain;
            }
            check(pops==3&&lastTime<.6f&&firstTime<.1f,"one lift makes a bounded three-pop tail at "+fps+" FPS");
            model=Loaded(fps);pops=0;firstTime=-1;
            for(int i=0;i<fps;i++) {
                model.Step(1f/fps,true,5900,0,12,i==0);
                if(model.Pop){pops++;if(firstTime<0)firstTime=i/(float)fps;}
            }
            check(pops==2&&firstTime>=.09f,"flame bang gets room before the quieter tail at "+fps+" FPS");
        }
        foreach(float speed in new[]{0f,-8f,1f}) {
            var model=new ExhaustOverrunModel();bool pop=false;
            for(int i=0;i<120;i++){model.Step(1f/60,true,5900,i<60?1:0,speed,false);pop|=model.Pop;}
            check(!pop,"no overrun while parked or reversing at speed "+speed);
        }
        var m=Loaded();bool any=false;
        for(int i=0;i<120;i++){m.Step(1f/60,true,5900,i%2==0?.7f:1,12,false);any|=m.Pop;}
        check(!any,"partial gas feathering cannot chatter exhaust");
        m=Loaded();m.Step(.02f,false,5900,0,12,false);any=false;
        for(int i=0;i<60;i++){m.Step(.02f,true,5900,0,12,false);any|=m.Pop;}
        check(!any,"focus, menu or exit reset cannot replay a stale lift");
        m=Loaded();m.Step(.02f,true,float.NaN,0,12,false);m.Step(.02f,true,5900,0,12,false);
        check(!m.Pop&&m.Gain==0,"invalid telemetry clears loaded exhaust state");
        m=Loaded();m.Step(.04f,true,5900,0,12,false);m.Step(.02f,true,5900,1,12,false);any=false;
        for(int i=0;i<10;i++){m.Step(.02f,true,5900,0,12,false);any|=m.Pop;}
        check(!any,"returning to gas cancels tail and cooldown prevents rapid rearming");
        foreach(int fps in new[]{30,60,144}) {
            m=Loaded(fps);float lowest=1;bool sustained=true;
            for(int i=0;i<fps*4;i++) {
                float time=i/(float)fps,rpm=5900-time*1000;
                m.Step(1f/fps,true,rpm,0,12-time*2,false);
                if(time>1){lowest=Math.Min(lowest,m.Level);sustained&=m.Level>.2f;}
            }
            check(sustained&&lowest>.2f&&m.LoopPitch<1,"coasting sustains a falling-pitch exhaust bed beyond the initial pops at "+fps+" FPS");
            for(int i=0;i<fps/2;i++)m.Step(1f/fps,true,3000,1,8,false);
            check(m.Level==0,"returning to gas fades sustained exhaust to silence at "+fps+" FPS");
        }
        m=Loaded();for(int i=0;i<60;i++)m.Step(.02f,true,3800,0,12,false);
        check(m.Level>.3f,"coast below the arming RPM retains its already started texture");
        m.Step(.02f,true,1400,0,8,false);
        check(m.Level==0&&!m.Pop,"low RPM cuts sustained overrun instead of rattling at idle");
        m=new ExhaustOverrunModel();for(int i=0;i<120;i++)m.Step(.02f,true,5000,0,12,false);
        check(m.Level==0,"loading or unpausing while coasting cannot create an unarmed exhaust bed");
    }
}
