using System;
using Rachel350Z;
static class Program {
    static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static TyreSmokeModel Run(float dt,float seconds,float slip,float load=3500,bool ground=true) {
        var m=new TyreSmokeModel();for(int i=0;i<(int)Math.Round(seconds/dt);i++)m.Step(dt,ground,slip,load);return m;
    }
    static void Main() {
        Check(Run(.02f,12,0).CloudRate==0,"pure rolling produces no smoke even under load");
        Check(Run(.02f,12,.7f).CloudRate==0,"minor slip does not produce a constant smoke trail");
        Check(Run(.02f,12,30,0).Density==0,"unloaded tyre cannot generate friction smoke");
        Check(Run(.02f,12,30,3500,false).Density==0,"wheelspin in air cannot emit smoke");
        var early=Run(.02f,.2f,28);var hot=Run(.02f,8,28);
        Check(hot.CloudRate>early.CloudRate*2,"sustained burnout builds a substantially denser plume");
        Check(Run(.02f,8,28,700).Density<hot.Density*.5f,"loaded outside tyre smokes more than lightly loaded inside tyre");
        Check(Run(.02f,8,3).Density<hot.Density*.1f,"small slip cannot create full burnout density");
        var warm=Run(.02f,6,28);float heat=warm.Heat;warm.Step(.02f,true,0,3500);
        Check(warm.Density==0&&warm.Heat>heat*.99f,"grip recovery stops emission without erasing accumulated visual heat");
        for(int i=0;i<1000;i++)warm.Step(.02f,true,0,3500);
        Check(warm.Heat<heat*.03f,"heat dissipates during normal rolling");
        float heatBefore=hot.Heat,densityBefore=hot.Density;hot.Step(0,false,0,0);
        Check(hot.Heat==heatBefore&&hot.Density==densityBefore,"pause freezes the heat model");
        var left=Run(.02f,8,30);var right=Run(.02f,8,0);
        Check(left.Density>.8f&&right.Density==0,"wheel models are independent");
        Check(TyreSmokeModel.Entrainment(800)>0&&TyreSmokeModel.Entrainment(-800)<0,"reverse wheelspin reverses the boundary-layer vortex");
        Check(Math.Abs(TyreSmokeModel.Entrainment(100000))<=13,"fast wheelspin cannot fling smoke at unbounded speed");
        var hz30=Run(1f/30,6,22);var hz144=Run(1f/144,6,22);
        Check(Math.Abs(hz30.Heat-hz144.Heat)<.0001f&&Math.Abs(hz30.Density-hz144.Density)<.0005f,"thermal response is independent of rendering frame rate");
        for(int i=0;i<8000;i++) {
            left.Step(.005f+i%5*.01f,i%13!=0,i%100,i%120*100);
            Check(left.Density>=0&&left.Density<=1&&left.Heat>=0&&left.Heat<=1,"finite bounded thermal state");
            Check(left.CloudRate<=210.001f&&left.WispRate<=260.001f,"bounded particle source rate");
        }
        foreach(float dt in new[]{float.NaN,float.PositiveInfinity,-1,.3f}) {
            left.Step(dt,true,30,3500);Check(left.Heat==0&&left.Density==0,"invalid time resets safely");
        }
        left.Step(.02f,true,float.NaN,3500);Check(left.Density==0,"invalid slip cannot create smoke");
        left.Step(.02f,true,30,float.PositiveInfinity);Check(left.Density==0,"invalid load cannot create smoke");
        Console.WriteLine(checks+" tyre smoke heat/emission checks passed; renderer requires the native Unity probe.");
    }
}
