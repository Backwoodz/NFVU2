using System;
using Rachel350Z;
static class Program {
    static int checks;
    static void Check(bool value,string text){if(!value)throw new Exception(text);checks++;Console.WriteLine("PASS "+text);}
    static bool Near(float a,float b,float tolerance=.0002f)=>Math.Abs(a-b)<tolerance;
    static void Cool(TransformerMinigunHeatModel h,float seconds){for(int i=0;i<(int)Math.Round(seconds*100);i++)h.Step(.01f,0);}
    static void Rounds(TransformerMinigunHeatModel h,int rounds){for(int i=0;i<rounds;i++)h.Step(.1f,1);}
    static void Main() {
        var h=new TransformerMinigunHeatModel();
        Cool(h,8);Check(h.Heat==0,"idle and spin-up without rounds cannot warm the gun");
        Rounds(h,19);Check(h.Heat==0,"nineteen actual rounds remain below visible incandescence");
        Rounds(h,11);Check(h.Heat>0&&h.Heat<.3f,"roughly three seconds of actual firing gives dim early heat");
        Rounds(h,30);Check(Near(h.Heat,1),"sixty 600-rpm rounds reach full heat at six seconds");
        Rounds(h,1000);Check(h.Heat==1,"sustained firing saturates at a finite bounded intensity");
        Cool(h,1.9f);Check(h.Heat==1,"released trigger retains full heat for nearly two seconds");
        Cool(h,.2f);Check(h.Heat<1&&h.Heat>.99f,"cooling begins after the two-second hold without a visible jump");
        Cool(h,2.4f);Check(Near(h.Heat,.5f,.002f),"halfway through the five-second cooldown retains half intensity");
        Cool(h,2.6f);Check(h.Heat==0,"about seven seconds after release the barrels become cold");
        Rounds(h,60);float before=h.Heat;
        foreach(float dt in new[]{0f,-1f,float.NaN,float.PositiveInfinity})h.Step(dt,2);
        Check(h.Heat==before,"paused and invalid frame times neither heat nor cool");
        h.Step(100,0);Check(h.Heat==before,"a giant inactive hitch is bounded rather than skipping the visible tail");
        Cool(h,4.4f);float cooled=h.Heat;h.Step(.1f,2);
        Check(h.Heat>cooled,"resuming real rounds reheats the existing thermal state");
        float other=new TransformerMinigunHeatModel().Heat;Check(other==0&&h.Heat>0,"each gun owns independent thermal history");
        h.Reset();Check(h.Heat==0,"despawn disposal can explicitly reset thermal state");
        var bounded=new TransformerMinigunHeatModel();for(int i=0;i<10;i++)bounded.Step(.1f,int.MaxValue);
        Check(bounded.Heat<.001f,"invalid huge shot batches cannot bypass the bounded projectile dispatcher");
        var negative=new TransformerMinigunHeatModel();for(int i=0;i<100;i++)negative.Step(.1f,-20);
        Check(negative.Heat==0,"negative round counts cannot create heat");
        float min=1,max=0;
        foreach(int hz in new[]{30,60,144,240}) {
            var sample=new TransformerMinigunHeatModel();int previous=0;
            for(int i=1;i<=hz*4;i++){int total=(int)Math.Floor(i*10d/hz+1e-8);sample.Step(1f/hz,total-previous);previous=total;}
            min=Math.Min(min,sample.Heat);max=Math.Max(max,sample.Heat);
        }
        Check(Near(min,.5f,.001f)&&max-min<.001f,"equal real rounds give the same heat across 30 to 240 Hz render rates");
        Check(TransformerMinigunHeatModel.FrontMask(.29f,.63f)==0&&TransformerMinigunHeatModel.FrontMask(.30f,.63f)==0,"rear original barrel geometry stays outside the heat mask");
        Check(TransformerMinigunHeatModel.FrontMask(.45f,.63f)>0&&TransformerMinigunHeatModel.FrontMask(.45f,.63f)<1,"front metal develops a smooth local gradient");
        Check(TransformerMinigunHeatModel.FrontMask(.60f,.63f)==1,"muzzle metal receives full mask weight");
        Check(TransformerMinigunHeatModel.FrontMask(float.NaN,.63f)==0&&TransformerMinigunHeatModel.FrontMask(.5f,float.PositiveInfinity)==0&&TransformerMinigunHeatModel.FrontMask(.5f,.2f)==0,"invalid or rear-only source bounds cannot spread heat over the gun");
        Console.WriteLine(checks+" minigun thermal checks passed.");
    }
}
