using System;
using Rachel350Z;

static class VisualChecks {
    public static void Run(Action<bool,string> check) {
        var v=new NitroVisualState();
        for(int i=0;i<60;i++)v.Step(1f/60,false,true,.7f);
        check(v.Amount==0,"coasting/hover without actual boost has no screen effect");
        v.Step(1f/60,true,true,.7f);
        check(v.Amount>0&&v.Amount<.7f,"boost eases in rather than flashing on");
        for(int i=0;i<120;i++)v.Step(1f/60,true,true,.7f);
        check(Math.Abs(v.Amount-.7f)<.0001f,"sustained boost respects configured strength");
        float before=v.Amount;v.Step(1f/60,false,true,.7f);
        check(v.Amount>0&&v.Amount<before,"release/exhausted tank eases out");
        for(int i=0;i<90;i++)v.Step(1f/60,false,true,.7f);
        check(v.Amount==0,"fade reaches zero so camera work can be disabled");
        v.Step(.05f,true,true,1);v.Step(0,true,false,1);
        check(v.Amount==0,"pause/menu/focus/exit gate clears immediately even at zero dt");
        v.Step(.05f,true,true,1);v.Step(.05f,true,true,0);
        check(v.Amount==0,"zero strength disables immediately");
        v.Step(.05f,true,true,1);v.Step(.05f,true,true,float.NaN);
        check(v.Amount==0,"invalid strength cannot poison rendering");
        v.Step(float.NaN,true,true,1);v.Step(-1,true,true,1);
        check(v.Amount==0,"invalid elapsed time cannot activate an effect");
        var slow=new NitroVisualState();var fast=new NitroVisualState();
        for(int i=0;i<30;i++)slow.Step(1f/30,true,true,1);
        for(int i=0;i<144;i++)fast.Step(1f/144,true,true,1);
        check(Math.Abs(slow.Amount-fast.Amount)<.00001f,"boost envelope is consistent at 30 and 144 FPS");
        check(NitroVisualState.EdgeMask(.5f,.53f,.3f,.1f,.7f,.4f)==0,"central sight line stays sharp");
        check(NitroVisualState.EdgeMask(.4f,.2f,.3f,.1f,.7f,.4f)==0,"projected vehicle stays sharp");
        check(NitroVisualState.EdgeMask(.05f,.8f,.3f,.1f,.7f,.4f)>.8f,"surrounding scene receives the effect");
        check(NitroVisualState.EdgeMask(.95f,.95f,0,0,1,1)==0,"near-plane protection disables blur across the image");
        bool bounded=true;
        for(int y=0;y<=20;y++)for(int x=0;x<=32;x++) {
            float m=NitroVisualState.EdgeMask(x/32f,y/20f,.3f,.1f,.7f,.4f);
            bounded&=m>=0&&m<=1;
        }
        check(bounded,"screen mask remains within valid blend range");
        var nitro=new NitroState();
        nitro.StepFlight(.02f,true,true,1,25);
        v.Step(.02f,nitro.Boosting,true,1);
        check(v.Amount>0,"actual aerial boost drives the same visual envelope");
        v.Reset();check(v.Amount==0,"vehicle/camera replacement discards the previous envelope");
    }
}
