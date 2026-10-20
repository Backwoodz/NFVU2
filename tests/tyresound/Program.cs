using System;
using Rachel350Z;
static class Program {
    static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static TyreSoundModel Sound(float speed,float side,float spin,float skid=1,int contacts=4,bool burnout=false){
        var model=new TyreSoundModel();for(int i=0;i<150;i++)model.Step(.02f,true,speed,side,spin,skid,contacts,burnout);return model;
    }
    static void Main(){
        var mild=Sound(12,2,2);var heavy=Sound(25,11,12);
        Check(heavy.Level>mild.Level+.15f,"actual slip velocity still changes loudness when normalized skid is saturated");
        Check(heavy.HardMix>mild.HardMix+.4f,"slide texture responds to sideways speed and rear wheelspin");
        Check(heavy.Pitch>mild.Pitch+.1f,"fast hard drift changes tone rather than fixed pitch one");
        Check(Sound(0,0,0).Level==0&&Sound(1,0,0).Level==0,"idle and slow rolling are silent");
        Check(Sound(20,0,0,0).Level==0,"normal straight driving does not squeal");
        Check(Sound(0,0,16,1,4,true).Level>.6,"grounded stationary burnout can squeal");
        Check(Sound(20,8,12,1,0).Level==0,"airborne wheels stay silent");
        Check(Sound(20,8,12,1,2).Level<Sound(20,8,12).Level,"loss of tyre contact reduces sound");
        var transition=Sound(20,8,12);float before=transition.Level;
        transition.Step(.02f,true,20,0,0,0,4,false);Check(transition.Level>0&&transition.Level<before,"grip recovery fades smoothly");
        for(int i=0;i<80;i++)transition.Step(.02f,false,0,0,0,0,4,false);
        Check(transition.Level==0,"parked/disabled sound settles completely to silence");
        var model=new TyreSoundModel();for(int i=0;i<3000;i++){
            model.Step(i%2==0?.01f:.03f,true,i%40,i%15,i%70,1,i%5,i%29==0);
            Check(model.Level>=0&&model.Level<=1&&model.HardMix>=0&&model.HardMix<=1&&model.Pitch>=.8f&&model.Pitch<=1.35f,"bounded model across contact and slip changes");
            Check(Math.Abs(model.SoftGain*model.SoftGain+model.HardGain*model.HardGain-model.Level*model.Level)<.0001f,"equal-power crossfade keeps summed energy bounded");
        }
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,-1f,0}){model.Step(bad,true,20,8,8,1,4,false);Check(model.Level==0,"invalid time cannot produce sound spikes");}
        model.Step(.02f,true,float.NaN,float.PositiveInfinity,float.NaN,float.NaN,4,false);Check(model.Level==0,"invalid telemetry is sanitized");
        Console.WriteLine(checks+" dynamic tyre audio response checks passed; no native playback tested.");
    }
}
