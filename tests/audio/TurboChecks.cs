using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Rachel350Z;
static class TurboChecks {
    static TurboSoundModel Charged(float throttle,float load,float rpm=5900) {
        var model=new TurboSoundModel();
        for(int i=0;i<120;i++)model.Step(1f/60,true,rpm,throttle,false,false,load);
        return model;
    }
    public static void Run(Action<bool,string> check,string output) {
        var light=Charged(.4f,.4f);var full=Charged(1,1);var noLoad=Charged(1,0);
        check(light.Charge<full.Charge*.5f&&light.ChargeGain<full.ChargeGain*.3f,"partial load produces much quieter turbo than full load at equal RPM");
        check(noLoad.Charge==0&&noLoad.ChargeGain==0,"RPM and open throttle without engine load do not build boost");
        light.Step(1f/60,true,5900,0,false,false,0);full.Step(1f/60,true,5900,0,false,false,0);
        check(light.BlowOff==1&&full.BlowOff==2&&light.BlowOffGain<full.BlowOffGain*.55f,"release volume captures prior pressure instead of using fixed full volume");
        check(full.ReleasedCharge>.9f&&full.BlowOffGain>.24f&&full.BlowOffGain<.29f&&full.Pressure<0,"full lift releases stored boost at a moderately raised gain and enters negative pressure");
        float captured=full.BlowOffGain;
        full.Step(1f/60,true,5900,0,false,false,0);
        check(full.BlowOff==0&&full.BlowOffGain==0&&captured>0,"release is a one-frame event whose captured gain can outlive charge");
        var slight=Charged(1,1);bool jitterEvent=false;
        for(int i=0;i<60;i++){slight.Step(1f/60,true,5900,i%2==0?.97f:1,false,false,1);jitterEvent|=slight.BlowOff!=0;}
        check(!jitterEvent,"small controller trigger jitter cannot cause release events");
        float min=0,max=-30;int turns=0;float previous=full.Pressure,lastDelta=0;
        for(int i=0;i<60;i++) {
            full.Step(1f/120,true,5900,0,false,false,0);
            if(i<36){float delta=full.Pressure-previous;if(delta*lastDelta<0)turns++;lastDelta=delta;min=Math.Min(min,full.Pressure);max=Math.Max(max,full.Pressure);}
            previous=full.Pressure;
        }
        check(turns>=3&&max-min>1&&min>=-30&&max<=30,"lift-off needle flutters briefly inside the signed scale");
        check(Math.Abs(full.Pressure+18)<.001f,"flutter decays to stable high-RPM overrun vacuum");
        for(int i=0;i<120;i++)full.Step(1f/60,true,950,0,false,false,0);
        check(full.Pressure==0&&full.ChargeGain==0,"idle settles needle and turbo audio to zero");
        full=Charged(1,1);full.Reset();full.Step(1f/60,true,5900,0,false,false,0);
        check(full.BlowOff==0&&full.ChargeGain==0,"menu/focus reset prevents false blow-off when throttle is cleared");
        foreach(int fps in new[]{30,60,144}) {
            var m=new TurboSoundModel();for(int i=0;i<fps;i++)m.Step(1f/fps,true,5900,1,false,false,1);
            m.Step(1f/fps,true,5900,0,false,false,0);float eventGain=m.BlowOffGain;
            for(int i=0;i<fps;i++)m.Step(1f/fps,true,5900,0,false,false,0);
            check(Math.Abs(eventGain-TurboSoundModel.HighSampleGain*TurboSoundModel.ReleaseMixGain)<.001f&&Math.Abs(m.Pressure+18)<.001f,"full-charge release and vacuum are frame-rate independent at "+fps+" FPS");
            m=Charged(1,1);int events=0;float released=0;
            for(int i=1;i<=fps;i++) {
                float gas=Math.Max(0,1-i/(fps*.2f));m.Step(1f/fps,true,5900,gas,false,false,gas);
                if(m.BlowOff!=0){events++;released=m.ReleasedCharge;}
            }
            check(events==1&&released>.2f,"controller trigger lift across 0.2 seconds releases once at "+fps+" FPS");
        }
        var feather=Charged(1,1);bool featherEvent=false;
        for(int i=0;i<120;i++) {
            float gas=i%40<20?.6f:1;
            feather.Step(1f/60,true,5900,gas,false,false,gas);featherEvent|=feather.BlowOff!=0;
        }
        check(!featherEvent,"substantial throttle feathering without closing the accelerator does not vent repeatedly");
        var shiftRelease=Charged(1,1);var fullRelease=Charged(1,1);
        shiftRelease.Step(1f/60,true,5900,1,true,false,1);fullRelease.Step(1f/60,true,5900,0,false,false,0);
        check(shiftRelease.BlowOff!=0&&shiftRelease.BlowOffGain<fullRelease.BlowOffGain*.4f,"gear changes are much quieter than lifting fully at equal boost");
        float lastEnvelope=1;bool envelopeOk=true;
        for(int i=0;i<120;i++) {
            float envelope=TurboSoundModel.ReleaseEnvelope(i/120f);
            envelopeOk&=envelope>=0&&envelope<=lastEnvelope;lastEnvelope=envelope;
        }
        check(envelopeOk&&lastEnvelope==0&&TurboSoundModel.ReleaseEnvelope(.3f)<.4f,"release tail fades monotonically to silence instead of playing the long sample at full gain");
        check(TurboSoundModel.ReleaseEnvelope(float.NaN)==0&&TurboSoundModel.ReleaseEnvelope(-1)==0,"invalid release ages cannot poison source gain");
        PerspectiveChecks.Run(check,output);
        // A reproducible low/full/no-load comparison, rendered from actual local
        // recordings by tools/render_audio_preview.py without loudness normalisation.
        using(var writer=new StreamWriter(Path.Combine(Path.GetDirectoryName(output),"turbo-comparison-state.csv"))) {
            writer.WriteLine("time,scenario,charge_gain,charge_pitch,blowoff,blowoff_gain,pressure,release_envelope");
            for(int scenario=0;scenario<3;scenario++) {
                var m=new TurboSoundModel();float releaseAge=1;
                for(int i=0;i<300;i++) {
                    float local=i/60f,gas=local>=1&&local<3?(scenario==0?.4f:1):0;
                    m.Step(1f/60,true,local<1?950:5900,gas,false,false,scenario==2?0:gas);
                    releaseAge=m.BlowOff!=0?0:releaseAge+1f/60;
                    writer.WriteLine(string.Join(",",new[]{scenario*5+local,(float)scenario,m.ChargeGain,m.ChargePitch,(float)m.BlowOff,m.BlowOffGain,m.Pressure,TurboSoundModel.ReleaseEnvelope(releaseAge)}.Select(v=>v.ToString("R",CultureInfo.InvariantCulture))));
                }
            }
        }
    }
}
